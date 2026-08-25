namespace Gravedigger2026.Core.Config
{
    /// <summary>One row of Audio_BgmConfig.csv (SPEC_04 §9.29).</summary>
    public sealed class BgmConfigRow
    {
        public string BgmId;
        public string Context;
        public string ClipId;
        public bool Loop;
        public int Weight;
        public float Volume;
    }
}
