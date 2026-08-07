using System;
using System.Collections.Generic;
using Gravedigger2026.Core.Config;
using Gravedigger2026.Core.Pathing;
using Gravedigger2026.Gameplay.Pathing;
using UnityEngine;
using UnityEngine.AI;

namespace Gravedigger2026.Gameplay.PushMap
{
    /// <summary>
    /// MP-04/05: loyal advance via FlowField; engage → GoalKind=AttackSlot (SPEC_03 §3.12/§3.14).
    /// Samples MassMoveScheduler steer; applies NavMeshAgent.Move — no per-frame SetDestination.
    /// Capture-zone monsters do NOT pause advance. Rebels do not advance.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PushMapAdvanceView : MonoBehaviour
    {
        private const float NavMeshSampleRadius = 12f;
        private const float SoldierDemoRadius = 0.1f;
        private const float DefaultAttackRange = 1f;

        private Func<IReadOnlyList<PushMapMonsterAgentView>> _monstersProvider;
        private MassMoveScheduler _scheduler;
        private NavMeshAgent _agent;
        private float _moveSpeed = 3.5f;
        private float _attackRange = DefaultAttackRange;
        private AttackMode _attackMode = AttackMode.Melee;
        private string _attackerId;
        private bool _isRebel;
        private int _moveId;

        private AttackSlotService _attackSlots;

        public bool IsRebel => _isRebel;
        public int MoveId => _moveId;
        public float AgentRadius => SoldierDemoRadius;
        public float AttackRange => _attackRange;
        public AttackMode AttackMode => _attackMode;
        public string AttackerId => _attackerId;

        public void Bind(
            MassMoveScheduler scheduler,
            int moveId,
            float moveSpeed,
            Func<IReadOnlyList<PushMapMonsterAgentView>> monstersProvider = null,
            float attackRange = DefaultAttackRange,
            AttackMode attackMode = AttackMode.Melee,
            string attackerId = null,
            AttackSlotService attackSlots = null)
        {
            _scheduler = scheduler ?? throw new ArgumentNullException(nameof(scheduler));
            _moveId = moveId;
            _monstersProvider = monstersProvider;
            _moveSpeed = Mathf.Max(0.1f, moveSpeed);
            _attackRange = Mathf.Max(0.05f, attackRange);
            _attackMode = attackMode;
            _attackerId = string.IsNullOrEmpty(attackerId) ? gameObject.name : attackerId;
            _attackSlots = attackSlots;

            _agent = GetComponent<NavMeshAgent>();
            if (_agent == null)
            {
                _agent = gameObject.AddComponent<NavMeshAgent>();
            }

            _agent.speed = _moveSpeed;
            _agent.stoppingDistance = 0f;
            _agent.angularSpeed = 720f;
            _agent.acceleration = 24f;
            _agent.radius = SoldierDemoRadius;
            _agent.height = 0.1f;
            _agent.autoBraking = false;
            // Facing via Animator DirIndex in PushMap as in Defend (SPEC_04 §15.2).
            _agent.updateRotation = false;
            // Field/slot follow: LocalDetour owns friendlies (no RVO scale scheme).
            _agent.obstacleAvoidanceType = ObstacleAvoidanceType.NoObstacleAvoidance;

            _scheduler.Register(_moveId, SoldierDemoRadius, MassMoveScheduler.DetourGroupLoyal);
            _scheduler.SetGoal(_moveId, GoalKind.Objective);
            TryWarpOntoNavMesh();
            ClearPathingState();

            var taskLabel = GetComponent<WarriorTaskDebugLabelView>();
            if (taskLabel == null)
            {
                taskLabel = gameObject.AddComponent<WarriorTaskDebugLabelView>();
            }

            taskLabel.Bind(_scheduler, _moveId);
        }

        public void SetRebel(bool isRebel)
        {
            _isRebel = isRebel;
            if (isRebel)
            {
                _scheduler?.SetPaused(_moveId, true);
                ClearPathingState();
            }
        }

        /// <summary>
        /// Nearest living monster inside Demo engage detect (MP-05: enter AttackSlot, leave Objective field).
        /// </summary>
        public bool TryGetEngageMonster(out PushMapMonsterAgentView monster)
        {
            monster = null;
            var list = _monstersProvider != null ? _monstersProvider() : null;
            if (list == null || list.Count == 0)
            {
                return false;
            }

            var bestDist = float.MaxValue;
            for (var i = 0; i < list.Count; i++)
            {
                var m = list[i];
                if (m == null || !m.IsAlive)
                {
                    continue;
                }

                var detect = Mathf.Max(m.AttackRange, m.BodyRadius + SoldierDemoRadius);
                if (detect <= 0f)
                {
                    continue;
                }

                var d = Vector3.Distance(transform.position, m.transform.position);
                if (d > detect)
                {
                    continue;
                }

                if (d < bestDist)
                {
                    bestDist = d;
                    monster = m;
                }
            }

            return monster != null;
        }

        /// <summary>XZ sample for MassMoveScheduler (Active=false when rebel).</summary>
        public MassMoveSample BuildSample()
        {
            var pos = transform.position;
            return new MassMoveSample(
                _moveId,
                new Vector2(pos.x, pos.z),
                SoldierDemoRadius,
                active: !_isRebel && isActiveAndEnabled);
        }

        private void LateUpdate()
        {
            if (_isRebel || _agent == null || _scheduler == null)
            {
                return;
            }

            if (!_agent.isOnNavMesh)
            {
                TryWarpOntoNavMesh();
                if (!_agent.isOnNavMesh)
                {
                    return;
                }
            }

            if (!_scheduler.TryGetSteer(_moveId, out var steer) || steer.sqrMagnitude < 1e-8f)
            {
                ClearPathingState();
                return;
            }

            // No SetDestination — follow scheduler steer (Objective field or AttackSlot).
            if (_agent.hasPath)
            {
                _agent.ResetPath();
            }

            _agent.isStopped = false;
            var delta = new Vector3(steer.x, 0f, steer.y) * (_moveSpeed * Time.deltaTime);
            _agent.Move(delta);
        }

        private void OnDisable()
        {
            _attackSlots?.Release(_attackerId);
            _scheduler?.Unregister(_moveId);
        }

        private void ClearPathingState()
        {
            if (_agent == null)
            {
                return;
            }

            if (_agent.isOnNavMesh)
            {
                if (_agent.hasPath)
                {
                    _agent.ResetPath();
                }

                _agent.isStopped = true;
                _agent.velocity = Vector3.zero;
            }
        }

        private void TryWarpOntoNavMesh()
        {
            if (_agent == null || _agent.isOnNavMesh)
            {
                return;
            }

            if (NavMesh.SamplePosition(transform.position, out var hit, NavMeshSampleRadius, NavMesh.AllAreas))
            {
                _agent.Warp(hit.position);
            }
        }
    }
}
