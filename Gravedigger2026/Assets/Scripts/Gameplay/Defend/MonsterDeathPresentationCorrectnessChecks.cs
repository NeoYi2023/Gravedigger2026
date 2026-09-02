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
            CheckShadowScaleMul(sb);
            CheckKnockbackDirectionOffset(sb);
            CheckKnockbackDirectionStepIndexMax(sb);
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

        private static void CheckShadowScaleMul(StringBuilder sb)
        {
            if (MonsterDeathPresentation.ComputeShadowScaleMul(0f) > 0f)
            {
                sb.AppendLine("ShadowScaleMul: height 0 should return 0");
            }

            var atGround = MonsterDeathPresentation.ComputeShadowScaleMul(1e-4f);
            var min = CombatRuntimeTuning.DeathKnockbackShadowScaleMin;
            if (Mathf.Abs(atGround - min) > 0.05f)
            {
                sb.AppendLine($"ShadowScaleMul: near ground {atGround:F4} expect ≈{min:F4}");
            }

            var peak = CombatRuntimeTuning.DeathKnockbackPeakHeight;
            if (peak > 1e-5f)
            {
                var atPeak = MonsterDeathPresentation.ComputeShadowScaleMul(peak);
                if (Mathf.Abs(atPeak - 1f) > 0.01f)
                {
                    sb.AppendLine($"ShadowScaleMul: at peak {atPeak:F4} expect 1");
                }
            }
        }

        private static void CheckKnockbackDirectionOffset(StringBuilder sb)
        {
            var monster = new Vector3(0f, 0f, 0f);
            var killer = new Vector3(-1f, 0f, 0f);
            const float distance = 2f;

            if (!MonsterDeathPresentation.TryDirectionalKnockbackTarget(
                    monster,
                    killer,
                    distance,
                    out var zeroOffsetTarget,
                    0f))
            {
                sb.AppendLine("KnockbackDirection: expected success at offset 0");
                return;
            }

            if (Mathf.Abs(zeroOffsetTarget.x - distance) > Epsilon || Mathf.Abs(zeroOffsetTarget.z) > Epsilon)
            {
                sb.AppendLine(
                    $"KnockbackDirection: offset 0 target={zeroOffsetTarget} expect ({distance},0,0)");
            }

            var spreadHalf = CombatRuntimeTuning.DeathKnockbackDirectionSpreadHalfDegrees;
            if (spreadHalf <= 0f)
            {
                return;
            }

            if (!MonsterDeathPresentation.TryDirectionalKnockbackTarget(
                    monster,
                    killer,
                    distance,
                    out var positiveTarget,
                    spreadHalf))
            {
                sb.AppendLine("KnockbackDirection: expected success at +SpreadHalf");
                return;
            }

            var expectedPositive = MonsterDeathPresentation.RotatePlanarDirection(Vector2.right, spreadHalf);
            var expectX = expectedPositive.x * distance;
            var expectZ = expectedPositive.y * distance;
            if (Mathf.Abs(positiveTarget.x - expectX) > 0.01f ||
                Mathf.Abs(positiveTarget.z - expectZ) > 0.01f)
            {
                sb.AppendLine(
                    $"KnockbackDirection: +SpreadHalf target=({positiveTarget.x:F4},{positiveTarget.z:F4}) " +
                    $"expect ({expectX:F4},{expectZ:F4})");
            }

            if (!MonsterDeathPresentation.TryDirectionalKnockbackTarget(
                    monster,
                    killer,
                    distance,
                    out var negativeTarget,
                    -spreadHalf))
            {
                sb.AppendLine("KnockbackDirection: expected success at -SpreadHalf");
                return;
            }

            var expectedNegative = MonsterDeathPresentation.RotatePlanarDirection(Vector2.right, -spreadHalf);
            expectX = expectedNegative.x * distance;
            expectZ = expectedNegative.y * distance;
            if (Mathf.Abs(negativeTarget.x - expectX) > 0.01f ||
                Mathf.Abs(negativeTarget.z - expectZ) > 0.01f)
            {
                sb.AppendLine(
                    $"KnockbackDirection: -SpreadHalf target=({negativeTarget.x:F4},{negativeTarget.z:F4}) " +
                    $"expect ({expectX:F4},{expectZ:F4})");
            }
        }

        private static void CheckKnockbackDirectionStepIndexMax(StringBuilder sb)
        {
            var spreadHalf = CombatRuntimeTuning.DeathKnockbackDirectionSpreadHalfDegrees;
            var step = CombatRuntimeTuning.DeathKnockbackDirectionRandomStepDegrees;
            var maxIndex = MonsterDeathPresentation.GetKnockbackDirectionStepIndexMax();

            if (spreadHalf <= 0f || step <= 0f)
            {
                if (maxIndex != 0)
                {
                    sb.AppendLine($"KnockbackDirectionStepIndexMax: disabled random expected 0 got {maxIndex}");
                }

                return;
            }

            var expect = Mathf.FloorToInt(spreadHalf / step);
            if (maxIndex != expect)
            {
                sb.AppendLine($"KnockbackDirectionStepIndexMax: {maxIndex} expect {expect}");
            }

            var maxOffset = MonsterDeathPresentation.ComputeKnockbackDirectionOffsetDegrees(maxIndex);
            if (Mathf.Abs(maxOffset - maxIndex * step) > Epsilon)
            {
                sb.AppendLine($"KnockbackDirectionOffset: max index offset {maxOffset:F4} expect {maxIndex * step:F4}");
            }
        }
    }
}
