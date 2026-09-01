using System;
using System.Collections.Generic;
using Gravedigger2026.Core.Combat;
using UnityEngine;

namespace Gravedigger2026.Gameplay.Defend
{
    /// <summary>
    /// D-083 in-flight / landing corpse smash hit sweep (SPEC_04 §15.5). View holds
    /// <c>alreadyHit</c>; rules settle via Session <c>TryApplyCorpseSmashDamage</c>.
    /// </summary>
    public static class CorpseProjectileSmashSweep
    {
        public delegate void LivingMonsterEnumerator(Action<string, Vector2, float> visit);

        public static void TryHitNearby(
            Vector2 corpseXZ,
            string corpseRuntimeId,
            string killerWarriorId,
            float killerOutgoingDamage,
            HashSet<string> alreadyHit,
            LivingMonsterEnumerator enumerateLiving,
            Func<string, string, float, string, bool> tryApplySmash)
        {
            if (alreadyHit == null
                || enumerateLiving == null
                || tryApplySmash == null
                || killerOutgoingDamage <= 0f
                || string.IsNullOrEmpty(corpseRuntimeId))
            {
                return;
            }

            enumerateLiving((targetRuntimeId, targetXZ, targetBodyRadius) =>
            {
                if (string.IsNullOrEmpty(targetRuntimeId)
                    || string.Equals(targetRuntimeId, corpseRuntimeId, StringComparison.Ordinal)
                    || alreadyHit.Contains(targetRuntimeId))
                {
                    return;
                }

                if (!CorpseSmashCombatMath.IsWithinSmashHitRadius(corpseXZ, targetXZ, targetBodyRadius))
                {
                    return;
                }

                alreadyHit.Add(targetRuntimeId);
                tryApplySmash(corpseRuntimeId, killerWarriorId, killerOutgoingDamage, targetRuntimeId);
            });
        }
    }
}
