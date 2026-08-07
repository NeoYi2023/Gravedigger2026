using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Gravedigger2026.Core.Config;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Gravedigger2026.Core.Pathing
{
    /// <summary>
    /// MP-07 headless 200v200 move-stack stress (SPEC_04 §9.7).
    /// Measures <see cref="MassMoveScheduler.Tick"/> + ≤50 AttackSlot refresh via Stopwatch.
    /// No Transform/Animator; no CalculatePath / HighQuality RVO.
    /// </summary>
    public static class MassPathingPerfStress
    {
        public const int DefaultPerSide = 200;
        public const float MoveLogicBudgetMs = 2.5f;
        public const int DefaultWarmupFrames = 10;
        public const int DefaultMeasureFrames = 120;
        public const float SimDt = 1f / 60f;
        public const float MoveSpeed = 2.5f;
        public const float AgentRadius = MassMoveScheduler.DefaultAgentRadius;
        public const float AttackRange = 1.2f;
        public const int DummyTargetCount = 8;
        public const float CellSize = FlowFieldService.DefaultCellSize;

        /// <summary>Fallback knobs if over budget (SPEC_04 §9.7 order).</summary>
        public const string FallbackGuidance =
            "Over-budget fallbacks (try in order): (1) raise FlowField cellSize toward 0.5; " +
            "(2) lower AttackSlot N (MeleeSlotCount/RangedSlotCount); " +
            "(3) lower MassMoveScheduler.MaxRecalcPerFrame / slot-refresh budget.";

        public static MassPathingPerfStressResult Run(
            int perSide = DefaultPerSide,
            int measureFrames = DefaultMeasureFrames,
            int warmupFrames = DefaultWarmupFrames)
        {
            perSide = Mathf.Max(1, perSide);
            measureFrames = Mathf.Max(1, measureFrames);
            warmupFrames = Mathf.Max(0, warmupFrames);

            var flow = new FlowFieldService();
            var scheduler = new MassMoveScheduler();
            var slots = new AttackSlotService();
            var samples = new List<MassMoveSample>(perSide * 2);
            var positions = new Vector2[perSide * 2];
            var monsterTargetIndex = new int[perSide];
            var dummyTargets = new Vector3[DummyTargetCount];
            var slotCursor = 0;
            var maxSteerRecalc = 0;
            var maxSlotRefresh = 0;
            var frameMs = new double[measureFrames];

            var half = new Vector2(20f, 10f);
            flow.Configure(Vector3.zero, half, CellSize);
            var goalA = new Vector3(14f, 0f, 0f);
            var goalB = new Vector3(-14f, 0f, 0f);
            flow.Rebuild(goalA, StubFullyWalkableMask.Instance);
            scheduler.BindFlowField(flow);

            for (var t = 0; t < DummyTargetCount; t++)
            {
                var ang = (t / (float)DummyTargetCount) * Mathf.PI * 2f;
                dummyTargets[t] = new Vector3(Mathf.Cos(ang) * 6f, 0f, Mathf.Sin(ang) * 3f);
            }

            for (var i = 0; i < perSide; i++)
            {
                var id = i + 1;
                var row = i / 20;
                var col = i % 20;
                positions[i] = new Vector2(-16f + col * 0.35f, -8f + row * 0.7f);
                scheduler.Register(id, AgentRadius, MassMoveScheduler.DetourGroupLoyal);
                scheduler.SetGoal(id, GoalKind.Objective);
            }

            for (var i = 0; i < perSide; i++)
            {
                var id = perSide + i + 1;
                var row = i / 20;
                var col = i % 20;
                positions[perSide + i] = new Vector2(16f - col * 0.35f, 8f - row * 0.7f);
                monsterTargetIndex[i] = i % DummyTargetCount;
                scheduler.Register(id, AgentRadius, MassMoveScheduler.DetourGroupMonster);
                scheduler.SetGoal(id, GoalKind.AttackSlot);
            }

            var totalFrames = warmupFrames + measureFrames;
            var midRebuildFrame = warmupFrames + measureFrames / 2;
            var sw = new Stopwatch();

            for (var frame = 0; frame < totalFrames; frame++)
            {
                if (frame == midRebuildFrame)
                {
                    flow.Rebuild(goalB, StubFullyWalkableMask.Instance);
                }

                sw.Restart();

                samples.Clear();
                for (var i = 0; i < perSide; i++)
                {
                    samples.Add(new MassMoveSample(i + 1, positions[i], AgentRadius, true));
                }

                for (var i = 0; i < perSide; i++)
                {
                    samples.Add(
                        new MassMoveSample(perSide + i + 1, positions[perSide + i], AgentRadius, true));
                }

                var slotBudget = Mathf.Min(MassMoveScheduler.MaxRecalcPerFrame, perSide);
                var slotDone = 0;
                for (var n = 0; n < slotBudget; n++)
                {
                    if (slotCursor >= perSide)
                    {
                        slotCursor = 0;
                    }

                    var mi = slotCursor++;
                    var attackerId = $"m{mi}";
                    var targetId = $"t{monsterTargetIndex[mi]}";
                    var targetPos = dummyTargets[monsterTargetIndex[mi]];
                    var attackerPos = new Vector3(positions[perSide + mi].x, 0f, positions[perSide + mi].y);
                    if (slots.TryClaim(
                            attackerId,
                            targetId,
                            AttackRange,
                            targetPos,
                            out var worldPos,
                            AttackMode.Melee,
                            attackerPos))
                    {
                        scheduler.SetGoal(
                            perSide + mi + 1,
                            GoalKind.AttackSlot,
                            new Vector2(worldPos.x, worldPos.z));
                    }

                    slotDone++;
                }

                scheduler.Tick(samples);
                sw.Stop();

                maxSteerRecalc = Mathf.Max(maxSteerRecalc, scheduler.LastFrameRecalcCount);
                maxSlotRefresh = Mathf.Max(maxSlotRefresh, slotDone);

                if (frame >= warmupFrames)
                {
                    frameMs[frame - warmupFrames] = sw.Elapsed.TotalMilliseconds;
                }

                Integrate(positions, scheduler, perSide);
            }

            Array.Sort(frameMs);
            double sum = 0;
            double max = 0;
            for (var i = 0; i < frameMs.Length; i++)
            {
                sum += frameMs[i];
                if (frameMs[i] > max)
                {
                    max = frameMs[i];
                }
            }

            var avg = sum / frameMs.Length;
            var p95Index = Mathf.Clamp((int)(frameMs.Length * 0.95f), 0, frameMs.Length - 1);
            var p95 = frameMs[p95Index];

            var structuralOk =
                flow.RebuildCount <= 3 &&
                maxSteerRecalc <= MassMoveScheduler.MaxRecalcPerFrame &&
                maxSlotRefresh <= MassMoveScheduler.MaxRecalcPerFrame &&
                MassMoveScheduler.MaxRecalcPerFrame == 50;

            var withinBudget = avg <= MoveLogicBudgetMs;
            var sb = new StringBuilder(512);
            sb.AppendLine("[MassPathingPerfStress] MP-07 200v200 move-logic report");
            sb.AppendLine($"  Agents: {perSide}+{perSide} = {perSide * 2}");
            sb.AppendLine($"  Frames: warmup={warmupFrames} measure={measureFrames} dt={SimDt:F4}");
            sb.AppendLine($"  MoveLogic ms/frame: avg={avg:F3}  p95={p95:F3}  max={max:F3}  budget={MoveLogicBudgetMs:F1}");
            sb.AppendLine($"  WithinBudget(avg≤{MoveLogicBudgetMs}): {(withinBudget ? "YES" : "NO")}");
            sb.AppendLine($"  FlowField.RebuildCount={flow.RebuildCount} (expect ≤3: start+mid goal switch)");
            sb.AppendLine($"  MaxSteerRecalc/frame={maxSteerRecalc}  MaxSlotRefresh/frame={maxSlotRefresh} (cap={MassMoveScheduler.MaxRecalcPerFrame})");
            sb.AppendLine($"  StructuralOk={structuralOk} (no all-units CalculatePath / HighQuality RVO in this harness)");
            sb.AppendLine($"  {FallbackGuidance}");

            var result = new MassPathingPerfStressResult(
                perSide,
                perSide * 2,
                measureFrames,
                avg,
                max,
                p95,
                flow.RebuildCount,
                maxSteerRecalc,
                maxSlotRefresh,
                withinBudget,
                structuralOk,
                sb.ToString());

            Debug.Log(result.Report);
            return result;
        }

        private static void Integrate(Vector2[] positions, MassMoveScheduler scheduler, int perSide)
        {
            var total = perSide * 2;
            for (var i = 0; i < total; i++)
            {
                var id = i + 1;
                if (!scheduler.TryGetSteer(id, out var steer) || steer.sqrMagnitude < 1e-8f)
                {
                    continue;
                }

                positions[i] += steer.normalized * (MoveSpeed * SimDt);
            }
        }
    }

    public readonly struct MassPathingPerfStressResult
    {
        public readonly int PerSide;
        public readonly int AgentCount;
        public readonly int MeasureFrames;
        public readonly double AvgMoveLogicMs;
        public readonly double MaxMoveLogicMs;
        public readonly double P95MoveLogicMs;
        public readonly int FlowFieldRebuildCount;
        public readonly int MaxSteerRecalcObserved;
        public readonly int MaxSlotRefreshObserved;
        public readonly bool WithinBudget;
        public readonly bool StructuralOk;
        public readonly string Report;

        public MassPathingPerfStressResult(
            int perSide,
            int agentCount,
            int measureFrames,
            double avgMoveLogicMs,
            double maxMoveLogicMs,
            double p95MoveLogicMs,
            int flowFieldRebuildCount,
            int maxSteerRecalcObserved,
            int maxSlotRefreshObserved,
            bool withinBudget,
            bool structuralOk,
            string report)
        {
            PerSide = perSide;
            AgentCount = agentCount;
            MeasureFrames = measureFrames;
            AvgMoveLogicMs = avgMoveLogicMs;
            MaxMoveLogicMs = maxMoveLogicMs;
            P95MoveLogicMs = p95MoveLogicMs;
            FlowFieldRebuildCount = flowFieldRebuildCount;
            MaxSteerRecalcObserved = maxSteerRecalcObserved;
            MaxSlotRefreshObserved = maxSlotRefreshObserved;
            WithinBudget = withinBudget;
            StructuralOk = structuralOk;
            Report = report ?? string.Empty;
        }
    }
}
