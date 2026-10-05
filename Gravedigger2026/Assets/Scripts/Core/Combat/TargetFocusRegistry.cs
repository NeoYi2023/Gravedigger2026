using System.Collections.Generic;

namespace Gravedigger2026.Core.Combat
{
    /// <summary>
    /// Per-attacker current attack lock (SPEC_03 §3.12 TargetFocus).
    /// Pure C#: used by nearest-band free-first soldier pick. Releasing AttackSlot
    /// in range must not clear focus — callers own SetFocus / ClearFocus cadence.
    /// </summary>
    public sealed class TargetFocusRegistry
    {
        private readonly Dictionary<string, string> _focusByAttacker =
            new Dictionary<string, string>();
        private readonly Dictionary<string, int> _countByTarget =
            new Dictionary<string, int>();

        public void SetFocus(string attackerId, string targetId)
        {
            if (string.IsNullOrEmpty(attackerId))
            {
                return;
            }

            if (string.IsNullOrEmpty(targetId))
            {
                ClearFocus(attackerId);
                return;
            }

            if (_focusByAttacker.TryGetValue(attackerId, out var prev) &&
                prev == targetId)
            {
                return;
            }

            if (!string.IsNullOrEmpty(prev))
            {
                Decrement(prev);
            }

            _focusByAttacker[attackerId] = targetId;
            if (_countByTarget.TryGetValue(targetId, out var n))
            {
                _countByTarget[targetId] = n + 1;
            }
            else
            {
                _countByTarget[targetId] = 1;
            }
        }

        public void ClearFocus(string attackerId)
        {
            if (string.IsNullOrEmpty(attackerId) ||
                !_focusByAttacker.TryGetValue(attackerId, out var prev))
            {
                return;
            }

            _focusByAttacker.Remove(attackerId);
            Decrement(prev);
        }

        public bool TryGetFocus(string attackerId, out string targetId)
        {
            targetId = null;
            if (string.IsNullOrEmpty(attackerId) ||
                !_focusByAttacker.TryGetValue(attackerId, out targetId) ||
                string.IsNullOrEmpty(targetId))
            {
                targetId = null;
                return false;
            }

            return true;
        }

        /// <summary>Focus count on <paramref name="targetId"/> (0 if unknown).</summary>
        public int GetFocusCount(string targetId)
        {
            if (string.IsNullOrEmpty(targetId))
            {
                return 0;
            }

            return _countByTarget.TryGetValue(targetId, out var n) ? n : 0;
        }

        /// <summary>
        /// Focus count excluding <paramref name="excludeAttackerId"/>'s own lock
        /// (so a monster does not treat itself as engaging its sticky target).
        /// </summary>
        public int GetFocusCountExcluding(string targetId, string excludeAttackerId)
        {
            var n = GetFocusCount(targetId);
            if (n <= 0 || string.IsNullOrEmpty(excludeAttackerId))
            {
                return n;
            }

            if (_focusByAttacker.TryGetValue(excludeAttackerId, out var mine) &&
                mine == targetId)
            {
                return n - 1;
            }

            return n;
        }

        public void Clear()
        {
            _focusByAttacker.Clear();
            _countByTarget.Clear();
        }

        private void Decrement(string targetId)
        {
            if (string.IsNullOrEmpty(targetId) ||
                !_countByTarget.TryGetValue(targetId, out var n))
            {
                return;
            }

            if (n <= 1)
            {
                _countByTarget.Remove(targetId);
            }
            else
            {
                _countByTarget[targetId] = n - 1;
            }
        }
    }
}
