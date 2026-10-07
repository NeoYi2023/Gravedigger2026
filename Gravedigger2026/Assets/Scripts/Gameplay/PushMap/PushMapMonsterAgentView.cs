using System;
using System.Collections.Generic;
using Gravedigger2026.Core.Combat;
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
        private AggroMode _runtimeAggroMode;
        private Transform _protagonist;
        private Func<IReadOnlyList<PushMapAdvanceView>> _warriorsProvider;
        private Action<string> _onHitProtagonist;
        private Func<string, string, float, bool> _onHitWarrior;
        private Func<bool> _isStunned;
        private Func<float> _getSlowMoveMul;
        private Func<float> _getSlowAttackMul;
        private AttackSlotService _attackSlots;
        private MassMoveScheduler _scheduler;
        private TargetFocusRegistry _targetFocus;
        private readonly List<MonsterNearestWarriorPicker.Candidate> _nearestCandidateScratch =
            new List<MonsterNearestWarriorPicker.Candidate>(32);
        private float _retargetInterval = 1f;
        private float _retargetTimer;
        private float _attackCooldown;
        private enum MonsterAttackPhase
        {
            Idle = 0,
            Windup = 1
        }

        private MonsterAttackPhase _attackPhase = MonsterAttackPhase.Idle;
        private float _windupRemaining;
        private TargetKind _windupTargetKind;
        private string _windupWarriorId;
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
        private bool _deathKnockWasAnimating;
        private bool _corpseSmashEnabled;
        private string _killerWarriorId = string.Empty;
        private float _killerOutgoingDamage;
        private readonly HashSet<string> _corpseSmashHit = new HashSet<string>(StringComparer.Ordinal);
        private Func<string, string, float, string, bool> _tryApplyCorpseSmash;
        private CorpseProjectileSmashSweep.LivingMonsterEnumerator _enumerateLivingMonsters;
        private DeathKnockbackGroundShadowView _knockbackShadow;
        private bool _combatGameplayEnabled = true;
        /// <summary>COC only. Ignore-obstacle monsters drive XZ straight and stop on carved walls.</summary>
        private bool _cocDriveStraight;
        /// <summary>
        /// COC only (SPEC_03 §3.21). Default off so PushMap / Defend / SearchExtract keep radius re-filter.
        /// When on, ActiveChase may latch past AlertRadius until this monster dies.
        /// </summary>
        private bool _cocActiveChaseLatchEnabled;
        /// <summary>COC ActiveChase: set by the first in-radius loyal soldier, a confirmed hit, or the current victim dying.</summary>
        private bool _cocAggroLatched;
        /// <summary>
        /// Alert-circle center on the ground (XZ). Rewritten from this monster every scan,
        /// including the scan right after the current soldier dies. Never a spawn point or a corpse.
        /// </summary>
        private Vector2 _alertCenterXZ;
        private bool _alertCenterReady;
        /// <summary>Last living warrior this monster chose. Used to notice a kill and rescan.</summary>
        private string _lastChaseWarriorId;
        /// <summary>COC ActiveChase has already chosen a soldier. Later scans ignore AlertRadius.</summary>
        private bool _cocEverEngaged;
        /// <summary>COC stage unlocked body-nearest chase after the first detect or hit.</summary>
        private bool _cocChaseUnlocked;
        /// <summary>COC stage writes the living soldier this monster must chase. Null uses table detect.</summary>
        private PushMapAdvanceView _cocForcedWarrior;
        /// <summary>Ground XZ at Bind, after spawn spread and the local nav warp.</summary>
        private Vector2 _spawnXZ;
        /// <summary>COC attack-stuck return. Ignores chase and damage until the spawn point.</summary>
        private bool _cocReturningHome;
        private float _cocAttackStuckTimer;
        private Vector3 _cocAttackStuckOrigin;
        private int _cocActionHash;
        private float _cocActionNormalized;
        private bool _cocAttackStuckWindowReady;
        private const float CocAttackStuckSeconds = 2f;
        private const float CocActionAdvanceEpsilon = 0.05f;
        /// <summary>COC only. Treat-as-target wall handed in by the overlay. Null on every other mode.</summary>
        private Transform _cocInterceptTarget;
        private string _cocInterceptTargetId;
        private float _cocInterceptBodyRadius;
        private Func<bool> _cocInterceptTryHit;
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
        public float TargetValue =>
            _config != null ? Mathf.Max(0f, _config.TargetValue) : MonsterConfigRow.DefaultTargetValue;
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

        /// <summary>Stationary stances never move (SPEC_03 §3.14 / D-102 runtime AggroMode).</summary>
        public bool IsStationary =>
            _runtimeAggroMode == AggroMode.StationaryActive
            || _runtimeAggroMode == AggroMode.StationaryPassive;

        public bool IsActiveChase => _runtimeAggroMode == AggroMode.ActiveChase;

        public bool CocChaseUnlocked => _cocChaseUnlocked;

        public bool IsCocReturningHome => _cocReturningHome;

        /// <summary>Passive stances stay idle until provoked (SPEC_03 §3.14 / D-102 runtime AggroMode).</summary>
        public bool IsPassive =>
            _runtimeAggroMode == AggroMode.PassiveChase
            || _runtimeAggroMode == AggroMode.StationaryPassive;

        public void SetCombatGameplayEnabled(bool enabled)
        {
            _combatGameplayEnabled = enabled;
        }

        /// <summary>
        /// COC overlay (D-100 Approach A). When true, chase pauses the scheduler and
        /// <see cref="NavMeshAgent.Move"/>s toward the current soldier so carved walls stop the body.
        /// PushMap / SearchExtract leave this unset.
        /// </summary>
        public void SetCocDriveStraight(bool enabled)
        {
            _cocDriveStraight = enabled;
        }

        /// <summary>
        /// COC overlay (SPEC_03 §3.21). When true, ActiveChase latches after the first loyal
        /// soldier inside AlertRadius (XZ) or after a confirmed hit, then retargets the nearest
        /// loyal soldier with no AlertRadius cap until this monster dies.
        /// PushMap / SearchExtract leave this unset.
        /// </summary>
        public void SetCocActiveChaseLatch(bool enabled)
        {
            _cocActiveChaseLatchEnabled = enabled;
            if (!enabled)
            {
                _cocAggroLatched = false;
            }
        }

        /// <summary>
        /// COC confirmed soldier hit. ActiveChase latches even if the shooter is outside AlertRadius.
        /// Passive stances stay on <see cref="NotifyProvoked"/> and are not latched here.
        /// </summary>
        public void NotifyCocSoldierHit()
        {
            if (!_alive || !_cocActiveChaseLatchEnabled || IsStationary)
            {
                return;
            }

            RefreshAlertCenter();
            if (_runtimeAggroMode == AggroMode.ActiveChase)
            {
                _cocAggroLatched = true;
            }
        }

        /// <summary>COC: after the first detect or hit, the stage supplies the chase soldier.</summary>
        public void UnlockCocChase()
        {
            _cocChaseUnlocked = true;
            _cocEverEngaged = true;
            _cocAggroLatched = true;
        }

        /// <summary>Nearest living soldier chosen by the COC stage. Null falls back to table detect.</summary>
        public void SetCocBodyTarget(PushMapAdvanceView warrior)
        {
            if (warrior != null && (!warrior.IsCombatActive || warrior.IsRebel))
            {
                warrior = null;
            }

            _cocForcedWarrior = warrior;
        }

        /// <summary>
        /// Walk back to the Bind spawn point. Arriving clears the chase latch and ends invincibility.
        /// </summary>
        public void DriveCocReturnHome(AttackSlotService slots, MassMoveScheduler scheduler)
        {
            if (!_cocReturningHome || !_alive || scheduler == null || _moveId == 0)
            {
                return;
            }

            if (IsAtSpawn())
            {
                CompleteCocReturnHome(slots, scheduler);
                return;
            }

            ReleaseSlotClaim(slots);
            ClearCocIntercept();
            SetCocBodyTarget(null);
            scheduler.SetPaused(_moveId, false);
            scheduler.SetGoal(_moveId, GoalKind.FormationHome, _spawnXZ);
        }

        /// <summary>Snap the alert circle onto this monster's current ground position.</summary>
        public void RefreshAlertCenter()
        {
            var pos = transform.position;
            _alertCenterXZ = new Vector2(pos.x, pos.z);
            _alertCenterReady = true;
        }

        public CocObstaclePathMode ObstaclePathMode =>
            _config != null ? _config.ObstaclePathMode : CocObstaclePathMode.TreatAsObstacle;

        public string CocInterceptTargetId => _cocInterceptTargetId;

        /// <summary>Current chase soldier or protagonist, ignoring a wall intercept.</summary>
        public bool TryGetCocChaseAim(out Vector3 worldPosition)
        {
            worldPosition = default;
            if (ResolveTarget(out var warrior, out var protagonist) == TargetKind.None)
            {
                return false;
            }

            var aim = warrior != null ? warrior.transform : protagonist;
            if (aim == null)
            {
                return false;
            }

            worldPosition = aim.position;
            return true;
        }

        /// <summary>
        /// COC overlay (D-100 slice 03b). While set, chase and attack this wall with the
        /// monster's own attack speed and anim. The hit callback is the −1 channel.
        /// </summary>
        public void SetCocIntercept(string targetId, Transform target, float bodyRadius, Func<bool> tryHit)
        {
            if (target == null || tryHit == null || string.IsNullOrEmpty(targetId))
            {
                ClearCocIntercept();
                return;
            }

            _cocInterceptTarget = target;
            _cocInterceptTargetId = targetId;
            _cocInterceptBodyRadius = Mathf.Max(0.05f, bodyRadius);
            _cocInterceptTryHit = tryHit;
        }

        public void ClearCocIntercept()
        {
            _cocInterceptTarget = null;
            _cocInterceptTargetId = null;
            _cocInterceptTryHit = null;
        }

        public void SetCorpseSmashBridge(
            Func<string, string, float, string, bool> tryApplyCorpseSmash,
            CorpseProjectileSmashSweep.LivingMonsterEnumerator enumerateLivingMonsters)
        {
            _tryApplyCorpseSmash = tryApplyCorpseSmash;
            _enumerateLivingMonsters = enumerateLivingMonsters;
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
            Func<float> getSlowAttackMul = null,
            TargetFocusRegistry targetFocus = null)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _runtimeAggroMode = config.AggroMode;
            _protagonist = protagonist;
            _warriorsProvider = warriorsProvider;
            _onHitProtagonist = onHitProtagonist;
            _onHitWarrior = onHitWarrior;
            _isStunned = isStunned;
            _getSlowMoveMul = getSlowMoveMul;
            _getSlowAttackMul = getSlowAttackMul;
            _attackSlots = attackSlots;
            _scheduler = scheduler;
            _targetFocus = targetFocus;
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
            _cocDriveStraight = false;
            _cocActiveChaseLatchEnabled = false;
            _cocAggroLatched = false;
            _alertCenterReady = false;
            _lastChaseWarriorId = null;
            _cocEverEngaged = false;
            _cocChaseUnlocked = false;
            _cocForcedWarrior = null;
            _cocReturningHome = false;
            ResetCocAttackStuck();
            ClearCocIntercept();

            _agent = GetComponent<NavMeshAgent>();
            if (_agent == null)
            {
                _agent = gameObject.AddComponent<NavMeshAgent>();
            }

            _agent.enabled = true;
            _agent.agentTypeID = SpecialMoveNavMesh.DefaultAgentTypeId;

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
            if (!_agent.isOnNavMesh &&
                SpecialMoveNavMesh.SampleWalkable(
                    _agent,
                    transform.position,
                    warpSample,
                    out var hit))
            {
                _agent.Warp(hit.position);
            }

            _spawnXZ = new Vector2(transform.position.x, transform.position.z);

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

        /// <summary>
        /// SPEC_04 §9.23/§15.5: after Bind→ResetToIdle, apply spawn InitialFacing DirIndex.
        /// </summary>
        public void ApplySpawnInitialFacing(int dirIndex)
        {
            EnsureAnim();
            if (_anim == null)
            {
                return;
            }

            var unit = WarriorAnimView.DirIndexToUnitXZ(dirIndex);
            _lastDesiredDirXZ = unit;
            _lastSteerDirXZ = unit;
            _anim.ForceSetFacing(unit);
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

        /// <summary>D-102: one-shot runtime AggroMode after berserk. Does not rewrite the table row.</summary>
        public void SetRuntimeAggroMode(AggroMode mode)
        {
            _runtimeAggroMode = mode;
            if (_agent != null && _alive && !IsStationary)
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
            switch (_runtimeAggroMode)
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
        /// optional directional knockback. Fake-death uses lighter corpse darken.
        /// </summary>
        public void NotifyKilled(
            Vector3? killerWorldPos = null,
            float knockbackDistance = 0f,
            string killerWarriorId = null,
            float killerOutgoingDamage = 0f,
            bool fakeDeathCorpse = false)
        {
            if (!_alive)
            {
                return;
            }

            _alive = false;
            _cocReturningHome = false;
            ResetCocAttackStuck();
            ClearRangedWindup();
            _stuckHold.Reset();
            _gait.Reset();
            ClearTargetFocus();
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
            var corpseDarkenMul = fakeDeathCorpse
                ? WarriorAnimView.FakeDeathCorpseDarkenMul
                : WarriorAnimView.CorpseDarkenMul;
            _anim.PlayDie(preferDie2, corpseDarkenMul);

            _killerWarriorId = killerWarriorId ?? string.Empty;
            _killerOutgoingDamage = Mathf.Max(0f, killerOutgoingDamage);
            _corpseSmashEnabled = MonsterDeathPresentation.ShouldEnableCorpseSmash(knockbackDistance);
            _corpseSmashHit.Clear();
            _deathKnockActive = false;
            _deathKnockWasAnimating = false;
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
                _deathKnockWasAnimating = true;
                EnsureKnockbackShadow().Show();
            }
        }

        private DeathKnockbackGroundShadowView EnsureKnockbackShadow()
        {
            if (_knockbackShadow == null)
            {
                _knockbackShadow = GetComponent<DeathKnockbackGroundShadowView>();
                if (_knockbackShadow == null)
                {
                    _knockbackShadow = gameObject.AddComponent<DeathKnockbackGroundShadowView>();
                }
            }

            return _knockbackShadow;
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
            _deathKnockWasAnimating = false;
            _corpseSmashEnabled = false;
            _corpseSmashHit.Clear();
            EnsureKnockbackShadow().Hide();
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

            var animating = MonsterDeathPresentation.TrySampleParabolicKnockback(
                _deathKnockOrigin,
                _deathKnockTarget,
                _deathKnockOrigin.y,
                _deathKnockStartedAt,
                MonsterDeathPresentation.DeathKnockbackSeconds,
                Time.time,
                out var pos);
            transform.position = pos;

            var heightAboveGround = pos.y - _deathKnockOrigin.y;
            EnsureKnockbackShadow().UpdateGroundShadow(
                _deathKnockOrigin.y,
                pos,
                BodyRadius,
                heightAboveGround);

            if (_corpseSmashEnabled)
            {
                var corpseXZ = new Vector2(pos.x, pos.z);
                if (animating)
                {
                    TickCorpseSmashSweep(corpseXZ);
                }
                else if (_deathKnockWasAnimating)
                {
                    TryCorpseSmashLanding(corpseXZ);
                }
            }

            _deathKnockWasAnimating = animating;
            if (!animating)
            {
                _deathKnockActive = false;
                EnsureKnockbackShadow().Hide();
            }
        }

        private void TickCorpseSmashSweep(Vector2 corpseXZ)
        {
            CorpseProjectileSmashSweep.TryHitNearby(
                corpseXZ,
                _attackerId,
                _killerWarriorId,
                _killerOutgoingDamage,
                _corpseSmashHit,
                _enumerateLivingMonsters,
                _tryApplyCorpseSmash);
        }

        private void TryCorpseSmashLanding(Vector2 corpseXZ)
        {
            TickCorpseSmashSweep(corpseXZ);
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
            if (_cocReturningHome ||
                !_alive ||
                IsStationary ||
                _config == null ||
                scheduler == null ||
                _moveId == 0)
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
                ClearTargetFocus();
                ReleaseSlotClaim(slots);
                scheduler.SetPaused(_moveId, true);
                return false;
            }

            Transform targetTf;
            string targetId;
            float targetBody;
            var intercepting = TryGetCocIntercept(out var interceptTf, out var interceptBody, out var interceptId);
            if (intercepting)
            {
                targetTf = interceptTf;
                targetId = interceptId;
                targetBody = interceptBody;
            }
            else if (ResolveTarget(out var warriorView, out var protagonistTf) == TargetKind.None)
            {
                ReleaseSlotClaim(slots);
                scheduler.SetPaused(_moveId, true);
                return false;
            }
            else
            {
                targetTf = warriorView != null ? warriorView.transform : protagonistTf;
                if (targetTf == null)
                {
                    ReleaseSlotClaim(slots);
                    scheduler.SetPaused(_moveId, true);
                    return false;
                }

                targetId = warriorView != null
                    ? warriorView.AttackerId
                    : "Protagonist";
                targetBody = warriorView != null
                    ? warriorView.AgentRadius
                    : AttackSlotService.DefaultTargetBodyRadius;
            }

            var dist = CombatReach.DistanceXZ(transform.position, targetTf.position);

            if (_cocDriveStraight && !intercepting)
            {
                ReleaseSlotClaim(slots);
                scheduler.SetPaused(_moveId, true);
                if (CombatReach.IsInAttackRange(dist, _config.AttackRange, BodyRadius, targetBody))
                {
                    StopMovement();
                }

                return true;
            }

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

            var dest = CombatReach.ChaseDestinationXZ(
                transform.position,
                targetTf.position,
                slotPos,
                _config.AttackRange,
                BodyRadius,
                targetBody,
                MassMoveScheduler.ArriveEpsilon);
            scheduler.SetPaused(_moveId, false);
            scheduler.SetGoal(_moveId, GoalKind.AttackSlot, dest);
            return true;
        }

        private void Update()
        {
            TickDeathKnockback();
            TryNotifyDeathPresentationComplete();
            TryNotifyReviveAnimComplete();

            if (!_alive || _config == null || !_combatGameplayEnabled)
            {
                ResetCocAttackStuck();
                return;
            }

            if (_cocReturningHome)
            {
                return;
            }

            if (IsStunnedNow())
            {
                StopMovement();
                _scheduler?.SetPaused(_moveId, true);
                if (_attackPhase == MonsterAttackPhase.Windup)
                {
                    ClearRangedWindup();
                }

                ResetCocAttackStuck();
                return;
            }

            // Legacy retarget timer kept for TargetSelect cadence; slot refresh is Stage-budgeted.
            _retargetTimer += Time.deltaTime;
            if (_retargetTimer >= _retargetInterval)
            {
                _retargetTimer = 0f;
            }

            if (_attackPhase == MonsterAttackPhase.Windup)
            {
                StopMovement();
                _scheduler?.SetPaused(_moveId, true);
                TickRangedWindup();
                ResetCocAttackStuck();
                return;
            }

            Transform targetTf;
            float targetBody;
            PushMapAdvanceView warriorView = null;
            TargetKind targetKind;
            if (TryGetCocIntercept(out var interceptTf, out var interceptBody, out _))
            {
                targetTf = interceptTf;
                targetBody = interceptBody;
                targetKind = TargetKind.CocIntercept;
            }
            else
            {
                targetKind = ResolveTarget(out warriorView, out var protagonistTf);
                if (targetKind == TargetKind.None)
                {
                    ResetCocAttackStuck();
                    return;
                }

                targetTf = targetKind == TargetKind.Warrior && warriorView != null
                    ? warriorView.transform
                    : protagonistTf;
                if (targetTf == null)
                {
                    ResetCocAttackStuck();
                    return;
                }

                targetBody = warriorView != null
                    ? warriorView.AgentRadius
                    : AttackSlotService.DefaultTargetBodyRadius;
            }

            var dist = CombatReach.DistanceXZ(transform.position, targetTf.position);
            if (!CombatReach.IsInAttackRange(dist, _config.AttackRange, BodyRadius, targetBody))
            {
                ResetCocAttackStuck();
                return;
            }

            TickCocAttackStuck();
            if (_cocReturningHome)
            {
                return;
            }

            StopMovement();

            _attackCooldown -= Time.deltaTime;
            if (_attackCooldown > 0f)
            {
                return;
            }

            FaceTowardForAttack(targetTf.position);

            if (AttackMode == AttackMode.Ranged)
            {
                BeginRangedWindup(targetKind, warriorView);
                return;
            }

            if (targetKind == TargetKind.CocIntercept)
            {
                _cocInterceptTryHit?.Invoke();
            }
            else if (targetKind == TargetKind.Protagonist)
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

                NoteVictimDown(warriorView);
            }

            var aspd = _config.AttackSpeed * ResolveSlowAttackMul();
            _anim?.PlayAttack(0, aspd);

            var interval = aspd > 0.01f ? 1f / aspd : 1f;
            _attackCooldown = Mathf.Max(0.2f, interval);
        }

        private void BeginRangedWindup(TargetKind targetKind, PushMapAdvanceView warriorView)
        {
            _attackPhase = MonsterAttackPhase.Windup;
            _windupRemaining = Mathf.Max(0f, _config.MeleeWindupSeconds);
            _windupTargetKind = targetKind;
            _windupWarriorId = warriorView != null ? warriorView.AttackerId : null;
            var aspd = _config.AttackSpeed * ResolveSlowAttackMul();
            var interval = aspd > 0.01f ? 1f / aspd : 1f;
            _attackCooldown = Mathf.Max(0.2f, interval);
            var hold = _windupRemaining > 0f ? _config.RangedWindupHoldFrame : 0;
            _anim?.PlayAttack(hold, aspd);
            if (_windupRemaining <= 0f)
            {
                TickRangedWindup();
            }
        }

        private void TickRangedWindup()
        {
            _windupRemaining -= Time.deltaTime;
            if (_windupRemaining > 0f)
            {
                return;
            }

            _anim?.ReleaseAttackHold();
            var targetKind = _windupTargetKind;
            var warriorId = _windupWarriorId;
            ClearRangedWindup();

            if (_config == null)
            {
                return;
            }

            if (targetKind == TargetKind.CocIntercept)
            {
                if (TryGetCocIntercept(out var interceptTf, out var interceptBody, out _) &&
                    CombatReach.IsInAttackRange(
                        CombatReach.DistanceXZ(transform.position, interceptTf.position),
                        _config.AttackRange,
                        BodyRadius,
                        interceptBody,
                        CombatReach.HitConfirmSlack))
                {
                    _cocInterceptTryHit?.Invoke();
                }

                return;
            }

            if (targetKind == TargetKind.Protagonist)
            {
                var tf = _protagonist;
                if (tf != null
                    && CombatReach.IsInAttackRange(
                        CombatReach.DistanceXZ(transform.position, tf.position),
                        _config.AttackRange,
                        BodyRadius,
                        AttackSlotService.DefaultTargetBodyRadius,
                        CombatReach.HitConfirmSlack))
                {
                    _onHitProtagonist?.Invoke($"Monster:{_config.MonsterId}");
                }

                return;
            }

            if (string.IsNullOrEmpty(warriorId) || _warriorsProvider == null)
            {
                return;
            }

            PushMapAdvanceView warriorView = null;
            var list = _warriorsProvider();
            if (list != null)
            {
                for (var i = 0; i < list.Count; i++)
                {
                    if (list[i] != null && list[i].AttackerId == warriorId)
                    {
                        warriorView = list[i];
                        break;
                    }
                }
            }

            if (warriorView == null)
            {
                return;
            }

            if (!CombatReach.IsInAttackRange(
                    CombatReach.DistanceXZ(transform.position, warriorView.transform.position),
                    _config.AttackRange,
                    BodyRadius,
                    warriorView.AgentRadius,
                    CombatReach.HitConfirmSlack))
            {
                return;
            }

            var applied = _onHitWarrior != null &&
                          _onHitWarrior(_attackerId, warriorView.AttackerId, _config.AttackPower);
            if (!applied)
            {
                Debug.LogWarning(
                    $"[PushMapMonster] {_config.MonsterId} hit warrior {warriorView.AttackerId} " +
                    "but Session did not settle (inactive / already dead).");
            }

            NoteVictimDown(warriorView);
        }

        private void ClearRangedWindup()
        {
            _attackPhase = MonsterAttackPhase.Idle;
            _windupRemaining = 0f;
            _windupTargetKind = TargetKind.None;
            _windupWarriorId = null;
            _anim?.ReleaseAttackHold();
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
                        if (_cocDriveStraight && !inAttackRange)
                        {
                            TryApplyCocStraightDrive();
                        }
                        else
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
            }

            TickAnimPresentation(inAttackRange, isLocomoting);
        }

        private void TryApplyCocStraightDrive()
        {
            if (!TryGetCombatTargetPosition(out var targetPos, out _))
            {
                return;
            }

            var to = targetPos - transform.position;
            to.y = 0f;
            if (to.sqrMagnitude < 1e-8f)
            {
                return;
            }

            if (_agent.hasPath)
            {
                _agent.ResetPath();
            }

            _agent.isStopped = false;
            var speed = ResolveEffectiveMoveSpeed(_gait.IsRun);
            _agent.speed = speed;
            var dir = to.normalized;
            _lastSteerDirXZ = dir;
            _lastDesiredDirXZ = dir;
            _agent.Move(dir * (speed * Time.deltaTime));
        }

        private bool IsAtSpawn()
        {
            var pos = transform.position;
            var dx = pos.x - _spawnXZ.x;
            var dz = pos.z - _spawnXZ.y;
            var epsilon = MassMoveScheduler.ArriveEpsilon;
            return dx * dx + dz * dz <= epsilon * epsilon;
        }

        private void TickCocAttackStuck()
        {
            if (!_cocActiveChaseLatchEnabled || IsStationary || _cocReturningHome)
            {
                ResetCocAttackStuck();
                return;
            }

            if (!IsCocActionFrozen(out var hash, out var normalized))
            {
                BeginCocAttackStuckWindow(hash, normalized);
                return;
            }

            var pos = transform.position;
            var dx = pos.x - _cocAttackStuckOrigin.x;
            var dz = pos.z - _cocAttackStuckOrigin.z;
            var epsilon = CombatRuntimeTuning.StuckDisplacementEpsilon;
            if (dx * dx + dz * dz >= epsilon * epsilon)
            {
                BeginCocAttackStuckWindow(hash, normalized);
                return;
            }

            _cocAttackStuckTimer += Time.deltaTime;
            if (_cocAttackStuckTimer >= CocAttackStuckSeconds)
            {
                BeginCocReturnHome();
            }
        }

        private bool IsCocActionFrozen(out int hash, out float normalized)
        {
            hash = 0;
            normalized = 0f;
            if (_anim == null || !_anim.TryGetActionStamp(out hash, out normalized))
            {
                return false;
            }

            if (!_cocAttackStuckWindowReady)
            {
                return false;
            }

            if (hash != _cocActionHash)
            {
                return false;
            }

            if (normalized > _cocActionNormalized + CocActionAdvanceEpsilon)
            {
                return false;
            }

            if (normalized < _cocActionNormalized - CocActionAdvanceEpsilon)
            {
                return false;
            }

            return true;
        }

        private void BeginCocAttackStuckWindow(int hash, float normalized)
        {
            _cocAttackStuckWindowReady = true;
            _cocAttackStuckTimer = 0f;
            _cocAttackStuckOrigin = transform.position;
            _cocActionHash = hash;
            _cocActionNormalized = normalized;
        }

        private void ResetCocAttackStuck()
        {
            _cocAttackStuckWindowReady = false;
            _cocAttackStuckTimer = 0f;
        }

        private void BeginCocReturnHome()
        {
            _cocReturningHome = true;
            ResetCocAttackStuck();
            _stuckHold.Reset();
            ClearRangedWindup();
            ClearCocIntercept();
            SetCocBodyTarget(null);
            _anim?.ResetToIdle();
        }

        private void CompleteCocReturnHome(AttackSlotService slots, MassMoveScheduler scheduler)
        {
            _cocReturningHome = false;
            _cocChaseUnlocked = false;
            _cocAggroLatched = false;
            _cocEverEngaged = false;
            _cocForcedWarrior = null;
            _lastChaseWarriorId = null;
            ResetCocAttackStuck();
            _stuckHold.Reset();
            ClearCocIntercept();
            ReleaseSlotClaim(slots);
            if (scheduler != null && _moveId != 0)
            {
                scheduler.SetPaused(_moveId, true);
            }

            StopMovement();
            _anim?.ResetToIdle();
        }

        private void EvaluateLocomotion(out bool inAttackRange, out bool isLocomoting)
        {
            inAttackRange = false;
            isLocomoting = false;
            if (_cocReturningHome)
            {
                _stuckHold.Tick(false, transform.position, Time.deltaTime);
                isLocomoting = _alive && !IsStunnedNow() && !IsAtSpawn();
                return;
            }

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
            var hasStraightIntent = _cocDriveStraight &&
                                    _alive &&
                                    !IsStunnedNow() &&
                                    !IsStationary &&
                                    !inAttackRange;
            var wantsMove = _alive &&
                            !IsStunnedNow() &&
                            !IsStationary &&
                            !inAttackRange &&
                            (hasSteerIntent || hasStraightIntent);
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

            var moveReference = _gait.IsRun
                ? WarriorAnimView.MonsterRunAnimReferenceSpeed
                : WarriorAnimView.MonsterWalkAnimReferenceSpeed;
            _anim.SetMoving(
                isLocomoting,
                ResolveMoveTargetDistanceXZ(),
                _gait.IsRun,
                WarriorAnimView.ResolveMovePlaybackRate(
                    ResolveEffectiveMoveSpeed(_gait.IsRun),
                    moveReference));
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

            if (TryGetCocIntercept(out var interceptTf, out var interceptBody, out _))
            {
                targetPos = interceptTf.position;
                targetBodyRadius = interceptBody;
                return true;
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
            _knockbackShadow?.Hide();

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
            Warrior = 2,
            CocIntercept = 3
        }

        private bool TryGetCocIntercept(out Transform target, out float bodyRadius, out string targetId)
        {
            target = _cocInterceptTarget;
            bodyRadius = _cocInterceptBodyRadius;
            targetId = _cocInterceptTargetId;
            return target != null && _cocInterceptTryHit != null && !string.IsNullOrEmpty(targetId);
        }

        private bool IsAggroActive => !IsPassive || _provoked;

        private TargetKind ResolveTarget(out PushMapAdvanceView warrior, out Transform protagonist)
        {
            warrior = null;
            protagonist = _protagonist;
            RefreshAlertCenter();
            if (_cocForcedWarrior != null)
            {
                if (_cocForcedWarrior.IsCombatActive && !_cocForcedWarrior.IsRebel)
                {
                    warrior = _cocForcedWarrior;
                    _lastChaseWarriorId = warrior.AttackerId;
                    return TargetKind.Warrior;
                }

                _cocForcedWarrior = null;
            }

            NoteDeadChaseVictim();

            if (_config == null || !IsAggroActive)
            {
                SyncTargetFocus(TargetKind.None, null);
                return TargetKind.None;
            }

            var alertRadius = _alertRadius;
            TargetKind kind;
            switch (_config.TargetSelect)
            {
                case TargetSelect.PreferProtagonist:
                    if (protagonist != null &&
                        WithinDetect(
                            protagonist.position,
                            alertRadius,
                            AttackSlotService.DefaultTargetBodyRadius))
                    {
                        kind = TargetKind.Protagonist;
                        break;
                    }

                    warrior = NearestLoyalWarriorWithin(alertRadius);
                    kind = warrior != null ? TargetKind.Warrior : TargetKind.None;
                    break;

                case TargetSelect.PreferWarrior:
                    warrior = NearestLoyalWarriorWithin(alertRadius);
                    if (warrior != null)
                    {
                        kind = TargetKind.Warrior;
                        break;
                    }

                    kind = protagonist != null &&
                           WithinDetect(
                               protagonist.position,
                               alertRadius,
                               AttackSlotService.DefaultTargetBodyRadius)
                        ? TargetKind.Protagonist
                        : TargetKind.None;
                    break;

                default:
                    kind = NearestAny(alertRadius, out warrior, out protagonist);
                    break;
            }

            if (kind == TargetKind.Warrior && warrior != null && !string.IsNullOrEmpty(warrior.AttackerId))
            {
                _lastChaseWarriorId = warrior.AttackerId;
                if (_cocActiveChaseLatchEnabled
                    && _runtimeAggroMode == AggroMode.ActiveChase
                    && !IsStationary)
                {
                    _cocEverEngaged = true;
                    _cocAggroLatched = true;
                }
            }

            SyncTargetFocus(kind, warrior);
            return kind;
        }

        /// <summary>Ground XZ distance from the alert center, which tracks this monster.</summary>
        private float DistanceFromAlertCenter(Vector3 world)
        {
            RefreshAlertCenter();

            var dx = world.x - _alertCenterXZ.x;
            var dz = world.z - _alertCenterXZ.y;
            return Mathf.Sqrt(dx * dx + dz * dz);
        }

        private Vector3 AlertCenterWorld
        {
            get
            {
                RefreshAlertCenter();
                return new Vector3(_alertCenterXZ.x, transform.position.y, _alertCenterXZ.y);
            }
        }

        /// <summary>
        /// Current chase soldier died: snap the alert center back onto this monster and keep COC ActiveChase latched.
        /// </summary>
        private void NoteDeadChaseVictim()
        {
            if (!_cocActiveChaseLatchEnabled
                || _runtimeAggroMode != AggroMode.ActiveChase
                || IsStationary
                || string.IsNullOrEmpty(_lastChaseWarriorId)
                || IsLivingLoyalWarrior(_lastChaseWarriorId))
            {
                return;
            }

            _cocAggroLatched = true;
            _lastChaseWarriorId = null;
            RefreshAlertCenter();
            ClearTargetFocus();
        }

        private void NoteVictimDown(PushMapAdvanceView warrior)
        {
            if (warrior == null || warrior.IsCombatActive)
            {
                return;
            }

            if (!_cocActiveChaseLatchEnabled
                || _runtimeAggroMode != AggroMode.ActiveChase
                || IsStationary)
            {
                return;
            }

            _cocAggroLatched = true;
            if (string.Equals(_lastChaseWarriorId, warrior.AttackerId, StringComparison.Ordinal))
            {
                _lastChaseWarriorId = null;
            }

            RefreshAlertCenter();
            ClearTargetFocus();
            TryRefreshChaseGoal(_attackSlots, _scheduler);
        }

        private bool IsLivingLoyalWarrior(string warriorId)
        {
            var list = _warriorsProvider != null ? _warriorsProvider() : null;
            if (list == null || string.IsNullOrEmpty(warriorId))
            {
                return false;
            }

            for (var i = 0; i < list.Count; i++)
            {
                var w = list[i];
                if (w != null
                    && !w.IsRebel
                    && w.IsCombatActive
                    && string.Equals(w.AttackerId, warriorId, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
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

            return DistanceFromAlertCenter(targetPos) <= Mathf.Max(0.01f, detect);
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
                bestDist = DistanceFromAlertCenter(protagonist.position);
                kind = TargetKind.Protagonist;
            }

            var nearestWarrior = NearestLoyalWarriorWithin(alertRadius);
            if (nearestWarrior != null)
            {
                var d = DistanceFromAlertCenter(nearestWarrior.transform.position);
                if (d < bestDist)
                {
                    warrior = nearestWarrior;
                    kind = TargetKind.Warrior;
                }
            }

            return kind;
        }

        /// <summary>COC ActiveChase only: latched chase ignores AlertRadius until this monster dies.</summary>
        private bool IsCocActiveChasePersistent =>
            _cocActiveChaseLatchEnabled
            && _cocAggroLatched
            && _runtimeAggroMode == AggroMode.ActiveChase
            && !IsStationary;

        private void TryLatchCocActiveChase(IReadOnlyList<PushMapAdvanceView> list, float alertRadius)
        {
            if (_cocAggroLatched
                || !_cocActiveChaseLatchEnabled
                || _runtimeAggroMode != AggroMode.ActiveChase
                || IsStationary
                || list == null)
            {
                return;
            }

            var detect = Mathf.Max(0.01f, alertRadius);
            for (var i = 0; i < list.Count; i++)
            {
                var w = list[i];
                if (w == null || w.IsRebel || !w.IsCombatActive || string.IsNullOrEmpty(w.AttackerId))
                {
                    continue;
                }

                if (DistanceFromAlertCenter(w.transform.position) <= detect)
                {
                    _cocAggroLatched = true;
                    return;
                }
            }
        }

        /// <summary>Strict nearest living loyal soldier by this monster's body XZ. No alert radius, no sticky band.</summary>
        private PushMapAdvanceView NearestByBody(IReadOnlyList<PushMapAdvanceView> list)
        {
            PushMapAdvanceView best = null;
            var bestDist = float.MaxValue;
            if (list == null)
            {
                return null;
            }

            for (var i = 0; i < list.Count; i++)
            {
                var w = list[i];
                if (w == null || w.IsRebel || !w.IsCombatActive || string.IsNullOrEmpty(w.AttackerId))
                {
                    continue;
                }

                var dist = CombatReach.DistanceXZ(transform.position, w.transform.position);
                if (dist < bestDist)
                {
                    bestDist = dist;
                    best = w;
                }
            }

            return best;
        }

        private PushMapAdvanceView NearestLoyalWarriorWithin(float alertRadius)
        {
            var list = _warriorsProvider != null ? _warriorsProvider() : null;
            if (list == null || list.Count == 0)
            {
                return null;
            }

            TryLatchCocActiveChase(list, alertRadius);
            var cocUnlocked = _cocActiveChaseLatchEnabled
                              && _runtimeAggroMode == AggroMode.ActiveChase
                              && !IsStationary
                              && (_cocAggroLatched || _cocEverEngaged);
            if (cocUnlocked)
            {
                _cocAggroLatched = true;
                return NearestByBody(list);
            }

            var chasePersistent = IsPassive && _provoked && !IsStationary;
            _nearestCandidateScratch.Clear();
            for (var i = 0; i < list.Count; i++)
            {
                var w = list[i];
                if (w == null || w.IsRebel || !w.IsCombatActive || string.IsNullOrEmpty(w.AttackerId))
                {
                    continue;
                }

                var d = DistanceFromAlertCenter(w.transform.position);
                var detect = IsStationary
                    ? CombatReach.MaxCenterDistance(_config.AttackRange, BodyRadius, w.AgentRadius)
                    : alertRadius;
                if (!chasePersistent && d > Mathf.Max(0.01f, detect))
                {
                    continue;
                }

                _nearestCandidateScratch.Add(
                    new MonsterNearestWarriorPicker.Candidate(w.AttackerId, w.transform.position));
            }

            if (_nearestCandidateScratch.Count == 0)
            {
                return null;
            }

            string currentFocus = null;
            _targetFocus?.TryGetFocus(_attackerId, out currentFocus);
            var pickedId = MonsterNearestWarriorPicker.Pick(
                AlertCenterWorld,
                _nearestCandidateScratch,
                _targetFocus,
                _attackerId,
                currentFocus,
                CombatRuntimeTuning.NearestTargetBandRelative,
                CombatRuntimeTuning.NearestTargetBandSlack);
            if (string.IsNullOrEmpty(pickedId))
            {
                return null;
            }

            for (var i = 0; i < list.Count; i++)
            {
                var w = list[i];
                if (w != null && w.AttackerId == pickedId)
                {
                    return w;
                }
            }

            return null;
        }

        private void SyncTargetFocus(TargetKind kind, PushMapAdvanceView warrior)
        {
            if (_targetFocus == null || string.IsNullOrEmpty(_attackerId))
            {
                return;
            }

            if (kind == TargetKind.Warrior && warrior != null && !string.IsNullOrEmpty(warrior.AttackerId))
            {
                _targetFocus.SetFocus(_attackerId, warrior.AttackerId);
            }
            else
            {
                _targetFocus.ClearFocus(_attackerId);
            }
        }

        private void ClearTargetFocus()
        {
            if (_targetFocus != null && !string.IsNullOrEmpty(_attackerId))
            {
                _targetFocus.ClearFocus(_attackerId);
            }
        }
    }
}
