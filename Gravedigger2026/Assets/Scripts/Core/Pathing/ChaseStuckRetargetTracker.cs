using System.Collections.Generic;
using Gravedigger2026.Core.Config;
using UnityEngine;

namespace Gravedigger2026.Core.Pathing
{
    /// <summary>
    /// Chase-stuck force retarget (SPEC_03 §3.12/§3.14 v0.84.25 Approach A; v0.84.26 fix).
    /// Pure C#: Views feed eligibility / displacement; when armed, engage select excludes
    /// recent stuck targets and bypasses stickiness. Independent of StuckHold (Idle only).
    /// </summary>
    public sealed class ChaseStuckRetargetTracker
    {
        private Vector3 _windowStartPos;
        private float _windowTimer;
        private float _stuckAccum;
        private float _cooldownRemaining;
        private bool _hasWindowStart;
        private string _armedExcludeId;
        private bool _armed;

        /// <summary>Recently abandoned target ids (anti A↔B thrash). Values = remaining seconds.</summary>
        private readonly Dictionary<string, float> _recentExcludeUntil =
            new Dictionary<string, float>(4);

        private readonly List<string> _expireScratch = new List<string>(4);

        /// <summary>Primary exclude while armed (just-stuck claim).</summary>
        public string ExcludeTargetId => _armed ? _armedExcludeId : null;

        /// <summary>True while a force-retarget exclude is active (bypass stickiness).</summary>
        public bool BypassStickiness => _armed && !string.IsNullOrEmpty(_armedExcludeId);

        public void Reset()
        {
            _armed = false;
            _armedExcludeId = null;
            _cooldownRemaining = 0f;
            _stuckAccum = 0f;
            _recentExcludeUntil.Clear();
            ClearWindow(default);
            _hasWindowStart = false;
        }

        /// <summary>
        /// True if <paramref name="targetId"/> should be skipped this select
        /// (armed exclude or still in recent blacklist).
        /// </summary>
        public bool IsExcluded(string targetId)
        {
            if (string.IsNullOrEmpty(targetId))
            {
                return false;
            }

            if (_armed &&
                string.Equals(targetId, _armedExcludeId, System.StringComparison.Ordinal))
            {
                return true;
            }

            return _recentExcludeUntil.ContainsKey(targetId);
        }

        /// <summary>
        /// Advance stuck windows / cooldown.
        /// <paramref name="eligible"/> = loyal, GoalKind=AttackSlot, not in AttackRange.
        /// Does <b>not</b> require non-zero steer — crowding often zeroes steer while still stuck.
        /// <paramref name="hasAlternateCandidate"/> must be true to arm (no empty Release).
        /// </summary>
        public void Tick(
            bool eligible,
            Vector3 worldPos,
            string currentTargetId,
            bool hasAlternateCandidate,
            float dt)
        {
            if (dt < 0f)
            {
                dt = 0f;
            }

            TickRecentExcludes(dt);

            if (_cooldownRemaining > 0f)
            {
                _cooldownRemaining -= dt;
                if (_cooldownRemaining < 0f)
                {
                    _cooldownRemaining = 0f;
                }
            }

            if (_armed)
            {
                // Stay armed until NotifyApplied or cooldown safety clear.
                if (_cooldownRemaining <= 0f)
                {
                    DisarmKeepBlacklist();
                }

                ClearWindow(worldPos);
                return;
            }

            if (!eligible || string.IsNullOrEmpty(currentTargetId))
            {
                _stuckAccum = 0f;
                ClearWindow(worldPos);
                return;
            }

            if (_cooldownRemaining > 0f)
            {
                ClearWindow(worldPos);
                return;
            }

            if (!_hasWindowStart)
            {
                _windowStartPos = worldPos;
                _hasWindowStart = true;
                _windowTimer = 0f;
            }

            // Sliding windows (same cadence as StuckHold detect): each window with
            // XZ disp &lt; epsilon credits StuckDetectWindowSeconds toward the retarget threshold.
            // Soft-collision micro-jostle within a single window still counts as stuck.
            _windowTimer += dt;
            var window = CombatRuntimeTuning.StuckDetectWindowSeconds;
            if (_windowTimer < window)
            {
                return;
            }

            var dx = worldPos.x - _windowStartPos.x;
            var dz = worldPos.z - _windowStartPos.z;
            var epsilon = CombatRuntimeTuning.StuckDisplacementEpsilon;
            _windowTimer = 0f;
            _windowStartPos = worldPos;

            if (dx * dx + dz * dz >= epsilon * epsilon)
            {
                _stuckAccum = 0f;
                return;
            }

            _stuckAccum += window;
            if (_stuckAccum < CombatRuntimeTuning.ChaseStuckRetargetSeconds)
            {
                return;
            }

            if (!hasAlternateCandidate)
            {
                _stuckAccum = 0f;
                return;
            }

            _armed = true;
            _armedExcludeId = currentTargetId;
            _recentExcludeUntil[currentTargetId] = CombatRuntimeTuning.ChaseStuckRetargetCooldownSeconds;
            _cooldownRemaining = CombatRuntimeTuning.ChaseStuckRetargetCooldownSeconds;
            _stuckAccum = 0f;
            ClearWindow(worldPos);
        }

        /// <summary>
        /// Call after engage select successfully chose a different target than the exclude id.
        /// Clears the arm; blacklist keeps the old id out for the cooldown window.
        /// </summary>
        public void NotifyApplied(string newTargetId)
        {
            if (!_armed)
            {
                return;
            }

            if (string.IsNullOrEmpty(newTargetId) ||
                string.Equals(newTargetId, _armedExcludeId, System.StringComparison.Ordinal))
            {
                return;
            }

            DisarmKeepBlacklist();
        }

        private void DisarmKeepBlacklist()
        {
            if (!string.IsNullOrEmpty(_armedExcludeId) &&
                !_recentExcludeUntil.ContainsKey(_armedExcludeId))
            {
                _recentExcludeUntil[_armedExcludeId] =
                    CombatRuntimeTuning.ChaseStuckRetargetCooldownSeconds;
            }

            _armed = false;
            _armedExcludeId = null;
        }

        private void TickRecentExcludes(float dt)
        {
            if (_recentExcludeUntil.Count == 0)
            {
                return;
            }

            _expireScratch.Clear();
            foreach (var key in _recentExcludeUntil.Keys)
            {
                _expireScratch.Add(key);
            }

            for (var i = 0; i < _expireScratch.Count; i++)
            {
                var key = _expireScratch[i];
                if (!_recentExcludeUntil.TryGetValue(key, out var left))
                {
                    continue;
                }

                left -= dt;
                if (left <= 0f)
                {
                    _recentExcludeUntil.Remove(key);
                }
                else
                {
                    _recentExcludeUntil[key] = left;
                }
            }
        }

        private void ClearWindow(Vector3 worldPos)
        {
            _windowTimer = 0f;
            _windowStartPos = worldPos;
            _hasWindowStart = true;
        }
    }
}
