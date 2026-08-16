using System.Collections.Generic;
using Gravedigger2026.Core.Config;
using UnityEngine;

namespace Gravedigger2026.Core.Pathing
{
    /// <summary>
    /// Frame-budgeted mass move (SPEC_04 §9.7 Approach B / MP-04+MP-05; B+ wiring SC-03).
    /// Pure C#: FlowField (Objective) or DesiredDestination (AttackSlot) + SpatialHash + LocalDetour.
    /// Objective inside <see cref="ObjectiveArriveRadius"/> → hold (soft separation; no goal-cell seek).
    /// Path/steer recalc ≤ <see cref="MaxRecalcPerFrame"/> units/frame (round-robin).
    /// LocalDetour neighbors are same DetourGroup only (friendlies; SPEC_03 §3.12).
    /// SC-03: owns a <see cref="SoftCollisionService"/> — Register/Unregister/Clear auto-sync
    /// bodies (getPos reads back the sampled position by id), <see cref="Tick"/> resolves
    /// repulsion each frame, and <see cref="Register"/> injects table
    /// <c>PushCoefficient</c> / <c>RepulsionScale</c>. Views compose
    /// delta = steer·speed·dt + TryGetCorrection at Move.
    /// </summary>
    public sealed class MassMoveScheduler
    {
        public static int MaxRecalcPerFrame => CombatRuntimeTuning.MassMoveMaxRecalcPerFrame;
        public static float DefaultAgentRadius => CombatRuntimeTuning.MassMoveDefaultAgentRadius;
        public static float ArriveEpsilon => CombatRuntimeTuning.MassMoveArriveEpsilon;
        /// <summary>Default = CaptureZone radius (SPEC_03 §3.14). Stage overrides from current zone.</summary>
        public static float DefaultObjectiveArriveRadius =>
            CombatRuntimeTuning.MassMoveDefaultObjectiveArriveRadius;
        public static float AttackSlotSeparationScale =>
            CombatRuntimeTuning.MassMoveAttackSlotSeparationScale;
        public const int DetourGroupLoyal = 0;
        public const int DetourGroupMonster = 1;

        private FlowFieldService _flowField;
        private readonly SpatialHash2D _hash = new SpatialHash2D();
        private readonly LocalDetourSolver _detour = new LocalDetourSolver();
        private readonly SoftCollisionService _softCollision = new SoftCollisionService();
        private readonly List<MassMoveAgentState> _agents = new List<MassMoveAgentState>(64);
        private readonly Dictionary<int, int> _indexById = new Dictionary<int, int>(64);
        private readonly List<SpatialHashEntry> _neighborBuffer = new List<SpatialHashEntry>(32);
        private readonly List<SpatialHashEntry> _friendlyBuffer = new List<SpatialHashEntry>(32);
        private int _recalcCursor;
        private float _objectiveArriveRadius = CombatConstantKeys.Safety.MassMoveDefaultObjectiveArriveRadius;

        public int AgentCount => _agents.Count;
        public int LastFrameRecalcCount { get; private set; }

        /// <summary>SC-03: soft-collision service owned by this scheduler (Debug: ResolveCollisions).</summary>
        public SoftCollisionService SoftCollision => _softCollision;

        /// <summary>
        /// SC-04 over-budget fallback knob (SPEC_04 §9.7 fallback ⑤): per-frame body budget
        /// forwarded to <see cref="SoftCollisionService.Tick"/>; default aligns with
        /// <see cref="MaxRecalcPerFrame"/>. Lower it to widen soft-collision frame-slicing
        /// (accept separation lag). Values &lt; 1 clamp to 1.
        /// </summary>
        public int SoftCollisionMaxBodiesPerFrame { get; set; } = SoftCollisionService.DefaultMaxBodiesPerFrame;

        /// <summary>
        /// XZ radius around FlowField goal for Objective hold (CaptureZone). Inside: stop seeking center.
        /// </summary>
        public float ObjectiveArriveRadius => _objectiveArriveRadius;

        public void BindFlowField(FlowFieldService flowField)
        {
            _flowField = flowField;
        }

        /// <summary>PushMap Stage sets from current <c>CaptureZone.Radius</c> (clamped ≥0.01).</summary>
        public void SetObjectiveArriveRadius(float radius)
        {
            _objectiveArriveRadius = Mathf.Max(0.01f, radius);
        }

        public void Clear()
        {
            _agents.Clear();
            _indexById.Clear();
            _hash.Clear();
            _neighborBuffer.Clear();
            _friendlyBuffer.Clear();
            _softCollision.Clear();
            _recalcCursor = 0;
            LastFrameRecalcCount = 0;
            _objectiveArriveRadius = DefaultObjectiveArriveRadius;
        }

