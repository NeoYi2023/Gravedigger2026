using System;
using System.Collections.Generic;
using Gravedigger2026.Core.Config;
using Gravedigger2026.Core.Pathing;
using Gravedigger2026.Gameplay.Combat;
using Gravedigger2026.Gameplay.Defend;
using UnityEngine;
using UnityEngine.AI;

namespace Gravedigger2026.Gameplay.PushMap
{
    /// <summary>
    /// PushMap monster View (SPEC_03 §3.14 / SPEC_04 §9.19 + §9.7 MP-05).
    /// AggroMode four-state preserved. Chase destination = AttackSlot (not target center);
    /// movement via MassMoveScheduler + LocalDetour — no per-frame CalculatePath / SetDestination.
    /// Hits keep AttackMode scheme D; protagonist → onHitProtagonist (ApplyShieldHit);
    /// loyal soldier → onHitWarrior → Session.TryApplyMonsterDamageToWarrior (PM-13).
    /// Presentation: WarriorAnimView DirIndex / IsRun / Attack1 (SPEC_04 §15.5 Approach A).
    /// Facing hysteresis+dwell lives in WarriorAnimView.SetFacing (v0.75.21); StuckHoldTracker
    /// (v0.75.30) forces Idle for 1s when blocked — presentation only.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PushMapMonsterAgentView : MonoBehaviour
    {
        private const float MoveAnimSpeedSqr = 0.01f;

        private MonsterConfigRow _config;
        private Transform _protagonist;
        private Func<IReadOnlyList<PushMapAdvanceView>> _warriorsProvider;
        private Action<string> _onHitProtagonist;
        private Func<string, string, float, bool> _onHitWarrior;
        private Func<bool> _isStunned;
        private Func<float> _getSlowMoveMul;
        private Func<float> _getSlowAttackMul;
        private AttackSlotService _attackSlots;
        private MassMoveScheduler _scheduler;
        private float _retargetInterval = 1f;
        private float _retargetTimer;
        private float _attackCooldown;
        private NavMeshAgent _agent;
        private WarriorAnimView _anim;
        private Vector3 _lastSteerDirXZ;
        /// <summary>Last MassMove pre-detour desired; drives DirIndex while moving (SPEC_04 §15.5 v0.83.31).</summary>
        private Vector3 _lastDesiredDirXZ;
        private readonly StuckHoldTracker _stuckHold = new StuckHoldTracker();
        private readonly MonsterMoveGait _gait = new MonsterMoveGait();
        private bool _alive = true;
        private bool _provoked;
        private bool _isBoss;
        private int _moveId;
        private string _attackerId;
        private bool _deathKnockActive;
        private Vector3 _deathKnockOrigin;
        private Vector3 _deathKnockTarget;
        private float _deathKnockStartedAt;
        private bool _combatGameplayEnabled = true;
        private Action _onDeathPresentationComplete;
        private Action _onReviveAnimComplete;
        private bool _deathPresentationCompleteSent;
        private bool _reviveAnimPendingComplete;
        private float _reviveFacingResyncUntil;
        private float _alertRadius;
        private bool _postReviveAlertApplied;

        public string MonsterId => _config != null ? _config.MonsterId : string.Empty;
        public string RuntimeTargetId => _attackerId;
        public bool IsAlive => _alive;
        public bool IsBoss => _isBoss;
        public int MoveId => _moveId;
        public float AttackRange => _config != null ? _config.AttackRange : 0f;
        /// <summary>Active detect radius; empty table cell defaults to AttackRange at load.</summary>
        public float AlertRadius => Mathf.Max(0f, _alertRadius);
        public float BodyRadius => _config != null ? Mathf.Max(0.05f, _config.BodyRadius) : 0.35f;
        public Vector2 FacingXZ
        {
            get
            {
                if (_anim != null &&
                    _anim.TryGetFacingUnitXZ(out var unit) &&
                    unit.x * unit.x + unit.z * unit.z > 1e-8f)
                {
                    return new Vector2(unit.x, unit.z).normalized;
                }

                if (_lastSteerDirXZ.sqrMagnitude > 1e-8f)
                {
                    return new Vector2(_lastSteerDirXZ.x, _lastSteerDirXZ.z).normalized;
                }

                return new Vector2(0f, -1f);
            }
        }
        public AttackMode AttackMode =>
            _config != null && _config.AttackMode == AttackMode.Ranged
                ? AttackMode.Ranged
                : AttackMode.Melee;

        /// <summary>Stationary stances never move (SPEC_03 §3.14).</summary>
        public bool IsStationary => _config != null &&
            (_config.AggroMode == AggroMode.StationaryActive || _config.AggroMode == AggroMode.StationaryPassive);

        /// <summary>Passive stances stay idle until provoked (SPEC_03 §3.14).</summary>
        public bool IsPassive => _config != null &&
            (_config.AggroMode == AggroMode.PassiveChase || _config.AggroMode == AggroMode.StationaryPassive);

        public void SetCombatGameplayEnabled(bool enabled)
        {
            _combatGameplayEnabled = enabled;
        }

        public void SetReviveCallbacks(
            Action onDeathPresentationComplete,
            Action onReviveAnimComplete)
        {
            _onDeathPresentationComplete = onDeathPresentationComplete;
            _onReviveAnimComplete = onReviveAnimComplete;
        }

        public void Bind(
            MonsterConfigRow config,
            Transform protagonist,
            Func<IReadOnlyList<PushMapAdvanceView>> warriorsProvider,
            Action<string> onHitProtagonist,
            float retargetIntervalSeconds = 1f,
            AttackSlotService attackSlots = null,
            MassMoveScheduler scheduler = null,
            int moveId = 0,
            Func<string, string, float, bool> onHitWarrior = null,
            Func<bool> isStunned = null,
            Func<float> getSlowMoveMul = null,
            Func<float> getSlowAttackMul = null)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _protagonist = protagonist;
            _warriorsProvider = warriorsProvider;
            _onHitProtagonist = onHitProtagonist;
            _onHitWarrior = onHitWarrior;
            _isStunned = isStunned;
            _getSlowMoveMul = getSlowMoveMul;
            _getSlowAttackMul = getSlowAttackMul;
            _attackSlots = attackSlots;
            _scheduler = scheduler;
            _moveId = moveId;
            _retargetInterval = Mathf.Max(0.1f, retargetIntervalSeconds);
            _retargetTimer = 0f;
            _attackCooldown = 0f;
            _alive = true;
            _provoked = false;
            _isBoss = false;
            _deathKnockActive = false;
            _deathPresentationCompleteSent = false;
            _reviveAnimPendingComplete = false;
            _reviveFacingResyncUntil = 0f;
            _alertRadius = config != null ? Mathf.Max(0f, config.AlertRadius) : 0f;
            _postReviveAlertApplied = false;
            _attackerId = gameObject.name;
            _combatGameplayEnabled = true;

            _agent = GetComponent<NavMeshAgent>();
            if (_agent == null)
            {
                _agent = gameObject.AddComponent<NavMeshAgent>();
            }

            _agent.enabled = true;

            _agent.speed = ResolveEffectiveMoveSpeed(false);
            _agent.stoppingDistance = 0f;
            _agent.angularSpeed = 720f;
            _agent.acceleration = 24f;
            // Edge-gap AttackRange (v0.75.24): soft-collision contact is already in reach.
            _agent.radius = BodyRadius;
            _agent.height = 0.1f;
            _agent.autoBraking = false;
            _agent.updateRotation = false;
            _agent.obstacleAvoidanceType = ObstacleAvoidanceType.NoObstacleAvoidance;

            // v0.73.9: local Warp only — do not SamplePosition(12) across AirWalls onto outer diamond.
            var warpSample = Mathf.Max(1f, BodyRadius * 3f);
            if (!_agent.isOnNavMesh && NavMesh.SamplePosition(transform.position, out var hit, warpSample, NavMesh.AllAreas))
            {
                _agent.Warp(hit.position);
            }

            RegisterWithScheduler(startPaused: true);

            if (IsStationary || (IsPassive && !_provoked))
            {
                StopMovement();
            }

            EnsureAnim();
            _lastSteerDirXZ = Vector3.zero;
            _lastDesiredDirXZ = Vector3.zero;
            _stuckHold.Reset();
            _gait.Reset();
            _anim.SetFacingYawFlip(_config != null && _config.FacingYawFlip == 1);
            if (_config != null)
            {
                _anim.ConfigureMonsterAnimPools(
                    _config.NormalAttackAnims,
                    _config.WalkAnims,
                    _config.RunAnims);
            }

            _anim.ResetToIdle();
        }

