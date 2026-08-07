using System;
using System.Collections.Generic;
using Gravedigger2026.Core.Config;
using Gravedigger2026.Core.Defend;
using Gravedigger2026.Core.Dig;
using Gravedigger2026.Core.Level;
using Gravedigger2026.Core.Pathing;
using Gravedigger2026.Core.PushMap;
using Gravedigger2026.Core.UpgradeManufacture;
using Gravedigger2026.Gameplay.Defend;
using Gravedigger2026.Gameplay.Dig;
using Gravedigger2026.Gameplay.Formation;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

namespace Gravedigger2026.Gameplay.PushMap
{
    /// <summary>
    /// PushMap stage presentation bridge (Approach A / PM-03–PM-09 + MassCombatPathing MP-04).
    /// Prepare reuses FormationEditorRoot; StartBattle initializes Shield/LOC. Objective chain,
    /// spawn/trap, AggroMode four-state, and PM-07 Boss clear: Demo kill = center dist ≤
    /// max(monster, soldier) AttackRange + ArriveEpsilon → NotifyKilled + TryNotifyBossKilled;
    /// VictorySettled → AddExperience → advance;
    /// CaptureLoot + DungeonUnlockIds on capture; LevelFailure does not credit Exp.
    /// PM-08: StartBattle NavMesh bake injects AirWall Not Walkable boxes (incl. 45°).
    /// MP-04: Bake AirWall → StaticBoxWalkableMask + FlowField Rebuild; advance via MassMoveScheduler
    /// (shared field + LocalDetour; no per-soldier SetDestination(Objective)).
    /// MP-05: engage/chase → AttackSlot claim + LocalDetour; slot refresh ≤50/frame; no per-frame CalculatePath.
    /// Combat camera: Runtime Ensure PushMapCamera (ortho top-down; Size=2).
    /// PM-09: PushMapCameraFollowController Auto/Manual + ResumeFollow.
    /// PM-10: BodyRadius spawn spread + NavMeshAgent.radius for RVO.
    /// v0.66: Bake → deploy → FireStartBattleSpawns; advance does not pause on capture probe.
    /// </summary>
    public sealed class PushMapStageController : MonoBehaviour
    {
        [SerializeField] private Transform _worldRoot;
        [SerializeField] private Camera _pushMapCamera;
        [SerializeField] private DefendHudView _hudView;

        private PushMapCameraFollowController _cameraFollow;
        private GameObject _resumeFollowButtonRoot;

        private DefendPrefabCatalog _catalog;
        private FormationPrefabCatalog _formationCatalog;
        private ConfigCsvRepository _configs;
        private ProtagonistProgressService _progress;
        private WarriorPoolService _warriorPool;
        private BattleFormationService _formation;
        private WarehouseService _warehouse;
        private DungeonUnlockService _dungeonUnlocks;
        private Action _onVictoryAdvance;
        private Action<string> _onLevelFailure;

        private PushMapSessionService _session;
        private GameObject _mapInstance;
        private FormationEditorController _formationEditor;
        private Vector2 _mapHalfExtents = new Vector2(5f, 2.5f);
        private Vector3 _mapCenter;
        private bool _running;
        private bool _driverOutcomeDispatched;

        private readonly List<ObjectivePoint> _objectives = new List<ObjectivePoint>();
        private readonly List<PushMapMonsterAgentView> _monsters = new List<PushMapMonsterAgentView>();
        private readonly List<PushMapAdvanceView> _advanceViews = new List<PushMapAdvanceView>();
        private readonly List<GameObject> _deployedViews = new List<GameObject>();
        private readonly Dictionary<string, SpawnPoint> _spawnPointsById = new Dictionary<string, SpawnPoint>(StringComparer.Ordinal);
        private readonly List<TrapZone> _trapZones = new List<TrapZone>();
        private BossPoint _bossPoint;
        private readonly List<MonsterAgentView> _probeMonsters = new List<MonsterAgentView>();
        private PushMapMonsterPresenceProbe _presenceProbe;
        private EngageZone _engageZone;
        private NavMeshDataInstance _navMeshInstance;
        private GameObject _battleProtagonistInstance;

        private FlowFieldService _flowField;
        private StaticBoxWalkableMask _flowWalkableMask;
        private MassMoveScheduler _moveScheduler;
        private AttackSlotService _attackSlots;
        private readonly List<MassMoveSample> _moveSamples = new List<MassMoveSample>(64);
        private int _nextAdvanceMoveId;
        private int _slotGoalCursor;
        private int _lastSlotGoalRefreshCount;
        private bool _flowFieldReady;

        public void ConfigureCatalog(DefendPrefabCatalog catalog, FormationPrefabCatalog formationCatalog = null)
        {
            if (catalog != null)
            {
                _catalog = catalog;
            }

            if (formationCatalog != null)
            {
                _formationCatalog = formationCatalog;
            }
        }

