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
    /// Hits keep AttackMode scheme D; protagonist hit → onHitProtagonist (ApplyShieldHit).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PushMapMonsterAgentView : MonoBehaviour
    {
        private const float SoldierDemoRadius = 0.1f;

        private MonsterConfigRow _config;
        private Transform _protagonist;
        private Func<IReadOnlyList<PushMapAdvanceView>> _warriorsProvider;
        private Action<string> _onHitProtagonist;
        private AttackSlotService _attackSlots;
        private MassMoveScheduler _scheduler;
        private float _retargetInterval = 1f;
        private float _retargetTimer;
        private float _attackCooldown;
        private NavMeshAgent _agent;
        private bool _alive = true;
        private bool _provoked;
        private bool _isBoss;
        private int _moveId;
        private string _attackerId;
        private Gameplay.Defend.MonsterAgentView _probeShim;

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

        /// <summary>Presence-probe shim (PM-05): the capture probe scans MonsterAgentView.IsAlive.</summary>
        public MonsterAgentView ProbeShim => _probeShim;

        public void AttachProbeShim(MonsterAgentView shim)
        {
            _probeShim = shim;
            if (_probeShim != null)
            {
                _probeShim.SyncAliveFrom(_alive);
            }
        }

        public void Bind(
            MonsterConfigRow config,
            Transform protagonist,
            Func<IReadOnlyList<PushMapAdvanceView>> warriorsProvider,
            Action<string> onHitProtagonist,
            float retargetIntervalSeconds = 1f,
            AttackSlotService attackSlots = null,
            MassMoveScheduler scheduler = null,
            int moveId = 0)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _protagonist = protagonist;
            _warriorsProvider = warriorsProvider;
            _onHitProtagonist = onHitProtagonist;
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
            var maxCombatRadius = Mathf.Max(0.05f, config.AttackRange - SoldierDemoRadius - 0.05f);
            _agent.radius = Mathf.Min(BodyRadius, maxCombatRadius);
            _agent.height = 1.8f;
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
            _probeShim?.SyncAliveFrom(false);
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

            if (slots == null ||
                !slots.TryClaim(
                    _attackerId,
                    targetId,
                    _config.AttackRange,
                    targetTf.position,
                    out var slotPos,
                    AttackMode,
                    transform.position,
                    warriorView != null ? SoldierDemoRadius : 0.35f))
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

            if (targetKind == TargetKind.Protagonist)
            {
                _onHitProtagonist?.Invoke($"Monster:{_config.MonsterId}");
            }
            else if (warriorView != null)
            {
                Debug.Log(
                    $"[PushMapMonster] {_config.MonsterId} hits warrior {warriorView.name} " +
                    $"for {_config.AttackPower} (warrior HP not tracked this slice).");
            }

            var interval = _config.AttackSpeed > 0.01f ? 1f / _config.AttackSpeed : 1f;
            _attackCooldown = Mathf.Max(0.2f, interval);
        }

        private void LateUpdate()
        {
            if (!_alive || IsStationary || _agent == null || _scheduler == null || _moveId == 0)
            {
                return;
            }

            if (!_agent.isOnNavMesh)
            {
                var warpSample = Mathf.Max(1f, BodyRadius * 3f);
                if (NavMesh.SamplePosition(transform.position, out var hit, warpSample, NavMesh.AllAreas))
                {
                    _agent.Warp(hit.position);
                }

                if (!_agent.isOnNavMesh)
                {
                    return;
                }
            }

            if (!_scheduler.TryGetSteer(_moveId, out var steer) || steer.sqrMagnitude < 1e-8f)
            {
                return;
            }

            if (_agent.hasPath)
            {
                _agent.ResetPath();
            }

            _agent.isStopped = false;
            var speed = Mathf.Max(0.1f, _config != null ? _config.MoveSpeed : 3f);
            var delta = new Vector3(steer.x, 0f, steer.y) * (speed * Time.deltaTime);
            _agent.Move(delta);
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
                if (w == null || w.IsRebel)
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
