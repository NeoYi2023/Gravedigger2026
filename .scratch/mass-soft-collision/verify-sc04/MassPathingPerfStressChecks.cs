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

            CheckSoftCollisionKnobs(sb);

            // SC-04: compare entry must run and stay structurally sound on both legs.
            var compare = MassPathingPerfStress.RunSoftCollisionCompare(perSide: 40, measureFrames: 20, warmupFrames: 5);
            if (!compare.On.StructuralOk || !compare.Off.StructuralOk)
            {
                sb.AppendLine(
                    $"SoftCollisionCompare structural failure: on={compare.On.StructuralOk} off={compare.Off.StructuralOk}.");
            }

            // ON leg must report resolve=true, OFF leg resolve=false.
            if (!compare.On.SoftCollisionResolve || compare.Off.SoftCollisionResolve)
            {
                sb.AppendLine(
                    $"SoftCollisionCompare resolve flags wrong: on={compare.On.SoftCollisionResolve} off={compare.Off.SoftCollisionResolve}.");
            }

            // Full 200v200 for Console report (numbers are machine-dependent).
            var full = MassPathingPerfStress.Run();
            Debug.Log(
                $"[MassPathingPerfStressChecks] full avg={full.AvgMoveLogicMs:F3}ms " +
                $"withinBudget={full.WithinBudget} structural={full.StructuralOk}");

            return sb.Length == 0 ? null : sb.ToString();
        }

        /// <summary>SC-04: B+ fallback knobs actually gate cost (SPEC_04 §9.7 fallback ④/⑤).</summary>
        private static void CheckSoftCollisionKnobs(StringBuilder sb)
        {
            // ⑤ SoftCollisionMaxBodiesPerFrame frames the per-frame resolve budget.
            var scheduler = new MassMoveScheduler();
            var posA = Vector2.zero;
            var posB = new Vector2(0.1f, 0f);
            var posC = new Vector2(0.2f, 0f);
            scheduler.Register(1, MassMoveScheduler.DefaultAgentRadius);
            scheduler.Register(2, MassMoveScheduler.DefaultAgentRadius);
            scheduler.Register(3, MassMoveScheduler.DefaultAgentRadius);
            var samples = new[]
            {
                new MassMoveSample(1, posA, MassMoveScheduler.DefaultAgentRadius, true),
                new MassMoveSample(2, posB, MassMoveScheduler.DefaultAgentRadius, true),
                new MassMoveSample(3, posC, MassMoveScheduler.DefaultAgentRadius, true),
            };
            scheduler.SoftCollisionMaxBodiesPerFrame = 1;
            scheduler.Tick(samples, 1f / 60f);
            if (scheduler.SoftCollision.LastFrameResolvedCount != 1)
            {
                sb.AppendLine(
                    $"SoftCollisionMaxBodiesPerFrame=1 but resolved {scheduler.SoftCollision.LastFrameResolvedCount}.");
            }

            // ④ QueryRadiusScale shrinks the neighbor query disk (0.1× → 0.04 < pair dist 0.1).
            var svc = new SoftCollisionService();
            svc.Register(1, 0.1f, () => posA);
            svc.Register(2, 0.1f, () => posB);
            svc.QueryRadiusScale = 1f;
            svc.Tick(1f / 60f);
            svc.TryGetCorrection(1, out var fullRadiusCorr);
            svc.QueryRadiusScale = 0.1f;
            svc.Tick(1f / 60f);
            svc.TryGetCorrection(1, out var tinyRadiusCorr);
            if (fullRadiusCorr.sqrMagnitude < 1e-8f)
            {
                sb.AppendLine("QueryRadiusScale probe: overlapping pair produced no correction at scale 1.0.");
            }

            if (tinyRadiusCorr.sqrMagnitude > 1e-8f)
            {
                sb.AppendLine("QueryRadiusScale=0.1 still gathered neighbors (expected zero correction).");
            }
        }
    }
}