        public void Begin(
            LevelStageContext context,
            ConfigCsvRepository configs,
            ProtagonistProgressService progress,
            WarriorPoolService warriorPool,
            BattleFormationService formation,
            WarehouseService warehouse = null,
            DungeonUnlockService dungeonUnlocks = null,
            Action onVictoryAdvance = null,
            Action<string> onLevelFailure = null)
        {
            EndInternal(destroyWorld: true);

            if (context?.PushMapConfig == null)
            {
                Debug.LogError("[PushMapStageController] Missing PushMapConfig.");
                return;
            }

            _configs = configs ?? throw new ArgumentNullException(nameof(configs));
            _progress = progress ?? throw new ArgumentNullException(nameof(progress));
            _warriorPool = warriorPool ?? throw new ArgumentNullException(nameof(warriorPool));
            _formation = formation ?? throw new ArgumentNullException(nameof(formation));
            _warehouse = warehouse;
            _dungeonUnlocks = dungeonUnlocks;
            _onVictoryAdvance = onVictoryAdvance;
            _onLevelFailure = onLevelFailure;
            _driverOutcomeDispatched = false;

            if (!_configs.IsLoaded)
            {
                _configs.TryLoadAll();
            }

            if (_catalog == null || !_catalog.TryGetMap(context.PushMapConfig.MapId, out var mapPrefab) || mapPrefab == null)
            {
                Debug.LogError(
                    $"[PushMapStageController] Map prefab missing for '{context.PushMapConfig.MapId}' (bind in DefendPrefabCatalog.Maps).");
                return;
            }

            EnsureWorldRoot();
            _mapInstance = Instantiate(mapPrefab, _worldRoot);
            _mapInstance.name = context.PushMapConfig.MapId;

            var bounds = _mapInstance.GetComponent<DigMapBounds>();
            _mapHalfExtents = bounds != null ? bounds.HalfExtents : new Vector2(5f, 2.5f);
            _mapCenter = bounds != null ? bounds.Center : _mapInstance.transform.position;

            CollectObjectives();
            CollectSpawnMarkers();
            _engageZone = _mapInstance.GetComponentInChildren<EngageZone>(true);

            EnsurePushMapCamera();
            ApplyPushMapCameraPose();
            if (_pushMapCamera != null)
            {
                _pushMapCamera.gameObject.SetActive(false);
            }

            _session = new PushMapSessionService();
            _session.LevelFailureRequested += HandleLevelFailureRequested;
            _session.VictorySettled += HandleVictorySettled;
            _session.ObjectiveCaptured += HandleObjectiveCaptured;
            _session.CurrentObjectiveChanged += HandleCurrentObjectiveChanged;
            _session.PushMapSpawnRequested += HandlePushMapSpawnRequested;
            _session.BeginPrepare(context.PushMapConfig);

            _presenceProbe = new PushMapMonsterPresenceProbe();
            _presenceProbe.BindMonstersProvider(ProvideLivingMonsters);

            if (_hudView != null)
            {
                _hudView.StartBattleRequested += HandleStartBattleRequested;
                _hudView.Show();
                _hudView.SetPrepareVisible(false);
                _hudView.SetCombatVisible(false);
                _hudView.SetPhaseText("推图战 — Prepare");
                _hudView.SetHint("布阵后点击「开战」；击杀 BOSS 通关入账经验；护盾归零失败不入账");
            }

            OpenFormationEditor();
            _running = true;
        }

        public void End()
        {
            EndInternal(destroyWorld: true);
        }

        private void EndInternal(bool destroyWorld)
        {
            _running = false;
            CloseFormationEditor();

            if (_session != null)
            {
                _session.LevelFailureRequested -= HandleLevelFailureRequested;
                _session.VictorySettled -= HandleVictorySettled;
                _session.ObjectiveCaptured -= HandleObjectiveCaptured;
                _session.CurrentObjectiveChanged -= HandleCurrentObjectiveChanged;
                _session.PushMapSpawnRequested -= HandlePushMapSpawnRequested;
                _session.Stop();
                _session = null;
            }

            _driverOutcomeDispatched = false;
            _dungeonUnlocks = null;

            if (_hudView != null)
            {
                _hudView.StartBattleRequested -= HandleStartBattleRequested;
                _hudView.SetPrepareVisible(false);
                _hudView.SetCombatVisible(false);
            }

            DisableCameraFollow();
            if (destroyWorld && _resumeFollowButtonRoot != null)
            {
                var canvas = _resumeFollowButtonRoot.GetComponentInParent<Canvas>();
                if (canvas != null)
                {
                    Destroy(canvas.gameObject);
                }
                else
                {
                    Destroy(_resumeFollowButtonRoot);
                }

                _resumeFollowButtonRoot = null;
            }

            ClearDeployedViews();
            ClearSpawnedMonsters();
            ClearFlowFieldPathing();
            _objectives.Clear();
            _spawnPointsById.Clear();
            _trapZones.Clear();
            _bossPoint = null;
            _presenceProbe = null;
            _engageZone = null;
            ReleaseNavMesh();

            if (destroyWorld && _battleProtagonistInstance != null)
            {
                Destroy(_battleProtagonistInstance);
                _battleProtagonistInstance = null;
            }

            if (destroyWorld && _mapInstance != null)
            {
                Destroy(_mapInstance);
                _mapInstance = null;
            }

            if (_pushMapCamera != null)
            {
                _pushMapCamera.gameObject.SetActive(false);
            }
        }

        private void HandleStartBattleRequested()
        {
            if (_session == null || _formation == null || _progress == null || _configs == null)
            {
                return;
            }

            var deployed = _formation.Entries.Count;
            var degree = _formation.ComputeLossOfControlDegree(_progress.ControlPowerCap);
            if (!_session.TryStartBattle(deployed, _progress.ProtagonistMaxHP, degree, _configs, out var error))
            {
                _hudView?.SetHint(error ?? "不可开战");
                Debug.Log($"[PushMapStage] StartBattle rejected: {error}");
                return;
            }

            CloseFormationEditor();
            EnsurePushMapCamera();
            if (_pushMapCamera != null)
            {
                _pushMapCamera.gameObject.SetActive(true);
                ApplyPushMapCameraPose();
            }

            ReleaseNavMesh();
            var airWallBoxes = CollectAirWallObstacles();
            _navMeshInstance = DefendNavMeshBaker.Bake(_mapCenter, _mapHalfExtents, airWallBoxes);
            ConfigureFlowFieldPathing(airWallBoxes);

            BeginObjectiveChain();
            DeployCombatUnits();
            TickMassCombatPathing();
            _session.FireStartBattleSpawns();
            ResolveStartBattleRebelRolls();
            _session.NotifyBossPointPresence(_bossPoint != null);
            EnableCameraFollowForCombat();

            if (_hudView != null)
            {
                _hudView.SetPrepareVisible(false);
                _hudView.SetCombatVisible(true);
                _hudView.SetPhaseText("推图战 — Combat");
                RefreshCombatHud();
                _hudView.SetHint(
                    $"忠诚兵推进/占领；进 BOSS AttackRange 击杀通关（Exp={_session.Config?.StageExpReward ?? 0}）；" +
                    "护盾归零失败不入账；拖拽镜头可手动平移，点「恢复跟随」回默认");
            }

            Debug.Log(
                $"[PushMapStage] Combat entered — PendingBoss={_session.PendingBossCount} BossPoint={(_bossPoint != null ? "yes" : "no")}.");
        }

