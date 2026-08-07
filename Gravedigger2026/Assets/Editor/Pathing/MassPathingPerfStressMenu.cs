#if UNITY_EDITOR
using Gravedigger2026.Core.Pathing;
using Gravedigger2026.Gameplay.Pathing;
using UnityEditor;
using UnityEngine;

namespace Gravedigger2026.Editor.Pathing
{
    /// <summary>MP-07 Debug menu: repeatable 200v200 move-logic Stopwatch (SPEC_04 §9.7).</summary>
    public static class MassPathingPerfStressMenu
    {
        [MenuItem("Gravedigger2026/Pathing/Run MassPathing 200v200 Perf Stress")]
        public static void Run200v200()
        {
            var result = MassPathingPerfStress.Run();
            EditorUtility.DisplayDialog(
                "MassPathing Perf Stress",
                $"avg={result.AvgMoveLogicMs:F3} ms  p95={result.P95MoveLogicMs:F3}  max={result.MaxMoveLogicMs:F3}\n" +
                $"budgetOK={result.WithinBudget}  structuralOK={result.StructuralOk}\n" +
                $"RebuildCount={result.FlowFieldRebuildCount}  agents={result.AgentCount}\n\n" +
                "Full report in Console.",
                "OK");
        }

        [MenuItem("Gravedigger2026/Pathing/Create MassPathingPerfStressView (scene)")]
        public static void CreateStressViewInScene()
        {
            var go = new GameObject("MassPathingPerfStress");
            go.AddComponent<MassPathingPerfStressView>();
            Selection.activeGameObject = go;
            Undo.RegisterCreatedObjectUndo(go, "Create MassPathingPerfStressView");
            Debug.Log(
                "[MassPathingPerfStress] Created scene host. ContextMenu → Start Live Sim (stubs) " +
                "or Run Headless 200v200.");
        }
    }
}
#endif
