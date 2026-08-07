using System.Collections.Generic;
using Gravedigger2026.Core.Config;
using Gravedigger2026.Core.Pathing;
using UnityEngine;

namespace Gravedigger2026.Gameplay.Pathing
{
    /// <summary>
    /// MP-07 Debug visual stress: spawn ~200+200 capsule stubs and drive Core move stack.
    /// Drop on an empty GameObject in any scene (or use Editor menu for headless numbers).
    /// </summary>
    public sealed class MassPathingPerfStressView : MonoBehaviour
    {
        [SerializeField] private int _perSide = MassPathingPerfStress.DefaultPerSide;
        [SerializeField] private int _measureFrames = MassPathingPerfStress.DefaultMeasureFrames;
        [SerializeField] private bool _spawnVisualStubs = true;
        [SerializeField] private bool _runHeadlessOnStart;
        [SerializeField] private bool _keepSimulating;
        [SerializeField] private float _stubScale = 0.15f;

        private MassPathingPerfStressResult _lastResult;
        private bool _hasLastResult;
        private Transform _stubRoot;
        private readonly List<Transform> _loyalStubs = new List<Transform>(256);
        private readonly List<Transform> _monsterStubs = new List<Transform>(256);

        private FlowFieldService _flow;
        private MassMoveScheduler _scheduler;
        private AttackSlotService _slots;
        private Vector2[] _positions;
        private int[] _monsterTargetIndex;
        private Vector3[] _dummyTargets;
        private readonly List<MassMoveSample> _samples = new List<MassMoveSample>(512);
        private int _slotCursor;
        private int _liveFrames;
        private double _liveSumMs;
        private double _liveMaxMs;
        private readonly System.Diagnostics.Stopwatch _sw = new System.Diagnostics.Stopwatch();

        public MassPathingPerfStressResult LastResult => _lastResult;

        private void Start()
        {
            if (_runHeadlessOnStart)
            {
                RunHeadless();
            }
        }

        private void Update()
        {
            if (!_keepSimulating || _scheduler == null || _positions == null)
            {
                return;
            }

            TickLiveFrame();
        }

        private void OnGUI()
        {
            if (!_hasLastResult && _liveFrames == 0)
            {
                return;
            }

            const float w = 520f;
            var rect = new Rect(12f, 12f, w, 140f);
            GUI.Box(rect, GUIContent.none);
            var style = new GUIStyle(GUI.skin.label) { fontSize = 13 };
            var y = 18f;
            if (_hasLastResult)
            {
                GUI.Label(
                    new Rect(20f, y, w - 20f, 22f),
                    $"Last headless avg={_lastResult.AvgMoveLogicMs:F3} ms  " +
                    $"budgetOK={_lastResult.WithinBudget}  agents={_lastResult.AgentCount}",
                    style);
                y += 22f;
            }

            if (_liveFrames > 0)
            {
                var avg = _liveSumMs / _liveFrames;
                GUI.Label(
                    new Rect(20f, y, w - 20f, 22f),
                    $"Live moveLogic avg={avg:F3} ms  max={_liveMaxMs:F3}  frames={_liveFrames}",
                    style);
                y += 22f;
            }

            GUI.Label(
                new Rect(20f, y, w - 20f, 40f),
                "Menu: Gravedigger2026/Pathing/Run MassPathing 200v200 Perf Stress\n" +
                "ContextMenu on this component: Run Headless / Start Live / Clear",
                style);
        }

        [ContextMenu("Run Headless 200v200")]
        public void RunHeadless()
        {
            _lastResult = MassPathingPerfStress.Run(_perSide, _measureFrames);
            _hasLastResult = true;
        }

        [ContextMenu("Start Live Sim (stubs)")]
        public void StartLiveSim()
        {
            ClearLive();
            SetupLiveCore();
            if (_spawnVisualStubs)
            {
                SpawnStubs();
            }

            _keepSimulating = true;
            _liveFrames = 0;
            _liveSumMs = 0;
            _liveMaxMs = 0;
        }

        [ContextMenu("Clear Live")]
        public void ClearLive()
        {
            _keepSimulating = false;
            _scheduler?.Clear();
            _scheduler = null;
            _flow = null;
            _slots = null;
            _positions = null;
            if (_stubRoot != null)
            {
                Destroy(_stubRoot.gameObject);
                _stubRoot = null;
            }

            _loyalStubs.Clear();
            _monsterStubs.Clear();
        }

        private void SetupLiveCore()
        {
            var perSide = Mathf.Max(1, _perSide);
            _flow = new FlowFieldService();
            _scheduler = new MassMoveScheduler();
            _slots = new AttackSlotService();
            _positions = new Vector2[perSide * 2];
            _monsterTargetIndex = new int[perSide];
            _dummyTargets = new Vector3[MassPathingPerfStress.DummyTargetCount];
            _slotCursor = 0;

            _flow.Configure(Vector3.zero, new Vector2(20f, 10f), MassPathingPerfStress.CellSize);
            _flow.Rebuild(new Vector3(14f, 0f, 0f), StubFullyWalkableMask.Instance);
            _scheduler.BindFlowField(_flow);

            for (var t = 0; t < _dummyTargets.Length; t++)
            {
                var ang = (t / (float)_dummyTargets.Length) * Mathf.PI * 2f;
                _dummyTargets[t] = new Vector3(Mathf.Cos(ang) * 6f, 0f, Mathf.Sin(ang) * 3f);
            }

            for (var i = 0; i < perSide; i++)
            {
                var id = i + 1;
                var row = i / 20;
                var col = i % 20;
                _positions[i] = new Vector2(-16f + col * 0.35f, -8f + row * 0.7f);
                _scheduler.Register(id, MassPathingPerfStress.AgentRadius, MassMoveScheduler.DetourGroupLoyal);
                _scheduler.SetGoal(id, GoalKind.Objective);
            }

            for (var i = 0; i < perSide; i++)
            {
                var id = perSide + i + 1;
                var row = i / 20;
                var col = i % 20;
                _positions[perSide + i] = new Vector2(16f - col * 0.35f, 8f - row * 0.7f);
                _monsterTargetIndex[i] = i % _dummyTargets.Length;
                _scheduler.Register(id, MassPathingPerfStress.AgentRadius, MassMoveScheduler.DetourGroupMonster);
                _scheduler.SetGoal(id, GoalKind.AttackSlot);
            }
        }