        private void ResolveStartBattleRebelRolls()
        {
            if (_session.LockedLossOfControlDegree <= 0f || _session.LockedLossOfControlTierId <= 0)
            {
                Debug.Log("[PushMapSession] LossOfControl Degree≤0 — no rebel rolls.");
                return;
            }

            var entries = _formation.Entries;
            for (var i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                if (entry == null || string.IsNullOrEmpty(entry.WarriorId))
                {
                    continue;
                }

                if (!_warriorPool.TryGet(entry.WarriorId, out var warrior) || warrior == null)
                {
                    continue;
                }

                var raceBonus = 0f;
                var gemBonus = 0f;
                if (_configs != null)
                {
                    if (!string.IsNullOrEmpty(warrior.RaceId)
                        && _configs.TryGetRace(warrior.RaceId, out var raceRow)
                        && raceRow != null)
                    {
                        raceBonus = raceRow.LossOfControlChanceBonus;
                    }

                    if (warrior.GemIds != null)
                    {
                        for (var g = 0; g < warrior.GemIds.Count; g++)
                        {
                            if (_configs.TryGetGem(warrior.GemIds[g], out var gemRow) && gemRow != null)
                            {
                                gemBonus += gemRow.LossOfControlChanceBonus;
                            }
                        }
                    }
                }

                var chance = LossOfControlMath.ComputeFinalLossChance(
                    _session.LockedTierChance, raceBonus, gemBonus, skillBonusSum: 0f);
                var roll = UnityEngine.Random.value;
                var rebel = roll < chance;
                if (rebel)
                {
                    var advance = FindAdvanceView(warrior.Id);
                    if (advance != null)
                    {
                        advance.SetRebel(true);
                    }
                }
                Debug.Log(
                    $"[PushMapSession] RebelRoll {warrior.Id} chance={chance:0.###} roll={roll:0.###} " +
                    $"→ {(rebel ? "REBEL" : "loyal")} (Tier={_session.LockedTierChance:0.###} Race={raceBonus:0.###} Gem={gemBonus:0.###})");
            }
        }

        private void Update()
        {
            if (!_running || _session == null || _session.Phase != PushMapPhase.Combat)
            {
                return;
            }

            var hasMonster = HasMonsterThreatInCurrentZone();
            _session.TickCapture(Time.deltaTime, hasMonster);
            TickMassCombatPathing();
            PollTrapEntry();
            PollMonsterDemoKill();
            PollPassiveProvocation();
        }

        /// <summary>
        /// Demo kill (WarriorCombat hit polish deferred): loyal soldier within
        /// max(monster, soldier) AttackRange + ArriveEpsilon → NotifyKilled.
        /// Uses soldier reach so AttackSlot ring arrival (ClassConfig.AttackRange) can kill
        /// when monster AttackRange is smaller — otherwise chase stalls forever with no damage.
        /// Boss also TryNotifyBossKilled.
        /// </summary>
        private void PollMonsterDemoKill()
        {
            if (_monsters.Count == 0 || _advanceViews.Count == 0 || _session == null || _session.OutcomeSettled)
            {
                return;
            }

            for (var m = 0; m < _monsters.Count; m++)
            {
                var monster = _monsters[m];
                if (monster == null || !monster.IsAlive)
                {
                    continue;
                }

                for (var i = 0; i < _advanceViews.Count; i++)
                {
                    var soldier = _advanceViews[i];
                    if (soldier == null || soldier.IsRebel)
                    {
                        continue;
                    }

                    var range = Mathf.Max(monster.AttackRange, soldier.AttackRange) +
                                MassMoveScheduler.ArriveEpsilon;
                    if (range <= 0f)
                    {
                        continue;
                    }

                    if (Vector3.Distance(monster.transform.position, soldier.transform.position) > range)
                    {
                        continue;
                    }

                    var isBoss = monster.IsBoss;
                    Debug.Log(
                        $"[PushMapStage] Demo monster kill — loyal soldier in reach of " +
                        $"'{(isBoss ? "BOSS " : "")}{monster.MonsterId}' " +
                        $"(max ranges + ArriveEpsilon={range:0.###}).");
                    monster.NotifyKilled();
                    if (isBoss)
                    {
                        _session.TryNotifyBossKilled();
                    }

                    break;
                }
            }
        }

        // PM-06 Demo contract: a loyal soldier's first entry into a passive monster's
        // AttackRange stands in for "soldier attacks first" → NotifyProvoked.
        private void PollPassiveProvocation()
        {
            if (_monsters.Count == 0 || _advanceViews.Count == 0)
            {
                return;
            }

            for (var m = 0; m < _monsters.Count; m++)
            {
                var monster = _monsters[m];
                if (monster == null || !monster.IsAlive || !monster.IsPassive || monster.IsBoss)
                {
                    continue;
                }

                var range = monster.AttackRange;
                if (range <= 0f)
                {
                    continue;
                }

                for (var i = 0; i < _advanceViews.Count; i++)
                {
                    var soldier = _advanceViews[i];
                    if (soldier == null || soldier.IsRebel)
                    {
                        continue;
                    }

                    if (Vector3.Distance(monster.transform.position, soldier.transform.position) <= range)
                    {
                        monster.NotifyProvoked();
                        break;
                    }
                }
            }
        }

        private void CollectObjectives()
        {
            _objectives.Clear();
            if (_mapInstance == null)
            {
                return;
            }

            var found = _mapInstance.GetComponentsInChildren<ObjectivePoint>(true);
            if (found != null)
            {
                _objectives.AddRange(found);
            }
            _objectives.Sort((a, b) => a.ObjectiveOrder.CompareTo(b.ObjectiveOrder));
        }

