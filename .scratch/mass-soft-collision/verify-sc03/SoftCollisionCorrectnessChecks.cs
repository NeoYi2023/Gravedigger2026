using System.Text;
using UnityEngine;

namespace Gravedigger2026.Core.Pathing
{
    /// <summary>
    /// Scene-free correctness checks for SC-01 acceptance (SPEC_03 §3.12 / SPEC_04 §9.7 B+ SoftCollision).
    /// Call <see cref="RunAll"/> from Editor/console or a future EditMode test.
    /// </summary>
    public static class SoftCollisionCorrectnessChecks
    {
        private const float Dt = 1f / 60f;

        /// <summary>Returns null on success; otherwise a multi-line failure report.</summary>
        public static string RunAll()
        {
            var sb = new StringBuilder();
            CheckOverlapSeparates(sb);
            CheckResolveOffAllowsOverlap(sb);
            CheckCoincidentDeterministicPush(sb);
            CheckFrameBudgetRoundRobinRetains(sb);
            CheckNeighborQueryBounded(sb);
            return sb.Length == 0 ? null : sb.ToString();
        }

        private static void CheckOverlapSeparates(StringBuilder sb)
        {
            var svc = new SoftCollisionService();
            var posA = Vector2.zero;
            var posB = new Vector2(0.1f, 0f);
            const float radius = 0.1f;
            svc.Register(1, radius, () => posA);
            svc.Register(2, radius, () => posB);

            svc.Tick(Dt);
            if (!svc.TryGetCorrection(1, out var corrA) || !svc.TryGetCorrection(2, out var corrB))
            {
                sb.AppendLine("OverlapSeparates: missing correction after Tick.");
                return;
            }

            if (corrA.x >= 0f || corrB.x <= 0f)
            {
                sb.AppendLine(
                    $"OverlapSeparates: pushes not anti-parallel along x ({corrA.x:F4}/{corrB.x:F4}).");
            }

            for (var step = 0; step < 10; step++)
            {
                posA += svc.TryGetCorrection(1, out var cA) ? cA : Vector2.zero;
                posB += svc.TryGetCorrection(2, out var cB) ? cB : Vector2.zero;
                svc.Tick(Dt);
            }

            var dist = (posB - posA).magnitude;
            if (dist < 2f * radius - 0.01f)
            {
                sb.AppendLine($"OverlapSeparates: dist {dist:F3} still < minDist 0.2 after 10 ticks.");
            }
        }

        private static void CheckResolveOffAllowsOverlap(StringBuilder sb)
        {
            var svc = new SoftCollisionService();
            var posA = Vector2.zero;
            var posB = new Vector2(0.1f, 0f);
            svc.Register(1, 0.1f, () => posA);
            svc.Register(2, 0.1f, () => posB);

            svc.Tick(Dt);
            svc.TryGetCorrection(1, out var before);
            if (before.sqrMagnitude < 1e-8f)
            {
                sb.AppendLine("ResolveOff: expected non-zero correction while resolving.");
            }

            svc.ResolveCollisions = false;
            svc.Tick(Dt);
            svc.TryGetCorrection(1, out var offA);
            svc.TryGetCorrection(2, out var offB);
            if (offA.sqrMagnitude > 1e-8f || offB.sqrMagnitude > 1e-8f)
            {
                sb.AppendLine("ResolveOff: corrections not zeroed with ResolveCollisions=false.");
            }

            if (svc.LastFrameResolvedCount != 0)
            {
                sb.AppendLine("ResolveOff: resolved count should be 0 while disabled.");
            }
        }

