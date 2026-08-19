namespace Gravedigger2026.Core.Config
{
    /// <summary>
    /// One row of Combat_FormationBondConfig (SPEC_04 §9.26).
    /// </summary>
    public sealed class FormationBondConfigRow
    {
        public string BondId;
        public int BondLevel;
        public string DisplayName;
        public string IconAssetId;
        public string Description;
        public string ActivationCondition;
        /// <summary>FK → SkillEffectConfig.SkillEffectId.</summary>
        public string BondBuff;
    }
}