        /// <summary>
        /// PM-08: map AirWall → Not Walkable Box sources for StartBattle bake (incl. Y 45°).
        /// </summary>
        private List<DefendNavMeshBaker.NavMeshBoxObstacle> CollectAirWallObstacles()
        {
            var boxes = new List<DefendNavMeshBaker.NavMeshBoxObstacle>();
            if (_mapInstance == null)
            {
                return boxes;
            }

            var walls = _mapInstance.GetComponentsInChildren<AirWall>(true);
            if (walls == null || walls.Length == 0)
            {
                return boxes;
            }

            for (var i = 0; i < walls.Length; i++)
            {
                var wall = walls[i];
                if (wall == null)
                {
                    continue;
                }

                boxes.Add(new DefendNavMeshBaker.NavMeshBoxObstacle(
                    wall.transform.position,
                    wall.FullSize,
                    wall.transform.rotation));
            }

            Debug.Log($"[PushMapStage] AirWall bake obstacles={boxes.Count}.");
            return boxes;
        }

        private void CollectSpawnMarkers()
        {
            _spawnPointsById.Clear();
            _trapZones.Clear();
            _bossPoint = null;
            if (_mapInstance == null)
            {
                return;
            }

            var spawnPoints = _mapInstance.GetComponentsInChildren<SpawnPoint>(true);
            for (var i = 0; i < spawnPoints.Length; i++)
            {
                var sp = spawnPoints[i];
                if (sp == null)
                {
                    continue;
                }

                var id = sp.SpawnPointId;
                if (string.IsNullOrEmpty(id))
                {
                    continue;
                }

                _spawnPointsById[id] = sp;
            }

            var traps = _mapInstance.GetComponentsInChildren<TrapZone>(true);
            for (var i = 0; i < traps.Length; i++)
            {
                if (traps[i] != null)
                {
                    _trapZones.Add(traps[i]);
                }
            }

            _bossPoint = _mapInstance.GetComponentInChildren<BossPoint>(true);
            Debug.Log(
                $"[PushMapStage] Markers collected — SpawnPoints={_spawnPointsById.Count} " +
                $"Traps={_trapZones.Count} BossPoint={(_bossPoint != null ? "yes" : "no")}");
        }

        private void BeginObjectiveChain()
        {
            if (_session == null)
            {
                return;
            }

            var orders = new List<int>(_objectives.Count);
            for (var i = 0; i < _objectives.Count; i++)
            {
                orders.Add(_objectives[i].ObjectiveOrder);
            }
            _session.TryBeginObjectiveChain(orders);
        }

        private ObjectivePoint ResolveCurrentObjective()
        {
            if (_session == null || _session.CurrentObjectiveOrder <= 0)
            {
                return null;
            }

            for (var i = 0; i < _objectives.Count; i++)
            {
                if (_objectives[i].ObjectiveOrder == _session.CurrentObjectiveOrder)
                {
                    return _objectives[i];
                }
            }
            return null;
        }

        private CaptureZone ResolveCurrentCaptureZone()
        {
            var objective = ResolveCurrentObjective();
            return objective != null ? objective.CaptureZone : null;
        }

        private bool HasMonsterThreatInCurrentZone()
        {
            var zone = ResolveCurrentCaptureZone();
            return zone != null && _presenceProbe != null && _presenceProbe.HasLivingMonster(zone);
        }

        /// <summary>PM-05: first loyal-soldier entry into a TrapZone notifies rules (once per point).</summary>
        private void PollTrapEntry()
        {
            if (_trapZones.Count == 0 || _advanceViews.Count == 0)
            {
                return;
            }

            for (var t = 0; t < _trapZones.Count; t++)
            {
                var trap = _trapZones[t];
                if (trap == null)
                {
                    continue;
                }

                for (var i = 0; i < _advanceViews.Count; i++)
                {
                    var soldier = _advanceViews[i];
                    if (soldier == null || soldier.IsRebel)
                    {
                        continue;
                    }

                    if (trap.ContainsXZ(soldier.transform.position))
                    {
                        _session.TryNotifyTrapEnter(trap.TrapZoneId);
                        break; // rules de-dupes per point; no need to check more soldiers this frame
                    }
                }
            }
        }

        private IReadOnlyList<MonsterAgentView> ProvideLivingMonsters()
        {
            _probeMonsters.Clear();
            for (var i = 0; i < _monsters.Count; i++)
            {
                var m = _monsters[i];
                if (m != null && m.IsAlive)
                {
                    _probeMonsters.Add(m.ProbeShim);
                }
            }
            return _probeMonsters;
        }

