using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace Gravedigger2026.Gameplay.Defend
{
    /// <summary>Runtime minimal NavMesh bake for Demo BattleMap (SPEC_04 §9.7 / D-041).</summary>
    public static class DefendNavMeshBaker
    {
        public static NavMeshDataInstance Bake(Vector3 center, Vector2 halfExtents)
        {
            var pad = 1.25f;
            var size = new Vector3(
                Mathf.Max(2f, halfExtents.x * 2f * pad),
                0.25f,
                Mathf.Max(2f, halfExtents.y * 2f * pad));

            var sources = new List<NavMeshBuildSource>(1)
            {
                new NavMeshBuildSource
                {
                    shape = NavMeshBuildSourceShape.Box,
                    transform = Matrix4x4.TRS(center, Quaternion.identity, Vector3.one),
                    size = size,
                    area = 0
                }
            };

            var bounds = new Bounds(center, new Vector3(size.x, 4f, size.z));
            var settings = NavMesh.GetSettingsByID(0);
            var data = NavMeshBuilder.BuildNavMeshData(
                settings,
                sources,
                bounds,
                center,
                Quaternion.identity);

            if (data == null)
            {
                Debug.LogError("[DefendNavMeshBaker] BuildNavMeshData returned null.");
                return default;
            }

            var instance = NavMesh.AddNavMeshData(data);
            Debug.Log($"[DefendNavMeshBaker] Baked NavMesh at {center} size={size}");
            return instance;
        }
    }
}
