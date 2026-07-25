namespace Gravedigger2026.Core.Config
{
    /// <summary>
    /// One row of Manufacture_GemConfig (SPEC_04 §9.10).
    /// </summary>
    public sealed class GemConfigRow
    {
        public string GemId;
        public GemType GemType;
        public StatBlock GemMult;
        public string Skills;
        public float SpiritCost;
        public float ControlPowerCost;
        public float LossOfControlChanceBonus;
    }
}