        private void EnsureAnim()
        {
            _anim = GetComponent<WarriorAnimView>();
            if (_anim == null)
            {
                _anim = gameObject.AddComponent<WarriorAnimView>();
            }
        }

        /// <summary>Marks this instance as a Boss clear target (PM-07 / IsBoss spawn row).</summary>
        public void MarkAsBoss(bool isBoss)
        {
            _isBoss = isBoss;
        }

        /// <summary>Demo "soldier attacks first" contract (PM-06): wakes a passive monster into its attack state.</summary>
        public void NotifyProvoked()
        {
            if (!_alive || _provoked)
            {
                return;
            }

            _provoked = true;
            if (_agent != null && !IsStationary)
            {
                _agent.speed = ResolveEffectiveMoveSpeed(_gait.IsRun);
            }
        }

        /// <summary>
        /// ActiveChase/PassiveChase → gait speed × mult × slow; Stationary* unused (no move).
        /// SPEC_03 §3.14 / SPEC_04 §9.19.
        /// </summary>
        private float ResolveEffectiveMoveSpeed(bool isRun)
        {
            if (_config == null)
            {
                return 3f;
            }

            var mult = 1f;
            switch (_config.AggroMode)
            {
                case AggroMode.ActiveChase:
                    mult = _config.ActiveMoveMult;
                    break;
                case AggroMode.PassiveChase:
                    mult = _config.PassiveMoveMult;
                    break;
            }

            return Mathf.Max(0.1f, _config.ResolveGaitSpeed(isRun) * mult * ResolveSlowMoveMul());
        }

