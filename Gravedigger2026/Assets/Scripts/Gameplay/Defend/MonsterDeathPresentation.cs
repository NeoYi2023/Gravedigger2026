using Gravedigger2026.Core.Config;
using UnityEngine;

namespace Gravedigger2026.Gameplay.Defend
{
    /// <summary>
    /// Monster-only death knockback (SPEC_04 §15.5): distance from OutgoingDamage/MaxHp
    /// × CombatConstantConfig coeffs; parabolic arc on Y; direction away from killer on XZ.
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
        /// End = M + normalize(M − S)_xz × distance; Y kept from M.
        /// Returns false when killer coincides with M (zero planar dir) or distance ≤ 0.
        /// </summary>
        public static bool TryDirectionalKnockbackTarget(
            Vector3 monsterDeathPos,
            Vector3 killerWorldPos,
            float distance,
            out Vector3 target)
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
            target = new Vector3(
                monsterDeathPos.x + dx * inv * distance,
                monsterDeathPos.y,
                monsterDeathPos.z + dz * inv * distance);
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