        private static void CheckCoincidentDeterministicPush(StringBuilder sb)
        {
            var pos = Vector2.zero;
            var svc1 = new SoftCollisionService();
            svc1.Register(7, 0.1f, () => pos);
            svc1.Register(9, 0.1f, () => pos);
            svc1.Tick(Dt);

            svc1.TryGetCorrection(7, out var a1);
            svc1.TryGetCorrection(9, out var b1);
            if (a1.sqrMagnitude < 1e-8f || b1.sqrMagnitude < 1e-8f)
            {
                sb.AppendLine("Coincident: zero-vector deadlock — push missing.");
                return;
            }

            if ((a1 + b1).sqrMagnitude > 1e-6f)
            {
                sb.AppendLine("Coincident: RuntimeId side pushes not anti-parallel.");
            }

            var svc2 = new SoftCollisionService();
            svc2.Register(7, 0.1f, () => pos);
            svc2.Register(9, 0.1f, () => pos);
            svc2.Tick(Dt);
            svc2.TryGetCorrection(7, out var a2);
            svc2.TryGetCorrection(9, out var b2);
            if ((a1 - a2).sqrMagnitude > 1e-12f || (b1 - b2).sqrMagnitude > 1e-12f)
            {
                sb.AppendLine("Coincident: pushes not deterministic across identical runs.");
            }
        }

        private static void CheckFrameBudgetRoundRobinRetains(StringBuilder sb)
        {
            var svc = new SoftCollisionService();
            var pos = Vector2.zero;
            svc.Register(1, 0.1f, () => pos);
            svc.Register(2, 0.1f, () => pos);
            svc.Register(3, 0.1f, () => pos);

            svc.Tick(Dt, maxBodiesPerFrame: 1);
            if (svc.LastFrameResolvedCount != 1)
            {
                sb.AppendLine($"Budget: resolved {svc.LastFrameResolvedCount} != 1.");
            }

            svc.TryGetCorrection(1, out var c1First);
            if (c1First.sqrMagnitude < 1e-8f)
            {
                sb.AppendLine("Budget: first round-robin body not resolved.");
            }

            svc.TryGetCorrection(2, out var c2First);
            svc.TryGetCorrection(3, out var c3First);
            if (c2First.sqrMagnitude > 1e-8f || c3First.sqrMagnitude > 1e-8f)
            {
                sb.AppendLine("Budget: bodies beyond budget were resolved in frame 1.");
            }

            svc.Tick(Dt, maxBodiesPerFrame: 1);
            svc.TryGetCorrection(1, out var c1Second);
            svc.TryGetCorrection(2, out var c2Second);
            if ((c1Second - c1First).sqrMagnitude > 1e-12f)
            {
                sb.AppendLine("Budget: approach-A retain broken — stale correction changed off-turn.");
            }

            if (c2Second.sqrMagnitude < 1e-8f)
            {
                sb.AppendLine("Budget: round-robin cursor did not advance to body 2.");
            }
        }

        private static void CheckNeighborQueryBounded(StringBuilder sb)
        {
            var svc = new SoftCollisionService();
            var grid = new Vector2[100];
            for (var i = 0; i < 100; i++)
            {
                grid[i] = new Vector2((i % 10) * 1.0f, (i / 10) * 1.0f);
            }

            for (var i = 0; i < 100; i++)
            {
                var captured = i;
                svc.Register(1000 + i, 0.1f, () => grid[captured]);
            }

            var intruder = new Vector2(0.1f, 0f);
            svc.Register(2000, 0.1f, () => intruder);

            svc.Tick(Dt);
            if (svc.LastFrameResolvedCount != SoftCollisionService.DefaultMaxBodiesPerFrame)
            {
                sb.AppendLine(
                    $"Bounded: resolved {svc.LastFrameResolvedCount} != budget {SoftCollisionService.DefaultMaxBodiesPerFrame}.");
            }

            // query 0.4 over cell 0.5 touches ≤4 buckets; a full-table scan would visit ~100.
            if (svc.LastQueryBucketsVisited > 9)
            {
                sb.AppendLine(
                    $"Bounded: query visited {svc.LastQueryBucketsVisited} buckets — O(n²) scan suspected.");
            }

            if (svc.TryGetCorrection(9999, out _))
            {
                sb.AppendLine("Bounded: unknown id should not have a correction.");
            }
        }
    }
}
