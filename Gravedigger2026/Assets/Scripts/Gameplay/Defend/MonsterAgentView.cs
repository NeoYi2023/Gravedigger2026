using System;
using System.Collections.Generic;
using Gravedigger2026.Core.Config;
using Gravedigger2026.Core.Defend;
using Gravedigger2026.Core.Pathing;
using Gravedigger2026.Gameplay.Combat;
using UnityEngine;
using UnityEngine.AI;

namespace Gravedigger2026.Gameplay.Defend
{
    /// <summary>
    /// Monster movement + normal-attack View (D-040 / MP-06).
    /// Chase destination = AttackSlot via MassMoveScheduler — no center SetDestination / CalculatePath.
    /// Presentation: WarriorAnimView DirIndex / IsRun / Attack1 (SPEC_04 §15.5 Approach A).
    /// StuckHoldTracker (v0.75.30) forces Idle for 1s when blocked — presentation only.
    /// </summary>
    public sealed class MonsterAgentView : MonoBehaviour
    {
        private const float NavMeshSampleRadius = 4f;
        private const float MoveAnimSpeedSqr = 0.01f;

        private DefendSessionService _session;
        private MonsterConfigRow _config;
        private string _runtimeId;
        private Transform _protagonist;
        private Func<IReadOnlyList<WarriorAgentView>> _warriorsProvider;
        private float _retargetInterval = 1f;
        private float _retargetTimer;
        private float _attackCooldown;
        private NavMeshAgent _agent;
        private WarriorAnimView _anim;
        private Vector3 _lastSteerDirXZ;
        private readonly StuckHoldTracker _stuckHold = new StuckHoldTracker();
        private bool _alive = true;
        private bool _probeOnly;
        private bool _deathKnockActive;
        private Vector3 _deathKnockOrigin;
        private Vector3 _deathKnockTarget;
        private float _deathKnockStartedAt;

        private MassMoveScheduler _scheduler;
        private AttackSlotService _attackSlots;
        private int _moveId;
        private string _attackerId;

        public string MonsterId => _config != null ? _config.MonsterId : string.Empty;
        public string RuntimeId => _runtimeId;
        public string RuntimeTargetId => _attackerId;
        public bool IsAlive => _alive;
        public int MoveId => _moveId;
        public float AttackRange => _config != null ? _config.AttackRange : 0f;

        public float BodyRadius =>
            _config != null ? Mathf.Max(0.05f, _config.BodyRadius) : AttackSlotService.DefaultTargetBodyRadius;

        public AttackMode AttackMode =>
            _config != null && _config.AttackMode == AttackMode.Ranged
                ? AttackMode.Ranged
                : AttackMode.Melee;

        /// <summary>
        /// PM-05 shim: presence-probe placeholder for PushMap (no DefendSessionService wired).
        /// Update/Retarget stay inert; IsAlive is driven via SyncAliveFrom by the real agent view.
        /// </summary>
        public void BindProbeOnly()
        {
            _probeOnly = true;
            _session = null;
            _config = null;
            _runtimeId = string.Empty;
            _protagonist = null;
            _warriorsProvider = null;
            _scheduler = null;
            _attackSlots = null;
            _moveId = 0;
            _attackerId = string.Empty;
            _alive = true;
        }

        /// <summary>Probe-shim liveness mirror (called by the owning PushMap agent view).</summary>
        public void SyncAliveFrom(bool alive)
        {
            if (_probeOnly)
            {
                _alive = alive;
            }
        }

