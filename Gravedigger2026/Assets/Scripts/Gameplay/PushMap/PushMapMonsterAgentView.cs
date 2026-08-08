using System;
using System.Collections.Generic;
using Gravedigger2026.Core.Config;
using Gravedigger2026.Core.Pathing;
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
    /// v0.75.10: chase facing stabilized (hysteresis + min dwell) + stuck-hold → Idle facing
    /// the chase target (SPEC_04 §15.5); presentation only — attack/slot rules unchanged.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PushMapMonsterAgentView : MonoBehaviour
    {
        private const float MoveAnimSpeedSqr = 0.01f;
        private const float FacingHysteresisDegrees = 12f;
        private const float FacingSwitchMinDwellSeconds = 0.12f;
        private const float StuckWindowSeconds = 0.25f;
        private const float StuckDisplacementEpsilon = 0.05f;

        // DirIndex (0E 1W 2S 3N 4NE 5NW 6SE 7SW) → quantization sector of WarriorAnimView.DirIndexFromXZ.
        private static readonly int[] DirIndexToSector = { 2, 6, 4, 0, 1, 7, 3, 5 };

        private MonsterConfigRow _config;
        private Transform _protagonist;
        private Func<IReadOnlyList<PushMapAdvanceView>> _warriorsProvider;
        private Action<string> _onHitProtagonist;
        private Func<string, string, float, bool> _onHitWarrior;
        private AttackSlotService _attackSlots;
        private MassMoveScheduler _scheduler;
        private float _retargetInterval = 1f;
        private float _retargetTimer;
        private float _attackCooldown;
        private NavMeshAgent _agent;
        private WarriorAnimView _anim;
        private Vector3 _lastSteerDirXZ;
        private int _facingDirIndex = -1;
        private float _facingSwitchTimer;
        private Vector3 _stuckWindowStartPos;
        private float _stuckWindowTimer;
        private bool _stuck;
        private bool _alive = true;
        private bool _provoked;
        private bool _isBoss;
        private int _moveId;
        private string _attackerId;

        public string MonsterId => _config != null ? _config.MonsterId : string.Empty;
        public string RuntimeTargetId => _attackerId;
        public bool IsAlive => _alive;
        public bool IsBoss => _isBoss;
        public int MoveId => _moveId;
        public float AttackRange => _config != null ? _config.AttackRange : 0f;
        public float BodyRadius => _config != null ? Mathf.Max(0.05f, _config.BodyRadius) : 0.35f;
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

        public void Bind(
            MonsterConfigRow config,
            Transform protagonist,
            Func<IReadOnlyList<PushMapAdvanceView>> warriorsProvider,
            Action<string> onHitProtagonist,
            float retargetIntervalSeconds = 1f,
            AttackSlotService attackSlots = null,
            MassMoveScheduler scheduler = null,
            int moveId = 0,
            Func<string, string, float, bool> onHitWarrior = null)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _protagonist = protagonist;
            _warriorsProvider = warriorsProvider;
            _onHitProtagonist = onHitProtagonist;
            _onHitWarrior = onHitWarrior;
            _attackSlots = attackSlots;
            _scheduler = scheduler;
            _moveId = moveId;
            _retargetInterval = Mathf.Max(0.1f, retargetIntervalSeconds);
            _retargetTimer = 0f;
            _attackCooldown = 0f;
            _alive = true;
            _provoked = false;
            _isBoss = false;
            _attackerId = gameObject.name;

            _agent = GetComponent<NavMeshAgent>();
            if (_agent == null)
            {
                _agent = gameObject.AddComponent<NavMeshAgent>();
            }

            _agent.speed = Mathf.Max(0.1f, config.MoveSpeed);
            _agent.stoppingDistance = 0f;
            _agent.angularSpeed = 720f;
            _agent.acceleration = 24f;
            // Combat radius must leave AttackRange reachable vs loyal Demo soldier radius 0.1.
            var maxCombatRadius = Mathf.Max(
                0.05f,
                config.AttackRange - BodyAppearanceConfigRow.DefaultBodyRadius - 0.05f);
            _agent.radius = Mathf.Min(BodyRadius, maxCombatRadius);
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

            if (!IsStationary && _scheduler != null && _moveId != 0)
            {
                _scheduler.Register(_moveId, _agent.radius, MassMoveScheduler.DetourGroupMonster);
                _scheduler.SetGoal(_moveId, GoalKind.AttackSlot);
                _scheduler.SetPaused(_moveId, true);
            }

            if (IsStationary || (IsPassive && !_provoked))
            {
                StopMovement();
            }

            EnsureAnim();
            _lastSteerDirXZ = Vector3.zero;
            _facingDirIndex = -1;
            _facingSwitchTimer = 0f;
            _stuck = false;
            _stuckWindowTimer = 0f;
            _stuckWindowStartPos = transform.position;
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
        }

        /// <summary>Presentation-side kill (PM-07 Boss Demo kill / future warrior damage); deactivates the view.</summary>
        public void NotifyKilled()
        {
            if (!_alive)
            {
                return;
            }

            _alive = false;
            ReleaseSlotClaim();
            // Soldiers claiming this monster as target — Stage also ReleaseAllForTarget.
            _attackSlots?.ReleaseAllForTarget(_attackerId);
            if (_scheduler != null && _moveId != 0)
            {
                _scheduler.Unregister(_moveId);
            }

            StopMovement();
            gameObject.SetActive(false);
        }

        /// <summary>XZ sample for MassMoveScheduler (inactive when dead/stationary/idle-passive).</summary>
        public MassMoveSample BuildSample()
        {
            var pos = transform.position;
            var active = _alive && !IsStationary && isActiveAndEnabled &&
                         (!IsPassive || _provoked);
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
            var dist = Vector3.Distance(transform.position, targetTf.position);

            // In AttackRange: hold and attack (no chase steer).
            if (dist <= _config.AttackRange)
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
                    warriorView != null ? warriorView.AgentRadius : 0.35f,
                    CombatMoveModePolicy.SurroundFor(GoalKind.AttackSlot, AttackMode)))
            {
                // No free walkable slot: still seek a ring-ish offset (not raw center stack).
                var ring = AttackSlotService.ComputeRingRadius(_config.AttackRange);
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
            if (!_alive || _config == null)
            {
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
            if (dist > _config.AttackRange)
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

            FaceToward(targetTf.position);

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

            var interval = _config.AttackSpeed > 0.01f ? 1f / _config.AttackSpeed : 1f;
            _attackCooldown = Mathf.Max(0.2f, interval);
        }

        private void LateUpdate()
        {
            _lastSteerDirXZ = Vector3.zero;

            if (_alive && !IsStationary && _agent != null && _scheduler != null && _moveId != 0)
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
                        var speed = Mathf.Max(0.1f, _config != null ? _config.MoveSpeed : 3f);
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

            TickStuckDetection(_lastSteerDirXZ.sqrMagnitude > 1e-8f);
            TickAnimPresentation();
        }

        /// <summary>
        /// v0.75.10 stuck-hold (SPEC_04 §15.5): steer non-zero but the XZ displacement over a
        /// StuckWindowSeconds window stays under StuckDisplacementEpsilon → stuck (blocked by
        /// soldiers/pack). Exit threshold is 2×epsilon so borderline jostle does not flap.
        /// </summary>
        private void TickStuckDetection(bool wantsMove)
        {
            if (!wantsMove)
            {
                _stuck = false;
                _stuckWindowTimer = 0f;
                _stuckWindowStartPos = transform.position;
                return;
            }

            _stuckWindowTimer += Time.deltaTime;
            if (_stuckWindowTimer < StuckWindowSeconds)
            {
                return;
            }

            var dx = transform.position.x - _stuckWindowStartPos.x;
            var dz = transform.position.z - _stuckWindowStartPos.z;
            var displacementSqr = dx * dx + dz * dz;
            var threshold = _stuck ? 2f * StuckDisplacementEpsilon : StuckDisplacementEpsilon;
            _stuck = displacementSqr < threshold * threshold;
            _stuckWindowTimer = 0f;
            _stuckWindowStartPos = transform.position;
        }

        /// <summary>
        /// In AttackRange → face target + idle; stuck (v0.75.10) → idle + face chase target;
        /// else chase → IsRun + stabilized DirIndex from steer (SPEC_04 §15.5).
        /// </summary>
        private void TickAnimPresentation()
        {
            if (_anim == null || !_alive)
            {
                return;
            }

            if (TryGetCombatTargetPosition(out var targetPos) &&
                Vector3.Distance(transform.position, targetPos) <= _config.AttackRange)
            {
                _anim.SetMoving(false);
                FaceToward(targetPos);
                return;
            }

            if (_stuck)
            {
                _anim.SetMoving(false);
                if (TryGetCombatTargetPosition(out var stuckTargetPos))
                {
                    FaceToward(stuckTargetPos);
                }

                return;
            }

            var moving = _lastSteerDirXZ.sqrMagnitude > MoveAnimSpeedSqr;
            _anim.SetMoving(moving);
            if (moving)
            {
                SetFacingStabilized(_lastSteerDirXZ);
            }
        }

        /// <summary>
        /// v0.75.10 facing stabilization (SPEC_04 §15.5): switch DirIndex only when the raw
        /// steer leaves the current sector by more than FacingHysteresisDegrees and the last
        /// switch was at least FacingSwitchMinDwellSeconds ago.
        /// </summary>
        private void SetFacingStabilized(Vector3 rawDirXZ)
        {
            _facingSwitchTimer += Time.deltaTime;
            var next = StabilizeDirIndex(_facingDirIndex, rawDirXZ, FacingHysteresisDegrees);
            if (next == _facingDirIndex)
            {
                return;
            }

            if (_facingDirIndex >= 0 && _facingSwitchTimer < FacingSwitchMinDwellSeconds)
            {
                return;
            }

            _facingDirIndex = next;
            _facingSwitchTimer = 0f;
            _anim.SetFacing(DirIndexToUnitXZ(next));
        }

        /// <summary>
        /// Keeps the current DirIndex unless the raw direction passes the current sector
        /// boundary by more than <paramref name="hysteresisDeg"/> (sector half-width 22.5°).
        /// </summary>
        private static int StabilizeDirIndex(int currentDirIndex, Vector3 rawDirXZ, float hysteresisDeg)
        {
            var candidate = WarriorAnimView.DirIndexFromXZ(rawDirXZ);
            if (currentDirIndex < 0 || currentDirIndex > 7 || candidate == currentDirIndex)
            {
                return candidate;
            }

            rawDirXZ.y = 0f;
            if (rawDirXZ.sqrMagnitude < 0.0001f)
            {
                return currentDirIndex;
            }

            var n = rawDirXZ.normalized;
            var deg = Mathf.Atan2(n.x, n.z) * Mathf.Rad2Deg;
            if (deg < 0f)
            {
                deg += 360f;
            }

            var currentCenterDeg = DirIndexToSector[currentDirIndex] * 45f;
            var delta = Mathf.Abs(Mathf.DeltaAngle(deg, currentCenterDeg));
            return delta > 22.5f + hysteresisDeg ? candidate : currentDirIndex;
        }

        /// <summary>Unit XZ vector at the sector center of <paramref name="dirIndex"/> (round-trips through DirIndexFromXZ).</summary>
        private static Vector3 DirIndexToUnitXZ(int dirIndex)
        {
            var rad = DirIndexToSector[dirIndex] * 45f * Mathf.Deg2Rad;
            return new Vector3(Mathf.Sin(rad), 0f, Mathf.Cos(rad));
        }

        private bool TryGetCombatTargetPosition(out Vector3 targetPos)
        {
            targetPos = default;
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
                // Keep the stabilized chase-facing state in sync with face-target writes.
                _facingDirIndex = WarriorAnimView.DirIndexFromXZ(to);
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

            var alertRadius = _config.AlertRadius;
            switch (_config.TargetSelect)
            {
                case TargetSelect.PreferProtagonist:
                    if (protagonist != null && WithinDetect(protagonist.position, alertRadius))
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

                    return protagonist != null && WithinDetect(protagonist.position, alertRadius)
                        ? TargetKind.Protagonist
                        : TargetKind.None;

                default:
                    return NearestAny(alertRadius, out warrior, out protagonist);
            }
        }

        private bool WithinDetect(Vector3 targetPos, float alertRadius)
        {
            var detect = IsStationary ? _config.AttackRange : alertRadius;
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

            if (protagonist != null && WithinDetect(protagonist.position, alertRadius))
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

            var detect = IsStationary ? _config.AttackRange : alertRadius;
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
