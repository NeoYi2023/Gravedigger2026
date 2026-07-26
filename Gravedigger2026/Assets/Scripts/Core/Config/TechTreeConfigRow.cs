namespace Gravedigger2026.Core.Config
{
    /// <summary>
    /// One TechTreeConfig row (SPEC_04 §9.16).
    /// </summary>
    public sealed class TechTreeConfigRow
    {
        public string TechId;
        public string IconId;
        public string DisplayName;
        public string EffectDescription;
        public string[] UnlockNextTechIds;
        public bool InitiallyUnlocked;
        public int LearnCost;
        public TechUiFrameType TechUiFrameType;
    }
}
