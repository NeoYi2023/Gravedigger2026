namespace Gravedigger2026.Core.Config
{
    /// <summary>
    /// One row of Combat_SkillEffectConfig (SPEC_04 §9.21b).
    /// </summary>
    public sealed class SkillEffectConfigRow
    {
        public string SkillEffectId;
        public string Notes;
        /// <summary>Registered PascalCase token; empty = not wired.</summary>
        public string EffectKind;
        /// <summary>Key=Value|Key=Value|…</summary>
        public string EffectParams;
        /// <summary>Pipeline hook enum string.</summary>
        public string TriggerHook;
    }
}
