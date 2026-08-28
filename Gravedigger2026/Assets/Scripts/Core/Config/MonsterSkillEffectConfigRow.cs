namespace Gravedigger2026.Core.Config
{
    /// <summary>SPEC_04 §9.21c — one row per MonsterSkillId (PushMap monster skills).</summary>
    public sealed class MonsterSkillEffectConfigRow
    {
        public string MonsterSkillId;
        public string DisplayName;
        public string EffectKind;
        public string EffectParams;
    }
}
