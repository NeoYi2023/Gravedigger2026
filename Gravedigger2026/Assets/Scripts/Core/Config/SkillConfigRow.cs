namespace Gravedigger2026.Core.Config
{
    /// <summary>
    /// One row of Combat_SkillConfig (SPEC_04 §9.21).
    /// Composite PK: SkillId + SkillLevel.
    /// </summary>
    public sealed class SkillConfigRow
    {
        public string SkillId;
        /// <summary>≥ 1.</summary>
        public int SkillLevel;
        /// <summary>Mode1 | Mode2; Mode2 CD starts on cast commit (D-069).</summary>
        public string CooldownMode;
        public string CastTarget;
        public string ExtraActivationCondition;
        public string DisplayName;
        public string Description;
        /// <summary>Missing/empty = no icon.</summary>
        public string IconAssetId;
        /// <summary>FK → SkillEffectConfig; effect body not loaded this slice.</summary>
        public string SkillEffectId;
        /// <summary>UI-021 tint only (D-070). Missing/empty column → false. Does not drive combat.</summary>
        public bool EffectImplemented;
        /// <summary>≥ 0; feeds SkillCooldown formula when the skill is cast (D-069).</summary>
        public float BaseCooldownSeconds;
        /// <summary>+/- ; missing → 0.</summary>
        public float LossOfControlChanceBonus;
    }
}
