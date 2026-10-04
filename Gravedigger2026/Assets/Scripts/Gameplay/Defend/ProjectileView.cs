using System;
using System.Collections.Generic;
using Gravedigger2026.Core.Config;
using Gravedigger2026.Core.Defend;
using UnityEngine;

namespace Gravedigger2026.Gameplay.Defend
{
    /// <summary>
    /// Kinematic ranged projectile (scheme D / Approach A). Soft-hit by distance; timeout = miss.
    /// Session-agnostic via <see cref="IProjectileCombatSession"/> so Defend and PushMap (PM-12)
    /// share the same Projectile prefab/View without binding each other's session lifetime.
    /// Generic pierce channel (SE-07): after a hit, Handler may grant ExtraHitsRemaining &gt; 0;
    /// the View then keeps current velocity (no per-projectile A*) and skips already-hit ids.
    /// </summary>
    public sealed class ProjectileView : MonoBehaviour
    {
        private readonly HashSet<string> _alreadyHit = new HashSet<string>(StringComparer.Ordinal);
        private readonly ProjectileHitFlightContext _flight = new ProjectileHitFlightContext();

        private IProjectileCombatSession _session;
        private string _warriorId;
        private string _targetRuntimeId;
        private Func<string, Transform> _resolveTarget;
        private Func<IReadOnlyList<string>> _enumerateAliveTargets;
        private float _speed;
        private float _timeoutRemaining;
        private float _hitRadius;
        private bool _settled;
        private bool _ballistic;
        private bool _arc;
        private bool _arcCommittedHit;
        private bool _lingering;
        private float _arcProgress;
        private float _arcLength;
        private float _arcPeak;
        private float _groundY;
        private float _lingerRemaining;
        private Vector3 _arcStart;
        private Vector3 _arcLanding;
        private Vector3 _lastKnownTargetPos;
        private Vector3 _lastMoveDir = Vector3.forward;
        private SpriteRenderer _visualRenderer;
        private float _stretchRemaining;
        private float _stretchDuration;
        private Vector3 _stretchStart;

        /// <summary>
        /// Visual child localScale XYZ (mage / mid-band straight / long arc; SPEC_04).
        /// </summary>
        public const float VisualLocalScale = 0.7f;

        /// <summary>
        /// Mid-band straight shots that would enter the hit radius sooner than this
        /// are stretched so the bolt renders before the hit despawn (SPEC_03 §3.12).
        /// </summary>
        public const float StraightMinVisibleSeconds = 0.2f;

        /// <summary>
        /// Applies BaseClass-specific projectile art. Sprite tip faces +Y in texture (SPEC_04 §15 Visual child).
        /// </summary>
        public void ApplyVisual(Sprite sprite)
        {
            if (sprite == null)
            {
                return;
            }

            var visual = transform.Find("Visual");
            if (visual == null)
            {
                var legacyBody = transform.Find("Body");
                if (legacyBody != null)
                {
                    legacyBody.gameObject.SetActive(false);
                }

                var go = new GameObject("Visual");
                go.transform.SetParent(transform, false);
                go.transform.localEulerAngles = new Vector3(90f, 0f, 0f);
                _visualRenderer = go.AddComponent<SpriteRenderer>();
                _visualRenderer.sortingOrder = 320;
            }
            else
            {
                _visualRenderer = visual.GetComponent<SpriteRenderer>();
                if (_visualRenderer == null)
                {
                    _visualRenderer = visual.gameObject.AddComponent<SpriteRenderer>();
                    _visualRenderer.sortingOrder = 320;
                }
            }

            _visualRenderer.sprite = sprite;
            _visualRenderer.transform.localScale = new Vector3(
                VisualLocalScale,
                VisualLocalScale,
                VisualLocalScale);
        }

        public void Launch(
            IProjectileCombatSession session,
            string warriorId,
            string targetRuntimeId,
            Func<string, Transform> resolveTarget,
            float speed,
            float timeoutSeconds,
            float hitRadius = -1f,
            Func<IReadOnlyList<string>> enumerateAliveTargets = null,
            bool arc = false,
            bool arcCommittedHit = false,
            Vector3 arcLanding = default,
            float arcPeak = 1f,
            float missLingerSeconds = 0f,
            bool stretchShortFlight = false)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _warriorId = warriorId ?? throw new ArgumentNullException(nameof(warriorId));
            _targetRuntimeId = targetRuntimeId ?? throw new ArgumentNullException(nameof(targetRuntimeId));
            _resolveTarget = resolveTarget ?? throw new ArgumentNullException(nameof(resolveTarget));
            _enumerateAliveTargets = enumerateAliveTargets;
            _speed = Mathf.Max(0.1f, speed);
            _timeoutRemaining = Mathf.Max(0.05f, timeoutSeconds);
            _hitRadius = Mathf.Max(
                0.05f,
                hitRadius < 0f ? CombatRuntimeTuning.ProjectileDefaultHitRadius : hitRadius);
            _settled = false;
            _ballistic = false;
            _arc = arc;
            _arcCommittedHit = arc && arcCommittedHit;
            _lingering = false;
            _arcProgress = 0f;
            _arcStart = transform.position;
            _groundY = arcLanding.y;
            _arcLanding = arc ? new Vector3(arcLanding.x, _groundY, arcLanding.z) : default;
            var flat = _arcLanding - _arcStart;
            flat.y = 0f;
            _arcLength = Mathf.Max(0.05f, flat.magnitude);
            _arcPeak = Mathf.Max(0.2f, arcPeak);
            _lingerRemaining = Mathf.Max(0f, missLingerSeconds);
            _alreadyHit.Clear();
            _flight.AlreadyHitRuntimeIds = _alreadyHit;
            _flight.ExtraHitsRemaining = 0;

