using System;
using System.Collections.Generic;
using Gravedigger2026.Core.Config;
using Gravedigger2026.Core.Defend;
using Gravedigger2026.Core.Pathing;
using Gravedigger2026.Core.PushMap;
using Gravedigger2026.Gameplay.Combat;
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
    /// PM-12 (Approach B, SPEC_04 §9.22): WarriorCombat scheme D — melee windup →
    /// TryConfirmMeleeHit; ranged → shared ProjectileView soft-hit → TryConfirmRangedHit
    /// (timeout = miss, no settlement). Combat params come from PushMapSessionService
    /// StartBattle registry (WarriorCombatMath + ClassConfig, mirrored from Defend).
    /// PM-13: CombatDead → PlayDie + stop acting (aligned with Defend WarriorAgentView).
    /// Presentation: WarriorAnimView SetMoving/DirIndex facing; PlayAttack on windup/fire.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PushMapAdvanceView : MonoBehaviour
    {
        private enum AttackPhase
        {
            IdleOrMove = 0,
            Windup = 1
        }

        private const float NavMeshSampleRadius = 12f;
        private const float DefaultAttackRange = 1f;
        private const float MoveAnimSpeedSqr = 0.01f;
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
        private float _baseMoveSpeed = 3.5f;
        private float _chaseMoveSpeedMult = ClassConfigRow.DefaultChaseMoveSpeedMult;
        private float _attackRange = DefaultAttackRange;
        private AttackMode _attackMode = AttackMode.Melee;
        private string _attackerId;
        private bool _isRebel;
        private int _moveId;
        private bool _diePlayed;
        private bool _stoppedActing;
        private float _bodyRadius = BodyAppearanceConfigRow.DefaultBodyRadius;
        private float _pushCoefficient = BodyAppearanceConfigRow.DefaultPushCoefficient;
        private float _repulsionScale = BodyAppearanceConfigRow.DefaultRepulsionScale;
        private bool _facingYawFlip;

        private PushMapSessionService _session;
        private GameObject _projectilePrefab;
        private Transform _projectileParent;
        private float _attackStartCooldown;
        private float _windupRemaining;
        private string _windupTargetId;
        private AttackPhase _attackPhase = AttackPhase.IdleOrMove;

        private AttackSlotService _attackSlots;
        /// <summary>Last MassMove steer XZ (LateUpdate); drives IsRun — not NavMeshAgent.velocity (SPEC_04 §15.5).</summary>
        private Vector3 _lastSteerDirXZ;
        private readonly StuckHoldTracker _stuckHold = new StuckHoldTracker();
        private AllyFootCircleView _footCircle;

        public bool IsRebel => _isRebel;
        public int MoveId => _moveId;
        public float AgentRadius => _bodyRadius;
        public string AttackerId => _attackerId;

        /// <summary>True while Session says this warrior can still act (not CombatDead).</summary>
        public bool IsCombatActive =>
            _session == null ||
            string.IsNullOrEmpty(_attackerId) ||
            _session.IsWarriorCombatActive(_attackerId);

        /// <summary>Session registry value (PM-12); Bind fallback when unregistered.</summary>
        public float AttackRange
        {
            get
            {
                if (_session != null &&
                    !string.IsNullOrEmpty(_attackerId) &&
                    _session.TryGetWarrior(_attackerId, out var state) &&
                    state != null)
                {
                    return state.AttackRange;
                }

                return _attackRange;
            }
        }

        /// <summary>Session registry value (PM-12); Bind fallback when unregistered.</summary>
        public AttackMode AttackMode
        {
            get
            {
                if (_session != null &&
                    !string.IsNullOrEmpty(_attackerId) &&
                    _session.TryGetWarrior(_attackerId, out var state) &&
                    state != null)
                {
                    return state.AttackMode;
                }

                return _attackMode;
            }
        }

        public void Bind(
            MassMoveScheduler scheduler,
            int moveId,
            float moveSpeed,
            Func<IReadOnlyList<PushMapMonsterAgentView>> monstersProvider = null,
            float attackRange = DefaultAttackRange,
            AttackMode attackMode = AttackMode.Melee,
            string attackerId = null,
            AttackSlotService attackSlots = null,
            PushMapSessionService session = null,
            GameObject projectilePrefab = null,
            Transform projectileParent = null,
            float bodyRadius = BodyAppearanceConfigRow.DefaultBodyRadius,
            bool facingYawFlip = false,
            float pushCoefficient = BodyAppearanceConfigRow.DefaultPushCoefficient,
            float repulsionScale = BodyAppearanceConfigRow.DefaultRepulsionScale,
            float chaseMoveSpeedMult = ClassConfigRow.DefaultChaseMoveSpeedMult)
        {
            _scheduler = scheduler ?? throw new ArgumentNullException(nameof(scheduler));
            _moveId = moveId;
            _monstersProvider = monstersProvider;
            _baseMoveSpeed = Mathf.Max(0.1f, moveSpeed);
            _chaseMoveSpeedMult = Mathf.Max(0f, chaseMoveSpeedMult);
            _attackRange = Mathf.Max(0.05f, attackRange);
            _attackMode = attackMode;
            _attackerId = string.IsNullOrEmpty(attackerId) ? gameObject.name : attackerId;
            _attackSlots = attackSlots;
            _session = session;
            _projectilePrefab = projectilePrefab;
            _projectileParent = projectileParent;
            _bodyRadius = Mathf.Max(0.05f, bodyRadius);
            _pushCoefficient = Mathf.Max(0f, pushCoefficient);
            _repulsionScale = Mathf.Max(0f, repulsionScale);
            _facingYawFlip = facingYawFlip;
            _attackStartCooldown = 0f;
            _windupRemaining = 0f;
            _windupTargetId = null;
            _attackPhase = AttackPhase.IdleOrMove;
            _diePlayed = false;
            _stoppedActing = false;
            _stuckHold.Reset();
            _lastSteerDirXZ = Vector3.zero;

            _agent = GetComponent<NavMeshAgent>();
            if (_agent == null)
            {
                _agent = gameObject.AddComponent<NavMeshAgent>();
            }

            ApplyEffectiveMoveSpeed();
            _agent.stoppingDistance = 0f;
            _agent.angularSpeed = 720f;
            _agent.acceleration = 24f;
            _agent.radius = _bodyRadius;
            _agent.height = 0.1f;
            _agent.autoBraking = false;
            // Facing via Animator DirIndex in PushMap as in Defend (SPEC_04 §15.2).
            _agent.updateRotation = false;
            // Field/slot follow: LocalDetour owns friendlies (no RVO scale scheme).
            _agent.obstacleAvoidanceType = ObstacleAvoidanceType.NoObstacleAvoidance;

            _scheduler.Register(
                _moveId,
                _bodyRadius,
                MassMoveScheduler.DetourGroupLoyal,
                _pushCoefficient,
                _repulsionScale);
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

            _anim.SetFacingYawFlip(_facingYawFlip);
            _anim.ResetToIdle();

            EnsureFootCircle();
            _footCircle.Bind(_bodyRadius);
            _footCircle.SetVisible(!_isRebel);
        }

        private void EnsureFootCircle()
        {
            if (_footCircle != null)
            {
                return;
            }

            _footCircle = GetComponent<AllyFootCircleView>();
            if (_footCircle == null)
            {
                _footCircle = gameObject.AddComponent<AllyFootCircleView>();
            }
        }

        private void HideFootCircle()
        {
            if (_footCircle != null)
            {
                _footCircle.SetVisible(false);
            }
        }

        /// <summary>
        /// Effective speed: base × ChaseMoveSpeedMult only when GoalKind=AttackSlot (SPEC_03 §3.12).
        /// </summary>
        private float ResolveEffectiveMoveSpeed()
        {
            var mult = 1f;
            if (_scheduler != null &&
                _scheduler.TryGetGoal(_moveId, out var kind, out _) &&
                kind == GoalKind.AttackSlot)
            {
                mult = _chaseMoveSpeedMult;
            }

            return Mathf.Max(0.1f, _baseMoveSpeed * mult);
        }

        private void ApplyEffectiveMoveSpeed()
        {
            if (_agent == null)
            {
                return;
            }

            _agent.speed = ResolveEffectiveMoveSpeed();
        }

        public void SetRebel(bool isRebel)
        {
            _isRebel = isRebel;
            if (isRebel)
            {
                HideFootCircle();
                _attackPhase = AttackPhase.IdleOrMove;
                _windupTargetId = null;
                _attackStartCooldown = 0f;
                _scheduler?.SetPaused(_moveId, true);
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

                // Detect reach = edge-gap AttackRange + both BodyRadii (SPEC_03 §3.12 v0.75.24).
                var detect = CombatReach.EngageDetectRadius(
                    m.AttackRange,
                    AttackRange,
                    m.BodyRadius,
                    _bodyRadius,
                    MassMoveScheduler.ArriveEpsilon);
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

        /// <summary>XZ sample for MassMoveScheduler (inactive when rebel / windup / combat-down).</summary>
        public MassMoveSample BuildSample()
        {
            var pos = transform.position;
            return new MassMoveSample(
                _moveId,
                new Vector2(pos.x, pos.z),
                _bodyRadius,
                active: !_isRebel &&
                        isActiveAndEnabled &&
                        IsCombatActive &&
                        _attackPhase != AttackPhase.Windup);
        }

        private void Update()
        {
            if (_isRebel)
            {
                TickAnimPresentation();
                return;
            }

            // PM-13: CombatDead / PermanentDeath mark → PlayDie once and stop acting.
            if (!IsCombatActive)
            {
                EnterCombatDeadPresentation();
                return;
            }

            TickCombat();
            TickAnimPresentation();
        }

        private void EnterCombatDeadPresentation()
        {
            if (_stoppedActing)
            {
                return;
            }

            if (_attackPhase == AttackPhase.Windup)
            {
                ClearWindup();
            }

            PlayDieOnce();
            StopActing();
        }

        private void PlayDieOnce()
        {
            if (_diePlayed || _anim == null)
            {
                return;
            }

            _diePlayed = true;
            HideFootCircle();
            _stuckHold.Reset();
            _anim.SetMoving(false);
            _anim.PlayDie();
        }

        /// <summary>Release slot / scheduler and freeze NavMeshAgent (Defend WarriorAgentView parity).</summary>
        private void StopActing()
        {
            if (_stoppedActing)
            {
                return;
            }

            _stoppedActing = true;
            _attackSlots?.Release(_attackerId);
            if (_scheduler != null && _moveId != 0)
            {
                _scheduler.Unregister(_moveId);
            }

            ClearPathingState();
            _attackPhase = AttackPhase.IdleOrMove;
            _windupTargetId = null;
        }

        /// <summary>
        /// PM-12 scheme D: engaged (AttackSlot claim on a living monster) + in AttackRange →
        /// melee windup / ranged projectile; settlement via session HitConfirm only.
        /// </summary>
        private void TickCombat()
        {
            if (_session == null)
            {
                return;
            }

            if (_attackPhase == AttackPhase.Windup)
            {
                TickWindup();
                return;
            }

            _attackStartCooldown = Mathf.Max(0f, _attackStartCooldown - Time.deltaTime);
            if (_attackStartCooldown > 0f)
            {
                return;
            }

            if (!TryResolveEngagedTarget(out var target))
            {
                return;
            }

            if (!_session.TryGetWarrior(_attackerId, out var state) || state == null)
            {
                return;
            }

            if (!CombatReach.IsInAttackRange(
                    Vector3.Distance(transform.position, target.transform.position),
                    state.AttackRange,
                    _bodyRadius,
                    target.BodyRadius))
            {
                return;
            }

            if (state.AttackMode == AttackMode.Melee)
            {
                BeginWindup(target, state);
            }
            else
            {
                FireProjectile(state, target);
            }
        }

        private void BeginWindup(PushMapMonsterAgentView target, DefendCombatWarriorState state)
        {
            _attackPhase = AttackPhase.Windup;
            _windupTargetId = target.RuntimeTargetId;
            _windupRemaining = Mathf.Max(0f, state.MeleeWindupSeconds);
            _attackStartCooldown = state.AttackSpeed > 0.01f ? 1f / state.AttackSpeed : 1f;
            _scheduler?.SetPaused(_moveId, true);
            ClearPathingState();

            if (_anim != null)
            {
                FaceTarget(target.transform.position);
                _anim.PlayAttack();
            }
        }

        private void TickWindup()
        {
            _windupRemaining -= Time.deltaTime;
            if (_windupRemaining > 0f)
            {
                return;
            }

            var target = FindMonsterByRuntimeId(_windupTargetId);
            var range = _session.TryGetWarrior(_attackerId, out var state) && state != null
                ? state.AttackRange
                : AttackRange;
            var inRange = target != null
                          && target.IsAlive
                          && CombatReach.IsInAttackRange(
                              Vector3.Distance(transform.position, target.transform.position),
                              range,
                              _bodyRadius,
                              target.BodyRadius,
                              CombatReach.HitConfirmSlack);

            _session.TryConfirmMeleeHit(_attackerId, _windupTargetId, inRange);
            ClearWindup();
        }

        private void ClearWindup()
        {
            _attackPhase = AttackPhase.IdleOrMove;
            _windupTargetId = null;
            _scheduler?.SetPaused(_moveId, false);
        }

        private void FireProjectile(DefendCombatWarriorState state, PushMapMonsterAgentView target)
        {
            _attackStartCooldown = state.AttackSpeed > 0.01f ? 1f / state.AttackSpeed : 1f;
            if (_projectilePrefab == null)
            {
                Debug.LogWarning($"[PushMapAdvance] {_attackerId} Ranged but Projectile Prefab missing.");
                return;
            }

            _scheduler?.SetPaused(_moveId, true);
            ClearPathingState();

            if (_anim != null)
            {
                FaceTarget(target.transform.position);
                _anim.PlayAttack();
            }

            var parent = _projectileParent != null ? _projectileParent : transform.parent;
            var go = Instantiate(_projectilePrefab, parent);
            go.name = $"Projectile_{_attackerId}";
            var spawnPos = transform.position + Vector3.up * 1.0f;
            go.transform.position = spawnPos;
            var to = target.transform.position - spawnPos;
            to.y = 0f;
            if (to.sqrMagnitude > 0.0001f)
            {
                go.transform.rotation = Quaternion.LookRotation(to.normalized, Vector3.up);
            }

            var view = go.GetComponent<ProjectileView>();
            if (view == null)
            {
                view = go.AddComponent<ProjectileView>();
            }

            view.Launch(
                _session,
                _attackerId,
                target.RuntimeTargetId,
                ResolveMonsterTransform,
                state.RangedProjectileSpeed,
                state.RangedTimeoutSeconds);

            _scheduler?.SetPaused(_moveId, false);
        }

        private void FaceTarget(Vector3 targetPos)
        {
            var toTarget = targetPos - transform.position;
            toTarget.y = 0f;
            if (toTarget.sqrMagnitude > 0.0001f)
            {
                _anim.SetFacing(toTarget);
            }
        }

        /// <summary>ProjectileView target resolver (PM-12 shared contract).</summary>
        private Transform ResolveMonsterTransform(string runtimeId)
        {
            var m = FindMonsterByRuntimeId(runtimeId);
            return m != null ? m.transform : null;
        }

        private PushMapMonsterAgentView FindMonsterByRuntimeId(string runtimeId)
        {
            if (string.IsNullOrEmpty(runtimeId))
            {
                return null;
            }

            var list = _monstersProvider != null ? _monstersProvider() : null;
            if (list == null)
            {
                return null;
            }

            for (var i = 0; i < list.Count; i++)
            {
                var m = list[i];
                if (m != null && string.Equals(m.RuntimeTargetId, runtimeId, StringComparison.Ordinal))
                {
                    return m;
                }
            }

            return null;
        }

        /// <summary>
        /// Engaged → face the claimed target; attack anim is driven by scheme D (windup /
        /// fire), movement anim by MassMove steer (same as Defend / monsters, SPEC_04 §15.5).
        /// </summary>
        private void TickAnimPresentation()
        {
            if (_anim == null)
            {
                return;
            }

            if (_isRebel)
            {
                _anim.SetMoving(false);
                return;
            }

            var inWindup = _attackPhase == AttackPhase.Windup;
            // MassMove uses Move()+ResetPath — velocity≈0; use steer like monsters (SPEC_04 §15.5).
            var wantsMove = !inWindup && _lastSteerDirXZ.sqrMagnitude > MoveAnimSpeedSqr;
            var moving = wantsMove && !_stuckHold.IsHolding;
            _anim.SetMoving(moving, ResolveMoveTargetDistanceXZ());

            if (TryResolveEngagedTarget(out var target))
            {
                FaceTarget(target.transform.position);
                return;
            }

            if (moving)
            {
                _anim.SetFacing(_lastSteerDirXZ);
            }
        }

        /// <summary>SPEC_04 §15.5: distance for attack→run interrupt gate (Objective / missing → +∞).</summary>
        private float ResolveMoveTargetDistanceXZ()
        {
            if (_scheduler == null || _moveId == 0)
            {
                return float.PositiveInfinity;
            }

            var p = transform.position;
            return _scheduler.GetAnimMoveTargetDistanceXZ(_moveId, new Vector2(p.x, p.z));
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
            if (_isRebel || _agent == null || _scheduler == null || !IsCombatActive)
            {
                _lastSteerDirXZ = Vector3.zero;
                _stuckHold.Tick(false, transform.position, Time.deltaTime);
                return;
            }

            if (_attackPhase == AttackPhase.Windup)
            {
                _lastSteerDirXZ = Vector3.zero;
                _stuckHold.Tick(false, transform.position, Time.deltaTime);
                return;
            }

            if (!_agent.isOnNavMesh)
            {
                TryWarpOntoNavMesh();
                if (!_agent.isOnNavMesh)
                {
                    _lastSteerDirXZ = Vector3.zero;
                    _stuckHold.Tick(false, transform.position, Time.deltaTime);
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
                _lastSteerDirXZ = Vector3.zero;
                _stuckHold.Tick(false, transform.position, Time.deltaTime);
                ClearPathingState();
                return;
            }

            // No SetDestination — follow scheduler steer (Objective field or AttackSlot).
            if (_agent.hasPath)
            {
                _agent.ResetPath();
            }

            _agent.isStopped = false;
            ApplyEffectiveMoveSpeed();
            var speed = ResolveEffectiveMoveSpeed();
            var delta = hasSteer
                ? new Vector3(steer.x, 0f, steer.y) * (speed * Time.deltaTime)
                : Vector3.zero;
            if (hasCorrection)
            {
                delta.x += correction.x;
                delta.z += correction.y;
            }

            _lastSteerDirXZ = hasSteer
                ? new Vector3(steer.x, 0f, steer.y)
                : Vector3.zero;
            _agent.Move(delta);

            var wantsMove = _lastSteerDirXZ.sqrMagnitude > MoveAnimSpeedSqr;
            _stuckHold.Tick(wantsMove, transform.position, Time.deltaTime);
            if (_stuckHold.IsHolding && _anim != null)
            {
                _anim.SetMoving(false);
            }
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
