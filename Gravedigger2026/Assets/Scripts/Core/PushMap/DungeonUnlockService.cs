using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Gravedigger2026.Core.PushMap
{
    /// <summary>
    /// Per-save-slot dungeon unlock set hook (SPEC_03 §3.14 / SPEC_04 §9.22 PM-07).
    /// PlayerPrefs-backed; dungeon gameplay body is TBD — unlock IDs are log-verifiable only.
    /// </summary>
    public sealed class DungeonUnlockService
    {
        private const string KeyPrefix = "Gravedigger2026.SaveSlot.";
        private const string UnlockSuffix = ".DungeonUnlocks";

        private readonly HashSet<string> _unlocked = new HashSet<string>(StringComparer.Ordinal);
        private int _slotIndex = -1;

        public int BoundSlotIndex => _slotIndex;
        public IReadOnlyCollection<string> UnlockedIds => _unlocked;

        public void BindSlot(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex > 2)
            {
                throw new ArgumentOutOfRangeException(nameof(slotIndex), slotIndex, "Slot index must be 0..2.");
            }

            _slotIndex = slotIndex;
            _unlocked.Clear();
            var raw = PlayerPrefs.GetString(UnlockKey(slotIndex), string.Empty);
            if (string.IsNullOrEmpty(raw))
            {
                return;
            }

            var parts = raw.Split('|');
            for (var i = 0; i < parts.Length; i++)
            {
                var id = parts[i]?.Trim();
                if (!string.IsNullOrEmpty(id))
                {
                    _unlocked.Add(id);
                }
            }
        }

        public void ClearBound()
        {
            _slotIndex = -1;
            _unlocked.Clear();
        }

        /// <summary>
        /// Adds dungeonId to the bound slot set. Returns true if newly unlocked.
        /// Empty id is ignored. Logs every attempt for Demo acceptance.
        /// </summary>
        public bool TryUnlock(string dungeonId)
        {
            if (string.IsNullOrEmpty(dungeonId))
            {
                return false;
            }

            var id = dungeonId.Trim();
            if (id.Length == 0)
            {
                return false;
            }

            if (_slotIndex < 0)
            {
                Debug.LogWarning(
                    $"[DungeonUnlock] TryUnlock('{id}') ignored — no save slot bound (hook still logged).");
                return false;
            }

            if (!_unlocked.Add(id))
            {
                Debug.Log($"[DungeonUnlock] Slot={_slotIndex} already unlocked '{id}' (set=[{FormatSet()}]).");
                return false;
            }

            Persist();
            Debug.Log($"[DungeonUnlock] Slot={_slotIndex} unlocked '{id}' → set=[{FormatSet()}]");
            return true;
        }

        /// <summary>Parses pipe-separated DungeonUnlockIds and tries each.</summary>
        public void UnlockEncoded(string dungeonUnlockIdsEncoded)
        {
            if (string.IsNullOrEmpty(dungeonUnlockIdsEncoded))
            {
                return;
            }

            var parts = dungeonUnlockIdsEncoded.Split('|');
            for (var i = 0; i < parts.Length; i++)
            {
                TryUnlock(parts[i]);
            }
        }

        private void Persist()
        {
            if (_slotIndex < 0)
            {
                return;
            }

            PlayerPrefs.SetString(UnlockKey(_slotIndex), FormatSet());
            PlayerPrefs.Save();
        }

        private string FormatSet()
        {
            if (_unlocked.Count == 0)
            {
                return string.Empty;
            }

            var sb = new StringBuilder();
            var first = true;
            foreach (var id in _unlocked)
            {
                if (!first)
                {
                    sb.Append('|');
                }

                sb.Append(id);
                first = false;
            }

            return sb.ToString();
        }

        private static string UnlockKey(int slotIndex)
        {
            return KeyPrefix + slotIndex + UnlockSuffix;
        }
    }
}
