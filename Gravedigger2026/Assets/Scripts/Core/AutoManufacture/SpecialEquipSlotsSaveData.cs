using System;

namespace Gravedigger2026.Core.AutoManufacture
{
    /// <summary>
    /// PlayerPrefs JSON DTO for protagonist MagicBook slots (SPEC_04 §6 / §9.24).
    /// </summary>
    [Serializable]
    public sealed class SpecialEquipSlotsSaveData
    {
        public const int SlotCount = 6;

        /// <summary>Length 6; empty slot = "".</summary>
        public string[] MagicBookIds = CreateEmptySlots();

        public static string[] CreateEmptySlots()
        {
            return new[] { string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty };
        }
    }
}