        private float ResolveSlowMoveMul()
        {
            return _getSlowMoveMul == null ? 1f : Mathf.Max(0f, _getSlowMoveMul());
        }

        private float ResolveSlowAttackMul()
        {
            return _getSlowAttackMul == null ? 1f : Mathf.Max(0f, _getSlowAttackMul());
        }

        /// <summary>
        /// Combat death presentation (SPEC_04 §15.5): PlayDie/Die2 by knockback + corpse latch;
        /// optional directional knockback.
        /// </summary>
        public void NotifyKilled(
            Vector3? killerWorldPos = null,
            float knockbackDistance = 0f)
        {
            if (!_alive)
            {
                return;
            }

            _alive = false;
            _stuckHold.Reset();
            _gait.Reset();
            ReleaseSlotClaim();
            // Soldiers claiming this monster as target — Stage also ReleaseAllForTarget.
            _attackSlots?.ReleaseAllForTarget(_attackerId);
            if (_scheduler != null && _moveId != 0)
            {
                _scheduler.Unregister(_moveId);
            }

            StopMovement();
            if (_agent != null)
            {
                _agent.enabled = false;
            }

            EnsureAnim();
            var preferDie2 = MonsterDeathPresentation.ShouldPreferDie2(knockbackDistance);
            _anim.PlayDie(preferDie2);

            _deathKnockActive = false;
            if (killerWorldPos.HasValue &&
                MonsterDeathPresentation.TryDirectionalKnockbackTarget(
                    transform.position,
                    killerWorldPos.Value,
                    knockbackDistance,
                    out var target))
            {
                _deathKnockOrigin = transform.position;
                _deathKnockTarget = target;
                _deathKnockStartedAt = Time.time;
                _deathKnockActive = true;
            }
        }

        /// <summary>Rules: revive delay elapsed — play reverse death anim.</summary>
        public void NotifyReviveStarted(float reviveAnimSeconds)
        {
            EnsureAnim();
            _reviveAnimPendingComplete = true;
            ApplyReviveInitialFacing();
            _anim.PlayReviveFromDeath(reviveAnimSeconds);
        }

