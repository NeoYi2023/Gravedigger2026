namespace Gravedigger2026.Core.Config
{
    /// <summary>Unified reward item catalog row (SPEC_04 §9.5a).</summary>
    public sealed class ItemCatalogConfigRow
    {
        public string ItemId;
        public string DisplayName;
        public string IconAssetId;
        public string ItemType;
        public string SourceTable;
        public string Description;
        public int SellPrice;
    }
}
