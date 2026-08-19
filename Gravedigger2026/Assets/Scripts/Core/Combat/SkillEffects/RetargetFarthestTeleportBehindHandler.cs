using System.Collections.Generic;
using Gravedigger2026.Core.Config;
using UnityEngine;

namespace Gravedigger2026.Core.Combat.SkillEffects
{
    /// <summary>
    /// Skill_12 瞬移 — on new target acquire, pick farthest living monster and land behind it.
    /// View supplies SampleWalkableXZ and performs Warp; this handler never touches Transform.
    /// </summary>
    public sealed class RetargetFarthestTeleportBehindHandler : ISkillEffectHandler
    {
        public string EffectKind => SkillEffectKind.RetargetFarthestTeleportBehind;

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

            if (context.HasTeleportOverride)
            {
                return;
            }

            if (!context.HasWarriorPositionXZ || context.SampleWalkableXZ == null)
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

            var monsters = context.AliveMonstersXZ;
            if (monsters == null || monsters.Count == 0)
            {
                return;
            }

            var warriorPos = context.WarriorPositionXZ;
            var bestIndex = -1;
            var bestDistSqr = -1f;
            for (var i = 0; i < monsters.Count; i++)
            {
                var entry = monsters[i];
                if (string.IsNullOrEmpty(entry.RuntimeId))
                {
                    continue;
                }

                var dx = entry.PositionXZ.x - warriorPos.x;
                var dz = entry.PositionXZ.y - warriorPos.y;
                var distSqr = dx * dx + dz * dz;
                if (distSqr <= bestDistSqr)
                {
                    continue;
                }

                bestDistSqr = distSqr;
                bestIndex = i;
            }

            if (bestIndex < 0)
            {
                return;
            }

            var target = monsters[bestIndex];
            var facing = target.FacingXZ;
            if (facing.sqrMagnitude < 1e-8f)
            {
                facing = target.PositionXZ - warriorPos;
            }

            if (facing.sqrMagnitude < 1e-8f)
            {
                facing = new Vector2(0f, -1f);
            }
            else
            {
                facing.Normalize();
            }

            var arrive = context.ArriveEpsilon > 0f ? context.ArriveEpsilon : 0.08f;
            var offset = Mathf.Max(0.05f, context.WarriorBodyRadius)
                         + Mathf.Max(0.05f, target.BodyRadius)
                         + arrive;
            var desired = target.PositionXZ - facing * offset;
            var sampleRadius = Mathf.Max(0.75f, offset);
            var sampled = context.SampleWalkableXZ(desired, sampleRadius);
            if (!sampled.HasValue)
            {
                Debug.Log(
                    $"[SkillEffect] {skillRow.SkillId} blink SamplePosition failed " +
                    $"target={target.RuntimeId} desired=({desired.x:0.##},{desired.y:0.##})");
                return;
            }

            context.OverrideTargetRuntimeId = target.RuntimeId;
            context.TeleportLandingXZ = sampled.Value;
            context.HasTeleportOverride = true;
            CommitInternalCooldown(context.Warrior, skillRow.SkillId, skillRow.BaseCooldownSeconds);
            context.CommittedInternalCooldown = true;
            context.TriggeredSkillId = skillRow.SkillId;
            Debug.Log(
                $"[SkillEffect] {skillRow.SkillId} blink farthest={target.RuntimeId} " +
                $"land=({sampled.Value.x:0.##},{sampled.Value.y:0.##}) " +
                $"cd={skillRow.BaseCooldownSeconds:0.##}s");
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
