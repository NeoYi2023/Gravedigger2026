using Gravedigger2026.Core.Config;
using UnityEngine;

namespace Gravedigger2026.Gameplay.Defend
{
    /// <summary>
    /// Monster-only death knockback (SPEC_04 §15.5): distance from OutgoingDamage/MaxHp
    /// × CombatConstantConfig coeffs; parabolic arc on Y; direction away from killer on XZ
    /// with optional symmetric random yaw offset.
    /// </summary>
    public static class MonsterDeathPresentation
    {
        /// <summary>Demo duration; aligns with parabolic param t∈[0,1]. ← CombatConstantConfig.</summary>
        public static float DeathKnockbackSeconds => CombatRuntimeTuning.DeathKnockbackSeconds;

        /// <summary>
        /// SPEC_04 §15.5: default Die2; when knockback distance reaches/exceeds threshold → Die.
        /// Returns true when Die2 should play (distance below threshold, and Controller has Die2).
        /// </summary>
        public static bool ShouldPreferDie2(float knockbackDistance) =>
            knockbackDistance < CombatRuntimeTuning.DeathDie2KnockbackThreshold;

        /// <summary>
        /// D-083 smash gate: distance ≥ DeathDie2KnockbackThreshold → flight sweep + landing smash.
        /// </summary>
        public static bool ShouldEnableCorpseSmash(float knockbackDistance) =>
            knockbackDistance >= CombatRuntimeTuning.DeathDie2KnockbackThreshold;

        /// <summary>
        /// raw = (OutgoingDamage / MaxHp) × RatioCoeff; clamp [Min, Max].
        /// OutgoingDamage ≤ 0 or MaxHp ≤ 0 → MaxDistance.
        /// </summary>
        public static float ComputeKnockbackDistance(float maxHp, float outgoingDamage)
        {
            var min = Mathf.Max(0f, CombatRuntimeTuning.DeathKnockbackMinDistance);
            var max = Mathf.Max(min, CombatRuntimeTuning.DeathKnockbackMaxDistance);
            if (maxHp <= 0f || outgoingDamage <= 0f)
            {
                return max;
            }

            var coeff = Mathf.Max(0f, CombatRuntimeTuning.DeathKnockbackRatioCoeff);
            var raw = (outgoingDamage / maxHp) * coeff;
            return Mathf.Clamp(raw, min, max);
        }

        /// <summary>
        /// N = floor(SpreadHalf / Step) when both &gt; 0; else 0.
        /// </summary>
        public static int GetKnockbackDirectionStepIndexMax()
        {
            var spreadHalf = CombatRuntimeTuning.DeathKnockbackDirectionSpreadHalfDegrees;
            var step = CombatRuntimeTuning.DeathKnockbackDirectionRandomStepDegrees;
            if (spreadHalf <= 0f || step <= 0f)
            {
                return 0;
            }

            return Mathf.FloorToInt(spreadHalf / step);
        }

        /// <summary>offsetDeg = stepIndex × DeathKnockbackDirectionRandomStepDegrees.</summary>
        public static float ComputeKnockbackDirectionOffsetDegrees(int stepIndex)
        {
            var step = CombatRuntimeTuning.DeathKnockbackDirectionRandomStepDegrees;
            if (step <= 0f)
            {
                return 0f;
            }

            return stepIndex * step;
        }

        /// <summary>
        /// k ~ UniformInteger[-N, +N]; offset = k × Step. Returns 0 when randomization disabled.
        /// </summary>
        public static float RollKnockbackDirectionOffsetDegrees()
        {
            var maxIndex = GetKnockbackDirectionStepIndexMax();
            if (maxIndex <= 0)
            {
                return 0f;
            }

            var k = Random.Range(-maxIndex, maxIndex + 1);
            return ComputeKnockbackDirectionOffsetDegrees(k);
        }

