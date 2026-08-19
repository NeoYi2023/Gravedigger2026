using Gravedigger2026.Core.Config;
using UnityEngine;

namespace Gravedigger2026.Core.Combat.SkillEffects
{
    /// <summary>Skill_06 震晕 — AA hit chance to AOE-stun living monsters around the hit target.</summary>
    public sealed class OnAaHitChanceAoeStunHandler : ISkillEffectHandler
    {
        private static readonly string[] AllowedKeys = { "Chance", "Radius", "StunSeconds" };

        public string EffectKind => SkillEffectKind.OnAaHitChanceAoeStun;

        public void Apply(SkillEffectContext context, SkillEffectConfigRow effectRow)
        {
            if (context == null || effectRow == null || context.Warrior == null || context.CombatStatus == null)
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

            var map = SkillEffectParams.Parse(effectRow.EffectParams, AllowedKeys);
            if (!SkillEffectParams.TryGetFloat(map, "Chance", out var chance) || chance <= 0f)
            {
                Debug.LogWarning(
                    $"[SkillEffect] {effectRow.SkillEffectId}: missing/invalid Chance.");
                return;
            }

            if (!SkillEffectParams.TryGetFloat(map, "Radius", out var radius) || radius < 0f)
            {
                Debug.LogWarning(
                    $"[SkillEffect] {effectRow.SkillEffectId}: missing/invalid Radius.");
                return;
            }

            if (!SkillEffectParams.TryGetFloat(map, "StunSeconds", out var stunSeconds) || stunSeconds <= 0f)
            {
                Debug.LogWarning(
                    $"[SkillEffect] {effectRow.SkillEffectId}: missing/invalid StunSeconds.");
                return;
            }

            if (Random.value > chance)
            {
                return;
            }

            var center = context.HitCenterXZ;
            var radiusSqr = radius * radius;
            var stunned = 0;
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

                    context.CombatStatus.ApplyMonsterStun(entry.RuntimeId, skillRow.SkillId, stunSeconds);
                    stunned++;
                }
            }

            context.TriggeredSkillId = skillRow.SkillId;
            Debug.Log(
                $"[SkillEffect] {skillRow.SkillId} AOE Stun PROC chance={chance:0.##} radius={radius:0.##} " +
                $"sec={stunSeconds:0.##} hit={context.TargetMonsterRuntimeId} stunned={stunned}");
        }
    }
}
