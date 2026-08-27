using UnityEngine;

namespace Gravedigger2026.Meta
{
    /// <summary>
    /// Fixed 3-slot local occupied flags via PlayerPrefs (SPEC_03 §3.4 / SPEC_04 §6).
    /// </summary>
    public sealed class SaveSlotService
    {
        public const int SlotCount = 3;

        private const string KeyPrefix = "Gravedigger2026.SaveSlot.";
        private const string OccupiedSuffix = ".Occupied";

        private readonly bool[] _occupied = new bool[SlotCount];

        public void Load()
        {
            for (var i = 0; i < SlotCount; i++)
            {
                _occupied[i] = PlayerPrefs.GetInt(OccupiedKey(i), 0) == 1;
            }
        }

        public bool IsOccupied(int slotIndex)
        {
            ValidateIndex(slotIndex);
            return _occupied[slotIndex];
        }

        public bool HasAnyOccupied()
        {
            for (var i = 0; i < SlotCount; i++)
            {
                if (_occupied[i])
                {
                    return true;
                }
            }

            return false;
        }

        public void Create(int slotIndex)
        {
            ValidateIndex(slotIndex);
            if (_occupied[slotIndex])
            {
                Debug.LogWarning($"SaveSlotService.Create: slot {slotIndex} already occupied.");
                return;
            }

            _occupied[slotIndex] = true;
            Persist(slotIndex);
        }

        public void Delete(int slotIndex)
        {
            ValidateIndex(slotIndex);
            _occupied[slotIndex] = false;
            Persist(slotIndex);
        }

        private void Persist(int slotIndex)
        {
            PlayerPrefs.SetInt(OccupiedKey(slotIndex), _occupied[slotIndex] ? 1 : 0);
            PlayerPrefs.Save();
        }

        private static string OccupiedKey(int slotIndex)
        {
            return KeyPrefix + slotIndex + OccupiedSuffix;
        }

        private static void ValidateIndex(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= SlotCount)
            {
                throw new System.ArgumentOutOfRangeException(nameof(slotIndex), slotIndex, "Slot index must be 0..2.");
            }
        }
    }
}