        private void HandlePushMapSpawnRequested(PushMapSpawnRequest request)
        {
            if (request == null || _configs == null || _catalog == null || _worldRoot == null)
            {
                return;
            }

            if (!_configs.TryGetMonster(request.MonsterId, out var monsterRow) || monsterRow == null)
            {
                Debug.LogWarning($"[PushMapStage] MonsterConfig missing: {request.MonsterId} — skip spawn.");
                return;
            }

            _catalog.TryGetMonsterModel(monsterRow.ModelId, out var modelPrefab);
            if (modelPrefab == null)
            {
                Debug.LogWarning(
                    $"[PushMapStage] Monster prefab missing: Assets/Prefabs/Defend/Monsters/{monsterRow.ModelId}.prefab — runtime temp cube.");
            }

            var basePos = ResolveSpawnPosition(request);
            var protagonistTf = _battleProtagonistInstance != null ? _battleProtagonistInstance.transform : null;
            var bodyRadius = Mathf.Max(0.05f, monsterRow.BodyRadius);

            var occupied = new List<PushMapSpawnSpread.Footprint>(_monsters.Count);
            for (var m = 0; m < _monsters.Count; m++)
            {
                var existing = _monsters[m];
                if (existing == null || !existing.IsAlive)
                {
                    continue;
                }

                occupied.Add(new PushMapSpawnSpread.Footprint(
                    existing.transform.position,
                    existing.BodyRadius));
            }

            var spreadPositions = new List<Vector3>(request.SpawnCount);
            PushMapSpawnSpread.ComputePositions(
                basePos,
                request.SpawnCount,
                bodyRadius,
                occupied,
                spreadPositions);

            for (var i = 0; i < request.SpawnCount; i++)
            {
                var pos = i < spreadPositions.Count ? spreadPositions[i] : basePos;

                GameObject go;
                if (modelPrefab != null)
                {
                    go = Instantiate(modelPrefab, _worldRoot);
                }
                else
                {
                    go = CreateTempMonsterVisual(monsterRow.ModelId);
                    go.transform.SetParent(_worldRoot, false);
                }

                go.name = $"Monster_{monsterRow.MonsterId}_{_monsters.Count}";
                go.transform.position = pos;

                var view = go.GetComponent<PushMapMonsterAgentView>();
                if (view == null)
                {
                    view = go.AddComponent<PushMapMonsterAgentView>();
                }

                var probeShim = go.GetComponent<MonsterAgentView>();
                if (probeShim == null)
                {
                    probeShim = go.AddComponent<MonsterAgentView>();
                }
                probeShim.BindProbeOnly();
                view.AttachProbeShim(probeShim);

                view.Bind(
                    monsterRow,
                    protagonistTf,
                    () => _advanceViews,
                    tag => _session?.ApplyShieldHit(tag),
                    1f,
                    _attackSlots,
                    _moveScheduler,
                    ++_nextAdvanceMoveId);
                if (request.IsBoss)
                {
                    view.MarkAsBoss(true);
                }

                _monsters.Add(view);
            }

            Debug.Log(
                $"[PushMapStage] Spawned {request.SpawnCount}x {request.MonsterId} at '{request.SpawnPointId}' " +
                $"({request.Trigger}; Boss={request.IsBoss}).");
        }

        private Vector3 ResolveSpawnPosition(PushMapSpawnRequest request)
        {
            if (request.IsBoss && _bossPoint != null)
            {
                return _bossPoint.transform.position;
            }

            if (!string.IsNullOrEmpty(request.SpawnPointId)
                && _spawnPointsById.TryGetValue(request.SpawnPointId, out var sp)
                && sp != null)
            {
                return sp.transform.position;
            }

            Debug.LogWarning(
                $"[PushMapStage] Spawn position unresolved for point '{request.SpawnPointId}' " +
                $"(Boss={request.IsBoss}) — fallback map edge.");
            return _mapCenter + new Vector3(_mapHalfExtents.x * 0.9f, 0f, 0f);
        }

        private static GameObject CreateTempMonsterVisual(string modelId)
        {
            var root = new GameObject(string.IsNullOrEmpty(modelId) ? "MonsterTemp" : modelId);
            var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.name = "Body";
            body.transform.SetParent(root.transform, false);
            body.transform.localScale = new Vector3(0.9f, 1.1f, 0.9f);
            body.transform.localPosition = new Vector3(0f, 0.55f, 0f);
            UnityEngine.Object.Destroy(body.GetComponent<Collider>());
            var renderer = body.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.material.color = new Color(0.75f, 0.25f, 0.25f);
            }

            return root;
        }

        private void ClearSpawnedMonsters()
        {
            for (var i = 0; i < _monsters.Count; i++)
            {
                if (_monsters[i] != null)
                {
                    Destroy(_monsters[i].gameObject);
                }
            }
            _monsters.Clear();
            _probeMonsters.Clear();
        }

        private void DeployCombatUnits()
        {
            ClearDeployedViews();

            var protagonistPrefab = _catalog != null ? _catalog.BattleProtagonistPrefab : null;
            if (protagonistPrefab == null)
            {
                Debug.LogError("[PushMapStage] BattleProtagonist prefab missing.");
            }
            else
            {
                _battleProtagonistInstance = Instantiate(protagonistPrefab, _worldRoot);
                _battleProtagonistInstance.name = "BattleProtagonist";
                _battleProtagonistInstance.transform.position = _mapCenter;
                _deployedViews.Add(_battleProtagonistInstance);
            }

            if (_formation == null || _warriorPool == null || _catalog == null)
            {
                return;
            }

            var entries = _formation.Entries;
            for (var i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                if (entry == null || string.IsNullOrEmpty(entry.WarriorId))
                {
                    continue;
                }

                if (!_warriorPool.TryGet(entry.WarriorId, out var warrior) || warrior == null)
                {
                    Debug.LogWarning($"[PushMapStage] Warrior '{entry.WarriorId}' missing from pool — skip deploy.");
                    continue;
                }

                if (!_catalog.TryGetWarriorAppearance(warrior.AppearanceId, out var appearancePrefab)
                    || appearancePrefab == null)
                {
                    Debug.LogWarning(
                        $"[PushMapStage] Appearance Prefab missing: Assets/Prefabs/Defend/Warriors/{warrior.AppearanceId}.prefab");
                    continue;
                }

                var go = Instantiate(appearancePrefab, _worldRoot);
                go.name = $"Warrior_{warrior.Id}";
                go.transform.position = new Vector3(
                    _mapCenter.x + entry.PositionX,
                    _mapCenter.y,
                    _mapCenter.z + entry.PositionZ);
                _deployedViews.Add(go);

                var attackRange = 1f;
                if (_configs != null &&
                    !string.IsNullOrEmpty(warrior.ClassId) &&
                    _configs.TryGetClass(warrior.ClassId, out var classRow) &&
                    classRow != null &&
                    classRow.AttackRange > 0.05f)
                {
                    attackRange = classRow.AttackRange;
                }

                var advance = go.GetComponent<PushMapAdvanceView>();
                if (advance == null)
                {
                    advance = go.AddComponent<PushMapAdvanceView>();
                }
                _nextAdvanceMoveId++;
                advance.Bind(
                    _moveScheduler,
                    _nextAdvanceMoveId,
                    3.5f,
                    ProvidePushMapMonsters,
                    attackRange,
                    warrior.AttackMode,
                    warrior.Id,
                    _attackSlots);
                _advanceViews.Add(advance);
            }

            Debug.Log($"[PushMapStage] Deployed protagonist + {_advanceViews.Count} loyal-capable warriors.");
        }

