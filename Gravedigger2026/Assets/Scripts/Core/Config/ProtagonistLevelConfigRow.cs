namespace Gravedigger2026.Core.Config
{
    /// <summary>
    /// One row of Manufacture_ProtagonistLevelConfig (SPEC_04 §9.8).
    /// </summary>
    public sealed class ProtagonistLevelConfigRow
    {
        public int Level;
        public long RequiredTotalExperience;
        public string UnlockedFeatureIds;
        public int TechPointsReward;
        public int ControlPowerCap;
        public int ProtagonistMaxHP;
    }
}
