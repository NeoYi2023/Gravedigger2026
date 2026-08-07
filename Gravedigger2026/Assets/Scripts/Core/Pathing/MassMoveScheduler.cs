using System.Collections.Generic;
using UnityEngine;

namespace Gravedigger2026.Core.Pathing
{
    /// <summary>
    /// Frame-budgeted mass move (SPEC_04 §9.7 Approach B / MP-04+MP-05).
    /// Pure C#: FlowField (Objective) or DesiredDestination (AttackSlot) + SpatialHash + LocalDetour.
    /// Path/steer recalc ≤ <see cref="MaxRecalcPerFrame"/> units/frame (round-robin).
    /// LocalDetour neighbors are same DetourGroup only (friendlies; SPEC_03 §3.12).
    /// </summary>
    public sealed class MassMoveScheduler
    {
        public const int MaxRecalcPerFrame = 50;
        public const float DefaultAgentRadius = 0.1f;
        public const float ArriveEpsilon = 0.08f;
        public const float AttackSlotSeparationScale = 0.35f;
        public const int DetourGroupLoyal = 0;
        public const int DetourGroupMonster = 1;

        private FlowFieldService _flowField;
        private readonly SpatialHash2D _hash = new SpatialHash2D();
        private readonly LocalDetourSolver _detour = new LocalDetourSolver();
        private readonly List<MassMoveAgentState> _agents = new List<MassMoveAgentState>(64);
        private readonly Dictionary<int, int> _indexById = new Dictionary<int, int>(64);
        private readonly List<SpatialHashEntry> _neighborBuffer = new List<SpatialHashEntry>(32);
        private readonly List<SpatialHashEntry> _friendlyBuffer = new List<SpatialHashEntry>(32);
        private int _recalcCursor;

        public int AgentCount => _agents.Count;
        public int LastFrameRecalcCount { get; private set; }

        public void BindFlowField(FlowFieldService flowField)
        {
            _flowField = flowField;
        }

        public void Clear()
        {
            _agents.Clear();
            _indexById.Clear();
            _hash.Clear();
            _neighborBuffer.Clear();
            _friendlyBuffer.Clear();
            _recalcCursor = 0;
            LastFrameRecalcCount = 0;
        }

        public void Register(int id, float radius = DefaultAgentRadius, int detourGroup = DetourGroupLoyal)
        {
            if (_indexById.ContainsKey(id))
            {
                return;
            }

            _indexById[id] = _agents.Count;
            _agents.Add(new MassMoveAgentState(id, Mathf.Max(0.01f, radius), detourGroup));
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
        /// Call once per frame from Stage: refresh positions, rebuild hash, recalc ≤50 steers.
        /// Presentation Views then read <see cref="TryGetSteer"/> and apply motion.
        /// </summary>
        public void Tick(IReadOnlyList<MassMoveSample> samples)
        {
            LastFrameRecalcCount = 0;
            if (_agents.Count == 0)
            {
                return;
            }

            ApplySamples(samples);
            RebuildHash();

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
            if (agent.GoalKind == GoalKind.Objective)
            {
                if (_flowField == null || !_flowField.HasField)
                {
                    agent.Steer = Vector2.zero;
                    _agents[index] = agent;
                    return;
                }

                var world = new Vector3(agent.Position.x, 0f, agent.Position.y);
                desired = _flowField.SampleDir(world);
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

            if (desired.sqrMagnitude < 1e-8f)
            {
                agent.Steer = Vector2.zero;
                _agents[index] = agent;
                return;
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
