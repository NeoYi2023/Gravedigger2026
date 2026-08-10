using UnityEngine;

namespace Gravedigger2026.Gameplay.Defend
{
    /// <summary>
    /// Monster-only death knockback helpers (SPEC_04 §15.5): mirror killer across death pos on XZ,
    /// then scale displacement by ClassConfig.DeathKnockbackMult.
    /// </summary>
    public static class MonsterDeathPresentation
    {
        /// <summary>Demo-locked duration for M→end linear lerp.</summary>
        public const float DeathKnockbackSeconds = 0.3f;

        /// <summary>
        /// Mirror T = 2M − S on XZ; end = M + (T − M) × deathKnockbackMult. Y kept from M.
        /// </summary>
        public static Vector3 MirrorKnockbackTarget(
            Vector3 monsterDeathPos,
            Vector3 killerWorldPos,
            float deathKnockbackMult = 1f)
        {
            var mirror = new Vector3(
                2f * monsterDeathPos.x - killerWorldPos.x,
                monsterDeathPos.y,
                2f * monsterDeathPos.z - killerWorldPos.z);
            var mult = Mathf.Max(0f, deathKnockbackMult);
            return monsterDeathPos + (mirror - monsterDeathPos) * mult;
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
