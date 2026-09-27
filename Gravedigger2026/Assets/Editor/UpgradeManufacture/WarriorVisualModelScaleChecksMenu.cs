#if UNITY_EDITOR
using Gravedigger2026.Core.UpgradeManufacture;
using UnityEditor;
using UnityEngine;

namespace Gravedigger2026.Editor.UpgradeManufacture
{
    public static class WarriorVisualModelScaleChecksMenu
    {
        [MenuItem("Gravedigger2026/AutoManufacture（自动制造）/Run Warrior VisualModelScale Correctness (D-082)（运行士兵 VisualModelScale 正确性）")]
        public static void Run()
        {
            var error = WarriorVisualModelScaleCorrectnessChecks.RunAll();
            if (error == null)
            {
                Debug.Log("[WarriorVisualModelScaleCorrectnessChecks] All checks passed (D-082).");
            }
            else
            {
                Debug.LogError($"[WarriorVisualModelScaleCorrectnessChecks] FAILED:\n{error}");
            }
        }
    }
}
#endif
