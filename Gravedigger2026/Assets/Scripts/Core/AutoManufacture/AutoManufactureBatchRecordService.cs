using System;
using System.Collections.Generic;
using UnityEngine;

namespace Gravedigger2026.Core.AutoManufacture
{
    /// <summary>
    /// Last AutoManufacture batch WarriorIds (SPEC_03 §3.15 / D-054 Approach A).
    /// PlayerPrefs JSON per slot + CampaignMode; Replace → immediate write when bound.
    /// </summary>
    public sealed class AutoManufactureBatchRecordService
    {
        private readonly List<string> _warriorIds = new List<string>();
        private int _slotIndex = -1;
        private CampaignMode _campaignMode = CampaignMode.Mode1;
        private bool _suppressPersist;

        public int BoundSlotIndex => _slotIndex;
        public CampaignMode BoundCampaignMode => _campaignMode;
        public IReadOnlyList<string> WarriorIds => _warriorIds;

        public void BindSlot(int slotIndex, CampaignMode campaignMode)
        {
            if (slotIndex < 0 || slotIndex > 2)
            {
                throw new ArgumentOutOfRangeException(nameof(slotIndex), slotIndex, "Slot index must be 0..2.");
            }

            _suppressPersist = true;
            try
            {
                _slotIndex = slotIndex;
                _campaignMode = campaignMode;
                _warriorIds.Clear();

                var raw = PlayerPrefs.GetString(PrefsKey(slotIndex, campaignMode), string.Empty);
                if (!string.IsNullOrEmpty(raw))
                {
                    var data = JsonUtility.FromJson<AutoManufactureBatchSaveData>(raw);
                    if (data != null && data.WarriorIds != null)
                    {
                        for (var i = 0; i < data.WarriorIds.Length; i++)
                        {
                            var id = data.WarriorIds[i];
                            if (!string.IsNullOrEmpty(id))
                            {
                                _warriorIds.Add(id);
                            }
                        }
                    }
                }
            }
            finally
            {
                _suppressPersist = false;
            }

            Debug.Log(
                $"[AutoManufactureBatch] Bound slot={slotIndex} mode={campaignMode} count={_warriorIds.Count}");
        }

        public void ClearBound()
        {
            _slotIndex = -1;
            _campaignMode = CampaignMode.Mode1;
            _warriorIds.Clear();
        }

        /// <summary>Replace last-batch Ids (including empty) and persist when bound.</summary>
        public void Replace(IReadOnlyList<string> warriorIds)
        {
            _warriorIds.Clear();
            if (warriorIds != null)
            {
                for (var i = 0; i < warriorIds.Count; i++)
                {
                    var id = warriorIds[i];
                    if (!string.IsNullOrEmpty(id))
                    {
                        _warriorIds.Add(id);
                    }
                }
            }

            PersistIfBound();
            Debug.Log($"[AutoManufactureBatch] Replace count={_warriorIds.Count} slot={_slotIndex} mode={_campaignMode}");
        }

        public static void DeleteSlotData(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex > 2)
            {
                return;
            }

            PlayerPrefs.DeleteKey(PrefsKey(slotIndex, CampaignMode.Mode1));
            PlayerPrefs.DeleteKey(PrefsKey(slotIndex, CampaignMode.Mode2));
            PlayerPrefs.Save();
        }

        private void PersistIfBound()
        {
            if (_suppressPersist || _slotIndex < 0)
            {
                return;
            }

            var data = new AutoManufactureBatchSaveData
            {
                WarriorIds = _warriorIds.ToArray()
            };
            PlayerPrefs.SetString(PrefsKey(_slotIndex, _campaignMode), JsonUtility.ToJson(data));
            PlayerPrefs.Save();
        }

        private static string PrefsKey(int slotIndex, CampaignMode mode)
        {
            return SaveSlotPrefsKeys.DataKey(slotIndex, mode, SaveSlotPrefsKeys.AutoManufactureBatchSuffix);
        }
    }
}
