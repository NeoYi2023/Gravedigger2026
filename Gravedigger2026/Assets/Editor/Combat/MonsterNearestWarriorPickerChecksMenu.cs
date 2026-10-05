#if UNITY_EDITOR
using Gravedigger2026.Core.Combat;
using UnityEditor;
using UnityEngine;

namespace Gravedigger2026.Editor.Combat
{
    public static class MonsterNearestWarriorPickerChecksMenu
    {
        [MenuItem("Gravedigger2026/Combat（战斗）/Run Nearest Warrior Band Correctness Checks（运行最近士兵带正确性检查）")]
        public static void Run()
        {
            var error = MonsterNearestWarriorPickerCorrectnessChecks.RunAll();
            if (string.IsNullOrEmpty(error))
            {
                Debug.Log(
                    "[MonsterNearestWarriorPickerCorrectnessChecks] All checks passed (nearest band + TargetFocus).");
            }
            else
            {
                Debug.LogError(
                    $"[MonsterNearestWarriorPickerCorrectnessChecks] FAILED:\n{error}");
            }
        }
    }
}
#endif
