using System.Collections.Generic;
using Gravedigger2026.Gameplay.Defend;
using UnityEngine;

namespace Gravedigger2026.Gameplay.PushMap
{
    /// <summary>
    /// Splits map AirWalls and painted AirWallTilemap cells for D-101 dual bake (SPEC_04 §9.22):
    /// default mesh includes every wall and tile; special mesh omits SupportsSpecialMove.
    /// </summary>
    public static class AirWallBakeCollector
    {
        public static void Collect(
            Transform mapRoot,
            List<DefendNavMeshBaker.NavMeshBoxObstacle> defaultMeshBoxes,
            List<DefendNavMeshBaker.NavMeshBoxObstacle> specialMeshBoxes,
            out Mesh defaultTileMesh,
            out Mesh specialTileMesh)
        {
            defaultTileMesh = null;
            specialTileMesh = null;
            if (defaultMeshBoxes == null || specialMeshBoxes == null)
            {
                return;
            }

            defaultMeshBoxes.Clear();
            specialMeshBoxes.Clear();
            if (mapRoot == null)
            {
                return;
            }

            var layers = mapRoot.GetComponentsInChildren<AirWallTilemap>(true);
            defaultTileMesh = AirWallTileMeshBuilder.Build(layers, omitSupportsSpecialMove: false);
            specialTileMesh = AirWallTileMeshBuilder.Build(layers, omitSupportsSpecialMove: true);

            var walls = mapRoot.GetComponentsInChildren<AirWall>(true);
            if (walls == null || walls.Length == 0)
            {
                return;
            }

            for (var i = 0; i < walls.Length; i++)
            {
                var wall = walls[i];
                if (wall == null)
                {
                    continue;
                }

                var box = new DefendNavMeshBaker.NavMeshBoxObstacle(
                    wall.transform.position,
                    wall.FullSize,
                    wall.transform.rotation);
                defaultMeshBoxes.Add(box);
                if (!wall.SupportsSpecialMove)
                {
                    specialMeshBoxes.Add(box);
                }
            }
        }
    }
}