        private IReadOnlyList<PushMapMonsterAgentView> ProvidePushMapMonsters()
        {
            return _monsters;
        }

        private PushMapAdvanceView FindAdvanceView(string warriorId)
        {
            for (var i = 0; i < _advanceViews.Count; i++)
            {
                var view = _advanceViews[i];
                if (view != null && view.gameObject.name == $"Warrior_{warriorId}")
                {
                    return view;
                }
            }
            return null;
        }

        private void ClearDeployedViews()
        {
            for (var i = 0; i < _deployedViews.Count; i++)
            {
                if (_deployedViews[i] != null)
                {
                    Destroy(_deployedViews[i]);
                }
            }
            _deployedViews.Clear();
            _advanceViews.Clear();
            _moveScheduler?.Clear();
            _attackSlots?.Clear();
            _battleProtagonistInstance = null;
        }

        private void EnsurePathingServices()
        {
            _flowField ??= new FlowFieldService();
            _flowWalkableMask ??= new StaticBoxWalkableMask();
            _moveScheduler ??= new MassMoveScheduler();
            _attackSlots ??= new AttackSlotService();
        }

        private void ConfigureFlowFieldPathing(IReadOnlyList<DefendNavMeshBaker.NavMeshBoxObstacle> airWallBoxes)
        {
            EnsurePathingServices();
            _flowWalkableMask.Clear();
            _moveScheduler.Clear();
            _attackSlots.Clear();
            _nextAdvanceMoveId = 0;
            _slotGoalCursor = 0;
            _lastSlotGoalRefreshCount = 0;
            _flowFieldReady = false;

            if (airWallBoxes != null)
            {
                for (var i = 0; i < airWallBoxes.Count; i++)
                {
                    var box = airWallBoxes[i];
                    _flowWalkableMask.AddBox(
                        StaticBoxWalkableMask.BoxObstacle.FromFullSize(box.Center, box.Size, box.Rotation));
                }
            }

            _flowField.Configure(_mapCenter, _mapHalfExtents, FlowFieldService.DefaultCellSize);
            _moveScheduler.BindFlowField(_flowField);
            _flowFieldReady = true;
            Debug.Log(
                $"[PushMapStage] FlowField configured — cell={_flowField.CellSize} " +
                $"grid={_flowField.Cols}x{_flowField.Rows} airWallMaskBoxes={_flowWalkableMask.BoxCount}.");
        }

        private void ClearFlowFieldPathing()
        {
            _flowFieldReady = false;
            _moveScheduler?.Clear();
            _attackSlots?.Clear();
            _flowWalkableMask?.Clear();
            _moveSamples.Clear();
            _nextAdvanceMoveId = 0;
            _slotGoalCursor = 0;
            _lastSlotGoalRefreshCount = 0;
        }

        private void RebuildFlowFieldTowardCurrentObjective()
        {
            if (!_flowFieldReady || _flowField == null || _flowWalkableMask == null)
            {
                return;
            }

            var objective = ResolveCurrentObjective();
            if (objective == null)
            {
                Debug.Log("[PushMapStage] FlowField Rebuild skipped — no CurrentObjective.");
                return;
            }

            var before = _flowField.RebuildCount;
            _flowField.Rebuild(objective.transform.position, _flowWalkableMask);
            Debug.Log(
                $"[PushMapStage] FlowField Rebuild shared field — Order={_session?.CurrentObjectiveOrder ?? 0} " +
                $"goal={objective.transform.position} RebuildCount={_flowField.RebuildCount} (was {before}) " +
                $"maskBoxes={_flowWalkableMask.BoxCount}.");
        }

        /// <summary>
        /// MP-05: budgeted AttackSlot claim/release + MassMoveScheduler steer (≤50 each, round-robin).
        /// </summary>
        private void TickMassCombatPathing()
        {
            if (_moveScheduler == null)
            {
                return;
            }

            var zone = ResolveCurrentCaptureZone();
            _moveScheduler.SetObjectiveArriveRadius(
                zone != null ? zone.Radius : MassMoveScheduler.DefaultObjectiveArriveRadius);

            TickAttackSlotGoals();

            _moveSamples.Clear();
            for (var i = 0; i < _advanceViews.Count; i++)
            {
                var view = _advanceViews[i];
                if (view != null)
                {
                    _moveSamples.Add(view.BuildSample());
                }
            }

            for (var i = 0; i < _monsters.Count; i++)
            {
                var monster = _monsters[i];
                if (monster != null && monster.IsAlive && !monster.IsStationary && monster.MoveId != 0)
                {
                    _moveSamples.Add(monster.BuildSample());
                }
            }

            _moveScheduler.Tick(_moveSamples);
        }

        private void TickAttackSlotGoals()
        {
            if (_attackSlots == null || _moveScheduler == null)
            {
                return;
            }

            // Build round-robin roster: loyal soldiers then chase-capable monsters.
            var rosterCount = _advanceViews.Count + _monsters.Count;
            if (rosterCount <= 0)
            {
                _lastSlotGoalRefreshCount = 0;
                return;
            }

            var budget = Mathf.Min(MassMoveScheduler.MaxRecalcPerFrame, rosterCount);
            var refreshed = 0;
            for (var n = 0; n < budget; n++)
            {
                if (_slotGoalCursor >= rosterCount)
                {
                    _slotGoalCursor = 0;
                }

                if (_slotGoalCursor < _advanceViews.Count)
                {
                    RefreshSoldierSlotGoal(_advanceViews[_slotGoalCursor]);
                }
                else
                {
                    var mi = _slotGoalCursor - _advanceViews.Count;
                    if (mi >= 0 && mi < _monsters.Count)
                    {
                        _monsters[mi]?.TryRefreshChaseGoal(_attackSlots, _moveScheduler);
                    }
                }

                _slotGoalCursor++;
                refreshed++;
            }

            _lastSlotGoalRefreshCount = refreshed;
        }

