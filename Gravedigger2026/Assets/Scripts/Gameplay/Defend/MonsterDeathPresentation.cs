using Gravedigger2026.Core.Config;
using UnityEngine;

namespace Gravedigger2026.Gameplay.Defend
{
    /// <summary>
    /// Monster-only death knockback (SPEC_04 §15.5): distance from MaxHp/OutgoingDamage
    /// × CombatConstantConfig coeffs; direction away from killer on XZ.
    /// </summary>
    public static class MonsterDeathPresentation
    {
        /// <summary>Demo-locked duration for M→end linear lerp.</summary>
        public const float DeathKnockbackSeconds = 0.3f;

        /// <summary>
        /// raw = (MaxHp / OutgoingDamage) × RatioCoeff; clamp [Min, Max].
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
            var raw = (maxHp / outgoingDamage) * coeff;
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

        /// <summary>Linear lerp origin→target by elapsed/duration; returns true while still animating.</summary>
        public static bool TrySampleKnockback(
            Vector3 origin,
            Vector3 target,
            float startedAt,
            float durationSeconds,
            float now,
            out Vector3 position)
        {
            if (durationSeconds <= 0.0001f)
            {
                position = target;
                return false;
            }

            var t = Mathf.Clamp01((now - startedAt) / durationSeconds);
            position = Vector3.Lerp(origin, target, t);
            return t < 1f;
        }
    }
}
