using System;
using System.Collections.Generic;
using Gravedigger2026.Core.Combat;
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
    public delegate bool CocObstacleAimResolver(string runtimeId, out Transform transform, out float bodyRadius);

    /// <summary>
    /// MP-04/05: loyal advance via FlowField; engage ??GoalKind=AttackSlot (SPEC_03 ?3.12/?3.14).
    /// Samples MassMoveScheduler steer; applies NavMeshAgent.Move ??no per-frame SetDestination.
    /// Capture-zone monsters do NOT pause advance. Rebels do not advance.
    /// PM-12 (Approach B, SPEC_04 ?9.22): WarriorCombat scheme D ??melee windup ??
    /// TryConfirmMeleeHit; ranged ??shared ProjectileView soft-hit ??TryConfirmRangedHit
    /// (timeout = miss, no settlement). Combat params come from PushMapSessionService
    /// StartBattle registry (WarriorCombatMath + ClassConfig, mirrored from Defend).
    /// D-069: Skill_03 burst occupies this attack channel (3? scheme D) when CD ready.
    /// D-073 SE-09: new-target acquire may Warp behind farthest enemy (rules pick; View Warp).
    /// PM-13: CombatDead ??PlayDie + stop acting (aligned with Defend WarriorAgentView).
    /// PM-13: CombatDead ??PlayDie + stop acting (aligned with Defend WarriorAgentView).
    /// Presentation: WarriorAnimView SetMoving/DirIndex facing; PlayAttack on windup/fire.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PushMapAdvanceView : MonoBehaviour
    {
        private enum AttackPhase
        {
            IdleOrMove = 0,
            Windup = 1,
            BurstRecover = 2
        }

        private const float NavMeshSampleRadius = 12f;
        private const float BlinkNavMeshSampleMinRadius = 0.75f;
        private const float DefaultAttackRange = 1f;
        private const float MoveAnimSpeedSqr = 0.01f;
        /// <summary>
        /// SPEC_03 ?3.14 v0.74.10: a rival must be closer than the claimed target by more
        /// than this margin to steal the claim ??dense packs + soft-collision jostle
        /// otherwise flip-flop the strictly-nearest target and starve the kill engage clock.
        /// </summary>
        private float _engageStickHysteresisMargin = CombatConstantKeys.Safety.EngageStickHysteresisMargin;

        private Func<IReadOnlyList<PushMapMonsterAgentView>> _monstersProvider;
        private Func<IReadOnlyList<PushMapAdvanceView>> _alliesProvider;
        private Func<Transform> _protagonistProvider;
        private readonly List<ParabolaBody> _parabolaBodies = new List<ParabolaBody>(24);
        private readonly List<string> _aliveMonsterIdsScratch = new List<string>(32);
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

        private IWarriorMassCombatSession _session;
        private GameObject _projectilePrefab;
        private Transform _projectileParent;
        private DefendPrefabCatalog _projectileCatalog;
        private float _attackStartCooldown;
        private float _windupRemaining;
        private string _windupTargetId;
        private AttackPhase _attackPhase = AttackPhase.IdleOrMove;
        private int _burstHitsRemaining;
        private float _burstRecoverRemaining;

        private AttackSlotService _attackSlots;
        private bool _formationHold;
        private string _formationSwingTargetId;
        private bool _formationPresentationFacing;
        private Vector3 _formationPresentationFacingXZ;
        /// <summary>Last MassMove steer XZ (LateUpdate); drives IsRun ??not NavMeshAgent.velocity (SPEC_04 ?15.5).</summary>
        private Vector3 _lastSteerDirXZ;
        /// <summary>Last MassMove pre-detour desired; drives DirIndex while moving (SPEC_04 ?15.5 v0.83.31).</summary>
        private Vector3 _lastDesiredDirXZ;
        private readonly StuckHoldTracker _stuckHold = new StuckHoldTracker();
        private readonly ChaseStuckRetargetTracker _chaseStuckRetarget = new ChaseStuckRetargetTracker();
        private bool _cocAttackPriority;
        private string _cocLockedTargetId;
        private float _cocObserveRange = ClassConfigRow.DefaultObserveRange;
        private ClassConfigRow _cocClass;
        private CocObstacleAimResolver _cocObstacleAim;
        private AllyFootCircleView _footCircle;
        private readonly List<MonsterWorldXZ> _targetAcquireCandidates = new List<MonsterWorldXZ>(32);

        public bool IsRebel => _isRebel;
        public int MoveId => _moveId;
        public float AgentRadius => _bodyRadius;
        public string AttackerId => _attackerId;
        public float CocObserveRange => _cocObserveRange;
        public string CocLockedTargetId => _cocLockedTargetId;
        public ClassConfigRow CocClass => _cocClass;

        /// <summary>
        /// D-100 Approach A: COC injects the scored lock; default nearest select stays unchanged.
        /// </summary>
        public void EnableCocAttackPriority(ClassConfigRow classRow, float observeRange)
        {
            _cocAttackPriority = true;
            _cocClass = classRow;
            _cocObserveRange = observeRange > 0f ? observeRange : ClassConfigRow.DefaultObserveRange;
        }

        public void SetCocObstacleAimResolver(CocObstacleAimResolver resolver)
        {
            _cocObstacleAim = resolver;
        }

        public void SetCocLockedTargetId(string targetId)
        {
            _cocLockedTargetId = string.IsNullOrEmpty(targetId) ? null : targetId;
        }

        public void SetBaseMoveSpeed(float moveSpeed)
        {
            _baseMoveSpeed = Mathf.Max(0.1f, moveSpeed);
            ApplyEffectiveMoveSpeed();
        }

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

        public void SetParabolaCrowd(
            Func<IReadOnlyList<PushMapAdvanceView>> allies,
            Func<Transform> protagonist)
        {
            _alliesProvider = allies;
            _protagonistProvider = protagonist;
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
            IWarriorMassCombatSession session = null,
            GameObject projectilePrefab = null,
            Transform projectileParent = null,
            DefendPrefabCatalog projectileCatalog = null,
            float bodyRadius = BodyAppearanceConfigRow.DefaultBodyRadius,
            bool facingYawFlip = false,
            float pushCoefficient = BodyAppearanceConfigRow.DefaultPushCoefficient,
            float repulsionScale = BodyAppearanceConfigRow.DefaultRepulsionScale,
            float chaseMoveSpeedMult = ClassConfigRow.DefaultChaseMoveSpeedMult,
            bool hasSpecialMove = false)
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
            _engageStickHysteresisMargin = CombatRuntimeTuning.EngageStickHysteresisMargin;
            _projectilePrefab = projectilePrefab;
            _projectileParent = projectileParent;
            _projectileCatalog = projectileCatalog;
            _bodyRadius = Mathf.Max(0.05f, bodyRadius);
            _pushCoefficient = Mathf.Max(0f, pushCoefficient);
            _repulsionScale = Mathf.Max(0f, repulsionScale);
            _facingYawFlip = facingYawFlip;
            _attackStartCooldown = 0f;
            _windupRemaining = 0f;
            _windupTargetId = null;
            _attackPhase = AttackPhase.IdleOrMove;
            _burstHitsRemaining = 0;
            _burstRecoverRemaining = 0f;
            _diePlayed = false;
            _stoppedActing = false;
            _stuckHold.Reset();
            _chaseStuckRetarget.Reset();
            _cocAttackPriority = false;
            _cocLockedTargetId = null;
            _cocClass = null;
            _cocObstacleAim = null;
            _cocObserveRange = ClassConfigRow.DefaultObserveRange;
            _lastSteerDirXZ = Vector3.zero;
            _lastDesiredDirXZ = Vector3.zero;

            _agent = GetComponent<NavMeshAgent>();
            if (_agent == null)
            {
                _agent = gameObject.AddComponent<NavMeshAgent>();
            }

            _agent.agentTypeID = SpecialMoveNavMesh.ResolveAgentTypeId(hasSpecialMove);

            ApplyEffectiveMoveSpeed();
            _agent.stoppingDistance = 0f;
            _agent.angularSpeed = 720f;
            _agent.acceleration = 24f;
            _agent.radius = _bodyRadius;
            _agent.height = 0.1f;
            _agent.autoBraking = false;
            // Facing via Animator DirIndex in PushMap as in Defend (SPEC_04 ?15.2).
            _agent.updateRotation = false;
            // Field/slot follow: LocalDetour owns friendlies (no RVO scale scheme).
            _agent.obstacleAvoidanceType = ObstacleAvoidanceType.NoObstacleAvoidance;

            _scheduler.Register(
                _moveId,
                _bodyRadius,
                MassMoveScheduler.DetourGroupLoyal,
                _pushCoefficient,
                _repulsionScale);
            if (hasSpecialMove)
            {
                _scheduler.SetUseSpecialFlowField(_moveId, true);
            }

            _scheduler.SetGoal(_moveId, GoalKind.Objective);
            TryWarpOntoNavMesh();
            ClearPathingState();

            var taskLabel = GetComponent<WarriorTaskDebugLabelView>();
            if (taskLabel == null)
            {
                taskLabel = gameObject.AddComponent<WarriorTaskDebugLabelView>();
            }

            taskLabel.Bind(_scheduler, _moveId, FormatSkillCdSuffix, ResolveEffectiveMoveSpeed);

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
        /// Effective speed: base ? ChaseMoveSpeedMult only when GoalKind=AttackSlot (SPEC_03 ?3.12).
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
                _burstHitsRemaining = 0;
                _burstRecoverRemaining = 0f;
                _attackStartCooldown = 0f;
                _scheduler?.SetPaused(_moveId, true);
                ClearPathingState();
            }
        }

        /// <summary>
        /// Nearest living monster inside Demo engage detect (MP-05: enter AttackSlot, leave
        /// Objective field). v0.82.55 Approach C: detect = max(weapon reach, monster AlertRadius).
        /// v0.74.10 sticky hysteresis (SPEC_03 ?3.14): while the claimed
        /// target is alive and still inside its detect radius, a rival steals the claim only
        /// when closer by more than engage stick hysteresis (CombatConstantConfig).
        /// v0.84.25/26: chase-stuck force retarget excludes stuck ids, bypasses stickiness,
        /// and only switches to an alternate that still has a free AttackSlot.
        /// </summary>
        /// <summary>
        /// D-094: Stage holds this soldier on a formation slot. When <paramref name="holdingSlot"/>
        /// is true, swings only at <paramref name="swingTargetId"/> (null = stay, do not chase).
        /// </summary>
        public void SetFormationHoldSwing(bool holdingSlot, string swingTargetId)
        {
            _formationHold = holdingSlot;
            _formationSwingTargetId = swingTargetId;
        }

        /// <summary>D-095: grouped walk/idle 8-dir follows the squad forward. Attack lock still wins.</summary>
        public void SetFormationPresentationFacing(bool active, Vector3 worldForwardXZ)
        {
            worldForwardXZ.y = 0f;
            _formationPresentationFacing = active && worldForwardXZ.sqrMagnitude > 0.0001f;
            if (_formationPresentationFacing)
            {
                _formationPresentationFacingXZ = worldForwardXZ;
            }
        }

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

            var bypassStickiness = _chaseStuckRetarget.BypassStickiness;

            PushMapMonsterAgentView best = null;
            var bestDist = float.MaxValue;
            PushMapMonsterAgentView claimed = null;
            var claimedDist = float.MaxValue;
            PushMapMonsterAgentView excludedClaimed = null;
            for (var i = 0; i < list.Count; i++)
            {
                var m = list[i];
                if (m == null || !IsMonsterEngageable(m))
                {
                    continue;
                }

                // Detect = max(weapon reach, monster AlertRadius) (SPEC_03 ?3.12 v0.82.55 C).
                var detect = CombatReach.EngageDetectRadius(
                    m.AttackRange,
                    AttackRange,
                    m.BodyRadius,
                    _bodyRadius,
                    MassMoveScheduler.ArriveEpsilon,
                    m.AlertRadius);
                if (detect <= 0f)
                {
                    continue;
                }

                var d = CombatReach.DistanceXZ(transform.position, m.transform.position);
                if (d > detect)
                {
                    continue;
                }

                if (hasClaim && m.RuntimeTargetId == claimedId)
                {
                    excludedClaimed = m;
                    claimedDist = d;
                }

                if (_chaseStuckRetarget.IsExcluded(m.RuntimeTargetId))
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
                }
            }

            if (bypassStickiness)
            {
                monster = PickForceRetargetMonster(list) ?? excludedClaimed;
            }
            else if (claimed != null)
            {
                var stolen = best != null && best != claimed &&
                             bestDist < claimedDist - _engageStickHysteresisMargin;
                monster = stolen ? best : claimed;
            }
            else if (TryBlinkOverrideTarget(list, out var blinkMonster))
            {
                monster = blinkMonster;
            }
            else
            {
                monster = best;
            }

            if (monster != null)
            {
                _chaseStuckRetarget.NotifyApplied(monster.RuntimeTargetId);
            }

            return monster != null;
        }

        /// <summary>
        /// Nearest engageable alternate that still has a free AttackSlot (v0.84.26).
        /// Avoids Release?full-ring fail ??FormationHome thrash.
        /// </summary>
        private PushMapMonsterAgentView PickForceRetargetMonster(
            IReadOnlyList<PushMapMonsterAgentView> list)
        {
            if (list == null || _attackSlots == null)
            {
                return null;
            }

            PushMapMonsterAgentView best = null;
            var bestDist = float.MaxValue;
            var surround = CombatMoveModePolicy.SurroundFor(GoalKind.AttackSlot, _attackMode);
            for (var i = 0; i < list.Count; i++)
            {
                var m = list[i];
                if (m == null || !IsMonsterEngageable(m))
                {
                    continue;
                }

                if (_chaseStuckRetarget.IsExcluded(m.RuntimeTargetId))
                {
                    continue;
                }

                var detect = CombatReach.EngageDetectRadius(
                    m.AttackRange,
                    AttackRange,
                    m.BodyRadius,
                    _bodyRadius,
                    MassMoveScheduler.ArriveEpsilon,
                    m.AlertRadius);
                if (detect <= 0f)
                {
                    continue;
                }

                var d = CombatReach.DistanceXZ(transform.position, m.transform.position);
                if (d > detect || d >= bestDist)
                {
                    continue;
                }

                if (!_attackSlots.HasAvailableSlot(
                        _attackerId,
                        m.RuntimeTargetId,
                        AttackRange,
                        m.transform.position,
                        _attackMode,
                        transform.position,
                        m.BodyRadius,
                        _bodyRadius,
                        surround))
                {
                    continue;
                }

                bestDist = d;
                best = m;
            }

            return best;
        }

        /// <summary>
        /// SE-09: new-target acquire asks the rules layer (farthest + landing). View only samples/Warps.
        /// </summary>
        private bool TryBlinkOverrideTarget(
            IReadOnlyList<PushMapMonsterAgentView> list,
            out PushMapMonsterAgentView monster)
        {
            monster = null;
            if (_session == null || list == null || _isRebel)
            {
                return false;
            }

            _targetAcquireCandidates.Clear();
            for (var i = 0; i < list.Count; i++)
            {
                var m = list[i];
                if (!IsMonsterEngageable(m))
                {
                    continue;
                }

                var p = m.transform.position;
                _targetAcquireCandidates.Add(new MonsterWorldXZ(
                    m.RuntimeTargetId,
                    new Vector2(p.x, p.z),
                    m.FacingXZ,
                    m.BodyRadius));
            }

            if (_targetAcquireCandidates.Count == 0)
            {
                return false;
            }

            var self = transform.position;
            if (!_session.TryAcquireWarriorTarget(
                    _attackerId,
                    new Vector2(self.x, self.z),
                    _bodyRadius,
                    _targetAcquireCandidates,
                    SampleBlinkLanding,
                    out var overrideId,
                    out var landing) ||
                string.IsNullOrEmpty(overrideId))
            {
                return false;
            }

            if (!TryFindMonsterByRuntimeId(list, overrideId, out monster) || monster == null)
            {
                return false;
            }

            if (!TryWarpToLanding(landing))
            {
                Debug.LogWarning(
                    $"[PushMapAdvance] blink Warp failed warrior={_attackerId} target={overrideId}");
            }

            FaceTarget(monster.transform.position);
            return true;
        }

        private bool TryFindMonsterByRuntimeId(
            IReadOnlyList<PushMapMonsterAgentView> list,
            string runtimeId,
            out PushMapMonsterAgentView monster)
        {
            monster = null;
            if (list == null || string.IsNullOrEmpty(runtimeId))
            {
                return false;
            }

            for (var i = 0; i < list.Count; i++)
            {
                var m = list[i];
                if (IsMonsterEngageable(m) &&
                    string.Equals(m.RuntimeTargetId, runtimeId, StringComparison.Ordinal))
                {
                    monster = m;
                    return true;
                }
            }

            return false;
        }

        private Vector2? SampleBlinkLanding(Vector2 desiredXZ, float sampleRadius)
        {
            var radius = Mathf.Max(BlinkNavMeshSampleMinRadius, sampleRadius);
            var origin = new Vector3(desiredXZ.x, transform.position.y, desiredXZ.y);
            if (!SpecialMoveNavMesh.SampleWalkable(_agent, origin, radius, out var hit))
            {
                return null;
            }

            var dx = hit.position.x - desiredXZ.x;
            var dz = hit.position.z - desiredXZ.y;
            if (dx * dx + dz * dz > radius * radius)
            {
                return null;
            }

            return new Vector2(hit.position.x, hit.position.z);
        }

        private bool TryWarpToLanding(Vector2 landingXZ)
        {
            if (_agent == null || !_agent.enabled)
            {
                return false;
            }

            var world = new Vector3(landingXZ.x, transform.position.y, landingXZ.y);
            if (!_agent.isOnNavMesh)
            {
                TryWarpOntoNavMesh();
            }

            if (!_agent.Warp(world))
            {
                return false;
            }

            if (_agent.hasPath)
            {
                _agent.ResetPath();
            }

            _agent.velocity = Vector3.zero;
            _scheduler?.SnapPosition(_moveId, landingXZ);
            return true;
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
                        _attackPhase != AttackPhase.Windup &&
                        _attackPhase != AttackPhase.BurstRecover);
        }

        private void Update()
        {
            if (_isRebel && !IsSkillBurstActive)
            {
                TickAnimPresentation();
                return;
            }

            // PM-13: CombatDead / PermanentDeath mark ??PlayDie once and stop acting.
            if (!IsCombatActive)
            {
                EnterCombatDeadPresentation();
                return;
            }

            // StartBattle camera intro: keep Idle, no attack / engage.
            if (_session != null && !_session.IsCombatGameplayActive)
            {
                TickAnimPresentation();
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

            if (_attackPhase == AttackPhase.BurstRecover || _burstHitsRemaining > 0)
            {
                ClearBurst();
            }

            GetComponent<WarriorSkillIconHudView>()?.ClearAll();
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
            _chaseStuckRetarget.Reset();
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
            _burstHitsRemaining = 0;
            _burstRecoverRemaining = 0f;
        }

        private bool IsSkillBurstActive =>
            _burstHitsRemaining > 0 || _attackPhase == AttackPhase.BurstRecover;

        /// <summary>
        /// PM-12 scheme D: engaged (AttackSlot claim on a living monster) + in AttackRange ??
        /// melee windup / ranged projectile; settlement via session HitConfirm only.
        /// D-069: Skill_03 occupies this channel for 3 sequential scheme-D hits when CD ready.
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

            if (_attackPhase == AttackPhase.BurstRecover)
            {
                TickBurstRecover();
                return;
            }

            if (TrySyncRebelFromSession())
            {
                return;
            }

            if (_burstHitsRemaining <= 0)
            {
                _attackStartCooldown = Mathf.Max(0f, _attackStartCooldown - Time.deltaTime);
            }

            if (_cocAttackPriority &&
                TryResolveCocObstacleAim(out var obstacleTransform, out var obstacleId, out var obstacleBody))
            {
                TickCombatAgainstAim(obstacleTransform, obstacleId, obstacleBody);
                return;
            }

            if (!TryResolveCombatTarget(out var target))
            {
                if (_burstHitsRemaining > 0)
                {
                    EndBurst();
                }

                return;
            }

            if (!_session.TryGetWarrior(_attackerId, out var state) || state == null)
            {
                return;
            }

            if (!CombatReach.IsInAttackRange(
                    CombatReach.DistanceXZ(transform.position, target.transform.position),
                    state.AttackRange,
                    _bodyRadius,
                    target.BodyRadius))
            {
                if (_burstHitsRemaining > 0)
                {
                    EndBurst();
                }

                return;
            }

            if (_burstHitsRemaining > 0)
            {
                FireBurstHit(state, target);
                return;
            }

            if (_session.TryCommitSkillBurst(_attackerId, out var hits) && hits > 0)
            {
                _burstHitsRemaining = hits;
                FireBurstHit(state, target);
                return;
            }

            if (_attackStartCooldown > 0f)
            {
                return;
            }

            BeginWindup(target, state, fromBurst: false);
        }

        private bool TryResolveCombatTarget(out PushMapMonsterAgentView target)
        {
            if (_cocAttackPriority)
            {
                target = null;
                if (string.IsNullOrEmpty(_cocLockedTargetId))
                {
                    return false;
                }

                return TryFindMonsterByRuntimeId(
                           _monstersProvider != null ? _monstersProvider() : null,
                           _cocLockedTargetId,
                           out target)
                       && target != null;
            }

            if (_formationHold)
            {
                target = null;
                if (string.IsNullOrEmpty(_formationSwingTargetId))
                {
                    return false;
                }

                return TryFindMonsterByRuntimeId(
                           _monstersProvider != null ? _monstersProvider() : null,
                           _formationSwingTargetId,
                           out target)
                       && target != null;
            }

            if (TryResolveEngagedTarget(out target))
            {
                return true;
            }

            // In-range hold may keep GoalKind=AttackSlot without a free ring slot
            // (SPEC_03 ?3.12 v0.82.57). Still allow the swing if the detect target is in reach.
            if (_scheduler == null ||
                !_scheduler.TryGetGoal(_moveId, out var kind, out _) ||
                kind != GoalKind.AttackSlot ||
                !TryGetEngageMonster(out target) ||
                target == null)
            {
                target = null;
                return false;
            }

            return true;
        }

        private bool TryResolveCocObstacleAim(out Transform target, out string targetId, out float bodyRadius)
        {
            target = null;
            targetId = null;
            bodyRadius = 0f;
            if (!_cocAttackPriority ||
                string.IsNullOrEmpty(_cocLockedTargetId) ||
                _cocObstacleAim == null)
            {
                return false;
            }

            if (!_cocObstacleAim(_cocLockedTargetId, out target, out bodyRadius) || target == null)
            {
                return false;
            }

            targetId = _cocLockedTargetId;
            return true;
        }

        private void TickCombatAgainstAim(Transform target, string targetId, float targetBodyRadius)
        {
            if (target == null || string.IsNullOrEmpty(targetId))
            {
                return;
            }

            if (!_session.TryGetWarrior(_attackerId, out var state) || state == null)
            {
                return;
            }

            if (!CombatReach.IsInAttackRange(
                    CombatReach.DistanceXZ(transform.position, target.position),
                    state.AttackRange,
                    _bodyRadius,
                    targetBodyRadius))
            {
                if (_burstHitsRemaining > 0)
                {
                    EndBurst();
                }

                return;
            }

            if (_attackStartCooldown > 0f)
            {
                return;
            }

            BeginWindupAt(target, targetId, state);
        }

        private void FireBurstHit(DefendCombatWarriorState state, PushMapMonsterAgentView target)
        {
            BeginWindup(target, state, fromBurst: true);
        }

        private void BeginWindup(
            PushMapMonsterAgentView target,
            DefendCombatWarriorState state,
            bool fromBurst)
        {
            _attackPhase = AttackPhase.Windup;
            _windupTargetId = target.RuntimeTargetId;
            _windupRemaining = Mathf.Max(0f, state.MeleeWindupSeconds);
            if (!fromBurst)
            {
                _attackStartCooldown = state.AttackSpeed > 0.01f ? 1f / state.AttackSpeed : 1f;
            }

            _scheduler?.SetPaused(_moveId, true);
            ClearPathingState();

            if (_anim != null)
            {
                FaceTarget(target.transform.position);
                var parabolaMeleePreview = state.AttackMode == AttackMode.Parabola
                    && PreviewParabolaMeleeAtWindup(state, target);
                var animPool = parabolaMeleePreview
                    ? state.ParabolaMeleeAttackAnims
                    : state.NormalAttackAnims;
                var hold = !parabolaMeleePreview
                           && (state.AttackMode == AttackMode.Ranged || state.AttackMode == AttackMode.Parabola)
                           && state.MeleeWindupSeconds > 0f
                    ? state.RangedWindupHoldFrame
                    : 0;
                _anim.ConfigureSoldierNormalAttackAnims(animPool);
                _anim.PlayAttack(hold, state.AttackSpeed);
            }
        }

        private void BeginWindupAt(Transform target, string targetId, DefendCombatWarriorState state)
        {
            _attackPhase = AttackPhase.Windup;
            _windupTargetId = targetId;
            _windupRemaining = Mathf.Max(0f, state.MeleeWindupSeconds);
            _attackStartCooldown = state.AttackSpeed > 0.01f ? 1f / state.AttackSpeed : 1f;
            _scheduler?.SetPaused(_moveId, true);
            ClearPathingState();
            if (_anim != null && target != null)
            {
                FaceTarget(target.position);
                _anim.ConfigureSoldierNormalAttackAnims(state.NormalAttackAnims);
                var hold = (state.AttackMode == AttackMode.Ranged || state.AttackMode == AttackMode.Parabola) &&
                           state.MeleeWindupSeconds > 0f
                    ? state.RangedWindupHoldFrame
                    : 0;
                _anim.PlayAttack(hold, state.AttackSpeed);
            }
        }

        /// <summary>
        /// Windup-start preview of the Parabola forward-arc melee gate (SPEC_04 §15.5).
        /// Fire resolution at windup end still runs Choose again.
        /// </summary>
        private bool PreviewParabolaMeleeAtWindup(
            DefendCombatWarriorState state,
            PushMapMonsterAgentView target)
        {
            var origin = transform.position;
            var aimPoint = target != null ? target.transform.position : origin + transform.forward;
            var aim = aimPoint - origin;
            aim.y = 0f;
            if (aim.sqrMagnitude < 0.0001f)
            {
                aim = transform.forward;
                aim.y = 0f;
            }

            var targetInEngage = IsMonsterEngageable(target)
                && CombatReach.IsInAttackRange(
                    CombatReach.DistanceXZ(origin, target.transform.position),
                    state.AttackRange,
                    _bodyRadius,
                    target.BodyRadius);

            FillParabolaBodies();
            var choice = ParabolaFirePolicy.Choose(
                origin,
                aimPoint,
                targetInEngage,
                state.AttackRange,
                state.ParabolaMeleeRange,
                state.ParabolaArcMinDistance,
                state.ParabolaHitRate,
                CombatRuntimeTuning.ParabolaForwardArcDegrees,
                0f,
                aim,
                _parabolaBodies);
            return choice.Kind == ParabolaShotKind.Melee;
        }

        private void TickWindup()
        {
            _windupRemaining -= Time.deltaTime;
            if (_windupRemaining > 0f)
            {
                return;
            }

            if (!_session.TryGetWarrior(_attackerId, out var state) || state == null)
            {
                ClearWindup();
                return;
            }

            var target = FindMonsterByRuntimeId(_windupTargetId);
            if (target == null &&
                _cocObstacleAim != null &&
                _cocObstacleAim(_windupTargetId, out var obstacleTransform, out var obstacleBody) &&
                obstacleTransform != null)
            {
                TickWindupAgainstAim(state, obstacleTransform, obstacleBody);
                return;
            }

            var range = state.AttackRange;
            var inRange = IsMonsterEngageable(target)
                          && CombatReach.IsInAttackRange(
                              CombatReach.DistanceXZ(transform.position, target.transform.position),
                              range,
                              _bodyRadius,
                              target.BodyRadius,
                              CombatReach.HitConfirmSlack);

            _anim?.ReleaseAttackHold();
            if (state.AttackMode == AttackMode.Parabola)
            {
                var melee = ResolveParabolaShot(state, target, inRange);
                ClearWindup();
                if (_burstHitsRemaining > 0)
                {
                    AfterBurstHit(melee: melee);
                }
            }
            else if (state.AttackMode == AttackMode.Ranged)
            {
                if (inRange && target != null)
                {
                    FireProjectile(state, target, fromBurst: _burstHitsRemaining > 0, fromWindupEnd: true);
                }

                ClearWindup();
                if (_burstHitsRemaining > 0)
                {
                    AfterBurstHit(melee: false);
                }
            }
            else
            {
                _session.TryConfirmMeleeHit(_attackerId, _windupTargetId, inRange);
                ClearWindup();
                if (_burstHitsRemaining > 0)
                {
                    AfterBurstHit(melee: true);
                }
            }
        }

        private void TickWindupAgainstAim(
            DefendCombatWarriorState state,
            Transform target,
            float targetBodyRadius)
        {
            var inRange = CombatReach.IsInAttackRange(
                CombatReach.DistanceXZ(transform.position, target.position),
                state.AttackRange,
                _bodyRadius,
                targetBodyRadius,
                CombatReach.HitConfirmSlack);

            _anim?.ReleaseAttackHold();
            if (state.AttackMode == AttackMode.Ranged || state.AttackMode == AttackMode.Parabola)
            {
                if (inRange)
                {
                    FireProjectileAtAim(state, target, _windupTargetId);
                }
            }
            else
            {
                _session.TryConfirmMeleeHit(_attackerId, _windupTargetId, inRange);
            }

            ClearWindup();
            if (_burstHitsRemaining > 0)
            {
                AfterBurstHit(melee: state.AttackMode == AttackMode.Melee);
            }
        }

        private void ClearWindup()
        {
            _attackPhase = AttackPhase.IdleOrMove;
            _windupTargetId = null;
            _scheduler?.SetPaused(_moveId, false);
            _anim?.ReleaseAttackHold();
        }

        private bool ResolveParabolaShot(
            DefendCombatWarriorState state,
            PushMapMonsterAgentView target,
            bool targetInEngageRange)
        {
            var origin = transform.position;
            var aimPoint = target != null ? target.transform.position : origin + transform.forward;
            var aim = aimPoint - origin;
            aim.y = 0f;
            if (aim.sqrMagnitude < 0.0001f)
            {
                aim = transform.forward;
                aim.y = 0f;
            }

            FillParabolaBodies();
            var choice = ParabolaFirePolicy.Choose(
                origin,
                aimPoint,
                targetInEngageRange && target != null,
                state.AttackRange,
                state.ParabolaMeleeRange,
                state.ParabolaArcMinDistance,
                state.ParabolaHitRate,
                CombatRuntimeTuning.ParabolaForwardArcDegrees,
                UnityEngine.Random.value,
                aim,
                _parabolaBodies);

            switch (choice.Kind)
            {
                case ParabolaShotKind.Melee:
                    _session.TryConfirmParabolaMeleeHit(_attackerId, choice.MeleeTargetId, true);
                    return true;
                case ParabolaShotKind.Straight:
                    if (target != null)
                    {
                        FireProjectile(
                            state,
                            target,
                            fromBurst: false,
                            fromWindupEnd: true,
                            stretchShortFlight: true);
                    }

                    return false;
                case ParabolaShotKind.ArcHit:
                case ParabolaShotKind.ArcMiss:
                    if (target != null)
                    {
                        FireParabolaArc(state, target, aim, choice.Kind == ParabolaShotKind.ArcHit);
                    }

                    return false;
                default:
                    return false;
            }
        }

        private void FireParabolaArc(
            DefendCombatWarriorState state,
            PushMapMonsterAgentView target,
            Vector3 aim,
            bool committedHit)
        {
            var flat = aim;
            flat.y = 0f;
            if (flat.sqrMagnitude < 0.0001f)
            {
                flat = Vector3.forward;
            }
            else
            {
                flat.Normalize();
            }

            var landing = committedHit
                ? target.transform.position
                : target.transform.position + flat * state.ParabolaMissOvershoot;
            landing.y = target.transform.position.y;
            var horiz = ParabolaFirePolicy.DistanceXZ(transform.position, landing);
            var peak = Mathf.Max(0.75f, horiz * 0.35f);
            FireProjectile(
                state,
                target,
                fromBurst: false,
                fromWindupEnd: true,
                arc: true,
                arcCommittedHit: committedHit,
                arcLanding: landing,
                arcPeak: peak,
                missLingerSeconds: state.ParabolaMissLingerSeconds);
        }

        private void FillParabolaBodies()
        {
            _parabolaBodies.Clear();
            var monsters = _monstersProvider != null ? _monstersProvider() : null;
            if (monsters != null)
            {
                for (var i = 0; i < monsters.Count; i++)
                {
                    var monster = monsters[i];
                    if (monster == null || !monster.IsAlive || string.IsNullOrEmpty(monster.RuntimeTargetId))
                    {
                        continue;
                    }

                    if (_session != null && !_session.IsMonsterAlive(monster.RuntimeTargetId))
                    {
                        continue;
                    }

                    _parabolaBodies.Add(new ParabolaBody
                    {
                        Id = monster.RuntimeTargetId,
                        Position = monster.transform.position,
                        Friendly = false
                    });
                }
            }

            var allies = _alliesProvider != null ? _alliesProvider() : null;
            if (allies != null)
            {
                for (var i = 0; i < allies.Count; i++)
                {
                    var other = allies[i];
                    if (other == null || string.Equals(other.AttackerId, _attackerId, StringComparison.Ordinal))
                    {
                        continue;
                    }

                    if (other.IsRebel != _isRebel)
                    {
                        continue;
                    }

                    if (_session != null && !_session.IsWarriorCombatActive(other.AttackerId))
                    {
                        continue;
                    }

                    _parabolaBodies.Add(new ParabolaBody
                    {
                        Id = other.AttackerId,
                        Position = other.transform.position,
                        Friendly = true
                    });
                }
            }

            if (!_isRebel)
            {
                var protagonist = _protagonistProvider != null ? _protagonistProvider() : null;
                if (protagonist != null)
                {
                    _parabolaBodies.Add(new ParabolaBody
                    {
                        Id = "Protagonist",
                        Position = protagonist.position,
                        Friendly = true
                    });
                }
            }
        }

        private void FireProjectileAtAim(
            DefendCombatWarriorState state,
            Transform target,
            string targetId)
        {
            if (target == null || string.IsNullOrEmpty(targetId))
            {
                return;
            }

            if (_projectilePrefab == null)
            {
                _session.TryConfirmRangedHit(_attackerId, targetId);
                return;
            }

            var parent = _projectileParent != null ? _projectileParent : transform.parent;
            var go = Instantiate(_projectilePrefab, parent);
            go.name = $"Projectile_{_attackerId}";
            var spawnPos = transform.position + Vector3.up * 1.0f;
            go.transform.position = spawnPos;
            var to = target.position - spawnPos;
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
                targetId,
                ResolveMonsterTransform,
                state.RangedProjectileSpeed,
                state.RangedTimeoutSeconds,
                hitRadius: -1f,
                enumerateAliveTargets: EnumerateAliveMonsterRuntimeIds);
            TryApplyProjectileVisual(view, state.BaseClass);
        }

        private void FireProjectile(
            DefendCombatWarriorState state,
            PushMapMonsterAgentView target,
            bool fromBurst,
            bool fromWindupEnd = false,
            bool arc = false,
            bool arcCommittedHit = false,
            Vector3 arcLanding = default,
            float arcPeak = 1f,
            float missLingerSeconds = 0f,
            float speedOverride = -1f,
            bool stretchShortFlight = false)
        {
            if (!fromBurst && !fromWindupEnd)
            {
                _attackStartCooldown = state.AttackSpeed > 0.01f ? 1f / state.AttackSpeed : 1f;
            }

            if (_projectilePrefab == null)
            {
                Debug.LogWarning($"[PushMapAdvance] {_attackerId} Ranged but Projectile Prefab missing.");
                if (fromBurst)
                {
                    EndBurst();
                }

                return;
            }

            if (!fromWindupEnd)
            {
                _scheduler?.SetPaused(_moveId, true);
                ClearPathingState();

                if (_anim != null)
                {
                    FaceTarget(target.transform.position);
                    _anim.ConfigureSoldierNormalAttackAnims(state.NormalAttackAnims);
                    _anim.PlayAttack(0, state.AttackSpeed);
                }
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
                speedOverride > 0f ? speedOverride : state.RangedProjectileSpeed,
                state.RangedTimeoutSeconds,
                hitRadius: -1f,
                enumerateAliveTargets: EnumerateAliveMonsterRuntimeIds,
                arc: arc,
                arcCommittedHit: arcCommittedHit,
                arcLanding: arcLanding,
                arcPeak: arcPeak,
                missLingerSeconds: missLingerSeconds,
                stretchShortFlight: stretchShortFlight);

            TryApplyProjectileVisual(view, state.BaseClass);

            if (!fromWindupEnd)
            {
                _scheduler?.SetPaused(_moveId, false);
                if (fromBurst)
                {
                    AfterBurstHit(melee: false);
                }
            }
        }

        private void TryApplyProjectileVisual(ProjectileView view, BaseClassKind baseClass)
        {
            if (view == null || _projectileCatalog == null)
            {
                return;
            }

            if (_projectileCatalog.TryGetProjectileSprite(baseClass, out var sprite))
            {
                view.ApplyVisual(sprite);
            }
        }

        private void AfterBurstHit(bool melee)
        {
            _burstHitsRemaining = Mathf.Max(0, _burstHitsRemaining - 1);
            if (_burstHitsRemaining <= 0)
            {
                EndBurst();
                return;
            }

            var recover = 0.05f;
            if (!melee &&
                _session != null &&
                _session.TryGetWarrior(_attackerId, out var state) &&
                state != null)
            {
                recover = Mathf.Max(0.05f, state.MeleeWindupSeconds);
            }

            _attackPhase = AttackPhase.BurstRecover;
            _burstRecoverRemaining = recover;
            _scheduler?.SetPaused(_moveId, true);
            ClearPathingState();
        }

        private void TickBurstRecover()
        {
            _burstRecoverRemaining -= Time.deltaTime;
            if (_burstRecoverRemaining > 0f)
            {
                return;
            }

            _attackPhase = AttackPhase.IdleOrMove;
            _scheduler?.SetPaused(_moveId, false);
        }

        private void EndBurst()
        {
            _burstHitsRemaining = 0;
            _burstRecoverRemaining = 0f;
            if (_attackPhase == AttackPhase.BurstRecover)
            {
                _attackPhase = AttackPhase.IdleOrMove;
            }

            _scheduler?.SetPaused(_moveId, false);
            if (_session != null &&
                _session.TryGetWarrior(_attackerId, out var state) &&
                state != null)
            {
                _attackStartCooldown = state.AttackSpeed > 0.01f ? 1f / state.AttackSpeed : 1f;
            }

            TrySyncRebelFromSession();
        }

        private void ClearBurst()
        {
            _burstHitsRemaining = 0;
            _burstRecoverRemaining = 0f;
            if (_attackPhase == AttackPhase.BurstRecover)
            {
                _attackPhase = AttackPhase.IdleOrMove;
            }

            _scheduler?.SetPaused(_moveId, false);
        }

        private bool TrySyncRebelFromSession()
        {
            if (_isRebel)
            {
                return true;
            }

            if (_session == null ||
                !_session.TryGetWarrior(_attackerId, out var state) ||
                state == null ||
                !state.IsRebel)
            {
                return false;
            }

            if (IsSkillBurstActive || _attackPhase == AttackPhase.Windup)
            {
                return false;
            }

            SetRebel(true);
            return true;
        }

        private string FormatSkillCdSuffix()
        {
            if (IsSkillBurstActive)
            {
                return " ??";
            }

            if (_session != null &&
                _session.TryGetSkillCooldownRemaining(_attackerId, out var remaining) &&
                remaining > 0.05f)
            {
                return $" CD:{remaining:0}";
            }

            return null;
        }

        private void FaceTarget(Vector3 targetPos)
        {
            var toTarget = targetPos - transform.position;
            toTarget.y = 0f;
            if (toTarget.sqrMagnitude > 0.0001f)
            {
                _anim.ForceSetFacing(toTarget);
            }
        }

        /// <summary>ProjectileView target resolver (PM-12 shared contract).</summary>
        private Transform ResolveMonsterTransform(string runtimeId)
        {
            var m = FindMonsterByRuntimeId(runtimeId);
            if (m != null)
            {
                return m.transform;
            }

            if (_cocObstacleAim != null &&
                _cocObstacleAim(runtimeId, out var obstacle, out _) &&
                obstacle != null)
            {
                return obstacle;
            }

            return null;
        }

        /// <summary>Generic pierce scan: alive monster RuntimeIds (SE-07).</summary>
        private IReadOnlyList<string> EnumerateAliveMonsterRuntimeIds()
        {
            _aliveMonsterIdsScratch.Clear();
            var list = _monstersProvider != null ? _monstersProvider() : null;
            if (list == null)
            {
                return _aliveMonsterIdsScratch;
            }

            for (var i = 0; i < list.Count; i++)
            {
                var m = list[i];
                if (m == null || string.IsNullOrEmpty(m.RuntimeTargetId))
                {
                    continue;
                }

                if (_session != null && !_session.IsMonsterTargetable(m.RuntimeTargetId))
                {
                    continue;
                }

                _aliveMonsterIdsScratch.Add(m.RuntimeTargetId);
            }

            return _aliveMonsterIdsScratch;
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
        /// Movement: IsRun from steer, DirIndex from LastDesired; attack facing is snapped
        /// once at PlayAttack (SPEC_04 ?15.5 v0.83.31).
        /// </summary>
        private void TickAnimPresentation()
        {
            if (_anim == null)
            {
                return;
            }

            CacheDesiredDirFromScheduler();

            if (_isRebel)
            {
                _anim.SetMoving(false);
                return;
            }

            var inWindup = _attackPhase == AttackPhase.Windup ||
                           _attackPhase == AttackPhase.BurstRecover;
            // MassMove uses Move()+ResetPath ??velocity??; use steer like monsters (SPEC_04 ?15.5).
            var wantsMove = !inWindup && _lastSteerDirXZ.sqrMagnitude > MoveAnimSpeedSqr;
            var moving = wantsMove && !_stuckHold.IsHolding;
            _anim.SetMoving(
                moving,
                ResolveMoveTargetDistanceXZ(),
                locomotionPlaybackRate: WarriorAnimView.ResolveMovePlaybackRate(
                    ResolveEffectiveMoveSpeed(),
                    WarriorAnimView.SoldierMoveAnimReferenceSpeed));

            if (_formationPresentationFacing)
            {
                _anim.SetFacing(_formationPresentationFacingXZ);
            }
            else if (moving)
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

            _anim.SetFacing(_lastDesiredDirXZ);
        }

        /// <summary>SPEC_04 ?15.5: distance for attack?run interrupt gate (Objective / missing ??+??.</summary>
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
                if (IsMonsterEngageable(m) && m.RuntimeTargetId == targetId)
                {
                    target = m;
                    return true;
                }
            }

            return false;
        }

        private bool IsMonsterEngageable(PushMapMonsterAgentView m)
        {
            if (m == null || !m.IsAlive || string.IsNullOrEmpty(m.RuntimeTargetId))
            {
                return false;
            }

            return _session == null || _session.IsMonsterTargetable(m.RuntimeTargetId);
        }

        private void LateUpdate()
        {
            if (_isRebel || _agent == null || _scheduler == null || !IsCombatActive)
            {
                _lastSteerDirXZ = Vector3.zero;
                _stuckHold.Tick(false, transform.position, Time.deltaTime);
                TickChaseStuckRetarget();
                return;
            }

            CacheDesiredDirFromScheduler();

            if (_session != null && !_session.IsCombatGameplayActive)
            {
                _lastSteerDirXZ = Vector3.zero;
                _stuckHold.Tick(false, transform.position, Time.deltaTime);
                TickChaseStuckRetarget();
                return;
            }

            if (_attackPhase == AttackPhase.Windup)
            {
                _lastSteerDirXZ = Vector3.zero;
                _stuckHold.Tick(false, transform.position, Time.deltaTime);
                TickChaseStuckRetarget();
                return;
            }

            if (!_agent.isOnNavMesh)
            {
                TryWarpOntoNavMesh();
                if (!_agent.isOnNavMesh)
                {
                    _lastSteerDirXZ = Vector3.zero;
                    _stuckHold.Tick(false, transform.position, Time.deltaTime);
                    TickChaseStuckRetarget();
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
                TickChaseStuckRetarget();
                ClearPathingState();
                return;
            }

            // No SetDestination ??follow scheduler steer (Objective field or AttackSlot).
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
            TickChaseStuckRetarget();
            if (_stuckHold.IsHolding && _anim != null)
            {
                _anim.SetMoving(false);
            }
        }

        /// <summary>
        /// SPEC_03 ?3.12 v0.84.25/26: arm exclude-id when AttackSlot chase is out of range and stuck.
        /// </summary>
        private void TickChaseStuckRetarget()
        {
            if (_cocAttackPriority || _isRebel || _scheduler == null || _moveId == 0 || _attackSlots == null)
            {
                _chaseStuckRetarget.Tick(false, transform.position, null, false, Time.deltaTime);
                return;
            }

            if (!_scheduler.TryGetGoal(_moveId, out var kind, out _) || kind != GoalKind.AttackSlot)
            {
                _chaseStuckRetarget.Tick(false, transform.position, null, false, Time.deltaTime);
                return;
            }

            if (!_attackSlots.TryGetClaimedTargetId(_attackerId, out var claimedId) ||
                string.IsNullOrEmpty(claimedId))
            {
                _chaseStuckRetarget.Tick(false, transform.position, null, false, Time.deltaTime);
                return;
            }

            if (!TryFindMonsterByRuntimeId(
                    _monstersProvider != null ? _monstersProvider() : null,
                    claimedId,
                    out var claimedMonster) ||
                claimedMonster == null)
            {
                _chaseStuckRetarget.Tick(false, transform.position, null, false, Time.deltaTime);
                return;
            }

            var distXZ = CombatReach.DistanceXZ(transform.position, claimedMonster.transform.position);
            var inRange = CombatReach.IsInAttackRange(
                distXZ,
                AttackRange,
                _bodyRadius,
                claimedMonster.BodyRadius);
            if (inRange || _attackPhase == AttackPhase.Windup || _attackPhase == AttackPhase.BurstRecover)
            {
                _chaseStuckRetarget.Tick(false, transform.position, claimedId, false, Time.deltaTime);
                return;
            }

            var hasAlternate = HasEngageAlternateWithFreeSlotExcluding(claimedId);
            _chaseStuckRetarget.Tick(
                eligible: true,
                worldPos: transform.position,
                currentTargetId: claimedId,
                hasAlternateCandidate: hasAlternate,
                dt: Time.deltaTime);
        }

        private bool HasEngageAlternateWithFreeSlotExcluding(string excludeId)
        {
            var list = _monstersProvider != null ? _monstersProvider() : null;
            if (list == null || list.Count == 0 || _attackSlots == null)
            {
                return false;
            }

            var surround = CombatMoveModePolicy.SurroundFor(GoalKind.AttackSlot, _attackMode);
            for (var i = 0; i < list.Count; i++)
            {
                var m = list[i];
                if (m == null || !IsMonsterEngageable(m))
                {
                    continue;
                }

                if (!string.IsNullOrEmpty(excludeId) &&
                    string.Equals(m.RuntimeTargetId, excludeId, StringComparison.Ordinal))
                {
                    continue;
                }

                if (_chaseStuckRetarget.IsExcluded(m.RuntimeTargetId))
                {
                    continue;
                }

                var detect = CombatReach.EngageDetectRadius(
                    m.AttackRange,
                    AttackRange,
                    m.BodyRadius,
                    _bodyRadius,
                    MassMoveScheduler.ArriveEpsilon,
                    m.AlertRadius);
                if (detect <= 0f)
                {
                    continue;
                }

                var d = CombatReach.DistanceXZ(transform.position, m.transform.position);
                if (d > detect)
                {
                    continue;
                }

                if (_attackSlots.HasAvailableSlot(
                        _attackerId,
                        m.RuntimeTargetId,
                        AttackRange,
                        m.transform.position,
                        _attackMode,
                        transform.position,
                        m.BodyRadius,
                        _bodyRadius,
                        surround))
                {
                    return true;
                }
            }

            return false;
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

            if (SpecialMoveNavMesh.SampleWalkable(_agent, transform.position, NavMeshSampleRadius, out var hit))
            {
                _agent.Warp(hit.position);
            }
        }
    }
}
