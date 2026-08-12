using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Gravedigger2026.Core.PushMap
{
    /// <summary>
    /// Per-save-slot + CampaignMode dungeon unlock set hook (SPEC_03 §3.14 / SPEC_04 §6).
    /// PlayerPrefs-backed; dungeon gameplay body is TBD — unlock IDs are log-verifiable only.
    /// </summary>
    public sealed class DungeonUnlockService
    {
        private readonly HashSet<string> _unlocked = new HashSet<string>(StringComparer.Ordinal);
        private int _slotIndex = -1;
        private CampaignMode _campaignMode = CampaignMode.Mode1;

        public int BoundSlotIndex => _slotIndex;
        public CampaignMode BoundCampaignMode => _campaignMode;
        public IReadOnlyCollection<string> UnlockedIds => _unlocked;

        public void BindSlot(int slotIndex, CampaignMode campaignMode)
        {
            if (slotIndex < 0 || slotIndex > 2)
            {
                throw new ArgumentOutOfRangeException(nameof(slotIndex), slotIndex, "Slot index must be 0..2.");
            }

            _slotIndex = slotIndex;
            _campaignMode = campaignMode;
            _unlocked.Clear();

            var key = UnlockKey(slotIndex, campaignMode);
            var raw = PlayerPrefs.GetString(key, string.Empty);
            var migratedFromLegacy = false;

            if (string.IsNullOrEmpty(raw) && campaignMode == CampaignMode.Mode1)
            {
                var legacyKey = SaveSlotPrefsKeys.LegacyDataKey(
                    slotIndex, SaveSlotPrefsKeys.DungeonUnlocksSuffix);
                raw = PlayerPrefs.GetString(legacyKey, string.Empty);
                if (!string.IsNullOrEmpty(raw))
                {
                    migratedFromLegacy = true;
                }
            }

            if (!string.IsNullOrEmpty(raw))
            {
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

            if (migratedFromLegacy)
            {
                Persist();
                PlayerPrefs.DeleteKey(
                    SaveSlotPrefsKeys.LegacyDataKey(slotIndex, SaveSlotPrefsKeys.DungeonUnlocksSuffix));
                PlayerPrefs.Save();
            }
        }

        public void ClearBound()
        {
            _slotIndex = -1;
            _campaignMode = CampaignMode.Mode1;
            _unlocked.Clear();
        }

        public static void DeleteSlotData(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex > 2)
            {
                return;
            }

            PlayerPrefs.DeleteKey(UnlockKey(slotIndex, CampaignMode.Mode1));
            PlayerPrefs.DeleteKey(UnlockKey(slotIndex, CampaignMode.Mode2));
            PlayerPrefs.DeleteKey(
                SaveSlotPrefsKeys.LegacyDataKey(slotIndex, SaveSlotPrefsKeys.DungeonUnlocksSuffix));
            PlayerPrefs.Save();
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
                Debug.Log($"[DungeonUnlock] Slot={_slotIndex} Mode={_campaignMode} already unlocked '{id}' (set=[{FormatSet()}]).");
                return false;
            }

            Persist();
            Debug.Log($"[DungeonUnlock] Slot={_slotIndex} Mode={_campaignMode} unlocked '{id}' → set=[{FormatSet()}]");
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

            PlayerPrefs.SetString(UnlockKey(_slotIndex, _campaignMode), FormatSet());
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

        private static string UnlockKey(int slotIndex, CampaignMode mode)
        {
            return SaveSlotPrefsKeys.DataKey(slotIndex, mode, SaveSlotPrefsKeys.DungeonUnlocksSuffix);
        }
    }
}