        public void Bind(
            DefendSessionService session,
            string runtimeId,
            MonsterConfigRow config,
            Transform protagonist,
            Func<IReadOnlyList<WarriorAgentView>> warriorsProvider,
            float retargetIntervalSeconds,
            MassMoveScheduler scheduler = null,
            AttackSlotService attackSlots = null,
            int moveId = 0)
        {
            _probeOnly = false;
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _runtimeId = runtimeId ?? throw new ArgumentNullException(nameof(runtimeId));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _protagonist = protagonist;
            _warriorsProvider = warriorsProvider;
            _retargetInterval = Mathf.Max(0.1f, retargetIntervalSeconds);
            _retargetTimer = 0f;
            _attackCooldown = 0f;
            _alive = true;
            _deathKnockActive = false;
            _scheduler = scheduler;
            _attackSlots = attackSlots;
            _moveId = moveId;
            _attackerId = runtimeId;

            _agent = GetComponent<NavMeshAgent>();
            if (_agent == null)
            {
                _agent = gameObject.AddComponent<NavMeshAgent>();
            }

            _agent.enabled = true;

            _agent.speed = Mathf.Max(0.1f, config.MoveSpeed);
            _agent.stoppingDistance = 0f;
            _agent.angularSpeed = 720f;
            _agent.acceleration = 24f;
            // Edge-gap AttackRange (v0.75.25): soft-collision contact is already in reach.
            _agent.radius = BodyRadius;
            _agent.height = 1.8f;
            _agent.autoBraking = false;
            _agent.updateRotation = false;
            _agent.obstacleAvoidanceType = ObstacleAvoidanceType.NoObstacleAvoidance;

            if (!_agent.isOnNavMesh &&
                NavMesh.SamplePosition(transform.position, out var hit, NavMeshSampleRadius, NavMesh.AllAreas))
            {
                _agent.Warp(hit.position);
            }

            if (_scheduler != null && _moveId != 0)
            {
                _scheduler.Register(
                    _moveId,
                    _agent.radius,
                    MassMoveScheduler.DetourGroupMonster,
                    Mathf.Max(0f, _config.PushCoefficient),
                    Mathf.Max(0f, _config.RepulsionScale));
                _scheduler.SetGoal(_moveId, GoalKind.AttackSlot);
                _scheduler.SetPaused(_moveId, true);
            }

            EnsureAnim();
            _lastSteerDirXZ = Vector3.zero;
            _stuckHold.Reset();
            _anim.SetFacingYawFlip(_config != null && _config.FacingYawFlip == 1);
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

        /// <summary>
        /// Combat death presentation (SPEC_04 §15.5): PlayDie + corpse latch; optional mirror knockback.
        /// </summary>
        public void NotifyKilled(
            Vector3? killerWorldPos = null,
            float deathKnockbackMult = ClassConfigRow.DefaultDeathKnockbackMult)
        {
            if (!_alive)
            {
                return;
            }

            _alive = false;
            _stuckHold.Reset();
            ReleaseSlotClaim();
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
            _anim.PlayDie();

            _deathKnockActive = false;
            if (killerWorldPos.HasValue)
            {
                _deathKnockOrigin = transform.position;
                _deathKnockTarget = MonsterDeathPresentation.MirrorKnockbackTarget(
                    _deathKnockOrigin,
                    killerWorldPos.Value,
                    deathKnockbackMult);
                _deathKnockStartedAt = Time.time;
                _deathKnockActive = true;
            }
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

        /// <summary>XZ sample for MassMoveScheduler.</summary>
        public MassMoveSample BuildSample()
        {
            var pos = transform.position;
            return new MassMoveSample(
                _moveId,
                new Vector2(pos.x, pos.z),
                _agent != null ? _agent.radius : BodyRadius,
                active: _alive && !_probeOnly && isActiveAndEnabled);
        }

        /// <summary>
        /// Budgeted chase goal refresh (Stage ≤50/frame). Returns true if chasing with a resolved target.
        /// </summary>
        public bool TryRefreshChaseGoal(AttackSlotService slots, MassMoveScheduler scheduler)
        {
            if (_probeOnly || !_alive || _config == null || scheduler == null || _moveId == 0)
            {
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

            var targetId = warriorView != null ? warriorView.AttackerId : "Protagonist";
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
            if (_probeOnly)
            {
                return;
            }

            TickDeathKnockback();

            if (!_alive || _session == null || !_session.IsActive || _session.Phase != DefendPhase.Combat
                || _config == null)
            {
                return;
            }

            if (!_session.IsMonsterAlive(_runtimeId))
            {
                NotifyKilled();
                return;
            }

            // TargetRetargetInterval: attack cadence / TargetSelect window; slot goals Stage-budgeted.
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

            _scheduler?.SetPaused(_moveId, true);
            StopMovement();

            _attackCooldown -= Time.deltaTime;
            if (_attackCooldown > 0f)
            {
                return;
            }

            FaceToward(targetTf.position);

            if (targetKind == TargetKind.Protagonist)
            {
                _session.ApplyProtagonistNormalHit($"Monster:{_config.MonsterId}");
            }
            else if (warriorView != null)
            {
                _session.TryApplyMonsterDamageToWarrior(
                    _runtimeId,
                    warriorView.WarriorId,
                    _config.AttackPower);
            }

            _anim?.PlayAttack();

            var interval = _config.AttackSpeed > 0.01f ? 1f / _config.AttackSpeed : 1f;
            _attackCooldown = Mathf.Max(0.2f, interval);
        }

        private void LateUpdate()
        {
            _lastSteerDirXZ = Vector3.zero;

            if (!_probeOnly && _alive && _agent != null && _scheduler != null && _moveId != 0 &&
                _config != null)
            {
                if (!_agent.isOnNavMesh)
                {
                    if (NavMesh.SamplePosition(
                            transform.position, out var hit, NavMeshSampleRadius, NavMesh.AllAreas))
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
                        var speed = Mathf.Max(0.1f, _config.MoveSpeed);
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

            if (!_probeOnly)
            {
                TickStuckHoldAndAnim();
            }
        }

        /// <summary>
        /// StuckHoldTracker (SPEC_04 §15.5 v0.75.30): wantsMove for 0.5s with XZ disp &lt;0.2
        /// → force Idle 1s. Attack-range Idle takes priority over hold.
        /// </summary>
        private void TickStuckHoldAndAnim()
        {
            var inAttackRange = false;
            Vector3 attackTargetPos = default;
            if (TryGetCombatTargetPosition(out attackTargetPos, out var body) && _config != null)
            {
                inAttackRange = CombatReach.IsInAttackRange(
                    Vector3.Distance(transform.position, attackTargetPos),
                    _config.AttackRange,
                    BodyRadius,
                    body);
            }

            var wantsMove = _alive &&
                            !inAttackRange &&
                            _lastSteerDirXZ.sqrMagnitude > MoveAnimSpeedSqr;
            _stuckHold.Tick(wantsMove, transform.position, Time.deltaTime);
            TickAnimPresentation(inAttackRange, attackTargetPos);
        }

        /// <summary>
        /// In AttackRange → face target + idle; stuck hold → idle + face chase target;
        /// else chase → IsRun + DirIndex from steer (SPEC_04 §15.5).
        /// </summary>
        private void TickAnimPresentation(bool inAttackRange, Vector3 attackTargetPos)
        {
            if (_anim == null || !_alive || _config == null)
            {
                return;
            }

            if (inAttackRange)
            {
                _anim.SetMoving(false);
                FaceToward(attackTargetPos);
                return;
            }

            if (_stuckHold.IsHolding)
            {
                _anim.SetMoving(false);
                if (TryGetCombatTargetPosition(out var stuckTargetPos, out _))
                {
                    FaceToward(stuckTargetPos);
                }

                return;
            }

            var moving = _lastSteerDirXZ.sqrMagnitude > MoveAnimSpeedSqr;
            _anim.SetMoving(moving);
            if (moving)
            {
                _anim.SetFacing(_lastSteerDirXZ);
            }
        }

        private bool TryGetCombatTargetPosition(out Vector3 targetPos, out float targetBodyRadius)
        {
            targetPos = default;
            targetBodyRadius = AttackSlotService.DefaultTargetBodyRadius;
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
            targetBodyRadius = warriorView != null
                ? warriorView.AgentRadius
                : AttackSlotService.DefaultTargetBodyRadius;
            return true;
        }

        private void FaceToward(Vector3 worldPos)
        {
            if (_anim == null)
            {
                return;
            }

            var to = worldPos - transform.position;
            to.y = 0f;
            if (to.sqrMagnitude > 0.0001f)
            {
                _anim.SetFacing(to);
            }
        }

        private void OnDisable()
        {
            if (_probeOnly)
            {
                return;
            }

            ReleaseSlotClaim();
            if (_scheduler != null && _moveId != 0)
            {
                _scheduler.Unregister(_moveId);
            }
        }

        private void ReleaseSlotClaim(AttackSlotService slots = null)
        {
            var svc = slots ?? _attackSlots;
            if (svc != null && !string.IsNullOrEmpty(_attackerId))
            {
                svc.Release(_attackerId);
            }
        }

        private void StopMovement()
        {
            if (_agent == null || !_agent.isOnNavMesh)
            {
                return;
            }

            if (_agent.hasPath)
            {
                _agent.ResetPath();
            }

            _agent.isStopped = true;
            _agent.velocity = Vector3.zero;
        }

        private enum TargetKind
        {
            None = 0,
            Protagonist = 1,
            Warrior = 2
        }

        private TargetKind ResolveTarget(out WarriorAgentView warrior, out Transform protagonist)
        {
            warrior = null;
            protagonist = _protagonist;

            if (_config == null)
            {
                return protagonist != null ? TargetKind.Protagonist : TargetKind.None;
            }

            switch (_config.TargetSelect)
            {
                case TargetSelect.PreferProtagonist:
                    if (protagonist != null)
                    {
                        return TargetKind.Protagonist;
                    }

                    warrior = NearestActiveWarriorOrNull();
                    return warrior != null ? TargetKind.Warrior : TargetKind.None;

                case TargetSelect.PreferWarrior:
                    warrior = NearestActiveWarriorOrNull();
                    if (warrior != null)
                    {
                        return TargetKind.Warrior;
                    }

                    return protagonist != null ? TargetKind.Protagonist : TargetKind.None;

                default:
                    return NearestAny(out warrior, out protagonist);
            }
        }

        private TargetKind NearestAny(out WarriorAgentView warrior, out Transform protagonist)
        {
            warrior = null;
            protagonist = _protagonist;
            var bestDist = float.MaxValue;
            var kind = TargetKind.None;

            if (protagonist != null)
            {
                bestDist = Vector3.Distance(transform.position, protagonist.position);
                kind = TargetKind.Protagonist;
            }

            var nearestWarrior = NearestActiveWarriorOrNull();
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

        private WarriorAgentView NearestActiveWarriorOrNull()
        {
            var list = _warriorsProvider != null ? _warriorsProvider() : null;
            if (list == null || list.Count == 0 || _session == null)
            {
                return null;
            }

            WarriorAgentView best = null;
            var bestDist = float.MaxValue;
            for (var i = 0; i < list.Count; i++)
            {
                var w = list[i];
                if (w == null || string.IsNullOrEmpty(w.WarriorId))
                {
                    continue;
                }

                if (!_session.IsWarriorCombatActive(w.WarriorId))
                {
                    continue;
                }

                var d = Vector3.Distance(transform.position, w.transform.position);
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
