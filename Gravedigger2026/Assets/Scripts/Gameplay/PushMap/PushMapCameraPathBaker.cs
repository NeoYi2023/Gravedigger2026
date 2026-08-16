using System.Collections.Generic;
using Gravedigger2026.Core.Config;
using Gravedigger2026.Core.Pathing;
using UnityEngine;

namespace Gravedigger2026.Gameplay.PushMap
{
    /// <summary>
    /// Bakes <see cref="PushMapCameraPath"/> polyline (SPEC_04 §9.22).
    /// Adjacent author waypoints: world-XZ straight samples at <see cref="SampleSpacing"/>.
    /// Editor or StartBattle; never per-soldier / per-frame. Not soldier pathing.
    /// </summary>
    public static class PushMapCameraPathBaker
    {
        public const float SampleSpacing = CombatConstantKeys.Safety.FlowFieldDefaultCellSize;

        public static bool TryBake(PushMapCameraPath path, out string error)
        {
            error = null;
            if (path == null)
            {
                error = "CameraFollowPath is null.";
                return false;
            }

            var waypoints = path.CollectWaypoints();
            if (waypoints.Length < 2)
            {
                error = "CameraFollowPath needs ≥2 CameraPathWaypoint children.";
                return false;
            }

            var worldPoints = new List<Vector3>(64);
            for (var i = 0; i < waypoints.Length - 1; i++)
            {
                var a = waypoints[i].transform.position;
                var b = waypoints[i + 1].transform.position;
                AppendSampled(worldPoints, a, b, a.y);
            }

            AppendIfFar(worldPoints, waypoints[waypoints.Length - 1].transform.position);

            if (worldPoints.Count < 2)
            {
                error = "Bake produced fewer than 2 points.";
                return false;
            }

            var local = new Vector3[worldPoints.Count];
            for (var i = 0; i < worldPoints.Count; i++)
            {
                local[i] = path.transform.InverseTransformPoint(worldPoints[i]);
            }

            path.SetBakedPoints(local);
            return true;
        }

        private static void AppendSampled(List<Vector3> points, Vector3 from, Vector3 to, float y)
        {
            var a = new Vector3(from.x, y, from.z);
            var b = new Vector3(to.x, y, to.z);
            AppendIfFar(points, a);
            var dx = b.x - a.x;
            var dz = b.z - a.z;
            var len = Mathf.Sqrt(dx * dx + dz * dz);
            if (len < SampleSpacing)
            {
                AppendIfFar(points, b);
                return;
            }

            var steps = Mathf.Max(1, Mathf.FloorToInt(len / SampleSpacing));
            for (var i = 1; i <= steps; i++)
            {
                var t = i / (float)steps;
                AppendIfFar(points, Vector3.Lerp(a, b, t));
            }
        }

        private static void AppendIfFar(List<Vector3> points, Vector3 world)
        {
            if (points.Count == 0)
            {
                points.Add(world);
                return;
            }

            var last = points[points.Count - 1];
            var dx = world.x - last.x;
            var dz = world.z - last.z;
            if (dx * dx + dz * dz < 0.01f)
            {
                return;
            }

            points.Add(world);
        }
    }
}
