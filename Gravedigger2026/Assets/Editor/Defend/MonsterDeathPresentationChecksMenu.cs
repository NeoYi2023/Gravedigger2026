#if UNITY_EDITOR
using Gravedigger2026.Core.Combat;
using Gravedigger2026.Gameplay.Defend;
using UnityEditor;
using UnityEngine;

namespace Gravedigger2026.Editor.Defend
{
    public static class MonsterDeathPresentationChecksMenu
    {
        [MenuItem("Gravedigger2026/Combat/Run Corpse Projectile Correctness Checks (D-083)")]
        public static void RunAll()
        {
            var error = CorpseProjectileCorrectnessChecks.RunAll();
            if (error == null)
            {
                Debug.Log("[CorpseProjectileCorrectnessChecks] All checks passed (D-083).");
            }
            else
            {
                Debug.LogError($"[CorpseProjectileCorrectnessChecks] FAILED:\n{error}");
            }
        }

        [MenuItem("Gravedigger2026/Combat/Run Corpse Projectile Parabolic Checks")]
        public static void RunParabolic()
        {
            var error = MonsterDeathPresentationCorrectnessChecks.RunAll();
            if (error == null)
            {
                Debug.Log("[MonsterDeathPresentationCorrectnessChecks] All checks passed (D-083 parabolic).");
            }
            else
            {
                Debug.LogError($"[MonsterDeathPresentationCorrectnessChecks] FAILED:\n{error}");
            }
        }

        [MenuItem("Gravedigger2026/Combat/Run Corpse Smash Rules Checks (D-083)")]
        public static void RunSmashRules()
        {
            var error = CorpseSmashCombatCorrectnessChecks.RunAll();
            if (error == null)
            {
                Debug.Log("[CorpseSmashCombatCorrectnessChecks] All checks passed (D-083 smash rules).");
            }
            else
            {
                Debug.LogError($"[CorpseSmashCombatCorrectnessChecks] FAILED:\n{error}");
            }
        }
    }
}
#endif
