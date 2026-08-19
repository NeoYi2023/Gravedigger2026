using System.Collections.Generic;
using Gravedigger2026.Core.Config;
using UnityEngine;

namespace Gravedigger2026.Core.Combat.SkillEffects
{
    /// <summary>Skill_07 冰冻 — AA hit AOE slow on living monsters around the hit target.</summary>
    public sealed class OnAaHitAoeSlowHandler : ISkillEffectHandler
    {
        private static readonly string[] AllowedKeys =
        {
            "Radius",
            "SlowMoveMul",
            "SlowAttackMul",
            "DurationSeconds",
            "InternalCooldownSeconds"
        };

        public string EffectKind => SkillEffectKind.OnAaHitAoeSlow;

        public void Apply(SkillEffectContext context, SkillEffectConfigRow effectRow)
        {
            if (context == null || effectRow == null || context.Warrior == null || context.CombatStatus == null)
            {
                return;
            }

            if (context.Warrior.IsRebel)
            {
                return;
            }

            if (!context.HasHitCenterXZ)
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
            if (!SkillEffectParams.TryGetFloat(map, "Radius", out var radius) || radius < 0f)
            {
                Debug.LogWarning(
                    $"[SkillEffect] {effectRow.SkillEffectId}: missing/invalid Radius.");
                return;
            }

            if (!SkillEffectParams.TryGetFloat(map, "SlowMoveMul", out var moveMul) || moveMul < 0f)
            {
                Debug.LogWarning(
                    $"[SkillEffect] {effectRow.SkillEffectId}: missing/invalid SlowMoveMul.");
                return;
            }

            if (!SkillEffectParams.TryGetFloat(map, "SlowAttackMul", out var attackMul) || attackMul < 0f)
            {
                Debug.LogWarning(
                    $"[SkillEffect] {effectRow.SkillEffectId}: missing/invalid SlowAttackMul.");
                return;
            }

            if (!SkillEffectParams.TryGetFloat(map, "DurationSeconds", out var duration) || duration <= 0f)
            {
                Debug.LogWarning(
                    $"[SkillEffect] {effectRow.SkillEffectId}: missing/invalid DurationSeconds.");
                return;
            }

            var fallbackCd = skillRow.BaseCooldownSeconds;
            if (SkillEffectParams.TryGetFloat(map, "InternalCooldownSeconds", out var paramCd) && paramCd > 0f)
            {
                fallbackCd = paramCd;
            }

            var center = context.HitCenterXZ;
            var radiusSqr = radius * radius;
            var slowed = 0;
            var monsters = context.AliveMonstersXZ;
            if (monsters != null)
            {
                for (var i = 0; i < monsters.Count; i++)
                {
                    var entry = monsters[i];
                    if (string.IsNullOrEmpty(entry.RuntimeId))
                    {
                        continue;
                    }

                    var dx = entry.PositionXZ.x - center.x;
                    var dz = entry.PositionXZ.y - center.y;
                    if (dx * dx + dz * dz > radiusSqr)
                    {
                        continue;
                    }

                    context.CombatStatus.ApplyMonsterSlow(
                        entry.RuntimeId,
                        skillRow.SkillId,
                        duration,
                        moveMul,
                        attackMul);
                    slowed++;
                }
            }

            CommitInternalCooldown(context.Warrior, skillRow.SkillId, fallbackCd);
            context.CommittedInternalCooldown = true;
            context.TriggeredSkillId = skillRow.SkillId;
            Debug.Log(
                $"[SkillEffect] {skillRow.SkillId} AOE Slow PROC radius={radius:0.##} " +
                $"moveMul={moveMul:0.##} atkMul={attackMul:0.##} sec={duration:0.##} " +
                $"hit={context.TargetMonsterRuntimeId} slowed={slowed}");
        }

        private static bool IsInternalCdActive(Defend.DefendCombatWarriorState warrior, string skillId)
        {
            return warrior?.SkillInternalCdRemaining != null
                   && warrior.SkillInternalCdRemaining.TryGetValue(skillId, out var remaining)
                   && remaining > 0f;
        }

        private static void CommitInternalCooldown(
            Defend.DefendCombatWarriorState warrior,
            string skillId,
            float fallbackSeconds)
        {
            if (warrior == null || string.IsNullOrWhiteSpace(skillId))
            {
                return;
            }

            var cdSeconds = fallbackSeconds;
            if (warrior.SkillInternalCooldownSeconds != null
                && warrior.SkillInternalCooldownSeconds.TryGetValue(skillId, out var seeded)
                && seeded > 0f)
            {
                cdSeconds = seeded;
            }

            if (cdSeconds <= 0f)
            {
                return;
            }

            if (warrior.SkillInternalCdRemaining == null)
            {
                warrior.SkillInternalCdRemaining = new Dictionary<string, float>(System.StringComparer.Ordinal);
            }

            warrior.SkillInternalCdRemaining[skillId] = cdSeconds;
        }
    }
}
