namespace Gravedigger2026.Core.Config
{
    /// <summary>
    /// One row of Manufacture_BodyPartConfig (SPEC_04 §9.12).
    /// </summary>
    public sealed class BodyPartConfigRow
    {
        public string BodyPartId;
        /// <summary>Item display name (SPEC_04 §9.12 DisplayName / 道具名称). Empty → UI falls back to BodyPartId.</summary>
        public string DisplayName;
        public float BodyLevel;
        public BodySlot BodySlot;
        public string RaceId;
        public float ControlPowerCost;
        public float SpiritCost;
        public StatBlock StatBonus;
        public float AutoConvert;
        public string Description;
        public string ArtAssetId;
        /// <summary>Arm only; 1 = Mode2 PrimaryHand (SPEC_04 §9.12). Missing column → 0.</summary>
        public int IsPrimaryHand;
        /// <summary>Mode2 class pool encoding ClassId|… . Missing → empty.</summary>
        public string ClassRestrict;
        /// <summary>Dig HUD primary-hand class bucket; same enum as ClassConfig.BaseClass. Missing → Unspecified.</summary>
        public BaseClassKind BaseClass;
        /// <summary>True when BodyPrimaryStat column present and valid.</summary>
        public bool HasBodyPrimaryStat;
        /// <summary>Mode2 remaining-part matcher; only when HasBodyPrimaryStat.</summary>
        public StatKind BodyPrimaryStat;
    }
}
