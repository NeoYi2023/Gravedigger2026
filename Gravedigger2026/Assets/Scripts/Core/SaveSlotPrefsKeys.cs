namespace Gravedigger2026.Core
{
    /// <summary>
    /// PlayerPrefs key helpers for per-slot + per-<see cref="CampaignMode"/> data (SPEC_04 §6).
    /// </summary>
    public static class SaveSlotPrefsKeys
    {
        public const string KeyPrefix = "Gravedigger2026.SaveSlot.";
        public const string WarriorPoolSuffix = ".WarriorPool";
        public const string BattleFormationSuffix = ".BattleFormation";
        public const string DungeonUnlocksSuffix = ".DungeonUnlocks";
        public const string SpecialEquipSlotsSuffix = ".SpecialEquipSlots";
        public const string AutoManufactureBatchSuffix = ".AutoManufactureBatch";
        public const string EquipCommonExpSuffix = ".EquipCommonExp";
        public const string ProtagonistEquipmentWarehouseSuffix = ".ProtagonistEquipmentWarehouse";
        public const string ShopProgressSuffix = ".ShopProgress";

        public static string ModeSegment(CampaignMode mode)
        {
            return ".CampaignMode" + ((int)mode).ToString();
        }

        public static string DataKey(int slotIndex, CampaignMode mode, string suffix)
        {
            return KeyPrefix + slotIndex + ModeSegment(mode) + suffix;
        }

        /// <summary>Pre-CampaignMode legacy key (Mode1 migration source).</summary>
        public static string LegacyDataKey(int slotIndex, string suffix)
        {
            return KeyPrefix + slotIndex + suffix;
        }
    }
}