        private void RefreshSoldierSlotGoal(PushMapAdvanceView soldier)
        {
            if (soldier == null || soldier.IsRebel || _moveScheduler == null || _attackSlots == null)
            {
                return;
            }

            if (!soldier.TryGetEngageMonster(out var monster) || monster == null)
            {
                _attackSlots.Release(soldier.AttackerId);
                _moveScheduler.SetPaused(soldier.MoveId, false);
                _moveScheduler.SetGoal(soldier.MoveId, GoalKind.Objective);
                return;
            }

            var targetBody = monster.BodyRadius;
            if (!_attackSlots.TryClaim(
                    soldier.AttackerId,
                    monster.RuntimeTargetId,
                    soldier.AttackRange,
                    monster.transform.position,
                    out var slotPos,
                    soldier.AttackMode,
                    soldier.transform.position,
                    targetBody))
            {
                // No free slot: keep Objective FlowField (do not hard-freeze). Overflow soldiers
                // continue advance / LocalDetour around the ring until a slot frees or Demo kill.
                _moveScheduler.SetPaused(soldier.MoveId, false);
                _moveScheduler.SetGoal(soldier.MoveId, GoalKind.Objective);
                return;
            }

            _moveScheduler.SetPaused(soldier.MoveId, false);
            _moveScheduler.SetGoal(
                soldier.MoveId,
                GoalKind.AttackSlot,
                new Vector2(slotPos.x, slotPos.z));
        }

        private void ReleaseNavMesh()
        {
            if (_navMeshInstance.valid)
            {
                NavMesh.RemoveNavMeshData(_navMeshInstance);
                _navMeshInstance = default;
            }
        }

        private void HandleObjectiveCaptured(int order)
        {
            CreditCaptureRewards(order);
            RefreshCombatHud();
        }

        private void CreditCaptureRewards(int order)
        {
            var loot = _session?.Config?.CaptureLoot;
            var unlockIds = _session?.Config?.DungeonUnlockIds;
            Debug.Log(
                $"[PushMapStage] Objective {order} captured — granting CaptureLoot='{loot}' Unlock='{unlockIds}' (no Exp).");

            if (_warehouse != null && _configs != null && !string.IsNullOrEmpty(loot))
            {
                var entries = LootDropParser.Parse(
                    loot,
                    msg => Debug.LogWarning($"[PushMapStage] {msg}"));
                for (var i = 0; i < entries.Count; i++)
                {
                    _warehouse.CreditLootEntry(
                        entries[i],
                        _configs,
                        (id, count) => Debug.Log($"[PushMapStage] CaptureLoot material +{count} {id}"),
                        spirit => Debug.Log($"[PushMapStage] CaptureLoot Spirit +{spirit}"));
                }
            }

            _dungeonUnlocks?.UnlockEncoded(unlockIds);
        }

        private void HandleCurrentObjectiveChanged(int newOrder)
        {
            RebuildFlowFieldTowardCurrentObjective();
            RefreshCombatHud();
        }

        private void RefreshCombatHud()
        {
            if (_hudView == null || _session == null)
            {
                return;
            }

            _hudView.SetCombatStatus(
                $"Shield {_session.Shield}/{_session.ShieldCap} · Degree={_session.LockedLossOfControlDegree:0.##} " +
                $"Tier={_session.LockedLossOfControlTierId} · 当前目标={_session.CurrentObjectiveOrder} · BossPending={_session.PendingBossCount}");
        }

        private void HandleVictorySettled(long stageExp)
        {
            if (!_running && _driverOutcomeDispatched)
            {
                return;
            }

            _running = false;
            DisableCameraFollow();
            WriteDungeonUnlocksOnClear();

            var before = _progress != null ? _progress.LifetimeExperience : 0L;
            var levels = _progress != null ? _progress.AddExperience(stageExp) : 0;
            var after = _progress != null ? _progress.LifetimeExperience : 0L;
            if (_hudView != null)
            {
                _hudView.SetPhaseText("推图战 — Ended");
                _hudView.SetHint($"BOSS 通关：+{stageExp} Exp（{before}→{after}）升{levels}级 → 推进阶段");
            }

            Debug.Log(
                $"[PushMapStage] Victory Exp +{stageExp} Lifetime={after} Level={_progress?.Level} (+{levels})");

            if (_driverOutcomeDispatched)
            {
                return;
            }

            _driverOutcomeDispatched = true;
            _onVictoryAdvance?.Invoke();
        }

        private void WriteDungeonUnlocksOnClear()
        {
            var unlockIds = _session?.Config?.DungeonUnlockIds;
            if (string.IsNullOrEmpty(unlockIds))
            {
                return;
            }

            Debug.Log($"[PushMapStage] Boss-clear DungeonUnlockIds='{unlockIds}'");
            _dungeonUnlocks?.UnlockEncoded(unlockIds);
        }

        private void HandleLevelFailureRequested()
        {
            if (!_running && _driverOutcomeDispatched)
            {
                return;
            }

            _running = false;
            DisableCameraFollow();
            if (_hudView != null)
            {
                _hudView.SetPhaseText("推图战 — Ended");
                _hudView.SetHint("LevelFailure：护盾归零 — 不入账本阶段经验，关卡中止");
            }

            Debug.LogWarning("[PushMapStage] LevelFailure — no stage Exp credited.");

            if (_driverOutcomeDispatched)
            {
                return;
            }

            _driverOutcomeDispatched = true;
            _onLevelFailure?.Invoke("PushMap 护盾归零");
        }

