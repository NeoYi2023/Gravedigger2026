using System.Text;
using UnityEngine;

namespace Gravedigger2026.Core.Pathing
{
    /// <summary>
    /// Structural checks for MP-07 (SPEC_04 §9.7). Wall-clock budget is machine-dependent —
    /// call <see cref="MassPathingPerfStress.Run"/> and read <see cref="MassPathingPerfStressResult.WithinBudget"/>.
    /// </summary>
    public static class MassPathingPerfStressChecks
    {
        /// <summary>Returns null on success; otherwise a multi-line failure report.</summary>
        public static string RunStructural()
        {
            var sb = new StringBuilder();
            if (MassMoveScheduler.MaxRecalcPerFrame != 50)
            {
                sb.AppendLine($"MaxRecalcPerFrame must be 50, was {MassMoveScheduler.MaxRecalcPerFrame}");
            }

            // Short run: verify Rebuild stays few and frame budgets hold.
            var result = MassPathingPerfStress.Run(perSide: 40, measureFrames: 30, warmupFrames: 5);
            if (!result.StructuralOk)
            {
                sb.AppendLine("StructuralOk=false on scaled-down run (40/side).");
            }

            if (result.FlowFieldRebuildCount > 3)
            {
                sb.AppendLine($"RebuildCount too high: {result.FlowFieldRebuildCount}");
            }

            if (result.MaxSteerRecalcObserved > MassMoveScheduler.MaxRecalcPerFrame)
            {
                sb.AppendLine($"Steer recalc exceeded budget: {result.MaxSteerRecalcObserved}");
            }

            if (result.MaxSlotRefreshObserved > MassMoveScheduler.MaxRecalcPerFrame)
            {
                sb.AppendLine($"Slot refresh exceeded budget: {result.MaxSlotRefreshObserved}");
            }

            // Full 200v200 for Console report (numbers are machine-dependent).
            var full = MassPathingPerfStress.Run();
            Debug.Log(
                $"[MassPathingPerfStressChecks] full avg={full.AvgMoveLogicMs:F3}ms " +
                $"withinBudget={full.WithinBudget} structural={full.StructuralOk}");

            return sb.Length == 0 ? null : sb.ToString();
        }
    }
}
