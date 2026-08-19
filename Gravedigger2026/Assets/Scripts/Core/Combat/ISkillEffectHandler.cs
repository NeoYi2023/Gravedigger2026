using Gravedigger2026.Core.Config;

namespace Gravedigger2026.Core.Combat
{
    public interface ISkillEffectHandler
    {
        string EffectKind { get; }
        void Apply(SkillEffectContext context, SkillEffectConfigRow effectRow);
    }
}
