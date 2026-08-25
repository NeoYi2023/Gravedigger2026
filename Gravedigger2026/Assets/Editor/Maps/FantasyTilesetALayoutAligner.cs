#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Gravedigger2026.Editor.Maps
{
    /// <summary>
    /// Corrects FantasyTileset_A cell layout to SmallScaleInt FantasyTileset by matching TileBase.name
    /// (Art Environment/Tiles only — does not copy SSI TileSpriteArray offsets).
    /// Preserves Ground F4_W in place (SSI has no such tile). Re-pins RT_WallA to its fixed slot.
    /// Must use Tilemap API — hand-edited palette YAML does not deserialize reliably.
    /// </summary>
    public static class FantasyTilesetALayoutAligner
    {
        private const string TargetPath = "Assets/Art/Maps/Palettes/FantasyTileset_A.prefab";
        private const string SourcePath =
            "Assets/SmallScaleInt/Fantasy kingdom Tileset/Environment/FantasyTileset.prefab";
        private const string ArtTilesDir = "Assets/Art/Maps/Environment/Tiles";
        private const string ArtAnimTilesDir = "Assets/Art/Maps/Environment/Animated tiles";
        private const string ArtRuleTilesDir = "Assets/Art/Maps/RuleTiles";
        private const string PreserveTileName = "Ground F4_W";
        private const string RtWallAName = "RT_WallA";
        private const int OverflowColumns = 50;
        private const string OneShotPrefsKey = "Gravedigger2026.FantasyTilesetA.Correct.v1";

        [InitializeOnLoadMethod]
        private static void AutoCorrectOnce()
        {
            EditorApplication.delayCall += TryAutoCorrectOnce;
        }

        private static void TryAutoCorrectOnce()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            if (EditorPrefs.GetBool(OneShotPrefsKey, false))
            {
                return;
            }

            try
            {
                if (CorrectFantasyTilesetAFromFantasyTileset())
                {
                    EditorPrefs.SetBool(OneShotPrefsKey, true);
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[FantasyTilesetALayoutAligner] Auto correct failed: {ex}");
            }
        }

        /// <summary>
        /// Rebinds Environment Tile→Sprite by name, then re-applies every FantasyTileset_A cell
        /// so Tilemap's cached TileSpriteArray matches each Tile's own sprite (palette icons).
        /// </summary>
        [MenuItem("Gravedigger2026/Maps/Refresh FantasyTileset_A Sprite Cache")]
        public static void RefreshFantasyTilesetASpriteCache()
        {
            var rebound = FantasyTilesetPaletteBuilder.RebindTileSpritesByName();
            if (AssetDatabase.LoadAssetAtPath<Object>(TargetPath) == null)
            {
                Debug.LogError($"[FantasyTilesetALayoutAligner] Missing target palette: {TargetPath}");
                return;
            }

            var root = PrefabUtility.LoadPrefabContents(TargetPath);
            try
            {
                var map = root.GetComponentInChildren<Tilemap>();
                if (map == null)
                {
                    Debug.LogError("[FantasyTilesetALayoutAligner] FantasyTileset_A has no Tilemap.");
                    return;
                }

                var cells = CollectFilledCells(map);
                map.ClearAllTiles();
                for (var i = 0; i < cells.Count; i++)
                {
                    map.SetTile(cells[i].Position, cells[i].Tile);
                }

                map.CompressBounds();
                PrefabUtility.SaveAsPrefabAsset(root, TargetPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log(
                    $"[FantasyTilesetALayoutAligner] Refreshed {TargetPath} sprite cache " +
                    $"(cells={cells.Count}, tileSpritesRebound={rebound}). " +
                    "Re-open Tile Palette → FantasyTileset_A.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        /// <summary>
        /// Legacy menu alias — same as Correct FantasyTileset_A From FantasyTileset.
        /// </summary>
        [MenuItem("Gravedigger2026/Maps/Align FantasyTileset_A Layout From SSI")]
        public static void AlignFantasyTilesetALayoutFromSsi()
        {
            CorrectFantasyTilesetAFromFantasyTileset();
        }

        [MenuItem("Gravedigger2026/Maps/Correct FantasyTileset_A From FantasyTileset")]
        public static void CorrectFantasyTilesetAFromFantasyTilesetMenu()
        {
            CorrectFantasyTilesetAFromFantasyTileset();
        }

        /// <summary>
        /// Correct FantasyTileset_A from vendor FantasyTileset by Tile asset name.
        /// Batch: <c>-executeMethod Gravedigger2026.Editor.Maps.FantasyTilesetALayoutAligner.CorrectFantasyTilesetAFromFantasyTileset</c>
        /// </summary>
        public static bool CorrectFantasyTilesetAFromFantasyTileset()
        {
            if (AssetDatabase.LoadAssetAtPath<Object>(TargetPath) == null)
            {
                Debug.LogError($"[FantasyTilesetALayoutAligner] Missing target palette: {TargetPath}");
                return false;
            }

            if (AssetDatabase.LoadAssetAtPath<Object>(SourcePath) == null)
            {
                Debug.LogError($"[FantasyTilesetALayoutAligner] Missing SSI source palette: {SourcePath}");
                return false;
            }

            var rebound = FantasyTilesetPaletteBuilder.RebindTileSpritesByName();
            var artByName = LoadArtTilesByName();

            var sourceRoot = PrefabUtility.LoadPrefabContents(SourcePath);
            var targetRoot = PrefabUtility.LoadPrefabContents(TargetPath);
            try
            {
                var sourceMap = sourceRoot.GetComponentInChildren<Tilemap>();
                var targetMap = targetRoot.GetComponentInChildren<Tilemap>();
                if (sourceMap == null || targetMap == null)
                {
                    Debug.LogError("[FantasyTilesetALayoutAligner] Source or target has no Tilemap.");
                    return false;
                }

                var sourceCells = CollectFilledCells(sourceMap);
                var previousA = CollectFilledCells(targetMap);
                var preserveCells = previousA
                    .Where(c => c.Tile != null && c.Tile.name == PreserveTileName)
                    .ToList();

                var placedNames = new HashSet<string>();
                var previousByName = CollectTilesByName(targetMap);

                targetMap.ClearAllTiles();

                var occupied = new HashSet<Vector3Int>();
                var matched = 0;
                var skippedRefOnly = 0;

                foreach (var cell in sourceCells)
                {
                    var name = cell.Tile.name;
                    if (!TryResolveArtTile(name, artByName, previousByName, out var tile))
                    {
                        skippedRefOnly++;
                        continue;
                    }

                    targetMap.SetTile(cell.Position, tile);
                    occupied.Add(cell.Position);
                    placedNames.Add(name);
                    matched++;
                }

                var preserved = PreserveInPlace(targetMap, occupied, preserveCells, artByName);
                foreach (var cell in preserveCells)
                {
                    placedNames.Add(cell.Tile.name);
                }

                var overflow = new List<TileBase>();
                foreach (var kv in previousByName)
                {
                    if (kv.Key == PreserveTileName || kv.Key == RtWallAName)
                    {
                        continue;
                    }

                    if (placedNames.Contains(kv.Key))
                    {
                        continue;
                    }

                    while (kv.Value.Count > 0)
                    {
                        overflow.Add(kv.Value.Dequeue());
                    }
                }

                var overflowPlaced = PlaceOverflow(targetMap, occupied, overflow, sourceCells);

                WallARuleTileBuilder.PinRtWallAOnTilemap(targetMap);
                occupied.Add(WallARuleTileBuilder.RtWallAPaletteSlot);

                // Re-apply all cells so TileSpriteArray matches each Tile's own sprite after rebind.
                var finalCells = CollectFilledCells(targetMap);
                targetMap.ClearAllTiles();
                for (var i = 0; i < finalCells.Count; i++)
                {
                    targetMap.SetTile(finalCells[i].Position, finalCells[i].Tile);
                }

                targetMap.CompressBounds();

                PrefabUtility.SaveAsPrefabAsset(targetRoot, TargetPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                Debug.Log(
                    $"[FantasyTilesetALayoutAligner] Corrected {TargetPath} from FantasyTileset: " +
                    $"matched={matched}, preserved={preserved} ({PreserveTileName}), " +
                    $"overflowPlaced={overflowPlaced}, skippedRefOnly={skippedRefOnly}, " +
                    $"tileSpritesRebound={rebound}, RT_WallA@{WallARuleTileBuilder.RtWallAPaletteSlot}. " +
                    "Re-open Tile Palette → FantasyTileset_A.");
                return true;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(sourceRoot);
                PrefabUtility.UnloadPrefabContents(targetRoot);
            }
        }

        private static bool TryResolveArtTile(
            string name,
            Dictionary<string, TileBase> artByName,
            Dictionary<string, Queue<TileBase>> previousByName,
            out TileBase tile)
        {
            if (artByName.TryGetValue(name, out tile) && tile != null)
            {
                return true;
            }

            if (previousByName.TryGetValue(name, out var queue) && queue.Count > 0)
            {
                tile = queue.Dequeue();
                return tile != null;
            }

            tile = null;
            return false;
        }

        private static Dictionary<string, TileBase> LoadArtTilesByName()
        {
            var dict = new Dictionary<string, TileBase>();
            AddTilesFromFolder(dict, ArtTilesDir);
            AddTilesFromFolder(dict, ArtAnimTilesDir);
            AddTilesFromFolder(dict, ArtRuleTilesDir);
            return dict;
        }

        private static void AddTilesFromFolder(Dictionary<string, TileBase> dict, string folder)
        {
            if (!AssetDatabase.IsValidFolder(folder))
            {
                return;
            }

            var guids = AssetDatabase.FindAssets("t:TileBase", new[] { folder });
            for (var i = 0; i < guids.Length; i++)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[i]);
                var tile = AssetDatabase.LoadAssetAtPath<TileBase>(path);
                if (tile == null)
                {
                    continue;
                }

                if (!dict.ContainsKey(tile.name))
                {
                    dict[tile.name] = tile;
                }
            }
        }

        /// <summary>
        /// Writes preserve tiles back to their previous cells; walks right if occupied.
        /// </summary>
        private static int PreserveInPlace(
            Tilemap targetMap,
            HashSet<Vector3Int> occupied,
            List<(Vector3Int Position, TileBase Tile)> preserveCells,
            Dictionary<string, TileBase> artByName)
        {
            var placed = 0;
            for (var i = 0; i < preserveCells.Count; i++)
            {
                var preferred = preserveCells[i].Position;
                var name = preserveCells[i].Tile.name;
                var tile = preserveCells[i].Tile;
                if (artByName.TryGetValue(name, out var artTile) && artTile != null)
                {
                    tile = artTile;
                }

                var pos = preferred;
                while (occupied.Contains(pos))
                {
                    pos = new Vector3Int(pos.x + 1, pos.y, 0);
                }

                targetMap.SetTile(pos, tile);
                occupied.Add(pos);
                placed++;

                if (pos != preferred)
                {
                    Debug.LogWarning(
                        $"[FantasyTilesetALayoutAligner] {name} preferred {preferred} occupied; placed at {pos}.");
                }
            }

            return placed;
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

                while (occupied.Contains(pos) || pos == WallARuleTileBuilder.RtWallAPaletteSlot)
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
