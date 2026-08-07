using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Gravedigger2026.Core.Pathing
{
    /// <summary>
    /// Scene-free correctness checks for MP-03 acceptance (SPEC_04 §9.7 LocalDetour).
    /// Call <see cref="RunAll"/> from Editor/console or a future EditMode test.
    /// </summary>
    public static class LocalDetourCorrectnessChecks
    {
        /// <summary>Returns null on success; otherwise a multi-line failure report.</summary>
        public static string RunAll()
        {
            var sb = new StringBuilder();
            CheckSteerMatchesDesiredWhenEmpty(sb);
            CheckForwardBlockerPicksSideBias(sb);
            CheckHashQueryDoesNotScanFullTable(sb);
            CheckHotPathReusesNeighborList(sb);
            return sb.Length == 0 ? null : sb.ToString();
        }

        private static void CheckSteerMatchesDesiredWhenEmpty(StringBuilder sb)
        {
            var solver = new LocalDetourSolver();
            var self = new LocalDetourAgent(1, Vector2.zero, 0.1f);
            var neighbors = new List<SpatialHashEntry>();
            var desired = new Vector2(1f, 0f).normalized;
            var steer = solver.Steer(desired, self, neighbors, separationScale: 0f);

            if ((steer - desired).sqrMagnitude > 1e-6f)
            {
                sb.AppendLine(
                    $"EmptyNeighbors: steer {steer} != desired {desired}.");
            }
        }

        private static void CheckForwardBlockerPicksSideBias(StringBuilder sb)
        {
            var solver = new LocalDetourSolver();
            var self = new LocalDetourAgent(1, Vector2.zero, 0.1f);
            // Friend dead ahead on +X; slight offset to +Y so left probe is clearer.
            var neighbors = new List<SpatialHashEntry>
            {
                new SpatialHashEntry(2, new Vector2(0.55f, 0.08f), 0.1f),
            };
            var desired = new Vector2(1f, 0f);
            var steer = solver.Steer(desired, self, neighbors, separationScale: 0f);

            if (steer.sqrMagnitude < 1e-6f)
            {
                sb.AppendLine("ForwardBlocker: steer magnitude ~0.");
                return;
            }

            var steerN = steer.normalized;
            var desiredN = desired.normalized;
            var forwardDot = Vector2.Dot(steerN, desiredN);
            if (forwardDot < 0.2f)
            {
                sb.AppendLine(
                    $"ForwardBlocker: steer reversed (dot={forwardDot:F3}).");
            }

            // Must leave the straight line: lateral component non-trivial.
            var lateral = Mathf.Abs(steerN.y);
            if (lateral < 0.15f)
            {
                sb.AppendLine(
                    $"ForwardBlocker: expected L/R bias, lateral={lateral:F3}, steer={steerN}.");
            }
        }

        private static void CheckHashQueryDoesNotScanFullTable(StringBuilder sb)
        {
            var hash = new SpatialHash2D(SpatialHash2D.DefaultCellSize);
            const int farCount = 200;
            for (var i = 0; i < farCount; i++)
            {
                // Spread far away so they land in many distant buckets.
                hash.Insert(i, new Vector2(50f + i * 0.6f, 50f), 0.1f);
            }

            // One local neighbor near origin.
            hash.Insert(9001, new Vector2(0.2f, 0f), 0.1f);

            var results = new List<SpatialHashEntry>(16);
            hash.QueryNeighbors(Vector2.zero, 1.0f, results);

            if (results.Count != 1 || results[0].Id != 9001)
            {
                sb.AppendLine(
                    $"HashQuery: expected only local id 9001, got count={results.Count}.");
            }

            // Must only visit nearby buckets, not all farCount buckets.
            if (hash.LastQueryBucketsVisited > 20)
            {
                sb.AppendLine(
                    $"HashQuery: visited {hash.LastQueryBucketsVisited} buckets (too many; full scan?).");
            }

            if (hash.LastQueryBucketsVisited >= hash.BucketCount && hash.BucketCount > 20)
            {
                sb.AppendLine(
                    $"HashQuery: visited all {hash.BucketCount} buckets — not hash-local.");
            }

            if (hash.Count != farCount + 1)
            {
                sb.AppendLine($"HashQuery: count {hash.Count} != {farCount + 1}.");
            }
        }

        private static void CheckHotPathReusesNeighborList(StringBuilder sb)
        {
            // Structural guarantee: Steer/Query take caller List and do not return new collections.
            // Run repeated Steers with one reused list; capacity must not explode from hidden allocs
            // inside our APIs (list growth only from explicit Add by caller).
            var hash = new SpatialHash2D();
            var solver = new LocalDetourSolver();
            var self = new LocalDetourAgent(0, Vector2.zero, 0.1f);
            var buffer = new List<SpatialHashEntry>(32);
            var capacityBefore = buffer.Capacity;

            for (var frame = 0; frame < 50; frame++)
            {
                hash.Clear();
                hash.Insert(1, new Vector2(0.4f, 0f), 0.1f);
                hash.Insert(2, new Vector2(-0.4f, 0.2f), 0.1f);
                buffer.Clear();
                hash.QueryNeighbors(
                    self.Position,
                    SpatialHash2D.RecommendedQueryRadius(self.Radius),
                    buffer);
                _ = solver.Steer(Vector2.right, self, buffer, separationScale: 0.5f);
            }

            if (buffer.Capacity > capacityBefore * 4 && buffer.Capacity > 128)
            {
                sb.AppendLine(
                    $"HotPathReuse: buffer capacity grew unexpectedly {capacityBefore}->{buffer.Capacity}.");
            }
        }
    }
}
