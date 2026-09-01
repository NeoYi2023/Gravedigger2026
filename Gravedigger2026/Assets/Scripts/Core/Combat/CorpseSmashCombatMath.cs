using Gravedigger2026.Core.Config;
using UnityEngine;

namespace Gravedigger2026.Core.Combat
{
    /// <summary>
    /// D-083 corpse smash damage and XZ hit-radius helpers (SPEC_03 §3.12 / SPEC_04 §15.5).
    /// Independent from Comfort / D-073 outgoing-damage pipeline.
    /// </summary>
    public static class CorpseSmashCombatMath
    {
        public static float ComputeSmashDamage(float killerOutgoingDamage)
        {
            if (killerOutgoingDamage <= 0f)
            {
                return 0f;
            }

            return killerOutgoingDamage * CombatRuntimeTuning.DeathCorpseSmashDamageMul;
        }

        /// <summary>
        /// XZ soft-hit test aligned with ProjectileView semantics.
        /// </summary>
        public static bool IsWithinSmashHitRadius(Vector2 corpseXZ, Vector2 targetXZ, float targetBodyRadius)
        {
            var hitRadius = CombatRuntimeTuning.DeathCorpseSmashHitRadius + Mathf.Max(0f, targetBodyRadius);
            var dx = corpseXZ.x - targetXZ.x;
            var dz = corpseXZ.y - targetXZ.y;
            return dx * dx + dz * dz <= hitRadius * hitRadius;
        }
    }
}