        private void SpawnStubs()
        {
            var rootGo = new GameObject("MassPathingPerfStubs");
            rootGo.transform.SetParent(transform, false);
            _stubRoot = rootGo.transform;
            var perSide = _perSide;
            var shader = Shader.Find("Standard") ?? Shader.Find("Universal Render Pipeline/Lit");
            var loyalMat = shader != null
                ? new Material(shader) { color = new Color(0.3f, 0.7f, 1f, 1f) }
                : null;
            var monsterMat = shader != null
                ? new Material(shader) { color = new Color(1f, 0.35f, 0.3f, 1f) }
                : null;

            for (var i = 0; i < perSide; i++)
            {
                var stub = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                stub.name = $"Loyal_{i}";
                stub.transform.SetParent(_stubRoot, false);
                stub.transform.localScale = Vector3.one * _stubScale;
                var col = stub.GetComponent<Collider>();
                if (col != null)
                {
                    Destroy(col);
                }

                var renderer = stub.GetComponent<Renderer>();
                if (renderer != null && loyalMat != null)
                {
                    renderer.sharedMaterial = loyalMat;
                }

                _loyalStubs.Add(stub.transform);
            }

            for (var i = 0; i < perSide; i++)
            {
                var stub = GameObject.CreatePrimitive(PrimitiveType.Cube);
                stub.name = $"Monster_{i}";
                stub.transform.SetParent(_stubRoot, false);
                stub.transform.localScale = Vector3.one * _stubScale;
                var col = stub.GetComponent<Collider>();
                if (col != null)
                {
                    Destroy(col);
                }

                var renderer = stub.GetComponent<Renderer>();
                if (renderer != null && monsterMat != null)
                {
                    renderer.sharedMaterial = monsterMat;
                }

                _monsterStubs.Add(stub.transform);
            }

            SyncStubTransforms();
        }

        private void TickLiveFrame()
        {
            var perSide = _perSide;
            _sw.Restart();

            _samples.Clear();
            for (var i = 0; i < perSide; i++)
            {
                _samples.Add(new MassMoveSample(i + 1, _positions[i], MassPathingPerfStress.AgentRadius, true));
            }

            for (var i = 0; i < perSide; i++)
            {
                _samples.Add(
                    new MassMoveSample(perSide + i + 1, _positions[perSide + i], MassPathingPerfStress.AgentRadius, true));
            }

            var slotBudget = Mathf.Min(MassMoveScheduler.MaxRecalcPerFrame, perSide);
            for (var n = 0; n < slotBudget; n++)
            {
                if (_slotCursor >= perSide)
                {
                    _slotCursor = 0;
                }

                var mi = _slotCursor++;
                var attackerId = $"m{mi}";
                var targetId = $"t{_monsterTargetIndex[mi]}";
                var targetPos = _dummyTargets[_monsterTargetIndex[mi]];
                var attackerPos = new Vector3(_positions[perSide + mi].x, 0f, _positions[perSide + mi].y);
                if (_slots.TryClaim(
                        attackerId,
                        targetId,
                        MassPathingPerfStress.AttackRange,
                        targetPos,
                        out var worldPos,
                        AttackMode.Melee,
                        attackerPos))
                {
                    _scheduler.SetGoal(
                        perSide + mi + 1,
                        GoalKind.AttackSlot,
                        new Vector2(worldPos.x, worldPos.z));
                }
            }

            _scheduler.Tick(_samples);
            _sw.Stop();

            var ms = _sw.Elapsed.TotalMilliseconds;
            _liveSumMs += ms;
            if (ms > _liveMaxMs)
            {
                _liveMaxMs = ms;
            }

            _liveFrames++;

            var total = perSide * 2;
            for (var i = 0; i < total; i++)
            {
                var id = i + 1;
                if (!_scheduler.TryGetSteer(id, out var steer) || steer.sqrMagnitude < 1e-8f)
                {
                    continue;
                }

                _positions[i] += steer.normalized * (MassPathingPerfStress.MoveSpeed * Time.deltaTime);
            }

            SyncStubTransforms();
        }

        private void SyncStubTransforms()
        {
            for (var i = 0; i < _loyalStubs.Count; i++)
            {
                var p = _positions[i];
                _loyalStubs[i].position = new Vector3(p.x, 0f, p.y);
            }

            var perSide = _perSide;
            for (var i = 0; i < _monsterStubs.Count; i++)
            {
                var p = _positions[perSide + i];
                _monsterStubs[i].position = new Vector3(p.x, 0f, p.y);
            }
        }

        private void OnDisable()
        {
            ClearLive();
        }
    }
}
