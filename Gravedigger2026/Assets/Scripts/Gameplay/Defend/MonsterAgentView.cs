using System;
using System.Collections.Generic;
using Gravedigger2026.Core.Config;
using Gravedigger2026.Core.Defend;
using UnityEngine;
using UnityEngine.AI;

namespace Gravedigger2026.Gameplay.Defend
{
    /// <summary>
    /// Monster movement + normal-attack View (NavMeshAgent). Rules own Shield / warrior HP via session.
    /// </summary>
    public sealed class MonsterAgentView : MonoBehaviour
    {
        private DefendSessionService _session;
        private MonsterConfigRow _config;
        private string _runtimeId;
        private Transform _protagonist;
        private Func<IReadOnlyList<WarriorAgentView>> _warriorsProvider;
        private float _retargetInterval = 1f;
        private float _retargetTimer;
        private float _attackCooldown;
        private NavMeshAgent _agent;
        private bool _alive = true;

        public string MonsterId => _config != null ? _config.MonsterId : string.Empty;
        public string RuntimeId => _runtimeId;
        public bool IsAlive => _alive;

        private bool _probeOnly;

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
            float retargetIntervalSeconds)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _runtimeId = runtimeId ?? throw new ArgumentNullException(nameof(runtimeId));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _protagonist = protagonist;
            _warriorsProvider = warriorsProvider;
            _retargetInterval = Mathf.Max(0.1f, retargetIntervalSeconds);
            _retargetTimer = 0f;
            _attackCooldown = 0f;
            _alive = true;

            _agent = GetComponent<NavMeshAgent>();
            if (_agent == null)
            {
                _agent = gameObject.AddComponent<NavMeshAgent>();
            }

            _agent.speed = Mathf.Max(0.1f, config.MoveSpeed);
            _agent.stoppingDistance = Mathf.Max(0.05f, config.AttackRange * 0.85f);
            _agent.angularSpeed = 720f;
            _agent.acceleration = 24f;
            _agent.radius = 0.03f;
            _agent.height = 1.8f;
            _agent.autoBraking = true;
            _agent.updateRotation = false;

            if (!_agent.isOnNavMesh && NavMesh.SamplePosition(transform.position, out var hit, 4f, NavMesh.AllAreas))
            {
                _agent.Warp(hit.position);
            }

            Retarget();
        }

        public void NotifyKilled()
        {
            if (!_alive)
            {
                return;
            }

            _alive = false;
            if (_agent != null && _agent.isOnNavMesh)
            {
                _agent.isStopped = true;
                _agent.ResetPath();
            }

            gameObject.SetActive(false);
        }

        private void Update()
        {
            if (_probeOnly)
            {
                return; // presence shim for PushMap; Defend behaviour disabled
            }

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

            _retargetTimer += Time.deltaTime;
            if (_retargetTimer >= _retargetInterval)
            {
                _retargetTimer = 0f;
                Retarget();
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
                _session.ApplyProtagonistNormalHit($"Monster:{_config.MonsterId}");
            }
            else if (warriorView != null)
            {
                _session.TryApplyMonsterDamageToWarrior(
                    _runtimeId,
                    warriorView.WarriorId,
                    _config.AttackPower);
            }

            var interval = _config.AttackSpeed > 0.01f ? 1f / _config.AttackSpeed : 1f;
            _attackCooldown = Mathf.Max(0.2f, interval);
        }

        private void Retarget()
        {
            if (_probeOnly || _agent == null || !_agent.isOnNavMesh || !_alive)
            {
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
