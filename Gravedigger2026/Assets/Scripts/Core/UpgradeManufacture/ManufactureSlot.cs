namespace Gravedigger2026.Core.UpgradeManufacture
{
    /// <summary>
    /// One manufacture slot instance: fixed kind + optionally placed item Id.
    /// </summary>
    public sealed class ManufactureSlot
    {
        public ManufactureSlot(ManufactureSlotKind kind)
        {
            Kind = kind;
        }

        public ManufactureSlotKind Kind { get; }
        public string ItemId { get; set; }
        public bool IsEmpty => string.IsNullOrEmpty(ItemId);
    }
}
