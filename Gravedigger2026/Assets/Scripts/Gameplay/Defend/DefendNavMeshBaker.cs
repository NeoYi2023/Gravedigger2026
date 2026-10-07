using System.Collections.Generic;
using Gravedigger2026.Gameplay.Maps;
using UnityEngine;
using UnityEngine.AI;

namespace Gravedigger2026.Gameplay.Defend
{
    /// <summary>
    /// Runtime IsoDiamond NavMesh bake for Demo BattleMap / PushMap (SPEC_04 §9.7 / D-041 / PM-08 / D-101).
    /// Optional Not Walkable boxes carve AirWall blockers at bake time.
    /// D-101: call twice with different agentTypeID and wall lists (default vs special-move).
    /// </summary>
    public static class DefendNavMeshBaker
    {
        /// <summary>Axis-aligned or rotated box treated as Not Walkable during bake.</summary>
        public readonly struct NavMeshBoxObstacle
        {
            public readonly Vector3 Center;
            public readonly Vector3 Size;
            public readonly Quaternion Rotation;

            public NavMeshBoxObstacle(Vector3 center, Vector3 size, Quaternion rotation)
            {
                Center = center;
                Size = new Vector3(
                    Mathf.Max(0.02f, size.x),
                    Mathf.Max(0.02f, size.y),
                    Mathf.Max(0.02f, size.z));
                Rotation = rotation;
            }
        }

        /// <summary>
        /// COC destructible carve uses this radius so the carved hole matches
        /// FootprintWorldSize instead of the project Humanoid 0.5.
        /// </summary>
        public const float CocCarveAgentRadius = 0.05f;

        public static NavMeshDataInstance Bake(Vector3 center, Vector2 halfExtents)
        {
            return Bake(center, halfExtents, null);
        }

        public static NavMeshDataInstance Bake(
            Vector3 center,
            Vector2 halfExtents,
            IReadOnlyList<NavMeshBoxObstacle> notWalkableBoxes)
        {
            return Bake(center, halfExtents, notWalkableBoxes, agentRadiusOverride: -1f);
        }

        public static NavMeshDataInstance Bake(
            Vector3 center,
            Vector2 halfExtents,
            IReadOnlyList<NavMeshBoxObstacle> notWalkableBoxes,
            float agentRadiusOverride)
        {
            return Bake(
                center,
                halfExtents,
                notWalkableBoxes,
                agentRadiusOverride,
                SpecialMoveNavMesh.DefaultAgentTypeId);
        }

        public static NavMeshDataInstance Bake(
            Vector3 center,
            Vector2 halfExtents,
            IReadOnlyList<NavMeshBoxObstacle> notWalkableBoxes,
            float agentRadiusOverride,
            int agentTypeID,
            Mesh notWalkableTileMesh = null)
        {
            var half = MapFootprintMath.SanitizeHalfExtents(halfExtents);
            var mesh = MapFootprintMath.BuildDiamondMesh(half, thickness: 0.2f);
            var tileVerts = notWalkableTileMesh != null ? notWalkableTileMesh.vertexCount : 0;
            var capacity = 1 + (notWalkableBoxes?.Count ?? 0) + (tileVerts > 0 ? 1 : 0);
            var sources = new List<NavMeshBuildSource>(capacity)
            {
                new NavMeshBuildSource
                {
                    shape = NavMeshBuildSourceShape.Mesh,
                    sourceObject = mesh,
                    transform = Matrix4x4.TRS(center, Quaternion.identity, Vector3.one),
                    area = 0
                }
            };

            var notWalkableArea = ResolveNotWalkableArea();
            if (notWalkableBoxes != null)
            {
                for (var i = 0; i < notWalkableBoxes.Count; i++)
                {
                    var box = notWalkableBoxes[i];
                    sources.Add(new NavMeshBuildSource
                    {
                        shape = NavMeshBuildSourceShape.Box,
                        size = box.Size,
                        transform = Matrix4x4.TRS(box.Center, box.Rotation, Vector3.one),
                        area = notWalkableArea
                    });
                }
            }

            if (tileVerts > 0)
            {
                sources.Add(new NavMeshBuildSource
                {
                    shape = NavMeshBuildSourceShape.Mesh,
                    sourceObject = notWalkableTileMesh,
                    transform = Matrix4x4.identity,
                    area = notWalkableArea
                });
            }

            var cover = MapFootprintMath.NavMeshCoverBoxSize(half, pad: 1.1f, height: 4f);
            var bounds = new Bounds(center, cover);
            var settings = NavMesh.GetSettingsByID(agentTypeID);
            if (agentTypeID != SpecialMoveNavMesh.DefaultAgentTypeId &&
                settings.agentTypeID != agentTypeID)
            {
                Debug.LogError(
                    $"[DefendNavMeshBaker] Unknown agentTypeID={agentTypeID}; skip bake.");
                ReleaseBakeMeshes(mesh, notWalkableTileMesh);
                return default;
            }

            if (agentRadiusOverride > 0f)
            {
                settings.agentRadius = agentRadiusOverride;
                settings.minRegionArea = 0.01f;
                settings.overrideVoxelSize = true;
                settings.voxelSize = Mathf.Max(0.02f, agentRadiusOverride * 0.5f);
            }

            var data = NavMeshBuilder.BuildNavMeshData(
                settings,
                sources,
                bounds,
                center,
                Quaternion.identity);

            if (data == null)
            {
                Debug.LogError("[DefendNavMeshBaker] BuildNavMeshData returned null.");
                ReleaseBakeMeshes(mesh, notWalkableTileMesh);
                return default;
            }

            var instance = NavMesh.AddNavMeshData(data);
            ReleaseBakeMeshes(mesh, notWalkableTileMesh);
            var wallCount = notWalkableBoxes?.Count ?? 0;
            Debug.Log(
                $"[DefendNavMeshBaker] Baked IsoDiamond mesh NavMesh at {center} half={half}" +
                $" agentTypeID={agentTypeID}" +
                (wallCount > 0 ? $" notWalkableBoxes={wallCount}" : string.Empty) +
                (tileVerts > 0 ? $" tileVerts={tileVerts}" : string.Empty));
            return instance;
        }

        private static void ReleaseBakeMeshes(Mesh walkMesh, Mesh tileMesh)
        {
            if (walkMesh != null)
            {
                Object.Destroy(walkMesh);
            }

            if (tileMesh != null)
            {
                Object.Destroy(tileMesh);
            }
        }

        private static int ResolveNotWalkableArea()
        {
            var area = NavMesh.GetAreaFromName("Not Walkable");
            return area >= 0 ? area : 1;
        }
    }
}
