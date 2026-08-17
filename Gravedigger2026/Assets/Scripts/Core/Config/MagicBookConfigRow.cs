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
        /// <summary>1 = chance-trigger MagicBook marker; unused this round (SPEC_04 §9.24).</summary>
        public int IsProbabilistic;
        /// <summary>Phase encoding Phase|Phase|… (e.g. SoldierManufacture|Combat).</summary>
        public string EffectPhase;
        /// <summary>Registered PascalCase token; empty = no effect (SPEC_04 §9.24).</summary>
        public string EffectPayload;
        /// <summary>Key=Value|Key=Value|…; empty = none/defaults.</summary>
        public string EffectParams;
        public string IconAssetId;
        public string DisplayName;
        public string Description;
        /// <summary>AllIn1 preset Id; empty = no visual (not a token).</summary>
        public string VisualStyleId;
        /// <summary>Missing/empty CSV → 0.</summary>
        public int VisualPriority;
        /// <summary>Missing/empty CSV → 1.</summary>
        public float VisualIntensityAdd;
    }
}
