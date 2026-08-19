using Gravedigger2026.Core.Config;
using UnityEngine;

namespace Gravedigger2026.Core.Combat.SkillEffects
{
    /// <summary>Skill_11 灼烧 — AA hit applies Burn DoT on the hit target (RefreshDuration).</summary>
    public sealed class OnAaHitApplyBurnHandler : ISkillEffectHandler
    {
        private const string StackModeRefreshDuration = "RefreshDuration";

        private static readonly string[] AllowedKeys =
        {
            "TickDamageMul",
            "TickIntervalSeconds",
            "DurationSeconds",
            "StackMode"
        };

        public string EffectKind => SkillEffectKind.OnAaHitApplyBurn;

        public void Apply(SkillEffectContext context, SkillEffectConfigRow effectRow)
        {
            if (context == null || effectRow == null || context.Warrior == null || context.CombatStatus == null)
            {
                return;
            }

            if (string.IsNullOrEmpty(context.TargetMonsterRuntimeId))
            {
                return;
            }

            if (context.TargetMonster != null
                && (!context.TargetMonster.IsAlive || context.TargetMonster.RemainingHp <= 0f))
            {
                return;
            }

            var skillRow = context.CurrentSkillRow;
            if (skillRow == null || string.IsNullOrWhiteSpace(skillRow.SkillId))
            {
                return;
            }

            var map = SkillEffectParams.Parse(effectRow.EffectParams, AllowedKeys);
            if (!SkillEffectParams.TryGetFloat(map, "TickDamageMul", out var tickDamageMul) || tickDamageMul <= 0f)
            {
                Debug.LogWarning(
                    $"[SkillEffect] {effectRow.SkillEffectId}: missing/invalid TickDamageMul.");
                return;
            }

            if (!SkillEffectParams.TryGetFloat(map, "TickIntervalSeconds", out var tickInterval) || tickInterval <= 0f)
            {
                Debug.LogWarning(
                    $"[SkillEffect] {effectRow.SkillEffectId}: missing/invalid TickIntervalSeconds.");
                return;
            }

            if (!SkillEffectParams.TryGetFloat(map, "DurationSeconds", out var duration) || duration <= 0f)
            {
                Debug.LogWarning(
                    $"[SkillEffect] {effectRow.SkillEffectId}: missing/invalid DurationSeconds.");
                return;
            }

            var stackMode = StackModeRefreshDuration;
            if (SkillEffectParams.TryGet(map, "StackMode", out var stackModeText)
                && !string.IsNullOrWhiteSpace(stackModeText))
            {
                stackMode = stackModeText.Trim();
            }

            var tickDamage = context.Warrior.NormalAttackPower * tickDamageMul;
            if (tickDamage <= 0f)
            {
                return;
            }

            context.CombatStatus.ApplyMonsterBurn(
                context.TargetMonsterRuntimeId,
                skillRow.SkillId,
                context.Warrior.WarriorId,
                duration,
                tickInterval,
                tickDamage,
                stackMode);

            context.TriggeredSkillId = skillRow.SkillId;
            Debug.Log(
                $"[SkillEffect] {skillRow.SkillId} Burn hit={context.TargetMonsterRuntimeId} " +
                $"tick={tickDamage:0.##} every={tickInterval:0.##}s for={duration:0.##}s " +
                $"mode={stackMode}");
        }
    }
}