            var target = _resolveTarget(_targetRuntimeId);
            _lastKnownTargetPos = target != null
                ? target.position
                : transform.position + transform.forward;
            var fwd = transform.forward;
            fwd.y = 0f;
            if (fwd.sqrMagnitude > 0.0001f)
            {
                _lastMoveDir = fwd.normalized;
            }

            _stretchStart = transform.position;
            _stretchRemaining = 0f;
            _stretchDuration = 0f;
            if (stretchShortFlight && !arc)
            {
                var gap = _lastKnownTargetPos - _stretchStart;
                gap.y = 0f;
                var gapDist = gap.magnitude;
                var timeToHit = Mathf.Max(0f, gapDist - _hitRadius) / _speed;
                if (timeToHit < StraightMinVisibleSeconds)
                {
                    _stretchDuration = StraightMinVisibleSeconds;
                    _stretchRemaining = StraightMinVisibleSeconds;
                }
            }
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

            if (_lingering)
            {
                _lingerRemaining -= Time.deltaTime;
                if (_lingerRemaining <= 0f)
                {
                    _settled = true;
                    Destroy(gameObject);
                }

                return;
            }

            _timeoutRemaining -= Time.deltaTime;
            if (_timeoutRemaining <= 0f)
            {
                DespawnMiss("timeout");
                return;
            }

            if (_arc)
            {
                TickArc();
                return;
            }

            if (_ballistic)
            {
                TickBallistic();
                return;
            }

            TickHoming();
        }

        private void TickArc()
        {
            if (_arcCommittedHit)
            {
                var target = _resolveTarget != null ? _resolveTarget(_targetRuntimeId) : null;
                if (target != null && _session != null && _session.IsMonsterAlive(_targetRuntimeId))
                {
                    _arcLanding = new Vector3(target.position.x, _groundY, target.position.z);
                }
            }

            _arcProgress += (_speed * Time.deltaTime) / _arcLength;
            if (_arcProgress >= 1f)
            {
                transform.position = _arcLanding;
                if (_arcCommittedHit)
                {
                    TryHitTarget(_targetRuntimeId, snapToTarget: true);
                }
                else
                {
                    _arc = false;
                    _lingering = true;
                }

                return;
            }

            var t = Mathf.Clamp01(_arcProgress);
            var pos = Vector3.Lerp(_arcStart, _arcLanding, t);
            pos.y = Mathf.Lerp(_arcStart.y, _groundY, t) + (4f * t * (1f - t) * _arcPeak);
            var dir = pos - transform.position;
            dir.y = 0f;
            transform.position = pos;
            if (dir.sqrMagnitude > 0.0001f)
            {
                _lastMoveDir = dir.normalized;
                transform.rotation = Quaternion.LookRotation(_lastMoveDir, Vector3.up);
            }
        }

        private void TickHoming()
        {
            var target = _resolveTarget != null ? _resolveTarget(_targetRuntimeId) : null;
            if (target != null && _session.IsMonsterAlive(_targetRuntimeId))
            {
                _lastKnownTargetPos = target.position;
            }

            if (_stretchRemaining > 0f)
            {
                TickStretchedHoming();
                return;
            }

            var pos = transform.position;
            var to = _lastKnownTargetPos - pos;
            to.y = 0f;
            var dist = to.magnitude;
            if (dist <= _hitRadius)
            {
                TryHitTarget(_targetRuntimeId, snapToTarget: true);
                return;
            }

            var step = _speed * Time.deltaTime;
            if (step >= dist)
            {
                transform.position = new Vector3(_lastKnownTargetPos.x, pos.y, _lastKnownTargetPos.z);
                TryHitTarget(_targetRuntimeId, snapToTarget: false);
                return;
            }

            var dir = to / dist;
            _lastMoveDir = dir;
            transform.position = pos + dir * step;
            if (dir.sqrMagnitude > 0.0001f)
            {
                transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
            }
        }

