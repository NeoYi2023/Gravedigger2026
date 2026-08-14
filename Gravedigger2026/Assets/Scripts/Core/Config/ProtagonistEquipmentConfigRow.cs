namespace Gravedigger2026.Core.Config
{
    /// <summary>
    /// One row of Protagonist_ProtagonistEquipmentConfig (SPEC_04 §9.25).
    /// Composite PK: EquipId + EquipLevel.
    /// </summary>
    public sealed class ProtagonistEquipmentConfigRow
    {
        public string EquipId;
        /// <summary>≥ 1.</summary>
        public int EquipLevel;
        public string DisplayName;
        public string IconAssetId;
        /// <summary>≤0 or missing on CSV → max-level row (no further level-up).</summary>
        public int ExpToNextLevel;
        public int ConvertExpValue;
        /// <summary>Domain or Domain|Domain|… (Dig / SoldierManufacture / Combat).</summary>
        public string EffectDomain;
        /// <summary>Dig: Attr_Value|… same style as TechEffect AttributeModifiers; other domains TBD.</summary>
        public string EquipEffect;
        public string Description;
    }
}
