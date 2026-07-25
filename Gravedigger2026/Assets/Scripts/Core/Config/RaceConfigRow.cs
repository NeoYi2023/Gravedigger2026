namespace Gravedigger2026.Core.Config
{
    /// <summary>
    /// One row of Manufacture_RaceConfig (SPEC_04 §9.11).
    /// </summary>
    public sealed class RaceConfigRow
    {
        public string RaceId;
        public string DisplayNameKey;
        public StatBlock RaceAdjustCoeff;
        public float LossOfControlChanceBonus;
    }
}