        public void Register(
            int id,
            float radius = CombatConstantKeys.Safety.MassMoveDefaultAgentRadius,
            int detourGroup = DetourGroupLoyal,
            float pushCoefficient = SoftCollisionService.DefaultPushCoefficient,
            float repulsionScale = SoftCollisionService.DefaultRepulsionScale)
        {
            if (radius <= 0f)
            {
                radius = DefaultAgentRadius;
            }

            if (_indexById.ContainsKey(id))
            {
                return;
            }

            _indexById[id] = _agents.Count;
            _agents.Add(new MassMoveAgentState(id, Mathf.Max(0.01f, radius), detourGroup));
            // SC-03: soft body mirrors this registration; getPos reads the live sampled position.
            // Overlap radius = BodyRadius; PushCoefficient scales shove impulse only (Approach B).
            // Per-body RepulsionScale from BodyAppearanceConfig / MonsterConfig (not GoalKind).
            _softCollision.Register(
                id,
                Mathf.Max(0.01f, radius),
                () => GetAgentPositionXZ(id),
                pushCoefficient);
            _softCollision.SetRepulsionScale(id, repulsionScale);
        }

        public void Unregister(int id)
        {
            if (!_indexById.TryGetValue(id, out var index))
            {
                return;
            }

            var last = _agents.Count - 1;
            if (index != last)
            {
                var moved = _agents[last];
                _agents[index] = moved;
                _indexById[moved.Id] = index;
            }

            _agents.RemoveAt(last);
            _indexById.Remove(id);
            _softCollision.Unregister(id);
            if (_recalcCursor >= _agents.Count)
            {
                _recalcCursor = 0;
            }
        }

        /// <summary>
        /// Rules/Stage sets goal: Objective → FlowField; AttackSlot/Home/ChaseAnchor → DesiredDestination XZ.
        /// </summary>
        public void SetGoal(int id, GoalKind kind, Vector2 desiredDestinationXZ = default)
        {
            if (!_indexById.TryGetValue(id, out var index))
            {
                return;
            }

            var agent = _agents[index];
            agent.GoalKind = kind;
            agent.DesiredDestination = desiredDestinationXZ;
            _agents[index] = agent;
        }

        public bool TryGetGoal(int id, out GoalKind kind, out Vector2 desiredDestinationXZ)
        {
            if (_indexById.TryGetValue(id, out var index))
            {
                var agent = _agents[index];
                kind = agent.GoalKind;
                desiredDestinationXZ = agent.DesiredDestination;
                return true;
            }

            kind = GoalKind.Objective;
            desiredDestinationXZ = default;
            return false;
        }

        /// <summary>
        /// SPEC_04 §15.5: planar distance for attack→run interrupt gate.
        /// AttackSlot / FormationHome / ChaseAnchor → |DesiredDestination − positionXZ|;
        /// Objective (FlowField) or missing id → +∞ (treat as far enough to interrupt).
        /// </summary>
        public float GetAnimMoveTargetDistanceXZ(int id, Vector2 positionXZ)
        {
            if (!TryGetGoal(id, out var kind, out var dest) || kind == GoalKind.Objective)
            {
                return float.PositiveInfinity;
            }

            return (dest - positionXZ).magnitude;
        }

        /// <summary>
        /// Call once per frame from Stage: refresh positions, rebuild hash, resolve soft
        /// collision (SC-03), recalc ≤50 steers. Presentation Views then read
        /// <see cref="TryGetSteer"/> + <see cref="TryGetCorrection"/> and apply motion.
        /// <paramref name="dt"/> caps this frame's soft-collision impulse (units = distance).
        /// </summary>
        public void Tick(IReadOnlyList<MassMoveSample> samples, float dt)
        {
            LastFrameRecalcCount = 0;
            if (_agents.Count == 0)
            {
                return;
            }

            ApplySamples(samples);
            RebuildHash();
            _softCollision.Tick(dt, Mathf.Max(1, SoftCollisionMaxBodiesPerFrame));

            var budget = Mathf.Min(MaxRecalcPerFrame, _agents.Count);
            for (var n = 0; n < budget; n++)
            {
                if (_agents.Count == 0)
                {
                    break;
                }

                if (_recalcCursor >= _agents.Count)
                {
                    _recalcCursor = 0;
                }

                RecalcSteerAt(_recalcCursor);
                _recalcCursor++;
                LastFrameRecalcCount++;
            }
        }

        public bool TryGetSteer(int id, out Vector2 steerXZ)
        {
            if (_indexById.TryGetValue(id, out var index))
            {
                steerXZ = _agents[index].Steer;
                return true;
            }

            steerXZ = Vector2.zero;
            return false;
        }

        /// <summary>
        /// SC-03: this frame's soft-collision position impulse (XZ distance, not a direction).
        /// Views add it to the steer-based move delta; it keeps bodies separating even when
        /// the steer is zero (Objective hold / windup).
        /// </summary>
        public bool TryGetCorrection(int id, out Vector2 correctionXz)
        {
            return _softCollision.TryGetCorrection(id, out correctionXz);
        }

        private Vector2 GetAgentPositionXZ(int id)
        {
            return _indexById.TryGetValue(id, out var index) ? _agents[index].Position : Vector2.zero;
        }

        public void SetPaused(int id, bool paused)
        {
            if (!_indexById.TryGetValue(id, out var index))
            {
                return;
            }

            var agent = _agents[index];
            agent.Paused = paused;
            if (paused)
            {
                agent.Steer = Vector2.zero;
            }

            _agents[index] = agent;
        }

