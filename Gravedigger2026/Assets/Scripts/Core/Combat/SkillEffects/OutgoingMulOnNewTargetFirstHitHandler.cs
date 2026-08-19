using Gravedigger2026.Core.Config;
using UnityEngine;

namespace Gravedigger2026.Core.Combat.SkillEffects
{
    /// <summary>Skill_04 先发制人 — first AA on a newly selected target multiplies outgoing damage.</summary>
    public sealed class OutgoingMulOnNewTargetFirstHitHandler : ISkillEffectHandler
    {
        private static readonly string[] AllowedKeys = { "Mul" };

        public string EffectKind => SkillEffectKind.OutgoingMulOnNewTargetFirstHit;

        public void Apply(SkillEffectContext context, SkillEffectConfigRow effectRow)
        {
            if (context == null || effectRow == null || !context.IsNewTargetFirstHit)
            {
                return;
            }

            var map = SkillEffectParams.Parse(effectRow.EffectParams, AllowedKeys);
            if (!SkillEffectParams.TryGetFloat(map, "Mul", out var mul) || mul <= 0f)
            {
                Debug.LogWarning(
                    $"[SkillEffect] {effectRow.SkillEffectId}: missing/invalid Mul in EffectParams.");
                return;
            }

            context.OutgoingDamage *= mul;
        }
    }
}
