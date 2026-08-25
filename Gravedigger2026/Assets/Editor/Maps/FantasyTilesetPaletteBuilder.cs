#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Tilemaps;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Gravedigger2026.Editor.Maps
{
    /// <summary>
    /// Rebuilds FantasyTileset Tile Palette via Unity API (hand-written YAML does not deserialize into Tilemap).
    /// Also rebinds Tile→Sprite by matching asset names under Environment/Sprites.
    /// </summary>
    public static class FantasyTilesetPaletteBuilder
    {
        private const string EnvDir = "Assets/Art/Maps/Environment";
        private const string TilesDir = EnvDir + "/Tiles";
        private const string SpritesDir = EnvDir + "/Sprites";
        private const string AnimTilesDir = EnvDir + "/Animated tiles";
        private const string PalettePath = "Assets/Art/Maps/Palettes/FantasyTileset.prefab";
        private const string OneShotPrefsKey = "Gravedigger2026.FantasyTilesetPalette.Rebuild.v2";
        private const int Columns = 50;

        [InitializeOnLoadMethod]
        private static void AutoRebuildOnce()
        {
            EditorApplication.delayCall += TryAutoRebuild;
        }

        private static void TryAutoRebuild()
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
                RebuildFantasyTilesetPalette();
                EditorPrefs.SetBool(OneShotPrefsKey, true);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[FantasyTilesetPaletteBuilder] Auto rebuild failed: {ex}");
            }
        }

        [MenuItem("Gravedigger2026/Maps/Rebuild FantasyTileset Palette")]
        public static void RebuildFantasyTilesetPalette()
        {
            if (!AssetDatabase.IsValidFolder(TilesDir))
            {
                Debug.LogError($"[FantasyTilesetPaletteBuilder] Missing tiles folder: {TilesDir}");
                return;
            }

            var rebound = RebindTileSpritesByName();
            var tiles = LoadPaintableTiles();
            if (tiles.Count == 0)
            {
                Debug.LogError("[FantasyTilesetPaletteBuilder] No TileBase with sprites found under Environment/Tiles.");
                return;
            }

            EnsureFolder("Assets/Art/Maps/Palettes");

            if (AssetDatabase.LoadAssetAtPath<Object>(PalettePath) != null)
            {
                AssetDatabase.DeleteAsset(PalettePath);
            }

            // Isometric + Manual — matches original Fantasy kingdom Environment palette cell size.
            var created = GridPaletteUtility.CreateNewPalette(
                "Assets/Art/Maps/Palettes",
                "FantasyTileset",
                GridLayout.CellLayout.Isometric,
                GridPalette.CellSizing.Manual,
                new Vector3(4f, 2f, 4f),
                GridLayout.CellSwizzle.XYZ);

            if (created == null)
            {
                Debug.LogError("[FantasyTilesetPaletteBuilder] CreateNewPalette returned null.");
                return;
            }

            // CreateNewPalette may append " 1" if name collided; normalize to PalettePath.
            var createdPath = AssetDatabase.GetAssetPath(created);
            if (createdPath != PalettePath)
            {
                if (AssetDatabase.LoadAssetAtPath<Object>(PalettePath) != null)
                {
                    AssetDatabase.DeleteAsset(PalettePath);
                }

                var err = AssetDatabase.MoveAsset(createdPath, PalettePath);
                if (!string.IsNullOrEmpty(err))
                {
                    Debug.LogError($"[FantasyTilesetPaletteBuilder] MoveAsset failed: {err}");
                    return;
                }
            }

            var root = PrefabUtility.LoadPrefabContents(PalettePath);
            try
            {
                var tilemap = root.GetComponentInChildren<Tilemap>();
                if (tilemap == null)
                {
                    Debug.LogError("[FantasyTilesetPaletteBuilder] Palette has no Tilemap.");
                    return;
                }

                tilemap.ClearAllTiles();
                for (var i = 0; i < tiles.Count; i++)
                {
                    var x = i % Columns;
                    var y = -(i / Columns);
                    tilemap.SetTile(new Vector3Int(x, y, 0), tiles[i]);
                }

                PrefabUtility.SaveAsPrefabAsset(root, PalettePath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }

            // Remove legacy duplicate under Environment (would appear as a second FantasyTileset in Tile Palette).
            var envPalette = EnvDir + "/FantasyTileset.prefab";
            if (AssetDatabase.LoadAssetAtPath<Object>(envPalette) != null)
            {
                AssetDatabase.DeleteAsset(envPalette);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log(
                $"[FantasyTilesetPaletteBuilder] Rebuilt {PalettePath}: {tiles.Count} tiles (sprites rebound={rebound}). Re-open Tile Palette → FantasyTileset.");
        }

        /// <summary>
        /// Binds each <c>Environment/Tiles/*.asset</c> sprite to the same-named sprite under
        /// <c>Environment/Sprites</c> (e.g. Stone A12_E → Stone A12_E).
        /// </summary>
        [MenuItem("Gravedigger2026/Maps/Rebind Environment Tile Sprites By Name")]
        public static void RebindEnvironmentTileSpritesByNameMenu()
        {
            var fixedCount = RebindTileSpritesByName();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log(
                $"[FantasyTilesetPaletteBuilder] Rebind Environment Tile sprites by name: fixed={fixedCount}. " +
                "If FantasyTileset_A icons still look wrong, run Refresh FantasyTileset_A Sprite Cache.");
        }

        public static int RebindTileSpritesByName()
        {
            var nameToSprite = new Dictionary<string, Sprite>();
            var spriteGuids = AssetDatabase.FindAssets("t:Sprite", new[] { SpritesDir });
            for (var i = 0; i < spriteGuids.Length; i++)
            {
                var path = AssetDatabase.GUIDToAssetPath(spriteGuids[i]);
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (sprite == null)
                {
                    continue;
                }

                var key = System.IO.Path.GetFileNameWithoutExtension(path);
                if (!nameToSprite.ContainsKey(key))
                {
                    nameToSprite[key] = sprite;
                }
            }

            var fixedCount = 0;
            var tileGuids = AssetDatabase.FindAssets("t:Tile", new[] { TilesDir });
            for (var i = 0; i < tileGuids.Length; i++)
            {
                var path = AssetDatabase.GUIDToAssetPath(tileGuids[i]);
                var tile = AssetDatabase.LoadAssetAtPath<Tile>(path);
                if (tile == null)
                {
                    continue;
                }

                var name = System.IO.Path.GetFileNameWithoutExtension(path);
                if (!TryResolveSprite(name, nameToSprite, out var sprite))
                {
                    continue;
                }

                if (tile.sprite == sprite)
                {
                    continue;
                }

                tile.sprite = sprite;
                EditorUtility.SetDirty(tile);
                fixedCount++;
            }

            return fixedCount;
        }

        private static bool TryResolveSprite(string tileName, Dictionary<string, Sprite> map, out Sprite sprite)
        {
            if (map.TryGetValue(tileName, out sprite))
            {
                return true;
            }

            // Unity multi-sprite suffix leftovers: "Ground A1_E_0" → "Ground A1_E"
            var underscore = tileName.LastIndexOf('_');
            if (underscore > 0
                && underscore < tileName.Length - 1
                && tileName.Substring(underscore + 1).All(char.IsDigit))
            {
                var stem = tileName.Substring(0, underscore);
                if (map.TryGetValue(stem, out sprite))
                {
                    return true;
                }
            }

            sprite = null;
            return false;
        }

        private static List<TileBase> LoadPaintableTiles()
        {
            var list = new List<TileBase>();
            var seen = new HashSet<string>();

            void AddFromFolder(string folder, bool requireSpriteOnTile)
            {
                if (!AssetDatabase.IsValidFolder(folder))
                {
                    return;
                }

                var guids = AssetDatabase.FindAssets("t:TileBase", new[] { folder });
                System.Array.Sort(guids, (a, b) =>
                    string.Compare(
                        AssetDatabase.GUIDToAssetPath(a),
                        AssetDatabase.GUIDToAssetPath(b),
                        System.StringComparison.OrdinalIgnoreCase));

                for (var i = 0; i < guids.Length; i++)
                {
                    var path = AssetDatabase.GUIDToAssetPath(guids[i]);
                    if (!seen.Add(path))
                    {
                        continue;
                    }

                    var tile = AssetDatabase.LoadAssetAtPath<TileBase>(path);
                    if (tile == null)
                    {
                        continue;
                    }

                    if (requireSpriteOnTile && tile is Tile t && t.sprite == null)
                    {
                        continue;
                    }

                    list.Add(tile);
                }
            }

            AddFromFolder(TilesDir, requireSpriteOnTile: true);
            AddFromFolder(AnimTilesDir, requireSpriteOnTile: false);
            // MapAutoTile Rule Tiles (RT_WallA) live on FantasyTileset_A via WallARuleTileBuilder — not this rebuild.
            return list;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            var parent = System.IO.Path.GetDirectoryName(path)?.Replace('\\', '/');
            var name = System.IO.Path.GetFileName(path);
            if (string.IsNullOrEmpty(parent) || string.IsNullOrEmpty(name))
            {
                return;
            }

            if (!AssetDatabase.IsValidFolder(parent))
            {
                EnsureFolder(parent);
            }

            AssetDatabase.CreateFolder(parent, name);
        }
    }
}
#endif
