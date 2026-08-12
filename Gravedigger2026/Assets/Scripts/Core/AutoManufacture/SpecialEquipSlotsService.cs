using System;
using Gravedigger2026.Core.Config;
using UnityEngine;

namespace Gravedigger2026.Core.AutoManufacture
{
    /// <summary>
    /// Protagonist MagicBook 6-slot persistence + equip gate (SPEC_03 §3.15 / SPEC_04 §6 / §9.24).
    /// PlayerPrefs JSON per slot + CampaignMode; mutate → immediate write when bound.
    /// </summary>
    public sealed class SpecialEquipSlotsService
    {
        public const int SlotCount = SpecialEquipSlotsSaveData.SlotCount;

        private readonly ConfigCsvRepository _configs;
        private readonly string[] _slots = SpecialEquipSlotsSaveData.CreateEmptySlots();
        private int _slotIndex = -1;
        private CampaignMode _campaignMode = CampaignMode.Mode1;

        public SpecialEquipSlotsService(ConfigCsvRepository configs)
        {
            _configs = configs ?? throw new ArgumentNullException(nameof(configs));
        }

        public int BoundSlotIndex => _slotIndex;
        public CampaignMode BoundCampaignMode => _campaignMode;

        /// <summary>Length always <see cref="SlotCount"/>; empty = "".</summary>
        public string GetSlot(int index)
        {
            if (index < 0 || index >= SlotCount)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            return _slots[index] ?? string.Empty;
        }

        public void BindSlot(int slotIndex, CampaignMode campaignMode)
        {
            if (slotIndex < 0 || slotIndex > 2)
            {
                throw new ArgumentOutOfRangeException(nameof(slotIndex), slotIndex, "Slot index must be 0..2.");
            }

            _slotIndex = slotIndex;
            _campaignMode = campaignMode;
            ClearLocalSlots();

            var key = PrefsKey(slotIndex, campaignMode);
            var raw = PlayerPrefs.GetString(key, string.Empty);
            if (!string.IsNullOrEmpty(raw))
            {
                var data = JsonUtility.FromJson<SpecialEquipSlotsSaveData>(raw);
                ApplyLoaded(data);
            }

            Debug.Log(
                $"[SpecialEquipSlots] Bound slot={slotIndex} mode={campaignMode} slots=[{FormatSlots()}]");
        }

