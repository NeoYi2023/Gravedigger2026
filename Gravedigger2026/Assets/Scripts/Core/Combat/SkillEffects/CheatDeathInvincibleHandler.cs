using Gravedigger2026.Core.Config;
using UnityEngine;

namespace Gravedigger2026.Core.Combat.SkillEffects
{
    /// <summary>Skill_05 坚挺 — lethal hit intercepts to HP=1 + timed invincibility.</summary>
    public sealed class CheatDeathInvincibleHandler : ISkillEffectHandler
    {
        private static readonly string[] AllowedKeys = { "InvincibleSeconds" };

        public string EffectKind => SkillEffectKind.CheatDeathInvincible;

        public void Apply(SkillEffectContext context, SkillEffectConfigRow effectRow)
        {
            if (context == null || effectRow == null || context.Warrior == null || context.WouldDieIntercepted)
            {
                return;
            }

            if (context.IncomingDamage <= 0f)
            {
                return;
            }

            if (context.Warrior.RemainingHp - context.IncomingDamage > 0f)
            {
                return;
            }

            var skillRow = context.CurrentSkillRow;
            if (skillRow == null || string.IsNullOrWhiteSpace(skillRow.SkillId))
            {
                return;
            }

            if (IsInternalCdActive(context.Warrior, skillRow.SkillId))
            {
                return;
            }

            var map = SkillEffectParams.Parse(effectRow.EffectParams, AllowedKeys);
            if (!SkillEffectParams.TryGetFloat(map, "InvincibleSeconds", out var invincibleSeconds)
                || invincibleSeconds <= 0f)
            {
                Debug.LogWarning(
                    $"[SkillEffect] {effectRow.SkillEffectId}: missing/invalid InvincibleSeconds.");
                return;
            }

            context.WouldDieIntercepted = true;
            context.TriggeredSkillId = skillRow.SkillId;
            context.Warrior.RemainingHp = 1f;
            context.CombatStatus?.ApplyWarriorInvincible(
                context.Warrior.WarriorId,
                skillRow.SkillId,
                invincibleSeconds);
            CommitInternalCooldown(context.Warrior, skillRow.SkillId);
        }

        private static bool IsInternalCdActive(Defend.DefendCombatWarriorState warrior, string skillId)
        {
            return warrior?.SkillInternalCdRemaining != null
                   && warrior.SkillInternalCdRemaining.TryGetValue(skillId, out var remaining)
                   && remaining > 0f;
        }

        private static void CommitInternalCooldown(Defend.DefendCombatWarriorState warrior, string skillId)
        {
            if (warrior?.SkillInternalCooldownSeconds == null
                || !warrior.SkillInternalCooldownSeconds.TryGetValue(skillId, out var cdSeconds)
                || cdSeconds <= 0f)
            {
                return;
            }

            if (warrior.SkillInternalCdRemaining == null)
            {
                warrior.SkillInternalCdRemaining =
                    new System.Collections.Generic.Dictionary<string, float>(System.StringComparer.Ordinal);
            }

            warrior.SkillInternalCdRemaining[skillId] = cdSeconds;
        }
    }
}
