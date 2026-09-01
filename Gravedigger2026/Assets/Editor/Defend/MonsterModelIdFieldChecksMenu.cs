#if UNITY_EDITOR
using Gravedigger2026.Core.Config;
using UnityEditor;
using UnityEngine;

namespace Gravedigger2026.Editor.Defend
{
    public static class MonsterModelIdFieldChecksMenu
    {
        [MenuItem("Gravedigger2026/Config/Run Monster ModelId Field Correctness Checks")]
        public static void RunAll()
        {
            var error = MonsterModelIdFieldCorrectnessChecks.RunAll();
            if (error == null)
            {
                Debug.Log("[MonsterModelIdFieldCorrectnessChecks] All checks passed.");
            }
            else
            {
                Debug.LogError($"[MonsterModelIdFieldCorrectnessChecks] FAILED:\n{error}");
            }
        }
    }
}
#endif
