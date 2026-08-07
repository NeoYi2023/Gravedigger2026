using System.Text;
using UnityEngine;

namespace Gravedigger2026.Core.Pathing
{
    /// <summary>
    /// Scene-free correctness checks for MP-01 acceptance (SPEC_04 §9.7).
    /// Call <see cref="RunAll"/> from Editor/console or a future EditMode test.
    /// </summary>
    public static class FlowFieldCorrectnessChecks
    {
        /// <summary>Returns null on success; otherwise a multi-line failure report.</summary>
        public static string RunAll()
        {
            var sb = new StringBuilder();
            CheckConvergence(sb);
            CheckObstacleNoWallCut(sb);
            CheckSharedBufferNoSecondSearch(sb);
            CheckNoFriendlyInApi(sb);
            return sb.Length == 0 ? null : sb.ToString();
        }

        private static void CheckConvergence(StringBuilder sb)
        {
            var field = new FlowFieldService();
            field.Configure(Vector3.zero, new Vector2(5f, 2.5f), 0.5f);
            var goal = new Vector3(0f, 0f, 0f);
            field.Rebuild(goal, StubFullyWalkableMask.Instance);

            var samples = new[]
            {
                new Vector3(3f, 0f, 0f),
                new Vector3(-3f, 0f, 0f),
                new Vector3(0f, 0f, 1.5f),
                new Vector3(0f, 0f, -1.5f),
                new Vector3(2f, 0f, 1f),
            };

            foreach (var start in samples)
            {
                var pos = start;
                var reached = false;
                for (var step = 0; step < 80; step++)
                {
                    var dir = field.SampleDir(pos);
                    if (dir.sqrMagnitude < 1e-6f)
                    {
                        var dx = pos.x - goal.x;
                        var dz = pos.z - goal.z;
                        if (dx * dx + dz * dz <= 1.0f)
                        {
                            reached = true;
                        }

                        break;
                    }

                    pos += new Vector3(dir.x, 0f, dir.y) * field.CellSize;
                    {
                        var dx = pos.x - goal.x;
                        var dz = pos.z - goal.z;
                        if (dx * dx + dz * dz <= 0.75f)
                        {
                            reached = true;
                            break;
                        }
                    }
                }

                if (!reached)
                {
                    sb.AppendLine(
                        $"Convergence fail from ({start.x:F1},{start.z:F1}) → ended ({pos.x:F2},{pos.z:F2})");
                }
            }
        }

        private static void CheckObstacleNoWallCut(StringBuilder sb)
        {
            var field = new FlowFieldService();
            field.Configure(Vector3.zero, new Vector2(5f, 2.5f), 0.5f);

            // Vertical wall strip blocking direct -X → +X through x≈0 (except a south gate).
            var mask = new RectBlockMask(
                minX: -0.4f, maxX: 0.4f,
                minZ: -0.5f, maxZ: 2.5f);

            var goal = new Vector3(2f, 0f, 0f);
            field.Rebuild(goal, mask);

            if (field.IsCellWalkable(0f, 1f))
            {
                sb.AppendLine("Obstacle cell (0,1) unexpectedly walkable.");
            }

            var blockedDir = field.SampleDir(new Vector3(0f, 0f, 1f));
            if (blockedDir.sqrMagnitude > 1e-6f)
            {
                sb.AppendLine(
                    $"Obstacle SampleDir non-zero: ({blockedDir.x:F2},{blockedDir.y:F2})");
            }

            var pos = new Vector3(-2f, 0f, 1f);
            if (float.IsInfinity(field.SampleIntegration(pos.x, pos.z)))
            {
                sb.AppendLine("West side unexpectedly unreachable around wall.");
                return;
            }

            for (var step = 0; step < 60; step++)
            {
                var dir = field.SampleDir(pos);
                if (dir.sqrMagnitude < 1e-6f)
                {
                    break;
                }

                var next = pos + new Vector3(dir.x, 0f, dir.y) * field.CellSize;
                if (mask.IsInsideBlock(next.x, next.z) && field.IsCellWalkable(next.x, next.z))
                {
                    sb.AppendLine(
                        $"Wall-cut: stepped into block at ({next.x:F2},{next.z:F2})");
                    break;
                }

                pos = next;
                var dx = pos.x - goal.x;
                var dz = pos.z - goal.z;
                if (dx * dx + dz * dz <= 1f)
                {
                    break;
                }
            }
        }

        private static void CheckSharedBufferNoSecondSearch(StringBuilder sb)
        {
            var field = new FlowFieldService();
            field.Configure(Vector3.zero, new Vector2(4f, 2f), 0.5f);
            field.Rebuild(Vector3.zero, StubFullyWalkableMask.Instance);
            var afterRebuild = field.RebuildCount;

            for (var i = 0; i < 20; i++)
            {
                field.SampleDir(new Vector3(i * 0.1f, 0f, 0.5f));
            }

            if (field.RebuildCount != afterRebuild)
            {
                sb.AppendLine(
                    $"SampleDir triggered Rebuild (count {afterRebuild} → {field.RebuildCount}).");
            }
        }

        private static void CheckNoFriendlyInApi(StringBuilder sb)
        {
            // Structural: Rebuild only accepts IFlowFieldWalkableMask (static). No unit-list overload.
            var rebuild = typeof(FlowFieldService).GetMethod(
                nameof(FlowFieldService.Rebuild),
                new[] { typeof(Vector3), typeof(IFlowFieldWalkableMask) });
            if (rebuild == null)
            {
                sb.AppendLine("Rebuild(goal, IFlowFieldWalkableMask) missing.");
            }

            foreach (var m in typeof(FlowFieldService).GetMethods())
            {
                if (m.Name != nameof(FlowFieldService.Rebuild))
                {
                    continue;
                }

                foreach (var p in m.GetParameters())
                {
                    if (p.ParameterType.IsArray ||
                        (p.ParameterType.IsGenericType &&
                         p.ParameterType.Name.StartsWith("List")))
                    {
                        sb.AppendLine(
                            $"Rebuild must not accept unit lists (found {p.ParameterType.Name}).");
                    }
                }
            }
        }

        /// <summary>Axis-aligned block treated as non-walkable (static AirWall stand-in).</summary>
        private sealed class RectBlockMask : IFlowFieldWalkableMask
        {
            private readonly float _minX;
            private readonly float _maxX;
            private readonly float _minZ;
            private readonly float _maxZ;

            public RectBlockMask(float minX, float maxX, float minZ, float maxZ)
            {
                _minX = minX;
                _maxX = maxX;
                _minZ = minZ;
                _maxZ = maxZ;
            }

            public bool IsWalkable(float worldX, float worldZ) => !IsInsideBlock(worldX, worldZ);

            public bool IsInsideBlock(float worldX, float worldZ)
            {
                return worldX >= _minX && worldX <= _maxX &&
                       worldZ >= _minZ && worldZ <= _maxZ;
            }
        }
    }
}
