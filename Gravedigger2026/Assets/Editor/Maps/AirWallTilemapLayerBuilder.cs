#if UNITY_EDITOR
using System.IO;
using Gravedigger2026.Gameplay.PushMap;
using UnityEditor;
using UnityEditor.Tilemaps;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Gravedigger2026.Editor.Maps
{
    /// <summary>
    /// Creates the air-wall brushes and an empty <see cref="AirWallTilemap"/> on combat maps
    /// (SPEC_04 §9.22 / §13). Does not move existing box <see cref="AirWall"/>s.
    /// </summary>
    public static class AirWallTilemapLayerBuilder
    {
        public const string TilesDir = "Assets/Art/Maps/Tiles/AirWall";
        public const string SpriteDir = TilesDir + "/Sprites";
        public const string SolidTilePath = TilesDir + "/AirWallTile.asset";
        public const string SpecialTilePath = TilesDir + "/AirWallSpecialTile.asset";
        public const string PalettePath = "Assets/Art/Maps/Palettes/AirWall.prefab";

        private const int SpriteWidth = 64;
        private const int SpriteHeight = 32;
        private const float SpritePixelsPerUnit = 64f;
        private const int AuthorSortingOrder = 8;

        private static readonly string[] CombatMapPrefabs =
        {
            "Assets/Prefabs/Maps/PushMap_Demo_01.prefab",
            "Assets/Prefabs/Maps/PushMap_Demo_02.prefab",
            "Assets/Prefabs/Maps/PushMap_Demo_03.prefab",
            "Assets/Prefabs/Maps/SearchExtract_Demo_01.prefab",
            "Assets/Prefabs/Maps/SearchExtract_Lv1_01.prefab",
            "Assets/Prefabs/Maps/SearchExtract_Lv2_01.prefab",
            "Assets/Prefabs/Maps/Coc_Lv2_01.prefab"
        };

        [MenuItem("Gravedigger2026/Maps（地图）/Ensure AirWall Tilemap Layer（确保空气墙 Tilemap 层）")]
        public static void EnsureAirWallTilemapLayer()
        {
            EnsureFolder(SpriteDir);
            var solidSprite = EnsureDiamondSprite(
                SpriteDir + "/AirWallTile.png",
                new Color(0.35f, 0.55f, 1f, 0.55f));
            var specialSprite = EnsureDiamondSprite(
                SpriteDir + "/AirWallSpecialTile.png",
                new Color(0.35f, 0.95f, 0.45f, 0.55f));
            var solid = EnsureTile(SolidTilePath, solidSprite, supportsSpecialMove: false);
            var special = EnsureTile(SpecialTilePath, specialSprite, supportsSpecialMove: true);
            EnsurePalette(solid, special);

            var added = 0;
            var present = 0;
            for (var i = 0; i < CombatMapPrefabs.Length; i++)
            {
                if (EnsureLayerOnMap(CombatMapPrefabs[i]))
                {
                    added++;
                }
                else
                {
                    present++;
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log(
                "[AirWallTilemapLayerBuilder] Brushes ready. " +
                $"Added empty AirWallTilemap on {added} map(s); already present on {present}. " +
                "Paint with Tile Palette → AirWall. Box AirWalls were left in place.");
        }

        private static bool EnsureLayerOnMap(string prefabPath)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) == null)
            {
                Debug.LogWarning($"[AirWallTilemapLayerBuilder] Map prefab missing: {prefabPath}");
                return false;
            }

            var root = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                if (root.GetComponentInChildren<AirWallTilemap>(true) != null)
                {
                    return false;
                }

                var grid = FindGroundGrid(root.transform);
                if (grid == null)
                {
                    Debug.LogWarning(
                        $"[AirWallTilemapLayerBuilder] {prefabPath} has no GroundTilemap Grid; layer skipped.");
                    return false;
                }

                var layerGo = new GameObject(AirWallTilemap.LayerName);
                layerGo.transform.SetParent(grid.transform, false);
                layerGo.AddComponent<Tilemap>();
                var renderer = layerGo.AddComponent<TilemapRenderer>();
                renderer.mode = TilemapRenderer.Mode.Chunk;
                renderer.sortingOrder = AuthorSortingOrder;
                layerGo.AddComponent<AirWallTilemap>();
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                return true;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static Grid FindGroundGrid(Transform root)
        {
            var grids = root.GetComponentsInChildren<Grid>(true);
            for (var i = 0; i < grids.Length; i++)
            {
                if (grids[i] != null && grids[i].gameObject.name == "GroundTilemap")
                {
                    return grids[i];
                }
            }

            return null;
        }

        private static void EnsurePalette(AirWallPaintTile solid, AirWallPaintTile special)
        {
            // Hand-written YAML Prefabs are not registered as Tile Palettes.
            // Always rebuild via GridPaletteUtility (same as FantasyTilesetPaletteBuilder).
            if (AssetDatabase.LoadAssetAtPath<Object>(PalettePath) != null)
            {
                AssetDatabase.DeleteAsset(PalettePath);
            }

            var created = GridPaletteUtility.CreateNewPalette(
                "Assets/Art/Maps/Palettes",
                "AirWall",
                GridLayout.CellLayout.Isometric,
                GridPalette.CellSizing.Manual,
                new Vector3(1f, 0.5f, 2f),
                GridLayout.CellSwizzle.XYZ);

            if (created == null)
            {
                Debug.LogError("[AirWallTilemapLayerBuilder] CreateNewPalette returned null for AirWall.");
                return;
            }

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
                    Debug.LogError($"[AirWallTilemapLayerBuilder] MoveAsset failed: {err}");
                    return;
                }
            }

            var root = PrefabUtility.LoadPrefabContents(PalettePath);
            try
            {
                var tilemap = root.GetComponentInChildren<Tilemap>(true);
                if (tilemap == null)
                {
                    Debug.LogError("[AirWallTilemapLayerBuilder] AirWall palette has no Tilemap.");
                    return;
                }

                tilemap.ClearAllTiles();
                tilemap.SetTile(new Vector3Int(0, 0, 0), solid);
                tilemap.SetTile(new Vector3Int(1, 0, 0), special);
                PrefabUtility.SaveAsPrefabAsset(root, PalettePath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static AirWallPaintTile EnsureTile(string path, Sprite sprite, bool supportsSpecialMove)
        {
            var tile = AssetDatabase.LoadAssetAtPath<AirWallPaintTile>(path);
            if (tile == null)
            {
                tile = ScriptableObject.CreateInstance<AirWallPaintTile>();
                ApplyTile(tile, sprite, supportsSpecialMove);
                AssetDatabase.CreateAsset(tile, path);
                return tile;
            }

            ApplyTile(tile, sprite, supportsSpecialMove);
            EditorUtility.SetDirty(tile);
            return tile;
        }

        private static void ApplyTile(AirWallPaintTile tile, Sprite sprite, bool supportsSpecialMove)
        {
            tile.sprite = sprite;
            tile.color = Color.white;
            tile.colliderType = Tile.ColliderType.None;
            tile.SetSupportsSpecialMove(supportsSpecialMove);
        }

        private static Sprite EnsureDiamondSprite(string path, Color color)
        {
            var texture = new Texture2D(SpriteWidth, SpriteHeight, TextureFormat.RGBA32, false);
            var clear = new Color(0f, 0f, 0f, 0f);
            for (var y = 0; y < SpriteHeight; y++)
            {
                for (var x = 0; x < SpriteWidth; x++)
                {
                    var nx = ((x + 0.5f) / SpriteWidth) * 2f - 1f;
                    var ny = ((y + 0.5f) / SpriteHeight) * 2f - 1f;
                    texture.SetPixel(x, y, Mathf.Abs(nx) + Mathf.Abs(ny) <= 1f ? color : clear);
                }
            }

            texture.Apply();
            var png = texture.EncodeToPNG();
            Object.DestroyImmediate(texture);
            File.WriteAllBytes(path, png);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spritePixelsPerUnit = SpritePixelsPerUnit;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.filterMode = FilterMode.Point;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                var settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                settings.spriteMeshType = SpriteMeshType.FullRect;
                settings.spriteAlignment = (int)SpriteAlignment.Center;
                importer.SetTextureSettings(settings);
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static void EnsureFolder(string assetPath)
        {
            if (AssetDatabase.IsValidFolder(assetPath))
            {
                return;
            }

            var parent = Path.GetDirectoryName(assetPath)?.Replace('\\', '/');
            var name = Path.GetFileName(assetPath);
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
            {
                EnsureFolder(parent);
            }

            AssetDatabase.CreateFolder(parent, name);
        }
    }
}
#endif
