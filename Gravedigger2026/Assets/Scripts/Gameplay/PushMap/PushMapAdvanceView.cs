using System;
using System.Collections.Generic;
using Gravedigger2026.Core.Config;
using Gravedigger2026.Core.Pathing;
using Gravedigger2026.Gameplay.Defend;
using Gravedigger2026.Gameplay.Pathing;
using UnityEngine;
using UnityEngine.AI;

namespace Gravedigger2026.Gameplay.PushMap
{
    /// <summary>
    /// MP-04/05: loyal advance via FlowField; engage → GoalKind=AttackSlot (SPEC_03 §3.12/§3.14).
    /// Samples MassMoveScheduler steer; applies NavMeshAgent.Move — no per-frame SetDestination.
    /// Capture-zone monsters do NOT pause advance. Rebels do not advance.
    /// Presentation (SPEC_03 §3.14 attack-presentation edge): WarriorAnimView drives
    /// SetMoving/DirIndex facing; while holding an AttackSlot claim on a living monster,
    /// faces the target and loops PlayAttack at <see cref="PushMapAttackAnimSeconds"/>.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PushMapAdvanceView : MonoBehaviour
    {
        private const float NavMeshSampleRadius = 12f;
        private const float SoldierDemoRadius = 0.1f;
        private const float DefaultAttackRange = 1f;
        /// <summary>SPEC_03 §3.14: fixed attack-anim loop interval (real AttackSpeed deferred).</summary>
        private const float PushMapAttackAnimSeconds = 0.6f;
        private const float MoveAnimSpeedSqr = 0.04f;
        /// <summary>
        /// SPEC_03 §3.14 v0.74.10: a rival must be closer than the claimed target by more
        /// than this margin to steal the claim — dense packs + soft-collision jostle
        /// otherwise flip-flop the strictly-nearest target and starve the kill engage clock.
        /// </summary>
        private const float EngageStickHysteresisMargin = 0.15f;

        private Func<IReadOnlyList<PushMapMonsterAgentView>> _monstersProvider;
        private MassMoveScheduler _scheduler;
        private NavMeshAgent _agent;
        private WarriorAnimView _anim;
        private float _attackAnimCooldown;
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

            _anim = GetComponent<WarriorAnimView>();
            if (_anim == null)
            {
                _anim = gameObject.AddComponent<WarriorAnimView>();
            }

            _anim.ResetToIdle();
            _attackAnimCooldown = 0f;
        }

        public void SetRebel(bool isRebel)
        {
            _isRebel = isRebel;
            if (isRebel)
            {
                _scheduler?.SetPaused(_moveId, true);
                _attackAnimCooldown = 0f;
                ClearPathingState();
            }
        }

        /// <summary>
        /// Nearest living monster inside Demo engage detect (MP-05: enter AttackSlot, leave
        /// Objective field). v0.74.10 sticky hysteresis (SPEC_03 §3.14): while the claimed
        /// target is alive and still inside its detect radius, a rival steals the claim only
        /// when closer by more than <see cref="EngageStickHysteresisMargin"/>.
        /// </summary>
        public bool TryGetEngageMonster(out PushMapMonsterAgentView monster)
        {
            monster = null;
            var list = _monstersProvider != null ? _monstersProvider() : null;
            if (list == null || list.Count == 0)
            {
                return false;
            }

            string claimedId = null;
            var hasClaim = _attackSlots != null &&
                           _attackSlots.TryGetClaimedTargetId(_attackerId, out claimedId);

            PushMapMonsterAgentView best = null;
            var bestDist = float.MaxValue;
            PushMapMonsterAgentView claimed = null;
            var claimedDist = float.MaxValue;
            for (var i = 0; i < list.Count; i++)
            {
                var m = list[i];
                if (m == null || !m.IsAlive)
                {
                    continue;
                }

                // Align with Demo kill reach so FlowField pass-by enters AttackSlot
                // before PollMonsterDemoKill (SPEC_03 §3.12 / §3.14).
                var detect = Mathf.Max(m.AttackRange, _attackRange, m.BodyRadius + SoldierDemoRadius) +
                             MassMoveScheduler.ArriveEpsilon;
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
                    best = m;
                }

                if (hasClaim && m.RuntimeTargetId == claimedId)
                {
                    claimed = m;
                    claimedDist = d;
                }
            }

