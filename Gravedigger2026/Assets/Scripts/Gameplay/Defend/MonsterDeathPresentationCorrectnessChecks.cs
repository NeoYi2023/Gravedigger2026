using System.Text;
using Gravedigger2026.Core.Config;
using UnityEngine;

namespace Gravedigger2026.Gameplay.Defend
{
    /// <summary>
    /// Scene-free checks for D-083 parabolic corpse projectile sampling (SPEC_04 §15.5).
    /// Expectations derive from applied <see cref="CombatRuntimeTuning"/> (CSV snapshot in Editor).
    /// </summary>
    public static class MonsterDeathPresentationCorrectnessChecks
    {
        private const float Epsilon = 1e-4f;

        public static string RunAll()
        {
            var sb = new StringBuilder();
            CheckParabolicEndpoints(sb);
            CheckParabolicPeak(sb);
            CheckDurationEnd(sb);
            CheckSmashGate(sb);
            CheckKnockbackDistanceUnchanged(sb);
            CheckShouldPreferDie2Unchanged(sb);
            return sb.Length == 0 ? null : sb.ToString();
        }

        private static void CheckParabolicEndpoints(StringBuilder sb)
        {
            var origin = new Vector3(0f, 0.5f, 0f);
            var end = new Vector3(3f, 0.5f, 4f);
            const float y0 = 0f;
            const float started = 10f;
            var duration = CombatRuntimeTuning.DeathKnockbackSeconds;

            if (!MonsterDeathPresentation.TrySampleParabolicKnockback(
                    origin,
                    end,
                    y0,
                    started,
                    duration,
                    started,
                    out var atStart))
            {
                sb.AppendLine("ParabolicEndpoints: expected animating at t=0");
            }

            if (Mathf.Abs(atStart.x) > Epsilon || Mathf.Abs(atStart.z) > Epsilon || Mathf.Abs(atStart.y - y0) > Epsilon)
            {
                sb.AppendLine($"ParabolicEndpoints: t=0 pos={atStart} expect (0,0,0)");
            }

            MonsterDeathPresentation.TrySampleParabolicKnockback(
                origin,
                end,
                y0,
                started,
                duration,
                started + duration,
                out var atEnd);

            if (Mathf.Abs(atEnd.x - end.x) > Epsilon || Mathf.Abs(atEnd.z - end.z) > Epsilon)
            {
                sb.AppendLine($"ParabolicEndpoints: t=1 XZ={atEnd.x:F4},{atEnd.z:F4} expect {end.x},{end.z}");
            }

            if (Mathf.Abs(atEnd.y - y0) > Epsilon)
            {
                sb.AppendLine($"ParabolicEndpoints: t=1 Y={atEnd.y:F4} expect y0={y0}");
            }
        }

        private static void CheckParabolicPeak(StringBuilder sb)
        {
            var origin = Vector3.zero;
            var end = new Vector3(2f, 0f, 2f);
            const float y0 = 0f;
            const float started = 0f;
            var duration = CombatRuntimeTuning.DeathKnockbackSeconds;
            var half = duration * 0.5f;

            MonsterDeathPresentation.TrySampleParabolicKnockback(
                origin,
                end,
                y0,
                started,
                duration,
                started + half,
                out var atPeak);

            var expectPeakY = CombatRuntimeTuning.DeathKnockbackPeakHeight;
            if (Mathf.Abs(atPeak.y - (y0 + expectPeakY)) > 0.01f)
            {
                sb.AppendLine($"ParabolicPeak: Y={atPeak.y:F4} expect ≈{y0 + expectPeakY:F4}");
            }
        }

        private static void CheckDurationEnd(StringBuilder sb)
        {
            var origin = Vector3.zero;
            var end = new Vector3(1f, 0f, 1f);
            const float started = 5f;
            var duration = CombatRuntimeTuning.DeathKnockbackSeconds;

            var stillAnimating = MonsterDeathPresentation.TrySampleParabolicKnockback(
                origin,
                end,
                0f,
                started,
                duration,
                started + duration - 0.001f,
                out _);

            if (!stillAnimating)
            {
                sb.AppendLine("DurationEnd: expected still animating just before duration");
            }

            var animatingAtEnd = MonsterDeathPresentation.TrySampleParabolicKnockback(
                origin,
                end,
                0f,
                started,
                duration,
                started + duration,
                out _);

            if (animatingAtEnd)
            {
                sb.AppendLine("DurationEnd: expected finished at duration end");
            }
        }

        private static void CheckSmashGate(StringBuilder sb)
        {
            var threshold = CombatRuntimeTuning.DeathDie2KnockbackThreshold;
            var below = threshold > 0.01f ? threshold - 0.01f : 0f;

            if (MonsterDeathPresentation.ShouldEnableCorpseSmash(below))
            {
                sb.AppendLine($"SmashGate: {below:F2} (below threshold {threshold:F2}) should be false");
            }

            if (!MonsterDeathPresentation.ShouldEnableCorpseSmash(threshold))
            {
                sb.AppendLine($"SmashGate: {threshold:F2} (at threshold) should be true");
            }
        }

        private static void CheckKnockbackDistanceUnchanged(StringBuilder sb)
        {
            const float maxHp = 100f;
            const float outgoing = 50f;
            var distance = MonsterDeathPresentation.ComputeKnockbackDistance(maxHp, outgoing);
            var min = CombatRuntimeTuning.DeathKnockbackMinDistance;
            var max = CombatRuntimeTuning.DeathKnockbackMaxDistance;
            var coeff = CombatRuntimeTuning.DeathKnockbackRatioCoeff;
            var raw = (outgoing / maxHp) * coeff;
            var expect = Mathf.Clamp(raw, min, max);
            if (Mathf.Abs(distance - expect) > Epsilon)
            {
                sb.AppendLine($"KnockbackDistance: {distance:F4} expect {expect:F4}");
            }
        }

        private static void CheckShouldPreferDie2Unchanged(StringBuilder sb)
        {
            var threshold = CombatRuntimeTuning.DeathDie2KnockbackThreshold;
            var below = threshold > 0.01f ? threshold - 0.01f : 0f;

            if (!MonsterDeathPresentation.ShouldPreferDie2(below))
            {
                sb.AppendLine($"ShouldPreferDie2: {below:F2} (below threshold {threshold:F2}) should be true");
            }

            if (MonsterDeathPresentation.ShouldPreferDie2(threshold))
            {
                sb.AppendLine($"ShouldPreferDie2: {threshold:F2} (at threshold) should be false");
            }
        }
    }
}
