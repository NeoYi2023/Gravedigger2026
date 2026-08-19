using System;
using Gravedigger2026.Core.Config;
using UnityEngine;

namespace Gravedigger2026.Core.Combat.SkillEffects
{
    /// <summary>
    /// Skill_10 贯穿 — extra ranged hits along current projectile velocity
    /// (SPEC_03 §3.12 / SPEC_04 §9.21b RangedPierceExtraHits).
    /// </summary>
    public sealed class RangedPierceExtraHitsHandler : ISkillEffectHandler
    {
        private static readonly string[] AllowedKeys = { "ExtraHitCount", "DamageMul" };

        public string EffectKind => SkillEffectKind.RangedPierceExtraHits;

        public void Apply(SkillEffectContext context, SkillEffectConfigRow effectRow)
        {
            if (context == null || effectRow == null || context.Warrior == null)
            {
                return;
            }

            var skillRow = context.CurrentSkillRow;
            if (skillRow == null || string.IsNullOrWhiteSpace(skillRow.SkillId))
            {
                return;
            }

            var map = SkillEffectParams.Parse(effectRow.EffectParams, AllowedKeys);
            if (!SkillEffectParams.TryGetFloat(map, "ExtraHitCount", out var extraRaw) || extraRaw < 1f)
            {
                Debug.LogWarning(
                    $"[SkillEffect] {effectRow.SkillEffectId}: missing/invalid ExtraHitCount.");
                return;
            }

            if (!SkillEffectParams.TryGetFloat(map, "DamageMul", out var damageMul) || damageMul <= 0f)
            {
                Debug.LogWarning(
                    $"[SkillEffect] {effectRow.SkillEffectId}: missing/invalid DamageMul.");
                return;
            }

            var extraHitCount = Mathf.Max(0, Mathf.RoundToInt(extraRaw));
            var alreadyHit = context.AlreadyHitRuntimeIds != null
                ? context.AlreadyHitRuntimeIds.Count
                : 0;
            var extraUsed = Math.Max(0, alreadyHit - 1);
            var remaining = extraHitCount - extraUsed;
            if (remaining < 0)
            {
                remaining = 0;
            }

            if (extraUsed >= 1)
            {
                context.OutgoingDamage *= damageMul;
            }

            if (remaining > context.ExtraHitsRemaining)
            {
                context.ExtraHitsRemaining = remaining;
            }

            if (extraUsed == 0 && extraHitCount > 0)
            {
                context.TriggeredSkillId = skillRow.SkillId;
            }

            Debug.Log(
                $"[SkillEffect] {skillRow.SkillId} Pierce extraHitCount={extraHitCount} " +
                $"used={extraUsed} remaining={remaining} mul={damageMul:0.##} " +
                $"hit={context.TargetMonsterRuntimeId}");
        }
    }
}