        public void ClearBound()
        {
            _slotIndex = -1;
            _campaignMode = CampaignMode.Mode1;
            ClearLocalSlots();
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

        /// <summary>
        /// Equip into the first empty slot. Rejects when full, unknown Id, or unique already equipped.
        /// </summary>
        public bool TryEquip(string magicBookId, out string error)
        {
            error = null;
            if (_slotIndex < 0)
            {
                error = "No save slot bound.";
                return false;
            }

            var id = NormalizeId(magicBookId);
            if (id == null)
            {
                error = "MagicBookId is empty.";
                return false;
            }

            if (!PassesUniqueGate(id, out error))
            {
                return false;
            }

            for (var i = 0; i < SlotCount; i++)
            {
                if (!string.IsNullOrEmpty(_slots[i]))
                {
                    continue;
                }

                _slots[i] = id;
                Persist();
                Debug.Log(
                    $"[SpecialEquipSlots] Equip '{id}' → index={i} slot={_slotIndex} mode={_campaignMode} slots=[{FormatSlots()}]");
                return true;
            }

            error = "Special equip slots are full.";
            return false;
        }

        /// <summary>Equip into a specific index (overwrite empty only; use Unequip first to replace).</summary>
        public bool TryEquipAt(int index, string magicBookId, out string error)
        {
            error = null;
            if (_slotIndex < 0)
            {
                error = "No save slot bound.";
                return false;
            }

            if (index < 0 || index >= SlotCount)
            {
                error = $"Slot index must be 0..{SlotCount - 1}.";
                return false;
            }

            if (!string.IsNullOrEmpty(_slots[index]))
            {
                error = $"Slot {index} is occupied.";
                return false;
            }

            var id = NormalizeId(magicBookId);
            if (id == null)
            {
                error = "MagicBookId is empty.";
                return false;
            }

            if (!PassesUniqueGate(id, out error))
            {
                return false;
            }

            _slots[index] = id;
            Persist();
            Debug.Log(
                $"[SpecialEquipSlots] EquipAt '{id}' → index={index} slot={_slotIndex} mode={_campaignMode} slots=[{FormatSlots()}]");
            return true;
        }

        public bool TryUnequip(int index, out string error)
        {
            error = null;
            if (_slotIndex < 0)
            {
                error = "No save slot bound.";
                return false;
            }

            if (index < 0 || index >= SlotCount)
            {
                error = $"Slot index must be 0..{SlotCount - 1}.";
                return false;
            }

            if (string.IsNullOrEmpty(_slots[index]))
            {
                error = $"Slot {index} is already empty.";
                return false;
            }

            var removed = _slots[index];
            _slots[index] = string.Empty;
            Persist();
            Debug.Log(
                $"[SpecialEquipSlots] Unequip '{removed}' ← index={index} slot={_slotIndex} mode={_campaignMode} slots=[{FormatSlots()}]");
            return true;
        }

        /// <summary>Enumerates non-empty equipped MagicBookIds (order = slot index).</summary>
        public void ForEachEquipped(Action<int, string> visitor)
        {
            if (visitor == null)
            {
                return;
            }

            for (var i = 0; i < SlotCount; i++)
            {
                var id = _slots[i];
                if (!string.IsNullOrEmpty(id))
                {
                    visitor(i, id);
                }
            }
        }

        private void Persist()
        {
            if (_slotIndex < 0)
            {
                return;
            }

            var data = new SpecialEquipSlotsSaveData
            {
                MagicBookIds = (string[])_slots.Clone()
            };
            PlayerPrefs.SetString(PrefsKey(_slotIndex, _campaignMode), JsonUtility.ToJson(data));
            PlayerPrefs.Save();
        }

        private void ApplyLoaded(SpecialEquipSlotsSaveData data)
        {
            if (data?.MagicBookIds == null)
            {
                return;
            }

            var n = Math.Min(SlotCount, data.MagicBookIds.Length);
            for (var i = 0; i < n; i++)
            {
                _slots[i] = data.MagicBookIds[i] ?? string.Empty;
            }
        }

        private void ClearLocalSlots()
        {
            for (var i = 0; i < SlotCount; i++)
            {
                _slots[i] = string.Empty;
            }
        }

        /// <summary>
        /// Unique gate from MagicBookConfig. Missing row (empty Demo table) → allow + warn; IsUnique=0.
        /// </summary>
        private bool PassesUniqueGate(string id, out string error)
        {
            error = null;
            if (!_configs.TryGetMagicBook(id, out var row) || row == null)
            {
                Debug.LogWarning(
                    $"[SpecialEquipSlots] MagicBookId '{id}' not in MagicBookConfig — allowing for Demo slot persistence.");
                return true;
            }

            if (row.IsUnique == 1 && CountEquipped(id) > 0)
            {
                error = $"Unique MagicBook '{id}' is already equipped.";
                return false;
            }

            return true;
        }

        private int CountEquipped(string magicBookId)
        {
            var count = 0;
            for (var i = 0; i < SlotCount; i++)
            {
                if (string.Equals(_slots[i], magicBookId, StringComparison.Ordinal))
                {
                    count++;
                }
            }

            return count;
        }

        private static string NormalizeId(string magicBookId)
        {
            if (string.IsNullOrEmpty(magicBookId))
            {
                return null;
            }

            var id = magicBookId.Trim();
            return id.Length == 0 ? null : id;
        }

        private string FormatSlots()
        {
            return string.Join("|", _slots);
        }

        private static string PrefsKey(int slotIndex, CampaignMode mode)
        {
            return SaveSlotPrefsKeys.DataKey(slotIndex, mode, SaveSlotPrefsKeys.SpecialEquipSlotsSuffix);
        }
    }
}