        /// <summary>
        /// Short mid-band straight flights (often already inside the hit radius at spawn)
        /// lerp to the target over <see cref="StraightMinVisibleSeconds"/>, then settle.
        /// </summary>
        private void TickStretchedHoming()
        {
            _stretchRemaining -= Time.deltaTime;
            var duration = Mathf.Max(0.05f, _stretchDuration);
            var t = 1f - Mathf.Clamp01(_stretchRemaining / duration);
            var dest = _lastKnownTargetPos;
            var pos = Vector3.Lerp(_stretchStart, new Vector3(dest.x, _stretchStart.y, dest.z), t);
            var dir = pos - transform.position;
            dir.y = 0f;
            transform.position = pos;
            if (dir.sqrMagnitude > 0.0001f)
            {
                _lastMoveDir = dir.normalized;
                transform.rotation = Quaternion.LookRotation(_lastMoveDir, Vector3.up);
            }

            if (_stretchRemaining <= 0f)
            {
                TryHitTarget(_targetRuntimeId, snapToTarget: true);
            }
        }

        private void TickBallistic()
        {
            var pos = transform.position;
            var dir = _lastMoveDir;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.0001f)
            {
                dir = Vector3.forward;
            }
            else
            {
                dir.Normalize();
            }

            transform.position = pos + dir * (_speed * Time.deltaTime);
            transform.rotation = Quaternion.LookRotation(dir, Vector3.up);

            if (!TryFindNearestUnhitInRadius(out var nextId))
            {
                return;
            }

            TryHitTarget(nextId, snapToTarget: false);
        }

        private bool TryFindNearestUnhitInRadius(out string nearestId)
        {
            nearestId = null;
            var ids = _enumerateAliveTargets != null ? _enumerateAliveTargets() : null;
            if (ids == null || ids.Count == 0)
            {
                return false;
            }

            var origin = transform.position;
            var bestSqr = _hitRadius * _hitRadius;
            for (var i = 0; i < ids.Count; i++)
            {
                var id = ids[i];
                if (string.IsNullOrEmpty(id) || _alreadyHit.Contains(id))
                {
                    continue;
                }

                if (_session == null || !_session.IsMonsterAlive(id))
                {
                    continue;
                }

                var t = _resolveTarget != null ? _resolveTarget(id) : null;
                if (t == null)
                {
                    continue;
                }

                var dx = t.position.x - origin.x;
                var dz = t.position.z - origin.z;
                var sqr = dx * dx + dz * dz;
                if (sqr > bestSqr)
                {
                    continue;
                }

                bestSqr = sqr;
                nearestId = id;
            }

            return nearestId != null;
        }

        private void TryHitTarget(string runtimeId, bool snapToTarget)
        {
            if (_settled || string.IsNullOrEmpty(runtimeId))
            {
                return;
            }

            if (_session == null || !_session.IsMonsterAlive(runtimeId))
            {
                if (!_ballistic)
                {
                    Debug.Log($"[Projectile] Miss (target gone) {_warriorId} -> {runtimeId}");
                    _settled = true;
                    Destroy(gameObject);
                }

                return;
            }

            if (snapToTarget)
            {
                var t = _resolveTarget != null ? _resolveTarget(runtimeId) : null;
                if (t != null)
                {
                    var p = transform.position;
                    transform.position = new Vector3(t.position.x, p.y, t.position.z);
                }
            }

            _alreadyHit.Add(runtimeId);
            _flight.AlreadyHitRuntimeIds = _alreadyHit;
            _flight.ExtraHitsRemaining = 0;

            var confirmed = false;
            if (_session is IProjectilePierceChannel pierce)
            {
                confirmed = pierce.TryConfirmRangedHit(_warriorId, runtimeId, _flight);
            }
            else
            {
                confirmed = _session.TryConfirmRangedHit(_warriorId, runtimeId);
            }

            if (!confirmed)
            {
                _alreadyHit.Remove(runtimeId);
                if (!_ballistic)
                {
                    Debug.Log($"[Projectile] Miss (unconfirmed) {_warriorId} -> {runtimeId}");
                    _settled = true;
                    Destroy(gameObject);
                }

                return;
            }

            if (_flight.ExtraHitsRemaining > 0)
            {
                EnterBallistic();
                return;
            }

            _settled = true;
            Destroy(gameObject);
        }

        private void EnterBallistic()
        {
            if (_ballistic)
            {
                return;
            }

            _arc = false;
            _ballistic = true;
            var fwd = transform.forward;
            fwd.y = 0f;
            if (fwd.sqrMagnitude > 0.0001f)
            {
                _lastMoveDir = fwd.normalized;
            }
            else if (_lastMoveDir.sqrMagnitude < 0.0001f)
            {
                _lastMoveDir = Vector3.forward;
            }
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
