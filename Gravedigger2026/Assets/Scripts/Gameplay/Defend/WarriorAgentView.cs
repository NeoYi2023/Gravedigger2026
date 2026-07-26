using System;
using System.Collections.Generic;
using Gravedigger2026.Core.Config;
using Gravedigger2026.Core.Defend;
using UnityEngine;
using UnityEngine.AI;

namespace Gravedigger2026.Gameplay.Defend
{
    /// <summary>
    /// Soldier NavMesh + melee windup / ranged projectile / Rebel nearest targeting (D-042 / D-043).
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

        private DefendSessionService _session;
        private string _warriorId;
        private EngageZone _engageZone;
        private Func<IReadOnlyList<MonsterAgentView>> _monstersProvider;
        private Func<IReadOnlyList<WarriorAgentView>> _warriorsProvider;
        private Func<Transform> _protagonistProvider;
        private GameObject _projectilePrefab;
        private Transform _projectileParent;
        private float _retargetInterval = 1f;
        private float _retargetTimer;
        private float _attackStartCooldown;
        private float _windupRemaining;
        private AttackPhase _attackPhase = AttackPhase.IdleOrMove;
        private RebelTargetKind _windupKind;
        private string _windupTargetId;
        private NavMeshAgent _agent;

        public string WarriorId => _warriorId;

        public void Bind(
            DefendSessionService session,
            string warriorId,
            EngageZone engageZone,
            Func<IReadOnlyList<MonsterAgentView>> monstersProvider,
            float retargetIntervalSeconds,
            GameObject projectilePrefab = null,
            Transform projectileParent = null,
            Func<IReadOnlyList<WarriorAgentView>> warriorsProvider = null,
            Func<Transform> protagonistProvider = null)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _warriorId = warriorId ?? throw new ArgumentNullException(nameof(warriorId));
            _engageZone = engageZone;
            _monstersProvider = monstersProvider ?? throw new ArgumentNullException(nameof(monstersProvider));
            _warriorsProvider = warriorsProvider;
            _protagonistProvider = protagonistProvider;
            _projectilePrefab = projectilePrefab;
            _projectileParent = projectileParent;
            _retargetInterval = Mathf.Max(0.1f, retargetIntervalSeconds);
            _retargetTimer = 0f;
            _attackStartCooldown = 0f;
            _windupRemaining = 0f;
            _attackPhase = AttackPhase.IdleOrMove;
            _windupKind = RebelTargetKind.None;
            _windupTargetId = null;

            if (!_session.TryGetWarrior(_warriorId, out var state) || state == null)
            {
                Debug.LogError($"[WarriorAgent] Missing combat state for '{_warriorId}'.");
                enabled = false;
                return;
            }

            _agent = GetComponent<NavMeshAgent>();
            if (_agent == null)
            {
                _agent = gameObject.AddComponent<NavMeshAgent>();
            }

            _agent.speed = Mathf.Max(0.1f, state.MoveSpeed);
            _agent.stoppingDistance = Mathf.Max(0.05f, state.AttackRange * 0.85f);
            _agent.angularSpeed = 720f;
            _agent.acceleration = 24f;
            _agent.radius = 0.35f;
            _agent.height = 1.8f;
            _agent.autoBraking = true;
            // SPEC_04 §15.2: facing via Animator DirIndex; do not yaw the Visual sprite.
            _agent.updateRotation = false;

            if (!_agent.isOnNavMesh && NavMesh.SamplePosition(transform.position, out var hit, 4f, NavMesh.AllAreas))
            {
                _agent.Warp(hit.position);
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
                StopAgent();
                return;
            }

            _retargetTimer += Time.deltaTime;
            if (_retargetTimer >= _retargetInterval)
            {
                _retargetTimer = 0f;
                RequestPath(state);
            }

            if (state.IsRebel)
            {
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

            if (Vector3.Distance(transform.position, target.transform.position) > state.AttackRange)
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

            if (Vector3.Distance(transform.position, target.transform.position) > state.AttackRange)
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
            if (!TryFindNearestRebelTarget(out var kind, out var targetId, out var targetPos)
                || _attackStartCooldown > 0f)
            {
                return;
            }

            if (Vector3.Distance(transform.position, targetPos) > state.AttackRange)
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
            if (_agent != null && _agent.isOnNavMesh)
            {
                _agent.isStopped = true;
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
                FindMonsterByRuntimeId,
                state.RangedProjectileSpeed,
                state.RangedTimeoutSeconds);

            if (_agent != null && _agent.isOnNavMesh)
            {
                _agent.isStopped = false;
            }
        }

        private void BeginWindup(RebelTargetKind kind, string targetId, DefendCombatWarriorState state)
        {
            _attackPhase = AttackPhase.Windup;
            _windupKind = kind;
            _windupTargetId = targetId;
            _windupRemaining = Mathf.Max(0f, state.MeleeWindupSeconds);
            _attackStartCooldown = state.AttackSpeed > 0.01f ? 1f / state.AttackSpeed : 1f;
            if (_agent != null && _agent.isOnNavMesh)
            {
                _agent.isStopped = true;
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
                          && Vector3.Distance(transform.position, target.transform.position) <= state.AttackRange + 0.05f;

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
                                  && Vector3.Distance(transform.position, tf.position) <= state.AttackRange + 0.05f;
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
                                  && Vector3.Distance(transform.position, other.transform.position)
                                  <= state.AttackRange + 0.05f;
                    _session.TryConfirmRebelHitOnWarrior(_warriorId, _windupTargetId, inRange);
                    break;
                }
                case RebelTargetKind.Monster:
                {
                    var target = FindMonsterByRuntimeId(_windupTargetId);
                    var inRange = target != null
                                  && target.IsAlive
                                  && Vector3.Distance(transform.position, target.transform.position)
                                  <= state.AttackRange + 0.05f;
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
            if (_agent != null && _agent.isOnNavMesh)
            {
                _agent.isStopped = false;
            }
        }

        private void RequestPath(DefendCombatWarriorState state)
        {
            if (_agent == null || !_agent.isOnNavMesh || _attackPhase == AttackPhase.Windup)
            {
                return;
            }

            Vector3 dest;
            if (state.IsRebel)
            {
                if (!TryFindNearestRebelTarget(out _, out _, out dest))
                {
                    return;
                }
            }
            else
            {
                var target = FindNearestEngageMonster();
                if (target == null)
                {
                    return;
                }

                dest = target.transform.position;
            }

            _agent.isStopped = false;
            _agent.stoppingDistance = Mathf.Max(0.05f, state.AttackRange * 0.85f);
            _agent.SetDestination(dest);
        }

        private bool TryFindNearestRebelTarget(out RebelTargetKind kind, out string targetId, out Vector3 position)
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

        private MonsterAgentView FindNearestEngageMonster()
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
            if (_agent != null && _agent.isOnNavMesh)
            {
                _agent.isStopped = true;
                _agent.ResetPath();
            }

            _attackPhase = AttackPhase.IdleOrMove;
            _windupKind = RebelTargetKind.None;
            _windupTargetId = null;
        }
    }
}
