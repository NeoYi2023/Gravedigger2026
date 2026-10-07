using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Gravedigger2026.Gameplay.PushMap
{
    /// <summary>
    /// Extrudes painted isometric cells into one Not-Walkable volume mesh (SPEC_04 §9.22).
    /// </summary>
    public static class AirWallTileMeshBuilder
    {
        /// <summary>Full vertical size, matching a typical AirWall box height (HalfExtents.y × 2).</summary>
        public const float ExtrudeHeight = 1.5f;

        public static Mesh Build(AirWallTilemap[] layers, bool omitSupportsSpecialMove)
        {
            if (layers == null || layers.Length == 0)
            {
                return null;
            }

            var verts = new List<Vector3>(64);
            var tris = new List<int>(128);
            for (var i = 0; i < layers.Length; i++)
            {
                AppendLayer(layers[i], omitSupportsSpecialMove, verts, tris);
            }

            if (verts.Count == 0)
            {
                return null;
            }

            var mesh = new Mesh
            {
                name = omitSupportsSpecialMove ? "AirWallTileSpecial" : "AirWallTileDefault"
            };
            mesh.SetVertices(verts);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        private static void AppendLayer(
            AirWallTilemap layer,
            bool omitSupportsSpecialMove,
            List<Vector3> verts,
            List<int> tris)
        {
            if (layer == null)
            {
                return;
            }

            var tilemap = layer.Tilemap;
            var grid = tilemap != null ? tilemap.layoutGrid : null;
            if (tilemap == null || grid == null)
            {
                return;
            }

            var bounds = tilemap.cellBounds;
            foreach (var cell in bounds.allPositionsWithin)
            {
                if (!tilemap.HasTile(cell))
                {
                    continue;
                }

                if (omitSupportsSpecialMove)
                {
                    var paint = tilemap.GetTile(cell) as AirWallPaintTile;
                    if (paint != null && paint.SupportsSpecialMove)
                    {
                        continue;
                    }
                }

                AddCell(grid, cell, verts, tris);
            }
        }

        private static void AddCell(Grid grid, Vector3Int cell, List<Vector3> verts, List<int> tris)
        {
            var c00 = grid.CellToWorld(cell);
            var c10 = grid.CellToWorld(cell + new Vector3Int(1, 0, 0));
            var c11 = grid.CellToWorld(cell + new Vector3Int(1, 1, 0));
            var c01 = grid.CellToWorld(cell + new Vector3Int(0, 1, 0));
            var up = Vector3.up * (ExtrudeHeight * 0.5f);
            var start = verts.Count;
            verts.Add(c00 - up);
            verts.Add(c10 - up);
            verts.Add(c11 - up);
            verts.Add(c01 - up);
            verts.Add(c00 + up);
            verts.Add(c10 + up);
            verts.Add(c11 + up);
            verts.Add(c01 + up);

            AddFace(tris, start, 0, 2, 1);
            AddFace(tris, start, 0, 3, 2);
            AddFace(tris, start, 4, 5, 6);
            AddFace(tris, start, 4, 6, 7);
            AddFace(tris, start, 0, 1, 5);
            AddFace(tris, start, 0, 5, 4);
            AddFace(tris, start, 1, 2, 6);
            AddFace(tris, start, 1, 6, 5);
            AddFace(tris, start, 2, 3, 7);
            AddFace(tris, start, 2, 7, 6);
            AddFace(tris, start, 3, 0, 4);
            AddFace(tris, start, 3, 4, 7);
        }

        private static void AddFace(List<int> tris, int start, int a, int b, int c)
        {
            tris.Add(start + a);
            tris.Add(start + b);
            tris.Add(start + c);
            tris.Add(start + a);
            tris.Add(start + c);
            tris.Add(start + b);
        }
    }
}
