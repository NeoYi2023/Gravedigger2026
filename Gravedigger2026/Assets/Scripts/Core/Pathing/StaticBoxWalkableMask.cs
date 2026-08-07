using System.Collections.Generic;
using UnityEngine;

namespace Gravedigger2026.Core.Pathing
{
    /// <summary>
    /// Static OBB blockers for FlowField (SPEC_04 §9.7 / §9.22 MP-04).
    /// Pure C#: AirWall / bake Not-Walkable boxes. Friendlies never added.
    /// </summary>
    public sealed class StaticBoxWalkableMask : IFlowFieldWalkableMask
    {
        public readonly struct BoxObstacle
        {
            public readonly Vector3 Center;
            public readonly Vector3 HalfExtents;
            public readonly Quaternion Rotation;

            public BoxObstacle(Vector3 center, Vector3 halfExtents, Quaternion rotation)
            {
                Center = center;
                HalfExtents = new Vector3(
                    Mathf.Max(0.01f, halfExtents.x),
                    Mathf.Max(0.01f, halfExtents.y),
                    Mathf.Max(0.01f, halfExtents.z));
                Rotation = rotation;
            }

            /// <summary>From full bake size (HalfExtents×2) as used by NavMeshBoxObstacle.</summary>
            public static BoxObstacle FromFullSize(Vector3 center, Vector3 fullSize, Quaternion rotation)
            {
                return new BoxObstacle(center, fullSize * 0.5f, rotation);
            }
        }

        private readonly List<BoxObstacle> _boxes = new List<BoxObstacle>(8);

        public int BoxCount => _boxes.Count;

        public void Clear()
        {
            _boxes.Clear();
        }

        public void AddBox(BoxObstacle box)
        {
            _boxes.Add(box);
        }

        public void AddBox(Vector3 center, Vector3 halfExtents, Quaternion rotation)
        {
            _boxes.Add(new BoxObstacle(center, halfExtents, rotation));
        }

        public bool IsWalkable(float worldX, float worldZ)
        {
            for (var i = 0; i < _boxes.Count; i++)
            {
                if (ContainsXZ(_boxes[i], worldX, worldZ))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool ContainsXZ(in BoxObstacle box, float worldX, float worldZ)
        {
            var local = Quaternion.Inverse(box.Rotation) * new Vector3(
                worldX - box.Center.x,
                0f,
                worldZ - box.Center.z);
            return Mathf.Abs(local.x) <= box.HalfExtents.x
                   && Mathf.Abs(local.z) <= box.HalfExtents.z;
        }
    }
}