        /// <summary>Rules: HP restored — re-enable combat locomotion (darken cleared when invincible ends).</summary>
        public void NotifyRevived(float? postReviveAlertRadius = null)
        {
            _alive = true;
            _deathPresentationCompleteSent = false;
            _reviveAnimPendingComplete = false;
            _deathKnockActive = false;
            _stuckHold.Reset();
            _gait.Reset();

            if (postReviveAlertRadius.HasValue && !_postReviveAlertApplied)
            {
                _alertRadius = Mathf.Max(0f, postReviveAlertRadius.Value);
                _postReviveAlertApplied = true;
            }

            if (_agent != null)
            {
                _agent.enabled = true;
                _agent.speed = ResolveEffectiveMoveSpeed(_gait.IsRun);
                var warpSample = Mathf.Max(1f, BodyRadius * 3f);
                if (!_agent.isOnNavMesh &&
                    NavMesh.SamplePosition(transform.position, out var hit, warpSample, NavMesh.AllAreas))
                {
                    _agent.Warp(hit.position);
                }
            }

            // NotifyKilled Unregister'd us — must re-register before chase/attack can resume.
            RegisterWithScheduler(startPaused: false);

            EnsureAnim();
            _anim.SetFacingYawFlip(_config != null && _config.FacingYawFlip == 1);
            _anim.ResampleLocomotionAnims();
            _anim.EnsureLocomotionReady();
            _reviveFacingResyncUntil = Time.time + 0.5f;
            RefreshFacingAfterRevive();
        }

        private bool IsReviveFacingResyncActive => Time.time < _reviveFacingResyncUntil;

        private void ApplyAnimFacing(Vector3 worldDirXZ)
        {
            if (_anim == null || worldDirXZ.sqrMagnitude < 1e-8f)
            {
                return;
            }

            if (IsReviveFacingResyncActive)
            {
                _anim.ForceSetFacing(worldDirXZ);
            }
            else
            {
                _anim.SetFacing(worldDirXZ);
            }
        }

        /// <summary>
        /// D-074: before Die2/Die reverse-play, face toward TargetSelect target (8-dir for invincible idle).
        /// </summary>
        private void ApplyReviveInitialFacing()
        {
            if (_anim == null)
            {
                return;
            }

            if (TryGetCombatTargetPosition(out var targetPos, out _))
            {
                var to = targetPos - transform.position;
                to.y = 0f;
                if (to.sqrMagnitude > 1e-8f)
                {
                    _anim.ForceSetFacing(to);
                    return;
                }
            }

            if (_lastDesiredDirXZ.sqrMagnitude > 1e-8f)
            {
                _anim.ForceSetFacing(_lastDesiredDirXZ);
            }
            else if (_lastSteerDirXZ.sqrMagnitude > 1e-8f)
            {
                _anim.ForceSetFacing(_lastSteerDirXZ);
            }
        }

        private void RefreshFacingAfterRevive()
        {
            ApplyReviveInitialFacing();
        }

        /// <summary>D-074: post-revive invincible ended — restore normal sprite colors.</summary>
        public void NotifyPostReviveInvincibleEnded()
        {
            EnsureAnim();
            _anim?.ClearCorpseDarken();
        }

        private void TickDeathKnockback()
        {
            if (!_deathKnockActive)
            {
                return;
            }

            var animating = MonsterDeathPresentation.TrySampleKnockback(
                _deathKnockOrigin,
                _deathKnockTarget,
                _deathKnockStartedAt,
                MonsterDeathPresentation.DeathKnockbackSeconds,
                Time.time,
                out var pos);
            transform.position = pos;
            if (!animating)
            {
                _deathKnockActive = false;
            }
        }

        private void TryNotifyDeathPresentationComplete()
        {
            if (_alive || _deathPresentationCompleteSent || _onDeathPresentationComplete == null)
            {
                return;
            }

            if (_deathKnockActive)
            {
                return;
            }

            EnsureAnim();
            if (_anim == null || !_anim.IsDieLatched)
            {
                return;
            }

            _deathPresentationCompleteSent = true;
            _onDeathPresentationComplete.Invoke();
        }

        private void TryNotifyReviveAnimComplete()
        {
            if (!_reviveAnimPendingComplete || _onReviveAnimComplete == null)
            {
                return;
            }

            EnsureAnim();
            if (_anim != null && _anim.IsReviveAnimating)
            {
                return;
            }

            _reviveAnimPendingComplete = false;
            _onReviveAnimComplete.Invoke();
        }

        private bool IsStunnedNow()
        {
            return _isStunned != null && _isStunned();
        }