        /// <summary>Rotate a normalized XZ direction around world Y (degrees, CCW from above).</summary>
        public static Vector2 RotatePlanarDirection(Vector2 dir, float yawDegrees)
        {
            if (Mathf.Abs(yawDegrees) <= 1e-6f)
            {
                return dir;
            }

            var rad = yawDegrees * Mathf.Deg2Rad;
            var cos = Mathf.Cos(rad);
            var sin = Mathf.Sin(rad);
            return new Vector2(
                dir.x * cos - dir.y * sin,
                dir.x * sin + dir.y * cos);
        }

        /// <summary>
        /// End = M + normalize(M − S)_xz rotated by offset × distance; Y kept from M.
        /// Returns false when killer coincides with M (zero planar dir) or distance ≤ 0.
        /// </summary>
        public static bool TryDirectionalKnockbackTarget(
            Vector3 monsterDeathPos,
            Vector3 killerWorldPos,
            float distance,
            out Vector3 target,
            float? directionOffsetDegrees = null)
        {
            target = monsterDeathPos;
            if (distance <= 0f)
            {
                return false;
            }

            var dx = monsterDeathPos.x - killerWorldPos.x;
            var dz = monsterDeathPos.z - killerWorldPos.z;
            var lenSq = dx * dx + dz * dz;
            if (lenSq < 1e-8f)
            {
                return false;
            }

            var inv = 1f / Mathf.Sqrt(lenSq);
            var dir = new Vector2(dx * inv, dz * inv);
            var offset = directionOffsetDegrees ?? RollKnockbackDirectionOffsetDegrees();
            if (Mathf.Abs(offset) > 1e-6f)
            {
                dir = RotatePlanarDirection(dir, offset);
            }

            target = new Vector3(
                monsterDeathPos.x + dir.x * distance,
                monsterDeathPos.y,
                monsterDeathPos.z + dir.y * distance);
            return true;
        }

        /// <summary>
        /// Parabolic arc: XZ linear origin→end; Y = y0 + 4×PeakHeight×t×(1−t).
        /// Returns true while t &lt; 1.
        /// </summary>
        public static bool TrySampleParabolicKnockback(
            Vector3 origin,
            Vector3 end,
            float y0,
            float startedAt,
            float durationSeconds,
            float now,
            out Vector3 position)
        {
            if (durationSeconds <= 0.0001f)
            {
                position = new Vector3(end.x, y0, end.z);
                return false;
            }

            var t = Mathf.Clamp01((now - startedAt) / durationSeconds);
            var peak = CombatRuntimeTuning.DeathKnockbackPeakHeight;
            var y = y0 + 4f * peak * t * (1f - t);
            position = new Vector3(
                Mathf.Lerp(origin.x, end.x, t),
                y,
                Mathf.Lerp(origin.z, end.z, t));
            return t < 1f;
        }

        /// <summary>
        /// Shadow diameter scale for airborne corpse: lerp(Min, 1) by height/peak.
        /// Returns 0 when height ≤ epsilon or peak ≤ 0.
        /// </summary>
        public static float ComputeShadowScaleMul(float heightAboveGround)
        {
            if (heightAboveGround <= 1e-3f)
            {
                return 0f;
            }

            var peak = CombatRuntimeTuning.DeathKnockbackPeakHeight;
            if (peak <= 1e-5f)
            {
                return 0f;
            }

            var t = Mathf.Clamp01(heightAboveGround / peak);
            return Mathf.Lerp(
                CombatRuntimeTuning.DeathKnockbackShadowScaleMin,
                1f,
                t);
        }

        /// <summary>Delegates to parabolic sampling with y0 = origin.y (retired pure XZ Lerp).</summary>
        public static bool TrySampleKnockback(
            Vector3 origin,
            Vector3 target,
            float startedAt,
            float durationSeconds,
            float now,
            out Vector3 position) =>
            TrySampleParabolicKnockback(
                origin,
                target,
                origin.y,
                startedAt,
                durationSeconds,
                now,
                out position);
    }
}