            if (claimed != null)
            {
                var stolen = best != null && best != claimed &&
                             bestDist < claimedDist - EngageStickHysteresisMargin;
                monster = stolen ? best : claimed;
            }
            else
            {
                monster = best;
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

        private void Update()
        {
            TickAnimPresentation();
        }

        /// <summary>
        /// SPEC_03 §3.14 attack-presentation edge: engaged (AttackSlot claim on a living
        /// monster) → face target + loop PlayAttack at PushMapAttackAnimSeconds; otherwise
        /// SetMoving + DirIndex facing from velocity (same driver as Defend, SPEC_04 §15.5).
        /// </summary>
        private void TickAnimPresentation()
        {
            if (_anim == null)
            {
                return;
            }

            if (_isRebel)
            {
                _attackAnimCooldown = 0f;
                _anim.SetMoving(false);
                return;
            }

            if (TryResolveEngagedTarget(out var target))
            {
                var toTarget = target.transform.position - transform.position;
                toTarget.y = 0f;
                if (toTarget.sqrMagnitude > 0.0001f)
                {
                    _anim.SetFacing(toTarget);
                }

                _anim.SetMoving(false);
                _attackAnimCooldown -= Time.deltaTime;
                if (_attackAnimCooldown <= 0f)
                {
                    _attackAnimCooldown = PushMapAttackAnimSeconds;
                    _anim.PlayAttack();
                }

                return;
            }

            _attackAnimCooldown = 0f;
            var moving = _agent != null &&
                         _agent.isOnNavMesh &&
                         !_agent.isStopped &&
                         _agent.velocity.sqrMagnitude > MoveAnimSpeedSqr;
            _anim.SetMoving(moving);
            if (moving)
            {
                var vel = _agent.velocity;
                vel.y = 0f;
                if (vel.sqrMagnitude > 0.0001f)
                {
                    _anim.SetFacing(vel);
                }
            }
        }

        /// <summary>Engaged = GoalKind.AttackSlot + claimed target still alive (presentation mirror of Stage gates).</summary>
        private bool TryResolveEngagedTarget(out PushMapMonsterAgentView target)
        {
            target = null;
            if (_scheduler == null || _attackSlots == null)
            {
                return false;
            }

            if (!_scheduler.TryGetGoal(_moveId, out var kind, out _) || kind != GoalKind.AttackSlot)
            {
                return false;
            }

            if (!_attackSlots.TryGetClaimedTargetId(_attackerId, out var targetId))
            {
                return false;
            }

            var list = _monstersProvider != null ? _monstersProvider() : null;
            if (list == null)
            {
                return false;
            }

            for (var i = 0; i < list.Count; i++)
            {
                var m = list[i];
                if (m != null && m.IsAlive && m.RuntimeTargetId == targetId)
                {
                    target = m;
                    return true;
                }
            }

            return false;
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

            // SC-03: soft-collision impulse applies even on zero-steer frames (CaptureZone hold).
            var hasSteer = _scheduler.TryGetSteer(_moveId, out var steer) && steer.sqrMagnitude > 1e-8f;
            var hasCorrection =
                _scheduler.TryGetCorrection(_moveId, out var correction) &&
                correction.sqrMagnitude > 1e-8f;
            if (!hasSteer && !hasCorrection)
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
            var delta = hasSteer
                ? new Vector3(steer.x, 0f, steer.y) * (_moveSpeed * Time.deltaTime)
                : Vector3.zero;
            if (hasCorrection)
            {
                delta.x += correction.x;
                delta.z += correction.y;
            }

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
