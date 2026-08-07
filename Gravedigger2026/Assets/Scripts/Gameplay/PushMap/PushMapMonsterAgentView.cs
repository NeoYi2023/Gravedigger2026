using System;
using System.Collections.Generic;
using Gravedigger2026.Core.Config;
using Gravedigger2026.Gameplay.Defend;
using UnityEngine;
using UnityEngine.AI;

namespace Gravedigger2026.Gameplay.PushMap
{
    /// <summary>
    /// PM-06: PushMap monster movement + normal-attack View (NavMeshAgent) with AggroMode four-state
    /// (SPEC_03 §3.14 / SPEC_04 §9.19 + §9.23). ActiveChase: loyal soldier enters AlertRadius → chase
    /// until death. PassiveChase: idle until NotifyProvoked, then chase. StationaryActive: never moves,
    /// attacks a loyal soldier inside AttackRange. StationaryPassive: never moves, attacks only after
    /// NotifyProvoked with target still in AttackRange. Detection/provocation are loyal-only.
    /// Hits keep AttackMode scheme D; protagonist hit → onHitProtagonist (ApplyShieldHit).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PushMapMonsterAgentView : MonoBehaviour
    {
        private MonsterConfigRow _config;
        private Transform _protagonist;
        private Func<IReadOnlyList<PushMapAdvanceView>> _warriorsProvider;
        private Action<string> _onHitProtagonist;
        private float _retargetInterval = 1f;
        private float _retargetTimer;
        private float _attackCooldown;
        private NavMeshAgent _agent;
        private bool _alive = true;
        private bool _provoked;
        private bool _isBoss;
        private Gameplay.Defend.MonsterAgentView _probeShim;

        public string MonsterId => _config != null ? _config.MonsterId : string.Empty;
        public bool IsAlive => _alive;
        public bool IsBoss => _isBoss;

        /// <summary>Stationary stances never move (SPEC_03 §3.14).</summary>
        public bool IsStationary => _config != null &&
            (_config.AggroMode == AggroMode.StationaryActive || _config.AggroMode == AggroMode.StationaryPassive);

        /// <summary>Passive stances stay idle until provoked (SPEC_03 §3.14).</summary>
        public bool IsPassive => _config != null &&
            (_config.AggroMode == AggroMode.PassiveChase || _config.AggroMode == AggroMode.StationaryPassive);

        public float AttackRange => _config != null ? _config.AttackRange : 0f;
        public float BodyRadius => _config != null ? Mathf.Max(0.05f, _config.BodyRadius) : 0.35f;

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
            float retargetIntervalSeconds = 1f)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _protagonist = protagonist;
            _warriorsProvider = warriorsProvider;
            _onHitProtagonist = onHitProtagonist;
            _retargetInterval = Mathf.Max(0.1f, retargetIntervalSeconds);
            _retargetTimer = 0f;
            _attackCooldown = 0f;
            _alive = true;
            _provoked = false;
            _isBoss = false;

            _agent = GetComponent<NavMeshAgent>();
            if (_agent == null)
            {
                _agent = gameObject.AddComponent<NavMeshAgent>();
            }

            _agent.speed = Mathf.Max(0.1f, config.MoveSpeed);
            _agent.stoppingDistance = Mathf.Max(0.05f, config.AttackRange * 0.85f);
            _agent.angularSpeed = 720f;
            _agent.acceleration = 24f;
            _agent.radius = BodyRadius;
            _agent.height = 1.8f;
            _agent.autoBraking = true;
            _agent.updateRotation = false;

            if (!_agent.isOnNavMesh && NavMesh.SamplePosition(transform.position, out var hit, 12f, NavMesh.AllAreas))
            {
                _agent.Warp(hit.position);
            }

            if (IsStationary)
            {
                StopMovement();
            }
            else if (!IsPassive)
            {
                Retarget();
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
            if (!IsStationary)
            {
                Retarget();
            }
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
            StopMovement();
            gameObject.SetActive(false);
        }

        private void Update()
        {
            if (!_alive || _config == null)
            {
                return;
            }

            if (!IsStationary)
            {
                _retargetTimer += Time.deltaTime;
                if (_retargetTimer >= _retargetInterval)
                {
                    _retargetTimer = 0f;
                    Retarget();
                }
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

        private void Retarget()
        {
            if (_agent == null || !_agent.isOnNavMesh || !_alive || IsStationary)
            {
                return;
            }

            if (IsPassive && !_provoked)
            {
                StopMovement();
                return;
            }

            if (ResolveTarget(out var warriorView, out var protagonistTf) == TargetKind.None)
            {
                return;
            }

            var targetTf = warriorView != null ? warriorView.transform : protagonistTf;
            if (targetTf == null)
            {
                return;
            }

            _agent.isStopped = false;
            _agent.SetDestination(targetTf.position);
        }

        private void StopMovement()
        {
            if (_agent != null && _agent.isOnNavMesh)
            {
                _agent.isStopped = true;
                _agent.ResetPath();
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