        /// <summary>XZ sample for MassMoveScheduler (inactive when dead/stationary/idle-passive/stunned).</summary>
        public MassMoveSample BuildSample()
        {
            var pos = transform.position;
            var active = _alive && !IsStationary && isActiveAndEnabled &&
                         (!IsPassive || _provoked) &&
                         !IsStunnedNow();
            return new MassMoveSample(
                _moveId,
                new Vector2(pos.x, pos.z),
                _agent != null ? _agent.radius : BodyRadius,
                active);
        }

        /// <summary>
        /// Budgeted chase goal refresh (Stage ≤50/frame). Returns true if chasing with a resolved target.
        /// </summary>
        public bool TryRefreshChaseGoal(AttackSlotService slots, MassMoveScheduler scheduler)
        {
            if (!_alive || IsStationary || _config == null || scheduler == null || _moveId == 0)
            {
                return false;
            }

            if (IsStunnedNow())
            {
                ReleaseSlotClaim(slots);
                StopMovement();
                scheduler.SetPaused(_moveId, true);
                return false;
            }

            if (IsPassive && !_provoked)
            {
                ReleaseSlotClaim(slots);
                scheduler.SetPaused(_moveId, true);
                return false;
            }

            if (ResolveTarget(out var warriorView, out var protagonistTf) == TargetKind.None)
            {
                ReleaseSlotClaim(slots);
                scheduler.SetPaused(_moveId, true);
                return false;
            }

            var targetTf = warriorView != null ? warriorView.transform : protagonistTf;
            if (targetTf == null)
            {
                ReleaseSlotClaim(slots);
                scheduler.SetPaused(_moveId, true);
                return false;
            }

            var targetId = warriorView != null
                ? warriorView.AttackerId
                : "Protagonist";
            var targetBody = warriorView != null
                ? warriorView.AgentRadius
                : AttackSlotService.DefaultTargetBodyRadius;
            var dist = Vector3.Distance(transform.position, targetTf.position);

            // In AttackRange (edge-gap): hold and attack (no chase steer).
            if (CombatReach.IsInAttackRange(dist, _config.AttackRange, BodyRadius, targetBody))
            {
                ReleaseSlotClaim(slots);
                scheduler.SetPaused(_moveId, true);
                StopMovement();
                return true;
            }

            // SC-03: melee chase → Surround gap claim (B+); ranged → Chase (full ring).
            if (slots == null ||
                !slots.TryClaim(
                    _attackerId,
                    targetId,
                    _config.AttackRange,
                    targetTf.position,
                    out var slotPos,
                    AttackMode,
                    transform.position,
                    targetBody,
                    BodyRadius,
                    CombatMoveModePolicy.SurroundFor(GoalKind.AttackSlot, AttackMode)))
            {
                // No free walkable slot: still seek a ring-ish offset (not raw center stack).
                var ring = AttackSlotService.ComputeRingRadius(
                    _config.AttackRange,
                    BodyRadius,
                    targetBody);
                var away = transform.position - targetTf.position;
                away.y = 0f;
                if (away.sqrMagnitude < 1e-6f)
                {
                    away = Vector3.forward;
                }

                slotPos = targetTf.position + away.normalized * ring;
            }

            scheduler.SetPaused(_moveId, false);
            scheduler.SetGoal(
                _moveId,
                GoalKind.AttackSlot,
                new Vector2(slotPos.x, slotPos.z));
            return true;
        }

