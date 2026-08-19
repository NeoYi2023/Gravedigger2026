using System.Collections.Generic;
using Gravedigger2026.Core.Config;
using Gravedigger2026.Core.Defend;
using UnityEngine;

namespace Gravedigger2026.Core.Combat.SkillEffects
{
    /// <summary>Skill_09 渐入佳境 — timed stack bonus applied on outgoing damage settle.</summary>
    public sealed class StackingOutgoingMulTimedHandler : ISkillEffectHandler
    {
        private static readonly string[] AllowedKeys = { "StackBonus", "MaxTotalBonus", "TickSeconds" };

        public string EffectKind => SkillEffectKind.StackingOutgoingMulTimed;

        public void Apply(SkillEffectContext context, SkillEffectConfigRow effectRow)
        {
            if (context == null || effectRow == null || context.Warrior == null)
            {
                return;
            }

            if (context.Warrior.IsRebel)
            {
                return;
            }

            var hook = context.DispatchTriggerHook;
            if (string.Equals(hook, SkillEffectTriggerHook.OnSkillInternalCooldown, System.StringComparison.Ordinal))
            {
                ApplyStackTick(context, effectRow);
            }
            else if (string.Equals(hook, SkillEffectTriggerHook.OnOutgoingDamageSettle, System.StringComparison.Ordinal))
            {
                ApplyOutgoingMul(context);
            }
        }

        public static bool TryParseTickSeconds(SkillEffectConfigRow effectRow, SkillConfigRow skillRow, out float tickSeconds)
        {
            tickSeconds = 0f;
            if (effectRow == null)
            {
                return false;
            }

            var map = SkillEffectParams.Parse(effectRow.EffectParams, AllowedKeys);
            if (SkillEffectParams.TryGetFloat(map, "TickSeconds", out var fromParams) && fromParams > 0f)
            {
                tickSeconds = fromParams;
                return true;
            }

            if (skillRow != null && skillRow.BaseCooldownSeconds > 0f)
            {
                tickSeconds = skillRow.BaseCooldownSeconds;
                return true;
            }

            return false;
        }

        private static void ApplyStackTick(SkillEffectContext context, SkillEffectConfigRow effectRow)
        {
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
            if (!SkillEffectParams.TryGetFloat(map, "StackBonus", out var stackBonus) || stackBonus <= 0f)
            {
                Debug.LogWarning(
                    $"[SkillEffect] {effectRow.SkillEffectId}: missing/invalid StackBonus.");
                return;
            }

            if (!SkillEffectParams.TryGetFloat(map, "MaxTotalBonus", out var maxTotalBonus) || maxTotalBonus <= 0f)
            {
                Debug.LogWarning(
                    $"[SkillEffect] {effectRow.SkillEffectId}: missing/invalid MaxTotalBonus.");
                return;
            }

            if (!TryParseTickSeconds(effectRow, skillRow, out var tickSeconds) || tickSeconds <= 0f)
            {
                Debug.LogWarning(
                    $"[SkillEffect] {effectRow.SkillEffectId}: missing/invalid TickSeconds.");
                return;
            }

            var stack = GetOrCreateStack(context.Warrior, SkillEffectKind.StackingOutgoingMulTimed);
            var before = stack.CurrentBonus;
            stack.CurrentBonus = Mathf.Min(stack.CurrentBonus + stackBonus, maxTotalBonus);
            if (Mathf.Approximately(stack.CurrentBonus, before))
            {
                CommitInternalCooldown(context.Warrior, skillRow.SkillId, tickSeconds);
                return;
            }

            CommitInternalCooldown(context.Warrior, skillRow.SkillId, tickSeconds);
            context.TriggeredSkillId = skillRow.SkillId;
            context.SkillPersistOn = stack.CurrentBonus > 0f;
            context.SkillPersistSkillId = skillRow.SkillId;
            Debug.Log(
                $"[SkillEffect] {skillRow.SkillId} stack +{stackBonus:0.##} " +
                $"total={stack.CurrentBonus:0.##} cap={maxTotalBonus:0.##} " +
                $"warrior={context.Warrior.WarriorId}");
        }

        private static void ApplyOutgoingMul(SkillEffectContext context)
        {
            var stack = GetStack(context.Warrior, SkillEffectKind.StackingOutgoingMulTimed);
            if (stack == null || stack.CurrentBonus <= 0f)
            {
                return;
            }

            context.OutgoingDamage *= 1f + stack.CurrentBonus;
        }

        private static EffectStackState GetStack(DefendCombatWarriorState warrior, string effectKind)
        {
            if (warrior?.EffectStackByKind == null
                || !warrior.EffectStackByKind.TryGetValue(effectKind, out var stack))
            {
                return null;
            }

            return stack;
        }

        private static EffectStackState GetOrCreateStack(DefendCombatWarriorState warrior, string effectKind)
        {
            if (warrior.EffectStackByKind == null)
            {
                warrior.EffectStackByKind =
                    new Dictionary<string, EffectStackState>(System.StringComparer.Ordinal);
            }

            if (!warrior.EffectStackByKind.TryGetValue(effectKind, out var stack) || stack == null)
            {
                stack = new EffectStackState();
                warrior.EffectStackByKind[effectKind] = stack;
            }

            return stack;
        }

        private static bool IsInternalCdActive(DefendCombatWarriorState warrior, string skillId)
        {
            return warrior?.SkillInternalCdRemaining != null
                   && warrior.SkillInternalCdRemaining.TryGetValue(skillId, out var remaining)
                   && remaining > 0f;
        }

        private static void CommitInternalCooldown(DefendCombatWarriorState warrior, string skillId, float seconds)
        {
            if (warrior == null || string.IsNullOrWhiteSpace(skillId) || seconds <= 0f)
            {
                return;
            }

            if (warrior.SkillInternalCdRemaining == null)
            {
                warrior.SkillInternalCdRemaining = new Dictionary<string, float>(System.StringComparer.Ordinal);
            }

            warrior.SkillInternalCdRemaining[skillId] = seconds;
        }
    }
}
