namespace Gravedigger2026.Core.Config
{
    /// <summary>
    /// One row of Manufacture_MagicBookConfig (SPEC_04 §9.24).
    /// </summary>
    public sealed class MagicBookConfigRow
    {
        public string MagicBookId;
        /// <summary>1 = same Id cannot stack a second copy.</summary>
        public int IsUnique;
        /// <summary>Phase encoding Phase|Phase|… (e.g. SoldierManufacture|Combat).</summary>
        public string EffectPhase;
        /// <summary>Stub payload this round; empty = no effect.</summary>
        public string EffectPayload;
        public string IconAssetId;
        public string DisplayName;
        public string Description;
    }
}
