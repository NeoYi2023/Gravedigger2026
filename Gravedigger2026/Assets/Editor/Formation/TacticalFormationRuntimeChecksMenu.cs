#if UNITY_EDITOR
using Gravedigger2026.Core.TacticalFormation;
using UnityEditor;
using UnityEngine;

namespace Gravedigger2026.Editor.Formation
{
        /// <summary>TF-04a/04b/05 scene-free correctness (SPEC_03 §3.18 / SPEC_04 §9.7 / §9.30).</summary>
        public static class TacticalFormationRuntimeChecksMenu
        {
            [MenuItem("Gravedigger2026/Formation/Run Tactical Formation Runtime Correctness (TF-04a)")]
            public static void RunAll()
            {
                var error = TacticalFormationRuntimeCorrectnessChecks.RunAll();
                if (error == null)
                {
                    Debug.Log("[TacticalFormationRuntimeCorrectnessChecks] All checks passed (TF-04a/04b/05).");
                }
                else
                {
                    Debug.LogError($"[TacticalFormationRuntimeCorrectnessChecks] FAILED:\n{error}");
                }
            }
        }
}
#endif
