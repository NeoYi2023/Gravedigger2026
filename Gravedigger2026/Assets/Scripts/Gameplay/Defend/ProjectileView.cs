using System;
using Gravedigger2026.Core.Config;
using Gravedigger2026.Core.Defend;
using UnityEngine;

namespace Gravedigger2026.Gameplay.Defend
{
    /// <summary>
    /// Kinematic ranged projectile (scheme D / Approach A). Soft-hit by distance; timeout = miss.
    /// Session-agnostic via <see cref="IProjectileCombatSession"/> so Defend and PushMap (PM-12)
    /// share the same Projectile prefab/View without binding each other's session lifetime.
    /// </summary>
    public sealed class ProjectileView : MonoBehaviour
    {
        private IProjectileCombatSession _session;
        private string _warriorId;
        private string _targetRuntimeId;
        private Func<string, Transform> _resolveTarget;
        private float _speed;
        private float _timeoutRemaining;
        private float _hitRadius;
        private bool _settled;
        private Vector3 _lastKnownTargetPos;

        public void Launch(
            IProjectileCombatSession session,
            string warriorId,
            string targetRuntimeId,
            Func<string, Transform> resolveTarget,
            float speed,
            float timeoutSeconds,
            float hitRadius = -1f)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _warriorId = warriorId ?? throw new ArgumentNullException(nameof(warriorId));
            _targetRuntimeId = targetRuntimeId ?? throw new ArgumentNullException(nameof(targetRuntimeId));
            _resolveTarget = resolveTarget ?? throw new ArgumentNullException(nameof(resolveTarget));
            _speed = Mathf.Max(0.1f, speed);
            _timeoutRemaining = Mathf.Max(0.05f, timeoutSeconds);
            _hitRadius = Mathf.Max(
                0.05f,
                hitRadius < 0f ? CombatRuntimeTuning.ProjectileDefaultHitRadius : hitRadius);
            _settled = false;

            var target = _resolveTarget(_targetRuntimeId);
            _lastKnownTargetPos = target != null
                ? target.position
                : transform.position + transform.forward;
        }

        private void Update()
        {
            if (_settled)
            {
                return;
            }

            if (_session == null || !_session.IsProjectileCombatActive(_warriorId))
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

            var target = _resolveTarget != null ? _resolveTarget(_targetRuntimeId) : null;
            if (target != null && _session.IsMonsterAlive(_targetRuntimeId))
            {
                _lastKnownTargetPos = target.position;
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
