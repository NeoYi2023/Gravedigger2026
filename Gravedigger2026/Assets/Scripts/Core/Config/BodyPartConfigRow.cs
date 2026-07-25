namespace Gravedigger2026.Core.Config
{
    /// <summary>
    /// One row of Manufacture_BodyPartConfig (SPEC_04 §9.12).
    /// </summary>
    public sealed class BodyPartConfigRow
    {
        public string BodyPartId;
        public float BodyLevel;
        public BodySlot BodySlot;
        public string RaceId;
        public float ControlPowerCost;
        public float SpiritCost;
        public StatBlock StatBonus;
        public float AutoConvert;
        public string Description;
        public string ArtAssetId;
    }
}