        private void ApplySamples(IReadOnlyList<MassMoveSample> samples)
        {
            if (samples == null)
            {
                return;
            }

            for (var i = 0; i < samples.Count; i++)
            {
                var sample = samples[i];
                if (!_indexById.TryGetValue(sample.Id, out var index))
                {
                    continue;
                }

                var agent = _agents[index];
                agent.Position = sample.Position;
                agent.Active = sample.Active;
                if (sample.Radius > 0f)
                {
                    agent.Radius = sample.Radius;
                }

                _agents[index] = agent;
            }
        }

        private void RebuildHash()
        {
            _hash.Clear();
            for (var i = 0; i < _agents.Count; i++)
            {
                var agent = _agents[i];
                if (!agent.Active || agent.Paused)
                {
                    continue;
                }

                _hash.Insert(agent.Id, agent.Position, agent.Radius);
            }
        }

        private void RecalcSteerAt(int index)
        {
            var agent = _agents[index];
            if (!agent.Active || agent.Paused)
            {
                agent.Steer = Vector2.zero;
                _agents[index] = agent;
                return;
            }

            Vector2 desired;
            var objectiveHold = false;
            if (agent.GoalKind == GoalKind.Objective)
            {
                if (_flowField == null || !_flowField.HasField)
                {
                    agent.Steer = Vector2.zero;
                    _agents[index] = agent;
                    return;
                }

                var goal = _flowField.GoalWorld;
                var toGoal = new Vector2(goal.x - agent.Position.x, goal.z - agent.Position.y);
                var arriveR = _objectiveArriveRadius;
                if (toGoal.sqrMagnitude <= arriveR * arriveR)
                {
                    // Inside CaptureZone: do not seek goal-cell center (zero-vector pile-up).
                    desired = Vector2.zero;
                    objectiveHold = true;
                }
                else
                {
                    var world = new Vector3(agent.Position.x, 0f, agent.Position.y);
                    desired = _flowField.SampleDir(world);
                    if (desired.sqrMagnitude < 1e-8f && toGoal.sqrMagnitude > 1e-8f)
                    {
                        // Unreachable / zero cell while still outside zone → direct seek.
                        desired = toGoal.normalized;
                    }
                }
            }
            else
            {
                // AttackSlot / FormationHome / ChaseAnchor: straight toward DesiredDestination.
                var delta = agent.DesiredDestination - agent.Position;
                if (delta.sqrMagnitude <= ArriveEpsilon * ArriveEpsilon)
                {
                    agent.Steer = Vector2.zero;
                    _agents[index] = agent;
                    return;
                }

                desired = delta.normalized;
            }

            _neighborBuffer.Clear();
            var queryRadius = SpatialHash2D.RecommendedQueryRadius(agent.Radius);
            _hash.QueryNeighbors(agent.Position, queryRadius, _neighborBuffer);

            _friendlyBuffer.Clear();
            for (var i = 0; i < _neighborBuffer.Count; i++)
            {
                var n = _neighborBuffer[i];
                if (n.Id == agent.Id)
                {
                    continue;
                }

                if (!_indexById.TryGetValue(n.Id, out var nIndex))
                {
                    continue;
                }

                if (_agents[nIndex].DetourGroup != agent.DetourGroup)
                {
                    continue;
                }

                _friendlyBuffer.Add(n);
            }

            // Objective hold / zero desired: still run LocalDetour for soft separation (may be zero).
            if (!objectiveHold && desired.sqrMagnitude < 1e-8f && _friendlyBuffer.Count == 0)
            {
                agent.Steer = Vector2.zero;
                _agents[index] = agent;
                return;
            }

            var self = new LocalDetourAgent(agent.Id, agent.Position, agent.Radius);
            var sepScale = agent.GoalKind == GoalKind.AttackSlot || agent.GoalKind == GoalKind.ChaseAnchor
                ? AttackSlotSeparationScale
                : 1f;
            agent.Steer = _detour.Steer(desired, self, _friendlyBuffer, sepScale);
            _agents[index] = agent;
        }

        private struct MassMoveAgentState
        {
            public readonly int Id;
            public readonly int DetourGroup;
            public float Radius;
            public Vector2 Position;
            public Vector2 Steer;
            public Vector2 DesiredDestination;
            public GoalKind GoalKind;
            public bool Active;
            public bool Paused;

            public MassMoveAgentState(int id, float radius, int detourGroup)
            {
                Id = id;
                DetourGroup = detourGroup;
                Radius = radius;
                Position = Vector2.zero;
                Steer = Vector2.zero;
                DesiredDestination = Vector2.zero;
                GoalKind = GoalKind.Objective;
                Active = false;
                Paused = false;
            }
        }
    }

    /// <summary>Per-frame position sample fed into <see cref="MassMoveScheduler.Tick"/>.</summary>
    public readonly struct MassMoveSample
    {
        public readonly int Id;
        public readonly Vector2 Position;
        public readonly float Radius;
        public readonly bool Active;

        public MassMoveSample(int id, Vector2 position, float radius, bool active)
        {
            Id = id;
            Position = position;
            Radius = radius;
            Active = active;
        }
    }
}