        private void Update()
        {
            TickDeathKnockback();
            TryNotifyDeathPresentationComplete();
            TryNotifyReviveAnimComplete();

            if (!_alive || _config == null || !_combatGameplayEnabled)
            {
                return;
            }

            if (IsStunnedNow())
            {
                StopMovement();
                _scheduler?.SetPaused(_moveId, true);
                return;
            }

            // Legacy retarget timer kept for TargetSelect cadence; slot refresh is Stage-budgeted.
            _retargetTimer += Time.deltaTime;
            if (_retargetTimer >= _retargetInterval)
            {
                _retargetTimer = 0f;
            }

            var targetKind = ResolveTarget(out var warriorView, out var protagonistTf);
            if (targetKind == TargetKind.None)
            {
                return;
            }

            var targetTf = targetKind == TargetKind.Warrior && warriorView != null
                ? warriorView.transform
                : protagonistTf;
            if (targetTf == null)
            {
                return;
            }

            var dist = Vector3.Distance(transform.position, targetTf.position);
            var targetBody = warriorView != null
                ? warriorView.AgentRadius
                : AttackSlotService.DefaultTargetBodyRadius;
            if (!CombatReach.IsInAttackRange(dist, _config.AttackRange, BodyRadius, targetBody))
            {
                return;
            }

            StopMovement();
            _scheduler?.SetPaused(_moveId, true);

            _attackCooldown -= Time.deltaTime;
            if (_attackCooldown > 0f)
            {
                return;
            }

            FaceTowardForAttack(targetTf.position);

            if (targetKind == TargetKind.Protagonist)
            {
                _onHitProtagonist?.Invoke($"Monster:{_config.MonsterId}");
            }
            else if (warriorView != null)
            {
                var applied = _onHitWarrior != null &&
                              _onHitWarrior(_attackerId, warriorView.AttackerId, _config.AttackPower);
                if (!applied)
                {
                    Debug.LogWarning(
                        $"[PushMapMonster] {_config.MonsterId} hit warrior {warriorView.AttackerId} " +
                        "but Session did not settle (inactive / already dead).");
                }
            }

            _anim?.PlayAttack();

            var aspd = _config.AttackSpeed * ResolveSlowAttackMul();
            var interval = aspd > 0.01f ? 1f / aspd : 1f;
            _attackCooldown = Mathf.Max(0.2f, interval);
        }

        private void LateUpdate()
        {
            _lastSteerDirXZ = Vector3.zero;
            CacheDesiredDirFromScheduler();

            if (!_combatGameplayEnabled)
            {
                return;
            }

            var inAttackRange = false;
            var isLocomoting = false;
            if (_alive && _config != null)
            {
                EvaluateLocomotion(out inAttackRange, out isLocomoting);
                _gait.Tick(isLocomoting, _config.WalkToRunSeconds, Time.deltaTime);
            }
            else
            {
                _gait.Reset();
            }

            if (_alive && !IsStationary && _agent != null && _scheduler != null && _moveId != 0)
            {
                if (IsStunnedNow())
                {
                    StopMovement();
                    _scheduler.SetPaused(_moveId, true);
                }
                else
                {
                    if (!_agent.isOnNavMesh)
                    {
                        var warpSample = Mathf.Max(1f, BodyRadius * 3f);
                        if (NavMesh.SamplePosition(transform.position, out var hit, warpSample, NavMesh.AllAreas))
                        {
                            _agent.Warp(hit.position);
                        }
                    }

                    if (_agent.isOnNavMesh)
                    {
                        // SC-03: soft-collision impulse applies even on zero-steer frames (attack hold).
                        var hasSteer =
                            _scheduler.TryGetSteer(_moveId, out var steer) && steer.sqrMagnitude > 1e-8f;
                        var hasCorrection =
                            _scheduler.TryGetCorrection(_moveId, out var correction) &&
                            correction.sqrMagnitude > 1e-8f;
                        if (hasSteer || hasCorrection)
                        {
                            if (_agent.hasPath)
                            {
                                _agent.ResetPath();
                            }

                            _agent.isStopped = false;
                            var speed = ResolveEffectiveMoveSpeed(_gait.IsRun);
                            _agent.speed = speed;
                            var delta = hasSteer
                                ? new Vector3(steer.x, 0f, steer.y) * (speed * Time.deltaTime)
                                : Vector3.zero;
                            if (hasCorrection)
                            {
                                delta.x += correction.x;
                                delta.z += correction.y;
                            }

                            if (hasSteer)
                            {
                                _lastSteerDirXZ = new Vector3(steer.x, 0f, steer.y);
                            }

                            _agent.Move(delta);
                        }
                    }
                }
            }

            TickAnimPresentation(inAttackRange, isLocomoting);
        }

