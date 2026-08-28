using System;
using System.Collections.Generic;
using Gravedigger2026.Core.Config;
using Gravedigger2026.Core.Defend;
using Gravedigger2026.Core.Pathing;
using Gravedigger2026.Gameplay.Combat;
using Gravedigger2026.Gameplay.Pathing;
using UnityEngine;
using UnityEngine.AI;

namespace Gravedigger2026.Gameplay.Defend
{
    /// <summary>
    /// Soldier combat View (D-042 / D-043) + MassCombatPathing MP-06.
    /// Move via MassMoveScheduler (AttackSlot / FormationHome + LocalDetour); no center SetDestination.
    /// </summary>
    public sealed class WarriorAgentView : MonoBehaviour
    {
        private enum AttackPhase
        {
            IdleOrMove = 0,
            Windup = 1
        }

        private enum RebelTargetKind
        {
            None = 0,
            Protagonist = 1,
            Warrior = 2,
            Monster = 3
        }

        private const float MoveAnimSpeedSqr = 0.01f;
        private const float NavMeshSampleRadius = 4f;

        private DefendSessionService _session;
        private string _warriorId;
        private EngageZone _engageZone;
        private Func<IReadOnlyList<MonsterAgentView>> _monstersProvider;
        private Func<IReadOnlyList<WarriorAgentView>> _warriorsProvider;
        private Func<Transform> _protagonistProvider;
        private GameObject _projectilePrefab;
        private Transform _projectileParent;
        private DefendPrefabCatalog _projectileCatalog;
        private Vector3 _formationHome;
        private bool _hasFormationHome;
        private float _retargetInterval = 1f;
        private float _attackStartCooldown;
        private float _windupRemaining;
        private AttackPhase _attackPhase = AttackPhase.IdleOrMove;
        private RebelTargetKind _windupKind;
        private string _windupTargetId;
        private NavMeshAgent _agent;
        private WarriorAnimView _anim;
        private bool _diePlayed;
        private float _baseMoveSpeed = 3.5f;
        private float _chaseMoveSpeedMult = ClassConfigRow.DefaultChaseMoveSpeedMult;
        private float _bodyRadius = BodyAppearanceConfigRow.DefaultBodyRadius;
        private float _pushCoefficient = BodyAppearanceConfigRow.DefaultPushCoefficient;
        private float _repulsionScale = BodyAppearanceConfigRow.DefaultRepulsionScale;
        private bool _facingYawFlip;

        private MassMoveScheduler _scheduler;
        private AttackSlotService _attackSlots;
        private int _moveId;
        /// <summary>Last MassMove steer XZ (LateUpdate); drives IsRun — not NavMeshAgent.velocity (SPEC_04 §15.5).</summary>
        private Vector3 _lastSteerDirXZ;
        /// <summary>Last MassMove pre-detour desired; drives DirIndex while moving (SPEC_04 §15.5 v0.83.31).</summary>
        private Vector3 _lastDesiredDirXZ;
        private readonly StuckHoldTracker _stuckHold = new StuckHoldTracker();
        private AllyFootCircleView _footCircle;

        public string WarriorId => _warriorId;
        public string AttackerId => _warriorId;
        public int MoveId => _moveId;
        public float AgentRadius => _bodyRadius;
        public Vector3 FormationHome => _formationHome;
        public bool HasFormationHome => _hasFormationHome;
        /// <summary>From DefendGameplayConfig; Stage slot refresh is budgeted ≤50/frame (SPEC_04 §9.7).</summary>
        public float TargetRetargetInterval => _retargetInterval;

        public float AttackRange
        {
            get
            {
                if (_session != null &&
                    !string.IsNullOrEmpty(_warriorId) &&
                    _session.TryGetWarrior(_warriorId, out var state) &&
                    state != null)
                {
                    return state.AttackRange;
                }

                return 1f;
            }
        }

        public AttackMode AttackMode
        {
            get
            {
                if (_session != null &&
                    !string.IsNullOrEmpty(_warriorId) &&
                    _session.TryGetWarrior(_warriorId, out var state) &&
                    state != null)
                {
                    return state.AttackMode;
                }

                return AttackMode.Melee;
            }
        }

        public bool IsRebel
        {
            get
            {
                return _session != null &&
                       !string.IsNullOrEmpty(_warriorId) &&
                       _session.TryGetWarrior(_warriorId, out var state) &&
                       state != null &&
                       state.IsRebel;
            }
        }