        private void OpenFormationEditor()
        {
            CloseFormationEditor();
            if (_formationCatalog == null || _formationCatalog.FormationEditorRootPrefab == null)
            {
                Debug.LogError("[PushMapStage] FormationEditorRoot missing — falling back to HUD StartBattle only.");
                if (_hudView != null)
                {
                    _hudView.SetPrepareVisible(true);
                }

                EnsurePushMapCamera();
                if (_pushMapCamera != null)
                {
                    _pushMapCamera.gameObject.SetActive(true);
                    ApplyPushMapCameraPose();
                }

                return;
            }

            var instance = Instantiate(_formationCatalog.FormationEditorRootPrefab, transform);
            instance.name = "FormationEditorRoot(Clone)";
            _formationEditor = instance.GetComponent<FormationEditorController>();
            if (_formationEditor == null)
            {
                _formationEditor = instance.AddComponent<FormationEditorController>();
            }

            _formationEditor.StartBattleRequested += HandleStartBattleRequested;
            _formationEditor.Begin(
                FormationEditorMode.PushMapPrepare,
                _catalog,
                _warriorPool,
                _formation,
                _progress,
                null,
                _mapInstance);
        }

        private void CloseFormationEditor()
        {
            if (_formationEditor == null)
            {
                return;
            }

            _formationEditor.StartBattleRequested -= HandleStartBattleRequested;
            _formationEditor.End();
            Destroy(_formationEditor.gameObject);
            _formationEditor = null;
        }

        private void EnsureWorldRoot()
        {
            if (_worldRoot == null)
            {
                var root = new GameObject("PushMapWorldRoot");
                root.transform.SetParent(transform, false);
                _worldRoot = root.transform;
            }
        }

        /// <summary>
        /// Runtime StageRoot has no Prefab camera; create PushMapCamera matching DefendCamera contract.
        /// </summary>
        private void EnsurePushMapCamera()
        {
            if (_pushMapCamera != null)
            {
                EnsureCameraFollowComponent();
                return;
            }

            var camGo = new GameObject("PushMapCamera", typeof(Camera));
            camGo.transform.SetParent(transform, false);
            camGo.SetActive(false);
            _pushMapCamera = camGo.GetComponent<Camera>();
            _pushMapCamera.orthographic = true;
            _pushMapCamera.clearFlags = CameraClearFlags.SolidColor;
            _pushMapCamera.backgroundColor = new Color(0.12f, 0.14f, 0.16f, 1f);
            _pushMapCamera.depth = 5;
            _pushMapCamera.nearClipPlane = 0.1f;
            _pushMapCamera.farClipPlane = 100f;
            EnsureCameraFollowComponent();
        }

        private void EnsureCameraFollowComponent()
        {
            if (_pushMapCamera == null)
            {
                return;
            }

            _cameraFollow = _pushMapCamera.GetComponent<PushMapCameraFollowController>();
            if (_cameraFollow == null)
            {
                _cameraFollow = _pushMapCamera.gameObject.AddComponent<PushMapCameraFollowController>();
            }
        }

        private void EnableCameraFollowForCombat()
        {
            EnsurePushMapCamera();
            EnsureResumeFollowButton();
            if (_cameraFollow == null)
            {
                return;
            }

            _cameraFollow.Bind(_pushMapCamera, _advanceViews, ResolveCurrentObjective);
            _cameraFollow.EnableForCombat();
        }

        private void DisableCameraFollow()
        {
            if (_cameraFollow != null)
            {
                _cameraFollow.Disable();
            }

            if (_resumeFollowButtonRoot != null)
            {
                _resumeFollowButtonRoot.SetActive(false);
            }
        }

        private void EnsureResumeFollowButton()
        {
            if (_resumeFollowButtonRoot != null)
            {
                if (_cameraFollow != null)
                {
                    var existing = _resumeFollowButtonRoot.GetComponentInChildren<Button>(true);
                    _cameraFollow.BindResumeButton(_resumeFollowButtonRoot, existing);
                }

                return;
            }

            var canvasGo = new GameObject(
                "PushMapResumeFollowCanvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 80;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);

            var buttonGo = new GameObject("ResumeFollowButton", typeof(RectTransform), typeof(Image), typeof(Button));
            buttonGo.transform.SetParent(canvasGo.transform, false);
            var rt = buttonGo.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.1f);
            rt.anchorMax = new Vector2(0.5f, 0.1f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(220f, 56f);
            buttonGo.GetComponent<Image>().color = new Color(0.28f, 0.42f, 0.55f, 0.95f);

            var labelGo = new GameObject("Label", typeof(RectTransform), typeof(Text));
            labelGo.transform.SetParent(buttonGo.transform, false);
            var labelRt = labelGo.GetComponent<RectTransform>();
            labelRt.anchorMin = Vector2.zero;
            labelRt.anchorMax = Vector2.one;
            labelRt.offsetMin = Vector2.zero;
            labelRt.offsetMax = Vector2.zero;
            var label = labelGo.GetComponent<Text>();
            label.text = "恢复跟随";
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Color.white;
            label.fontSize = 22;
            label.font = Resources.GetBuiltinResource<Font>("Arial.ttf");

            _resumeFollowButtonRoot = buttonGo;
            buttonGo.SetActive(false);

            var button = buttonGo.GetComponent<Button>();
            if (_cameraFollow != null)
            {
                _cameraFollow.BindResumeButton(_resumeFollowButtonRoot, button);
            }
        }

        /// <summary>
        /// Orthographic top-down pose aligned with Defend angle/height; Size fixed to 2.
        /// </summary>
        private void ApplyPushMapCameraPose()
        {
            if (_pushMapCamera == null)
            {
                return;
            }

            _pushMapCamera.transform.position = _mapCenter + new Vector3(0f, 18f, 0f);
            _pushMapCamera.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            _pushMapCamera.orthographic = true;
            _pushMapCamera.orthographicSize = 2f;
            _pushMapCamera.nearClipPlane = 0.1f;
            _pushMapCamera.farClipPlane = 100f;
        }
    }
}
