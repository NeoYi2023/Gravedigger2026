namespace Gravedigger2026.Core.Config
{
    /// <summary>
    /// One LossOfControl tier row (SPEC_04 §9.20).
    /// </summary>
    public sealed class LossOfControlConfigRow
    {
        public int TierId;
        public string DisplayName;
        public string Description;
        public float LossOfControlChance;
    }
}
