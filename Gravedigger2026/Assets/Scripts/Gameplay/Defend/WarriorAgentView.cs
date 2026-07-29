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
        private Vector3 _formationHome;
        private bool _hasFormationHome;
        private float _retargetInterval = 1f;
        private float _retargetTimer;
        private float _attackStartCooldown;
        private float _windupRemaining;
        private AttackPhase _attackPhase = AttackPhase.IdleOrMove;
        private RebelTargetKind _windupKind;
        private string _windupTargetId;
        private NavMeshAgent _agent;
        private WarriorAnimView _anim;
        private bool _diePlayed;
        private const float MoveAnimSpeedSqr = 0.04f;
        private const float FormationHomeStoppingDistance = 0.15f;

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
            Func<Transform> protagonistProvider = null,
            Vector3? formationHomeWorld = null)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _warriorId = warriorId ?? throw new ArgumentNullException(nameof(warriorId));
            _engageZone = engageZone;
            _monstersProvider = monstersProvider ?? throw new ArgumentNullException(nameof(monstersProvider));
            _warriorsProvider = warriorsProvider;
            _protagonistProvider = protagonistProvider;
            _projectilePrefab = projectilePrefab;
            _projectileParent = projectileParent;
            _hasFormationHome = formationHomeWorld.HasValue;
            _formationHome = formationHomeWorld ?? Vector3.zero;
            _retargetInterval = Mathf.Max(0.1f, retargetIntervalSeconds);
            _retargetTimer = 0f;
            _attackStartCooldown = 0f;
            _windupRemaining = 0f;
            _attackPhase = AttackPhase.IdleOrMove;
            _windupKind = RebelTargetKind.None;
            _windupTargetId = null;
            _diePlayed = false;

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

            _anim = GetComponent<WarriorAnimView>();
            if (_anim == null)
            {
                _anim = gameObject.AddComponent<WarriorAnimView>();
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

            _anim.ResetToIdle();

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
                PlayDieOnce();
                StopAgent();
                return;
            }

            _retargetTimer += Time.deltaTime;
            if (_retargetTimer >= _retargetInterval)
            {
                _retargetTimer = 0f;
                RequestPath(state);
            }

            TickAnimPresentation(state);

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

        private void TickAnimPresentation(DefendCombatWarriorState state)
        {
            if (_anim == null)
            {
                return;
            }

            var moving = _attackPhase != AttackPhase.Windup
                         && _agent != null
                         && _agent.isOnNavMesh
                         && !_agent.isStopped
                         && _agent.velocity.sqrMagnitude > MoveAnimSpeedSqr;

            if (_attackPhase != AttackPhase.Windup)
            {
                _anim.SetMoving(moving);
            }

            if (TryGetAimPoint(state, out var aimPoint))
            {
                var toAim = aimPoint - transform.position;
                toAim.y = 0f;
                if (toAim.sqrMagnitude > 0.0001f)
                {
                    _anim.SetFacing(toAim);
                    return;
                }
            }

            if (moving && _agent != null)
            {
                var vel = _agent.velocity;
                vel.y = 0f;
                if (vel.sqrMagnitude > 0.0001f)
                {
                    _anim.SetFacing(vel);
                }
            }
        }

        private bool TryGetAimPoint(DefendCombatWarriorState state, out Vector3 aimPoint)
        {
            aimPoint = default;
            if (state.IsRebel)
            {
                return TryFindNearestRebelTarget(out _, out _, out aimPoint);
            }

            var target = FindNearestEngageMonster();
            if (target == null)
            {
                return false;
            }

            aimPoint = target.transform.position;
            return true;
        }

        private void PlayDieOnce()
        {
            if (_diePlayed || _anim == null)
            {
                return;
            }

            _diePlayed = true;
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

            var toTarget = target.transform.position - transform.position;
            toTarget.y = 0f;
            if (_anim != null)
            {
                if (toTarget.sqrMagnitude > 0.0001f)
                {
                    _anim.SetFacing(toTarget);
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

            if (_anim != null)
            {
                if (TryResolveWindupAim(kind, targetId, out var aim))
                {
                    var toAim = aim - transform.position;
                    toAim.y = 0f;
                    if (toAim.sqrMagnitude > 0.0001f)
                    {
                        _anim.SetFacing(toAim);
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

            if (_anim != null)
            {
                _anim.SetMoving(false);
            }
        }

        private void RequestPath(DefendCombatWarriorState state)
        {
            if (_agent == null || !_agent.isOnNavMesh || _attackPhase == AttackPhase.Windup)
            {
                return;
            }

            Vector3 dest;
            float stoppingDistance;
            if (state.IsRebel)
            {
                if (!TryFindNearestRebelTarget(out _, out _, out dest))
                {
                    return;
                }

                stoppingDistance = Mathf.Max(0.05f, state.AttackRange * 0.85f);
            }
            else
            {
                var target = FindNearestEngageMonster();
                if (target != null)
                {
                    dest = target.transform.position;
                    stoppingDistance = Mathf.Max(0.05f, state.AttackRange * 0.85f);
                }
                else if (_hasFormationHome)
                {
                    // SPEC_03 §3.12: no EngageZone target → auto-return FormationHome; retarget keeps searching.
                    dest = _formationHome;
                    stoppingDistance = FormationHomeStoppingDistance;
                }
                else
                {
                    return;
                }
            }

            _agent.isStopped = false;
            _agent.stoppingDistance = stoppingDistance;
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
