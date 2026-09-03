using System;
using System.Collections.Generic;
using UnityEngine;

namespace Gravedigger2026.Core.Level
{
    /// <summary>
    /// Persist cleared GameplayOptionIds per SaveSlot + CampaignMode (SPEC_03 §3.9 / D-088).
    /// </summary>
    public sealed class LevelRouteProgressService
    {
        private int _slotIndex = -1;
        private CampaignMode _campaignMode = CampaignMode.Mode1;
        private readonly HashSet<string> _clearedOptionIds = new HashSet<string>(StringComparer.Ordinal);

        public int BoundSlotIndex => _slotIndex;
        public CampaignMode BoundCampaignMode => _campaignMode;

        public IEnumerable<string> ClearedOptionIds => _clearedOptionIds;

        public void BindSlot(int slotIndex, CampaignMode campaignMode)
        {
            if (slotIndex < 0 || slotIndex > 2)
            {
                throw new ArgumentOutOfRangeException(nameof(slotIndex), slotIndex, "Slot index must be 0..2.");
            }

            _slotIndex = slotIndex;
            _campaignMode = campaignMode;
            _clearedOptionIds.Clear();

            var key = ProgressKey(slotIndex, campaignMode);
            var raw = PlayerPrefs.GetString(key, string.Empty);
            if (string.IsNullOrEmpty(raw))
            {
                return;
            }

            try
            {
                var data = JsonUtility.FromJson<LevelRouteProgressSaveData>(raw);
                ApplyLoaded(data);
            }
            catch (Exception ex)
            {
                Debug.LogWarning(
                    $"[LevelRouteProgress] Failed to parse JSON key='{key}': {ex.Message}. Reset to empty.");
                _clearedOptionIds.Clear();
            }
        }

        public void ClearBound()
        {
            _slotIndex = -1;
            _campaignMode = CampaignMode.Mode1;
            _clearedOptionIds.Clear();
        }

        public static void DeleteSlotData(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex > 2)
            {
                return;
            }

            PlayerPrefs.DeleteKey(ProgressKey(slotIndex, CampaignMode.Mode1));
            PlayerPrefs.DeleteKey(ProgressKey(slotIndex, CampaignMode.Mode2));
            PlayerPrefs.Save();
        }

        public bool IsCleared(string optionId)
        {
            return !string.IsNullOrEmpty(optionId) && _clearedOptionIds.Contains(optionId);
        }

        /// <summary>
        /// Mark option cleared and persist immediately. Idempotent.
        /// </summary>
        public bool MarkCleared(string optionId)
        {
            if (_slotIndex < 0)
            {
                Debug.LogWarning("[LevelRouteProgress] MarkCleared ignored — no save slot bound.");
                return false;
            }

            if (string.IsNullOrEmpty(optionId))
            {
                return false;
            }

            if (!_clearedOptionIds.Add(optionId))
            {
                return false;
            }

            Persist();
            return true;
        }

        private void ApplyLoaded(LevelRouteProgressSaveData data)
        {
            if (data == null || data.ClearedOptionIds == null)
            {
                return;
            }

            for (var i = 0; i < data.ClearedOptionIds.Length; i++)
            {
                var id = data.ClearedOptionIds[i];
                if (string.IsNullOrEmpty(id))
                {
                    continue;
                }

                _clearedOptionIds.Add(id.Trim());
            }
        }

        private void Persist()
        {
            if (_slotIndex < 0)
            {
                return;
            }

            var list = new List<string>(_clearedOptionIds.Count);
            foreach (var id in _clearedOptionIds)
            {
                list.Add(id);
            }

            list.Sort(StringComparer.Ordinal);

            var data = new LevelRouteProgressSaveData
            {
                ClearedOptionIds = list.ToArray()
            };

            var key = ProgressKey(_slotIndex, _campaignMode);
            PlayerPrefs.SetString(key, JsonUtility.ToJson(data));
            PlayerPrefs.Save();
        }

        private static string ProgressKey(int slotIndex, CampaignMode mode)
        {
            return SaveSlotPrefsKeys.DataKey(slotIndex, mode, SaveSlotPrefsKeys.LevelRouteProgressSuffix);
        }
    }
}