        public void Bind(
            DefendSessionService session,
            string warriorId,
            EngageZone engageZone,
            Func<IReadOnlyList<MonsterAgentView>> monstersProvider,
            float retargetIntervalSeconds,
            GameObject projectilePrefab = null,
            Transform projectileParent = null,
            DefendPrefabCatalog projectileCatalog = null,
            Func<IReadOnlyList<WarriorAgentView>> warriorsProvider = null,
            Func<Transform> protagonistProvider = null,
            Vector3? formationHomeWorld = null,
            MassMoveScheduler scheduler = null,
            AttackSlotService attackSlots = null,
            int moveId = 0,
            float bodyRadius = BodyAppearanceConfigRow.DefaultBodyRadius,
            bool facingYawFlip = false,
            float pushCoefficient = BodyAppearanceConfigRow.DefaultPushCoefficient,
            float repulsionScale = BodyAppearanceConfigRow.DefaultRepulsionScale,
            float chaseMoveSpeedMult = ClassConfigRow.DefaultChaseMoveSpeedMult)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _warriorId = warriorId ?? throw new ArgumentNullException(nameof(warriorId));
            _engageZone = engageZone;
            _monstersProvider = monstersProvider ?? throw new ArgumentNullException(nameof(monstersProvider));
            _warriorsProvider = warriorsProvider;
            _protagonistProvider = protagonistProvider;
            _projectilePrefab = projectilePrefab;
            _projectileParent = projectileParent;
            _projectileCatalog = projectileCatalog;
            _hasFormationHome = formationHomeWorld.HasValue;
            _formationHome = formationHomeWorld ?? Vector3.zero;
            _retargetInterval = Mathf.Max(0.1f, retargetIntervalSeconds);
            _attackStartCooldown = 0f;
            _windupRemaining = 0f;
            _attackPhase = AttackPhase.IdleOrMove;
            _windupKind = RebelTargetKind.None;
            _windupTargetId = null;
            _diePlayed = false;
            _stuckHold.Reset();
            _lastSteerDirXZ = Vector3.zero;
            _lastDesiredDirXZ = Vector3.zero;
            _scheduler = scheduler;
            _attackSlots = attackSlots;
            _moveId = moveId;
            _bodyRadius = Mathf.Max(0.05f, bodyRadius);
            _pushCoefficient = Mathf.Max(0f, pushCoefficient);
            _repulsionScale = Mathf.Max(0f, repulsionScale);
            _facingYawFlip = facingYawFlip;
            _chaseMoveSpeedMult = Mathf.Max(0f, chaseMoveSpeedMult);

            if (!_session.TryGetWarrior(_warriorId, out var state) || state == null)
            {
                Debug.LogError($"[WarriorAgent] Missing combat state for '{_warriorId}'.");
                enabled = false;
                return;
            }

            _baseMoveSpeed = Mathf.Max(0.1f, state.MoveSpeed);

            _agent = GetComponent<NavMeshAgent>();
            if (_agent == null)
            {
                _agent = gameObject.AddComponent<NavMeshAgent>();
            }

            _anim = GetComponent<WarriorAnimView>();
            if (_anim == null)
            {
                _anim = gameObject.AddComponent<WarriorAnimView>();
            }

            _anim.SetFacingYawFlip(_facingYawFlip);
            ApplyEffectiveMoveSpeed();
            _agent.stoppingDistance = 0f;
            _agent.angularSpeed = 720f;
            _agent.acceleration = 24f;
            _agent.radius = _bodyRadius;
            _agent.height = 0.1f;
            _agent.autoBraking = false;
            // SPEC_04 §15.2: facing via Animator DirIndex; do not yaw the Visual sprite.
            _agent.updateRotation = false;
            _agent.obstacleAvoidanceType = ObstacleAvoidanceType.NoObstacleAvoidance;

            _anim.ResetToIdle();

            if (!_agent.isOnNavMesh &&
                NavMesh.SamplePosition(transform.position, out var hit, NavMeshSampleRadius, NavMesh.AllAreas))
            {
                _agent.Warp(hit.position);
            }

            if (_scheduler != null && _moveId != 0)
            {
                _scheduler.Register(
                    _moveId,
                    _bodyRadius,
                    MassMoveScheduler.DetourGroupLoyal,
                    _pushCoefficient,
                    _repulsionScale);
                if (_hasFormationHome)
                {
                    _scheduler.SetGoal(
                        _moveId,
                        GoalKind.FormationHome,
                        new Vector2(_formationHome.x, _formationHome.z));
                }

                var taskLabel = GetComponent<WarriorTaskDebugLabelView>();
                if (taskLabel == null)
                {
                    taskLabel = gameObject.AddComponent<WarriorTaskDebugLabelView>();
                }

                taskLabel.Bind(_scheduler, _moveId);
            }

