#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Gravedigger2026.Editor.Maps
{
    /// <summary>
    /// Aligns FantasyTileset_A cell layout to SmallScaleInt FantasyTileset by matching TileBase.name.
    /// Must use Tilemap API — hand-edited palette YAML does not deserialize reliably.
    /// </summary>
    public static class FantasyTilesetALayoutAligner
    {
        private const string TargetPath = "Assets/Art/Maps/Palettes/FantasyTileset_A.prefab";
        private const string SourcePath =
            "Assets/SmallScaleInt/Fantasy kingdom Tileset/Environment/FantasyTileset.prefab";
        private const int OverflowColumns = 50;

        [MenuItem("Gravedigger2026/Maps/Align FantasyTileset_A Layout From SSI")]
        public static void AlignFantasyTilesetALayoutFromSsi()
        {
            if (AssetDatabase.LoadAssetAtPath<Object>(TargetPath) == null)
            {
                Debug.LogError($"[FantasyTilesetALayoutAligner] Missing target palette: {TargetPath}");
                return;
            }

            if (AssetDatabase.LoadAssetAtPath<Object>(SourcePath) == null)
            {
                Debug.LogError($"[FantasyTilesetALayoutAligner] Missing SSI source palette: {SourcePath}");
                return;
            }

            var sourceRoot = PrefabUtility.LoadPrefabContents(SourcePath);
            var targetRoot = PrefabUtility.LoadPrefabContents(TargetPath);
            try
            {
                var sourceMap = sourceRoot.GetComponentInChildren<Tilemap>();
                var targetMap = targetRoot.GetComponentInChildren<Tilemap>();
                if (sourceMap == null || targetMap == null)
                {
                    Debug.LogError("[FantasyTilesetALayoutAligner] Source or target has no Tilemap.");
                    return;
                }

                var sourceCells = CollectFilledCells(sourceMap);
                var targetByName = CollectTilesByName(targetMap);

                targetMap.ClearAllTiles();

                var occupied = new HashSet<Vector3Int>();
                var matched = 0;
                var skippedRefOnly = 0;

                foreach (var cell in sourceCells)
                {
                    var name = cell.Tile.name;
                    if (!targetByName.TryGetValue(name, out var queue) || queue.Count == 0)
                    {
                        skippedRefOnly++;
                        continue;
                    }

                    var tile = queue.Dequeue();
                    targetMap.SetTile(cell.Position, tile);
                    occupied.Add(cell.Position);
                    matched++;
                }

                var overflow = new List<TileBase>();
                foreach (var kv in targetByName)
                {
                    while (kv.Value.Count > 0)
                    {
                        overflow.Add(kv.Value.Dequeue());
                    }
                }

                var overflowPlaced = PlaceOverflow(targetMap, occupied, overflow, sourceCells);

                // Expand Tilemap origin/size to cover all cells; stale bounds clip the Tile Palette view.
                targetMap.CompressBounds();

                PrefabUtility.SaveAsPrefabAsset(targetRoot, TargetPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                Debug.Log(
                    $"[FantasyTilesetALayoutAligner] Aligned {TargetPath} from SSI layout: " +
                    $"matched={matched}, overflowPlaced={overflowPlaced}, skippedRefOnly={skippedRefOnly}. " +
                    "Re-open Tile Palette → FantasyTileset_A.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(sourceRoot);
                PrefabUtility.UnloadPrefabContents(targetRoot);
            }
        }

        private static List<(Vector3Int Position, TileBase Tile)> CollectFilledCells(Tilemap map)
        {
            var list = new List<(Vector3Int, TileBase)>();
            var bounds = map.cellBounds;
            foreach (var pos in bounds.allPositionsWithin)
            {
                var tile = map.GetTile(pos);
                if (tile == null)
                {
                    continue;
                }

                list.Add((pos, tile));
            }

            return list;
        }

        private static Dictionary<string, Queue<TileBase>> CollectTilesByName(Tilemap map)
        {
            var dict = new Dictionary<string, Queue<TileBase>>();
            var bounds = map.cellBounds;
            foreach (var pos in bounds.allPositionsWithin)
            {
                var tile = map.GetTile(pos);
                if (tile == null)
                {
                    continue;
                }

                if (!dict.TryGetValue(tile.name, out var queue))
                {
                    queue = new Queue<TileBase>();
                    dict[tile.name] = queue;
                }

                queue.Enqueue(tile);
            }

            return dict;
        }

        /// <summary>
        /// Pack unmatched A tiles to the right of the reference occupied bounding box.
        /// </summary>
        private static int PlaceOverflow(
            Tilemap targetMap,
            HashSet<Vector3Int> occupied,
            List<TileBase> overflow,
            List<(Vector3Int Position, TileBase Tile)> sourceCells)
        {
            if (overflow.Count == 0)
            {
                return 0;
            }

            var maxY = 0;
            var maxX = 0;
            if (sourceCells.Count > 0)
            {
                maxY = sourceCells.Max(c => c.Position.y);
                maxX = sourceCells.Max(c => c.Position.x);
            }

            var startX = maxX + 2;
            var placed = 0;
            for (var i = 0; i < overflow.Count; i++)
            {
                var col = i % OverflowColumns;
                var row = i / OverflowColumns;
                var pos = new Vector3Int(startX + col, maxY - row, 0);

                // If somehow colliding, walk right until free.
                while (occupied.Contains(pos))
                {
                    pos = new Vector3Int(pos.x + 1, pos.y, 0);
                }

                targetMap.SetTile(pos, overflow[i]);
                occupied.Add(pos);
                placed++;
            }

            return placed;
        }
    }
}
#endif