        private void EvaluateLocomotion(out bool inAttackRange, out bool isLocomoting)
        {
            inAttackRange = false;
            isLocomoting = false;
            if (TryGetCombatTargetPosition(out var attackTargetPos, out var body) && _config != null)
            {
                inAttackRange = CombatReach.IsInAttackRange(
                    Vector3.Distance(transform.position, attackTargetPos),
                    _config.AttackRange,
                    BodyRadius,
                    body);
            }

            var hasSteerIntent = !IsStationary &&
                                 _scheduler != null &&
                                 _moveId != 0 &&
                                 _scheduler.TryGetSteer(_moveId, out var steerIntent) &&
                                 steerIntent.sqrMagnitude > MoveAnimSpeedSqr;
            var wantsMove = _alive &&
                            !IsStunnedNow() &&
                            !IsStationary &&
                            !inAttackRange &&
                            hasSteerIntent;
            _stuckHold.Tick(wantsMove, transform.position, Time.deltaTime);
            isLocomoting = wantsMove && !_stuckHold.IsHolding;
        }

        /// <summary>
        /// In AttackRange → idle (keep facing); stuck hold → idle keep facing;
        /// else chase → walk/run gait + DirIndex from LastDesired (SPEC_04 §15.5).
        /// </summary>
        private void TickAnimPresentation(bool inAttackRange, bool isLocomoting)
        {
            if (_anim == null || !_alive)
            {
                return;
            }

            if (IsStunnedNow())
            {
                _anim.SetMoving(false, 0f);
                return;
            }

            if (inAttackRange || _stuckHold.IsHolding)
            {
                _anim.SetMoving(false);
                return;
            }

            _anim.SetMoving(isLocomoting, ResolveMoveTargetDistanceXZ(), _gait.IsRun);
            if (isLocomoting)
            {
                ApplyMoveFacing();
            }
        }

        private void CacheDesiredDirFromScheduler()
        {
            _lastDesiredDirXZ = Vector3.zero;
            if (_scheduler == null || _moveId == 0)
            {
                return;
            }

            if (_scheduler.TryGetDesiredDir(_moveId, out var desired))
            {
                _lastDesiredDirXZ = new Vector3(desired.x, 0f, desired.y);
            }
        }

