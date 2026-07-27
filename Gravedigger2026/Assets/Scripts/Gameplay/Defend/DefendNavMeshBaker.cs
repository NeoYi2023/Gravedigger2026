using System.Collections.Generic;
using Gravedigger2026.Gameplay.Maps;
using UnityEngine;
using UnityEngine.AI;

namespace Gravedigger2026.Gameplay.Defend
{
    /// <summary>Runtime IsoDiamond NavMesh bake for Demo BattleMap (SPEC_04 §9.7 / D-041).</summary>
    public static class DefendNavMeshBaker
    {
        public static NavMeshDataInstance Bake(Vector3 center, Vector2 halfExtents)
        {
            var half = MapFootprintMath.SanitizeHalfExtents(halfExtents);
            var mesh = MapFootprintMath.BuildDiamondMesh(half, thickness: 0.2f);
            var sources = new List<NavMeshBuildSource>(1)
            {
                new NavMeshBuildSource
                {
                    shape = NavMeshBuildSourceShape.Mesh,
                    sourceObject = mesh,
                    transform = Matrix4x4.TRS(center, Quaternion.identity, Vector3.one),
                    area = 0
                }
            };

            var cover = MapFootprintMath.NavMeshCoverBoxSize(half, pad: 1.1f, height: 4f);
            var bounds = new Bounds(center, cover);
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
                Object.Destroy(mesh);
                return default;
            }

            var instance = NavMesh.AddNavMeshData(data);
            // Mesh was copied into bake data; release temp asset.
            Object.Destroy(mesh);
            Debug.Log($"[DefendNavMeshBaker] Baked IsoDiamond mesh NavMesh at {center} half={half}");
            return instance;
        }
    }
}
