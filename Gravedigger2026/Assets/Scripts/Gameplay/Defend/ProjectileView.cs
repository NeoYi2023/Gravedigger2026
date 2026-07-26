using System;
using Gravedigger2026.Core.Defend;
using UnityEngine;

namespace Gravedigger2026.Gameplay.Defend
{
    /// <summary>
    /// Kinematic ranged projectile (scheme D / Approach A). Soft-hit by distance; timeout = miss.
    /// </summary>
    public sealed class ProjectileView : MonoBehaviour
    {
        private DefendSessionService _session;
        private string _warriorId;
        private string _targetRuntimeId;
        private Func<string, MonsterAgentView> _resolveMonster;
        private float _speed;
        private float _timeoutRemaining;
        private float _hitRadius;
        private bool _settled;
        private Vector3 _lastKnownTargetPos;

        public void Launch(
            DefendSessionService session,
            string warriorId,
            string targetRuntimeId,
            Func<string, MonsterAgentView> resolveMonster,
            float speed,
            float timeoutSeconds,
            float hitRadius = 0.55f)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _warriorId = warriorId ?? throw new ArgumentNullException(nameof(warriorId));
            _targetRuntimeId = targetRuntimeId ?? throw new ArgumentNullException(nameof(targetRuntimeId));
            _resolveMonster = resolveMonster ?? throw new ArgumentNullException(nameof(resolveMonster));
            _speed = Mathf.Max(0.1f, speed);
            _timeoutRemaining = Mathf.Max(0.05f, timeoutSeconds);
            _hitRadius = Mathf.Max(0.05f, hitRadius);
            _settled = false;

            var target = _resolveMonster(_targetRuntimeId);
            _lastKnownTargetPos = target != null
                ? target.transform.position
                : transform.position + transform.forward;
        }

        private void Update()
        {
            if (_settled)
            {
                return;
            }

            if (_session == null
                || !_session.IsActive
                || _session.Phase != DefendPhase.Combat
                || !_session.IsWarriorCombatActive(_warriorId))
            {
                DespawnMiss("shooter inactive");
                return;
            }

            _timeoutRemaining -= Time.deltaTime;
            if (_timeoutRemaining <= 0f)
            {
                DespawnMiss("timeout");
                return;
            }

            var target = _resolveMonster != null ? _resolveMonster(_targetRuntimeId) : null;
            if (target != null && target.IsAlive)
            {
                _lastKnownTargetPos = target.transform.position;
            }

            var pos = transform.position;
            var to = _lastKnownTargetPos - pos;
            to.y = 0f;
            var dist = to.magnitude;
            if (dist <= _hitRadius)
            {
                TryHitAndDespawn();
                return;
            }

            var step = _speed * Time.deltaTime;
            if (step >= dist)
            {
                transform.position = new Vector3(_lastKnownTargetPos.x, pos.y, _lastKnownTargetPos.z);
                TryHitAndDespawn();
                return;
            }

            var dir = to / dist;
            transform.position = pos + dir * step;
            if (dir.sqrMagnitude > 0.0001f)
            {
                transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
            }
        }

        private void TryHitAndDespawn()
        {
            if (_settled)
            {
                return;
            }

            _settled = true;
            if (_session != null && _session.IsMonsterAlive(_targetRuntimeId))
            {
                _session.TryConfirmRangedHit(_warriorId, _targetRuntimeId);
            }
            else
            {
                Debug.Log($"[Projectile] Miss (target gone) {_warriorId} -> {_targetRuntimeId}");
            }

            Destroy(gameObject);
        }

        private void DespawnMiss(string reason)
        {
            if (_settled)
            {
                return;
            }

            _settled = true;
            Debug.Log($"[Projectile] Miss ({reason}) {_warriorId} -> {_targetRuntimeId}");
            Destroy(gameObject);
        }
    }
}