        private void ApplyMoveFacing()
        {
            if (_anim == null || _lastDesiredDirXZ.sqrMagnitude < 0.0001f)
            {
                return;
            }

            ApplyAnimFacing(_lastDesiredDirXZ);
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

        private bool TryGetCombatTargetPosition(out Vector3 targetPos, out float targetBodyRadius)
        {
            targetPos = default;
            targetBodyRadius = AttackSlotService.DefaultTargetBodyRadius;
            if (_config == null)
            {
                return false;
            }

            var kind = ResolveTarget(out var warriorView, out var protagonistTf);
            if (kind == TargetKind.None)
            {
                return false;
            }

            var targetTf = kind == TargetKind.Warrior && warriorView != null
                ? warriorView.transform
                : protagonistTf;
            if (targetTf == null)
            {
                return false;
            }

            targetPos = targetTf.position;
            if (warriorView != null)
            {
                targetBodyRadius = warriorView.AgentRadius;
            }

            return true;
        }

        /// <summary>Attack-range aim: write DirIndex immediately so Attack1_* picks the correct clip.</summary>
        private void FaceTowardForAttack(Vector3 worldPos)
        {
            if (_anim == null)
            {
                return;
            }

            var to = worldPos - transform.position;
            to.y = 0f;
            if (to.sqrMagnitude > 0.0001f)
            {
                _anim.ForceSetFacing(to);
            }
        }

        private void OnDisable()
        {
            ReleaseSlotClaim();
            if (_scheduler != null && _moveId != 0)
            {
                _scheduler.Unregister(_moveId);
            }
        }

        private void RegisterWithScheduler(bool startPaused)
        {
            if (IsStationary || _scheduler == null || _moveId == 0 || _config == null)
            {
                return;
            }

            _scheduler.Register(
                _moveId,
                _agent != null ? _agent.radius : BodyRadius,
                MassMoveScheduler.DetourGroupMonster,
                Mathf.Max(0f, _config.PushCoefficient),
                Mathf.Max(0f, _config.RepulsionScale));
            _scheduler.SetGoal(_moveId, GoalKind.AttackSlot);
            _scheduler.SetPaused(_moveId, startPaused);
        }

        private void ReleaseSlotClaim(AttackSlotService slots = null)
        {
            var svc = slots ?? _attackSlots;
            if (svc == null || string.IsNullOrEmpty(_attackerId))
            {
                return;
            }

            svc.Release(_attackerId);
        }

        private void StopMovement()
        {
            _gait.Reset();
            if (_agent != null && _agent.isOnNavMesh)
            {
                if (_agent.hasPath)
                {
                    _agent.ResetPath();
                }

                _agent.isStopped = true;
                _agent.velocity = Vector3.zero;
            }
        }

        private enum TargetKind
        {
            None = 0,
            Protagonist = 1,
            Warrior = 2
        }

        private bool IsAggroActive => !IsPassive || _provoked;

        private TargetKind ResolveTarget(out PushMapAdvanceView warrior, out Transform protagonist)
        {
            warrior = null;
            protagonist = _protagonist;

            if (_config == null || !IsAggroActive)
            {
                return TargetKind.None;
            }

            var alertRadius = _alertRadius;
            switch (_config.TargetSelect)
            {
                case TargetSelect.PreferProtagonist:
                    if (protagonist != null &&
                        WithinDetect(
                            protagonist.position,
                            alertRadius,
                            AttackSlotService.DefaultTargetBodyRadius))
                    {
                        return TargetKind.Protagonist;
                    }

                    warrior = NearestLoyalWarriorWithin(alertRadius);
                    return warrior != null ? TargetKind.Warrior : TargetKind.None;

                case TargetSelect.PreferWarrior:
                    warrior = NearestLoyalWarriorWithin(alertRadius);
                    if (warrior != null)
                    {
                        return TargetKind.Warrior;
                    }

                    return protagonist != null &&
                           WithinDetect(
                               protagonist.position,
                               alertRadius,
                               AttackSlotService.DefaultTargetBodyRadius)
                        ? TargetKind.Protagonist
                        : TargetKind.None;

                default:
                    return NearestAny(alertRadius, out warrior, out protagonist);
            }
        }

        private bool WithinDetect(Vector3 targetPos, float alertRadius, float targetBodyRadius)
        {
            var detect = IsStationary
                ? CombatReach.MaxCenterDistance(_config.AttackRange, BodyRadius, targetBodyRadius)
                : alertRadius;
            if (IsPassive && _provoked && !IsStationary)
            {
                // Provoked chase persists until death (SPEC_03 §3.14).
                return true;
            }

            return Vector3.Distance(transform.position, targetPos) <= Mathf.Max(0.01f, detect);
        }

        private TargetKind NearestAny(float alertRadius, out PushMapAdvanceView warrior, out Transform protagonist)
        {
            warrior = null;
            protagonist = _protagonist;
            var bestDist = float.MaxValue;
            var kind = TargetKind.None;

            if (protagonist != null &&
                WithinDetect(protagonist.position, alertRadius, AttackSlotService.DefaultTargetBodyRadius))
            {
                bestDist = Vector3.Distance(transform.position, protagonist.position);
                kind = TargetKind.Protagonist;
            }

            var nearestWarrior = NearestLoyalWarriorWithin(alertRadius);
            if (nearestWarrior != null)
            {
                var d = Vector3.Distance(transform.position, nearestWarrior.transform.position);
                if (d < bestDist)
                {
                    warrior = nearestWarrior;
                    kind = TargetKind.Warrior;
                }
            }

            return kind;
        }

        private PushMapAdvanceView NearestLoyalWarriorWithin(float alertRadius)
        {
            var list = _warriorsProvider != null ? _warriorsProvider() : null;
            if (list == null || list.Count == 0)
            {
                return null;
            }

            var chasePersistent = IsPassive && _provoked && !IsStationary;

            PushMapAdvanceView best = null;
            var bestDist = float.MaxValue;
            for (var i = 0; i < list.Count; i++)
            {
                var w = list[i];
                if (w == null || w.IsRebel || !w.IsCombatActive)
                {
                    continue;
                }

                var d = Vector3.Distance(transform.position, w.transform.position);
                var detect = IsStationary
                    ? CombatReach.MaxCenterDistance(_config.AttackRange, BodyRadius, w.AgentRadius)
                    : alertRadius;
                if (!chasePersistent && d > Mathf.Max(0.01f, detect))
                {
                    continue;
                }

                if (d < bestDist)
                {
                    bestDist = d;
                    best = w;
                }
            }

            return best;
        }
    }
}
