using System.Text;
using Gravedigger2026.Core.Config;
using UnityEngine;

namespace Gravedigger2026.Core.Combat
{
    /// <summary>
    /// Scene-free checks for D-083 corpse smash rules math (SPEC_04 §15.5).
    /// Expectations derive from applied <see cref="CombatRuntimeTuning"/>.
    /// </summary>
    public static class CorpseSmashCombatCorrectnessChecks
    {
        private const float Epsilon = 1e-4f;

        public static string RunAll()
        {
            var sb = new StringBuilder();
            CheckSmashDamageFormula(sb);
            CheckSmashHitRadius(sb);
            return sb.Length == 0 ? null : sb.ToString();
        }

        private static void CheckSmashDamageFormula(StringBuilder sb)
        {
            var mul = CombatRuntimeTuning.DeathCorpseSmashDamageMul;
            var dmg = CorpseSmashCombatMath.ComputeSmashDamage(100f);
            if (Mathf.Abs(dmg - 100f * mul) > Epsilon)
            {
                sb.AppendLine($"SmashDamage: expected {100f * mul}, got {dmg}");
            }

            if (CorpseSmashCombatMath.ComputeSmashDamage(0f) != 0f
                || CorpseSmashCombatMath.ComputeSmashDamage(-5f) != 0f)
            {
                sb.AppendLine("SmashDamage: non-positive killerOutgoingDamage must yield 0");
            }
        }

        private static void CheckSmashHitRadius(StringBuilder sb)
        {
            var smashRadius = CombatRuntimeTuning.DeathCorpseSmashHitRadius;
            const float body = 0.35f;
            var limit = smashRadius + body;

            if (!CorpseSmashCombatMath.IsWithinSmashHitRadius(Vector2.zero, new Vector2(limit, 0f), body))
            {
                sb.AppendLine("SmashHitRadius: expected hit at exact limit distance");
            }

            if (CorpseSmashCombatMath.IsWithinSmashHitRadius(
                    Vector2.zero,
                    new Vector2(limit + 0.01f, 0f),
                    body))
            {
                sb.AppendLine("SmashHitRadius: expected miss beyond limit distance");
            }

            if (!CorpseSmashCombatMath.IsWithinSmashHitRadius(Vector2.zero, Vector2.zero, body))
            {
                sb.AppendLine("SmashHitRadius: expected hit when centers coincide");
            }
        }
    }
}