            EnsureFootCircle();
            _footCircle.Bind(_bodyRadius);
            _footCircle.SetVisible(!state.IsRebel);
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

        private float ResolveEffectiveMoveSpeed()
        {
            var mult = 1f;
            if (_scheduler != null &&
                _moveId != 0 &&
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

        /// <summary>XZ sample for MassMoveScheduler (inactive when dead / windup).</summary>
        public MassMoveSample BuildSample()
        {
            var pos = transform.position;
            var combatActive = _session != null &&
                               !string.IsNullOrEmpty(_warriorId) &&
                               _session.IsWarriorCombatActive(_warriorId);
            var active = combatActive &&
                         isActiveAndEnabled &&
                         _attackPhase != AttackPhase.Windup;
            return new MassMoveSample(
                _moveId,
                new Vector2(pos.x, pos.z),
                _bodyRadius,
                active);
        }

        /// <summary>Nearest living monster inside EngageZone (loyal targeting).</summary>
        public MonsterAgentView FindNearestEngageMonster()
        {
            var list = _monstersProvider != null ? _monstersProvider() : null;
            if (list == null || list.Count == 0)
            {
                return null;
            }

            MonsterAgentView best = null;
            var bestDist = float.MaxValue;
            for (var i = 0; i < list.Count; i++)
            {
                var m = list[i];
                if (m == null || !m.IsAlive || string.IsNullOrEmpty(m.RuntimeId))
                {
                    continue;
                }

                if (_engageZone != null && !_engageZone.ContainsXZ(m.transform.position))
                {
                    continue;
                }

                if (_session != null && !_session.IsMonsterAlive(m.RuntimeId))
                {
                    continue;
                }

                var d = Vector3.Distance(transform.position, m.transform.position);
                if (d < bestDist)
                {
                    bestDist = d;
                    best = m;
                }
            }

            return best;
        }

        /// <summary>
        /// Rebel nearest target among protagonist / other soldiers / monsters (no EngageZone).
        /// </summary>
        public bool TryFindNearestRebelTarget(out string targetId, out Vector3 position, out float bodyRadius)
        {
            targetId = null;
            position = default;
            bodyRadius = 0.35f;
            if (!TryResolveNearestRebelTarget(out var kind, out targetId, out position))
            {
                return false;
            }

            bodyRadius = ResolveRebelTargetBodyRadius(kind, targetId);
            return true;
        }

        private float ResolveRebelTargetBodyRadius(RebelTargetKind kind, string targetId)
        {
            switch (kind)
            {
                case RebelTargetKind.Warrior:
                {
                    var other = FindWarriorById(targetId);
                    return other != null ? other.AgentRadius : _bodyRadius;
                }
                case RebelTargetKind.Monster:
                {
                    var m = FindMonsterByRuntimeId(targetId);
                    return m != null ? m.BodyRadius : AttackSlotService.DefaultTargetBodyRadius;
                }
                case RebelTargetKind.Protagonist:
                default:
                    return AttackSlotService.DefaultTargetBodyRadius;
            }
        }

        private void Update()
        {
            if (_session == null || string.IsNullOrEmpty(_warriorId))
            {
                return;
            }

            if (!_session.IsActive || _session.Phase != DefendPhase.Combat)
            {
                return;
            }

            if (!_session.IsWarriorCombatActive(_warriorId) || !_session.TryGetWarrior(_warriorId, out var state))
            {
                HideFootCircle();
                PlayDieOnce();
                StopAgent();
                return;
            }

            TickAnimPresentation();

            if (state.IsRebel)
            {
                HideFootCircle();
                TickRebel(state);
            }
            else if (state.AttackMode == AttackMode.Melee)
            {
                TickLoyalMelee(state);
            }
            else
            {
                TickLoyalRanged(state);
            }
        }

        private void LateUpdate()
        {
            if (_agent == null || _scheduler == null || _moveId == 0)
            {
                return;
            }

            CacheDesiredDirFromScheduler();

            if (_session == null ||
                !_session.IsActive ||
                _session.Phase != DefendPhase.Combat ||
                string.IsNullOrEmpty(_warriorId) ||
                !_session.IsWarriorCombatActive(_warriorId))
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
                if (NavMesh.SamplePosition(transform.position, out var hit, NavMeshSampleRadius, NavMesh.AllAreas))
                {
                    _agent.Warp(hit.position);
                }

                if (!_agent.isOnNavMesh)
                {
                    _lastSteerDirXZ = Vector3.zero;
                    _stuckHold.Tick(false, transform.position, Time.deltaTime);
                    return;
                }
            }

            // SC-03: soft-collision impulse applies even on zero-steer frames (hold separation).
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
            _attackSlots?.Release(_warriorId);
            if (_scheduler != null && _moveId != 0)
            {
                _scheduler.Unregister(_moveId);
            }
        }

        private void TickAnimPresentation()
        {
            if (_anim == null)
            {
                return;
            }

            CacheDesiredDirFromScheduler();

            // MassMove uses Move()+ResetPath — velocity≈0; use steer like monsters (SPEC_04 §15.5).
            var wantsMove = _attackPhase != AttackPhase.Windup
                            && _lastSteerDirXZ.sqrMagnitude > MoveAnimSpeedSqr;
            var moving = wantsMove && !_stuckHold.IsHolding;

            if (_attackPhase != AttackPhase.Windup)
            {
                _anim.SetMoving(moving, ResolveMoveTargetDistanceXZ());
            }

            if (moving)
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

        private void PlayDieOnce()
        {
            if (_diePlayed || _anim == null)
            {
                return;
            }

            _diePlayed = true;
            HideFootCircle();
            _stuckHold.Reset();
            _anim.PlayDie();
        }

        private void TickLoyalMelee(DefendCombatWarriorState state)
        {
            if (_attackPhase == AttackPhase.Windup)
            {
                TickWindupMonster(state);
                return;
            }

            _attackStartCooldown = Mathf.Max(0f, _attackStartCooldown - Time.deltaTime);
            var target = FindNearestEngageMonster();
            if (target == null || _attackStartCooldown > 0f)
            {
                return;
            }

            var dist = CombatReach.DistanceXZ(transform.position, target.transform.position);
            if (!CombatReach.IsInAttackRange(dist, state.AttackRange, _bodyRadius, target.BodyRadius))
            {
                return;
            }

            BeginWindup(RebelTargetKind.Monster, target.RuntimeId, state);
        }

        private void TickLoyalRanged(DefendCombatWarriorState state)
        {
            _attackStartCooldown = Mathf.Max(0f, _attackStartCooldown - Time.deltaTime);
            var target = FindNearestEngageMonster();
            if (target == null || _attackStartCooldown > 0f)
            {
                return;
            }

            var dist = CombatReach.DistanceXZ(transform.position, target.transform.position);
            if (!CombatReach.IsInAttackRange(dist, state.AttackRange, _bodyRadius, target.BodyRadius))
            {
                return;
            }

            FireProjectile(state, target);
        }

        private void TickRebel(DefendCombatWarriorState state)
        {
            if (_attackPhase == AttackPhase.Windup)
            {
                TickWindupRebel(state);
                return;
            }

            _attackStartCooldown = Mathf.Max(0f, _attackStartCooldown - Time.deltaTime);
            if (!TryResolveNearestRebelTarget(out var kind, out var targetId, out var targetPos)
                || _attackStartCooldown > 0f)
            {
                return;
            }

            var targetBody = ResolveRebelTargetBodyRadius(kind, targetId);
            var dist = CombatReach.DistanceXZ(transform.position, targetPos);
            if (!CombatReach.IsInAttackRange(dist, state.AttackRange, _bodyRadius, targetBody))
            {
                return;
            }

            if (kind == RebelTargetKind.Monster && state.AttackMode == AttackMode.Ranged)
            {
                var monster = FindMonsterByRuntimeId(targetId);
                if (monster != null)
                {
                    FireProjectile(state, monster);
                }

                return;
            }

            BeginWindup(kind, targetId, state);
        }

        private void FireProjectile(DefendCombatWarriorState state, MonsterAgentView target)
        {
            if (_projectilePrefab == null)
            {
                Debug.LogWarning($"[WarriorAgent] {_warriorId} Ranged but Projectile Prefab missing.");
                _attackStartCooldown = state.AttackSpeed > 0.01f ? 1f / state.AttackSpeed : 1f;
                return;
            }

            _attackStartCooldown = state.AttackSpeed > 0.01f ? 1f / state.AttackSpeed : 1f;
            _scheduler?.SetPaused(_moveId, true);
            ClearPathingState();

            var toTarget = target.transform.position - transform.position;
            toTarget.y = 0f;
            if (_anim != null)
            {
                if (toTarget.sqrMagnitude > 0.0001f)
                {
                    _anim.ForceSetFacing(toTarget);
                }

                _anim.PlayAttack();
            }

            var parent = _projectileParent != null ? _projectileParent : transform.parent;
            var go = Instantiate(_projectilePrefab, parent);
            go.name = $"Projectile_{_warriorId}";
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
                _warriorId,
                target.RuntimeId,
                ResolveMonsterTransform,
                state.RangedProjectileSpeed,
                state.RangedTimeoutSeconds);

            TryApplyProjectileVisual(view, state.BaseClass);

            _scheduler?.SetPaused(_moveId, false);
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

        private void BeginWindup(RebelTargetKind kind, string targetId, DefendCombatWarriorState state)
        {
            _attackPhase = AttackPhase.Windup;
            _windupKind = kind;
            _windupTargetId = targetId;
            _windupRemaining = Mathf.Max(0f, state.MeleeWindupSeconds);
            _attackStartCooldown = state.AttackSpeed > 0.01f ? 1f / state.AttackSpeed : 1f;
            _scheduler?.SetPaused(_moveId, true);
            ClearPathingState();

            if (_anim != null)
            {
                if (TryResolveWindupAim(kind, targetId, out var aim))
                {
                    var toAim = aim - transform.position;
                    toAim.y = 0f;
                    if (toAim.sqrMagnitude > 0.0001f)
                    {
                        _anim.ForceSetFacing(toAim);
                    }
                }

                _anim.PlayAttack();
            }
        }

        private bool TryResolveWindupAim(RebelTargetKind kind, string targetId, out Vector3 aim)
        {
            aim = default;
            switch (kind)
            {
                case RebelTargetKind.Protagonist:
                {
                    var tf = _protagonistProvider != null ? _protagonistProvider() : null;
                    if (tf == null)
                    {
                        return false;
                    }

                    aim = tf.position;
                    return true;
                }
                case RebelTargetKind.Warrior:
                {
                    var other = FindWarriorById(targetId);
                    if (other == null)
                    {
                        return false;
                    }

                    aim = other.transform.position;
                    return true;
                }
                case RebelTargetKind.Monster:
                {
                    var monster = FindMonsterByRuntimeId(targetId);
                    if (monster == null)
                    {
                        return false;
                    }

                    aim = monster.transform.position;
                    return true;
                }
                default:
                    return false;
            }
        }

        private void TickWindupMonster(DefendCombatWarriorState state)
        {
            _windupRemaining -= Time.deltaTime;
            if (_windupRemaining > 0f)
            {
                return;
            }

            var target = FindMonsterByRuntimeId(_windupTargetId);
            var inRange = target != null
                          && target.IsAlive
                          && CombatReach.IsInAttackRange(
                              CombatReach.DistanceXZ(transform.position, target.transform.position),
                              state.AttackRange,
                              _bodyRadius,
                              target.BodyRadius,
                              CombatReach.HitConfirmSlack);

            _session.TryConfirmMeleeHit(_warriorId, _windupTargetId, inRange);
            ClearWindup();
        }

        private void TickWindupRebel(DefendCombatWarriorState state)
        {
            _windupRemaining -= Time.deltaTime;
            if (_windupRemaining > 0f)
            {
                return;
            }

            switch (_windupKind)
            {
                case RebelTargetKind.Protagonist:
                {
                    var tf = _protagonistProvider != null ? _protagonistProvider() : null;
                    var inRange = tf != null
                                  && CombatReach.IsInAttackRange(
                                      CombatReach.DistanceXZ(transform.position, tf.position),
                                      state.AttackRange,
                                      _bodyRadius,
                                      AttackSlotService.DefaultTargetBodyRadius,
                                      CombatReach.HitConfirmSlack);
                    if (inRange)
                    {
                        _session.ApplyProtagonistNormalHit($"Rebel:{_warriorId}");
                    }

                    break;
                }
                case RebelTargetKind.Warrior:
                {
                    var other = FindWarriorById(_windupTargetId);
                    var inRange = other != null
                                  && CombatReach.IsInAttackRange(
                                      CombatReach.DistanceXZ(transform.position, other.transform.position),
                                      state.AttackRange,
                                      _bodyRadius,
                                      other.AgentRadius,
                                      CombatReach.HitConfirmSlack);
                    _session.TryConfirmRebelHitOnWarrior(_warriorId, _windupTargetId, inRange);
                    break;
                }
                case RebelTargetKind.Monster:
                {
                    var target = FindMonsterByRuntimeId(_windupTargetId);
                    var inRange = target != null
                                  && target.IsAlive
                                  && CombatReach.IsInAttackRange(
                                      CombatReach.DistanceXZ(transform.position, target.transform.position),
                                      state.AttackRange,
                                      _bodyRadius,
                                      target.BodyRadius,
                                      CombatReach.HitConfirmSlack);
                    if (state.AttackMode == AttackMode.Melee)
                    {
                        _session.TryConfirmMeleeHit(_warriorId, _windupTargetId, inRange);
                    }

                    break;
                }
            }

            ClearWindup();
        }

        private void ClearWindup()
        {
            _attackPhase = AttackPhase.IdleOrMove;
            _windupKind = RebelTargetKind.None;
            _windupTargetId = null;
            _scheduler?.SetPaused(_moveId, false);

            if (_anim != null)
            {
                _anim.SetMoving(false);
            }
        }

        private bool TryResolveNearestRebelTarget(out RebelTargetKind kind, out string targetId, out Vector3 position)
        {
            kind = RebelTargetKind.None;
            targetId = null;
            position = default;
            var bestDist = float.MaxValue;

            var protagonist = _protagonistProvider != null ? _protagonistProvider() : null;
            if (protagonist != null)
            {
                var d = Vector3.Distance(transform.position, protagonist.position);
                if (d < bestDist)
                {
                    bestDist = d;
                    kind = RebelTargetKind.Protagonist;
                    targetId = "Protagonist";
                    position = protagonist.position;
                }
            }

            var warriors = _warriorsProvider != null ? _warriorsProvider() : null;
            if (warriors != null)
            {
                for (var i = 0; i < warriors.Count; i++)
                {
                    var w = warriors[i];
                    if (w == null || string.Equals(w.WarriorId, _warriorId, StringComparison.Ordinal))
                    {
                        continue;
                    }

                    if (_session == null || !_session.IsWarriorCombatActive(w.WarriorId))
                    {
                        continue;
                    }

                    var d = Vector3.Distance(transform.position, w.transform.position);
                    if (d < bestDist)
                    {
                        bestDist = d;
                        kind = RebelTargetKind.Warrior;
                        targetId = w.WarriorId;
                        position = w.transform.position;
                    }
                }
            }

            var monsters = _monstersProvider != null ? _monstersProvider() : null;
            if (monsters != null)
            {
                for (var i = 0; i < monsters.Count; i++)
                {
                    var m = monsters[i];
                    if (m == null || !m.IsAlive || string.IsNullOrEmpty(m.RuntimeId))
                    {
                        continue;
                    }

                    if (_session != null && !_session.IsMonsterAlive(m.RuntimeId))
                    {
                        continue;
                    }

                    var d = Vector3.Distance(transform.position, m.transform.position);
                    if (d < bestDist)
                    {
                        bestDist = d;
                        kind = RebelTargetKind.Monster;
                        targetId = m.RuntimeId;
                        position = m.transform.position;
                    }
                }
            }

            return kind != RebelTargetKind.None;
        }

        /// <summary>ProjectileView target resolver (PM-12 shared contract).</summary>
        private Transform ResolveMonsterTransform(string runtimeId)
        {
            var m = FindMonsterByRuntimeId(runtimeId);
            return m != null ? m.transform : null;
        }

        private MonsterAgentView FindMonsterByRuntimeId(string runtimeId)
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
                if (m != null && string.Equals(m.RuntimeId, runtimeId, StringComparison.Ordinal))
                {
                    return m;
                }
            }

            return null;
        }

        private WarriorAgentView FindWarriorById(string warriorId)
        {
            if (string.IsNullOrEmpty(warriorId))
            {
                return null;
            }

            var list = _warriorsProvider != null ? _warriorsProvider() : null;
            if (list == null)
            {
                return null;
            }

            for (var i = 0; i < list.Count; i++)
            {
                var w = list[i];
                if (w != null && string.Equals(w.WarriorId, warriorId, StringComparison.Ordinal))
                {
                    return w;
                }
            }

            return null;
        }

        private void StopAgent()
        {
            _attackSlots?.Release(_warriorId);
            if (_scheduler != null && _moveId != 0)
            {
                _scheduler.Unregister(_moveId);
            }

            ClearPathingState();
            _attackPhase = AttackPhase.IdleOrMove;
            _windupKind = RebelTargetKind.None;
            _windupTargetId = null;
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
    }
}
