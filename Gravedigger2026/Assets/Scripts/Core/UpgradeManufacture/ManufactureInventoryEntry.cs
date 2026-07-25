namespace Gravedigger2026.Core.UpgradeManufacture
{
    /// <summary>
    /// One placeable warehouse item line shown by the manufacture panel.
    /// </summary>
    public sealed class ManufactureInventoryEntry
    {
        public string ItemId;
        public string Label;
        public int Available;
        public ManufactureSlotKind SlotKind;
    }
}
