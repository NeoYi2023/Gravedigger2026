#if UNITY_EDITOR
using System.IO;
using Gravedigger2026.Editor.Defend;
using Gravedigger2026.Gameplay.Dig;
using Gravedigger2026.Gameplay.Maps;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Gravedigger2026.Editor.Maps
{
    /// <summary>
    /// Copies Example Scene floor tiles into Art/Maps/Tiles and builds Isometric Tilemap Ground_* Prefabs
    /// (SPEC_04 §13 / v0.46.2). IsoDiamond half-extents from PaintRadius*cellSize.
    /// </summary>
    public static class MapTilemapAssetBuilder
    {
        public const string ArtTilesDir = "Assets/Art/Maps/Tiles";
        public const string ArtSpritesDir = ArtTilesDir + "/Sprites";
        public const string ArtPalettesDir = "Assets/Art/Maps/Palettes";
        public const string PrefabMapsDir = "Assets/Prefabs/Maps";

        private const string VendorEnvironmentDir =
            "Assets/SmallScaleInt/Character creator - Fantasy/Example Scene/Environment";
        private const string VendorSpritesDir = VendorEnvironmentDir + "/Sprites";

        private static readonly string[] SpriteFileNames =
        {
            "BLACK TILE.png",
            "Ground G1_E.png",
            "Ground TestRoom.png"
        };

        private static readonly string[] TileAssetNames =
        {
            "BLACK TILE",
            "Ground G1_E",
            "Ground TestRoom"
        };

        private static readonly string[] MapIds =
        {
            "Ground_01", "Ground_02", "Ground_03", "Ground_04", "Ground_05"
        };

        /// <summary>Half-extent of painted iso cell range (cells); world IsoDiamond vertex ~ DigMapBounds 5.</summary>
        private const int PaintRadius = 5;
        private const string OneShotPrefsKey = "Gravedigger2026.MapTilemap.Rebuild.v0462";

        [InitializeOnLoadMethod]
        private static void AutoRebuildOnce()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorApplication.delayCall += TryAutoRebuild;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredEditMode)
            {
                EditorApplication.delayCall += TryAutoRebuild;
            }
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
                EnsureTilesAndForceRebuildMaps();
                DefendAssetBuilder.EnsureEngageZonesAndSpawnPointsOnMaps();
                EditorPrefs.SetBool(OneShotPrefsKey, true);
                Debug.Log("[MapTilemapAssetBuilder] One-shot map Tilemap rebuild complete.");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[MapTilemapAssetBuilder] AutoRebuildOnce failed: {ex}");
            }
        }

        [MenuItem("Gravedigger2026/Maps/Ensure Tiles + Rebuild Map Tilemaps")]
        public static void EnsureTilesAndForceRebuildMaps()
        {
            EnsureFolders();
            EnsureSpritesCopied();
            EnsureTileAssets();
            for (var i = 0; i < MapIds.Length; i++)
            {
                EnsureMapPrefab(MapIds[i], i, forceRepaint: true);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            DefendAssetBuilder.EnsureEngageZonesAndSpawnPointsOnMaps();
            Debug.Log("[MapTilemapAssetBuilder] Tiles ensured; all Ground_* Tilemaps force-rebuilt + IsoDiamond Engage/Spawn.");
        }

        /// <summary>Batch / menu: align WalkSurface + EngageZone + SpawnClock to IsoDiamond without force-repainting tiles.</summary>
        [MenuItem("Gravedigger2026/Maps/Align IsoDiamond Footprints")]
        public static void AlignIsoDiamondFootprints()
        {
            for (var i = 0; i < MapIds.Length; i++)
            {
                EnsureMapPrefab(MapIds[i], i, forceRepaint: false);
            }

            DefendAssetBuilder.EnsureEngageZonesAndSpawnPointsOnMaps();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[MapTilemapAssetBuilder] IsoDiamond WalkSurface / Engage / Spawn aligned.");
        }

        [MenuItem("Gravedigger2026/Maps/Ensure Tiles Only")]
        public static void MenuEnsureTilesOnly()
        {
            EnsureFolders();
            EnsureSpritesCopied();
            EnsureTileAssets();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[MapTilemapAssetBuilder] Tiles/Sprites ensured under Art/Maps.");
        }

        /// <summary>Called by DigAssetBuilder: ensure tiles exist and map Prefabs have Tilemap (preserve hand paint).</summary>
        public static void EnsureMapsForDigBuilder(bool forceRepaint)
        {
            EnsureFolders();
            EnsureSpritesCopied();
            EnsureTileAssets();
            for (var i = 0; i < MapIds.Length; i++)
            {
                EnsureMapPrefab(MapIds[i], i, forceRepaint);
            }
        }

        public static void EnsureFolders()
        {
            EnsureFolder("Assets/Art");
            EnsureFolder("Assets/Art/Maps");
            EnsureFolder(ArtTilesDir);
            EnsureFolder(ArtSpritesDir);
            EnsureFolder(ArtPalettesDir);
            EnsureFolder("Assets/Prefabs");
            EnsureFolder(PrefabMapsDir);
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            var parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            var name = Path.GetFileName(path);
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

        public static void EnsureSpritesCopied()
        {
            for (var i = 0; i < SpriteFileNames.Length; i++)
            {
                var fileName = SpriteFileNames[i];
                var dest = $"{ArtSpritesDir}/{fileName}";
                if (AssetFileExists(dest) || AssetDatabase.LoadAssetAtPath<Object>(dest) != null)
                {
                    continue;
                }

                var src = $"{VendorSpritesDir}/{fileName}";
                if (AssetDatabase.LoadAssetAtPath<Texture2D>(src) == null)
                {
                    Debug.LogError($"[MapTilemapAssetBuilder] Vendor sprite missing: {src}");
                    continue;
                }

                // CopyAsset assigns a new GUID (no collision with SmallScaleInt).
                if (!AssetDatabase.CopyAsset(src, dest))
                {
                    Debug.LogError($"[MapTilemapAssetBuilder] CopyAsset failed: {src} → {dest}");
                }
            }

            AssetDatabase.Refresh();
        }

        private static bool AssetFileExists(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath) || !assetPath.StartsWith("Assets/"))
            {
                return false;
            }

            var full = Path.Combine(Application.dataPath, assetPath.Substring("Assets/".Length));
            return File.Exists(full);
        }

        public static void EnsureTileAssets()
        {
            for (var i = 0; i < TileAssetNames.Length; i++)
            {
                var tileName = TileAssetNames[i];
                var tilePath = $"{ArtTilesDir}/{tileName}.asset";
                var existing = AssetDatabase.LoadAssetAtPath<TileBase>(tilePath);
                if (existing != null)
                {
                    continue;
                }

                var spritePath = $"{ArtSpritesDir}/{SpriteFileNames[i]}";
                var sprite = LoadSprite(spritePath);
                if (sprite == null)
                {
                    Debug.LogError($"[MapTilemapAssetBuilder] Sprite missing for tile: {spritePath}");
                    continue;
                }

                // Prefer built-in Tile type when 2D Tilemap package is present.
                var tileType = System.Type.GetType("UnityEngine.Tilemaps.Tile, UnityEngine.TilemapModule")
                               ?? System.Type.GetType("UnityEngine.Tilemaps.Tile, Unity.2D.Tilemap");
                if (tileType == null)
                {
                    Debug.LogError(
                        $"[MapTilemapAssetBuilder] Tile type missing; ensure {tilePath} exists (YAML) or install com.unity.2d.tilemap.");
                    continue;
                }

                var tile = ScriptableObject.CreateInstance(tileType);
                var so = new SerializedObject(tile);
                var spriteField = so.FindProperty("m_Sprite");
                if (spriteField != null)
                {
                    spriteField.objectReferenceValue = sprite;
                }

                var colliderField = so.FindProperty("m_ColliderType");
                if (colliderField != null)
                {
                    colliderField.enumValueIndex = 0; // None
                }

                so.ApplyModifiedPropertiesWithoutUndo();
                AssetDatabase.CreateAsset(tile, tilePath);
            }
        }

        private static Sprite LoadSprite(string spritePath)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
            if (sprite != null)
            {
                return sprite;
            }

            var assets = AssetDatabase.LoadAllAssetsAtPath(spritePath);
            for (var a = 0; a < assets.Length; a++)
            {
                if (assets[a] is Sprite s)
                {
                    return s;
                }
            }

            return null;
        }

        public static void EnsureMapPrefab(string mapId, int variantIndex, bool forceRepaint)
        {
            var path = $"{PrefabMapsDir}/{mapId}.prefab";
            GameObject root;
            var loadedContents = false;
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null)
            {
                root = PrefabUtility.LoadPrefabContents(path);
                loadedContents = true;
            }
            else
            {
                root = new GameObject(mapId);
            }

            EnsureIsometricTilemap(root, variantIndex, forceRepaint);
            EnsureDigMapBounds(root);
            RemoveLegacyGroundVisual(root);
            EnsureWalkSurface(root);

            PrefabUtility.SaveAsPrefabAsset(root, path);
            if (loadedContents)
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
            else
            {
                Object.DestroyImmediate(root);
            }
        }

        private static void EnsureDigMapBounds(GameObject root)
        {
            var bounds = root.GetComponent<DigMapBounds>();
            if (bounds == null)
            {
                bounds = root.AddComponent<DigMapBounds>();
            }

            var half = MapFootprintMath.HalfExtentsFromIsoCell(PaintRadius, MapFootprintMath.DemoIsoCellSize);
            var so = new SerializedObject(bounds);
            var prop = so.FindProperty("_halfExtents");
            if (prop != null)
            {
                prop.vector2Value = half;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void RemoveLegacyGroundVisual(GameObject root)
        {
            var t = root.transform.Find("GroundVisual");
            if (t != null)
            {
                Object.DestroyImmediate(t.gameObject);
            }
        }

        private static void EnsureWalkSurface(GameObject root)
        {
            // Prefer iso formula; DigMapBounds is optional on Prefab (component may be missing in Hierarchy view).
            var half = MapFootprintMath.HalfExtentsFromIsoCell(PaintRadius, MapFootprintMath.DemoIsoCellSize);
            var bounds = root.GetComponent<DigMapBounds>();
            if (bounds != null)
            {
                half = bounds.HalfExtents;
            }

            var existing = root.transform.Find("WalkSurface");
            GameObject walk;
            if (existing != null)
            {
                walk = existing.gameObject;
            }
            else
            {
                walk = new GameObject("WalkSurface");
                walk.transform.SetParent(root.transform, false);
            }

            if (walk.GetComponent<MeshFilter>() == null)
            {
                walk.AddComponent<MeshFilter>();
            }

            if (walk.GetComponent<MeshRenderer>() == null)
            {
                walk.AddComponent<MeshRenderer>();
            }

            var box = walk.GetComponent<BoxCollider>();
            if (box != null)
            {
                Object.DestroyImmediate(box);
            }

            if (walk.GetComponent<MeshCollider>() == null)
            {
                walk.AddComponent<MeshCollider>();
            }

            var diamond = walk.GetComponent<WalkSurfaceIsoDiamond>();
            if (diamond == null)
            {
                diamond = walk.AddComponent<WalkSurfaceIsoDiamond>();
            }

            diamond.SetHalfExtents(half);
            MapFootprintMath.ApplyWalkSurfaceTransform(walk.transform);
            EditorUtility.SetDirty(walk);
        }

        private static void EnsureIsometricTilemap(GameObject root, int variantIndex, bool forceRepaint)
        {
            var gridTf = root.transform.Find("GroundTilemap");
            GameObject gridGo;
            if (gridTf == null)
            {
                gridGo = new GameObject("GroundTilemap");
                gridGo.transform.SetParent(root.transform, false);
            }
            else
            {
                gridGo = gridTf.gameObject;
            }

            // Rotate Grid so Isometric XY tiles lie on XZ and face Dig/Defend top-down camera (+Y).
            gridGo.transform.localPosition = Vector3.zero;
            gridGo.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            gridGo.transform.localScale = Vector3.one;

            var grid = gridGo.GetComponent<Grid>();
            if (grid == null)
            {
                grid = gridGo.AddComponent<Grid>();
            }

            grid.cellLayout = GridLayout.CellLayout.Isometric;
            grid.cellSize = MapFootprintMath.DemoIsoCellSize;
            grid.cellGap = Vector3.zero;
            grid.cellSwizzle = GridLayout.CellSwizzle.XYZ;

            var tilemapTf = gridGo.transform.Find("Tilemap");
            GameObject tilemapGo;
            if (tilemapTf == null)
            {
                tilemapGo = new GameObject("Tilemap");
                tilemapGo.transform.SetParent(gridGo.transform, false);
            }
            else
            {
                tilemapGo = tilemapTf.gameObject;
            }

            var tilemap = tilemapGo.GetComponent<Tilemap>();
            if (tilemap == null)
            {
                tilemap = tilemapGo.AddComponent<Tilemap>();
            }

            var renderer = tilemapGo.GetComponent<TilemapRenderer>();
            if (renderer == null)
            {
                renderer = tilemapGo.AddComponent<TilemapRenderer>();
            }

            renderer.sortOrder = TilemapRenderer.SortOrder.TopRight;
            renderer.mode = TilemapRenderer.Mode.Chunk;

            var hasTiles = tilemap.GetUsedTilesCount() > 0;
            if (forceRepaint || !hasTiles)
            {
                PaintDefaultPattern(tilemap, variantIndex);
            }
        }

        private static void PaintDefaultPattern(Tilemap tilemap, int variantIndex)
        {
            tilemap.ClearAllTiles();
            var fill = LoadTile(variantIndex % 2 == 0 ? "Ground G1_E" : "Ground TestRoom");
            var border = LoadTile("BLACK TILE");
            if (fill == null)
            {
                Debug.LogError("[MapTilemapAssetBuilder] Fill tile missing — abort paint.");
                return;
            }

            for (var y = -PaintRadius; y <= PaintRadius; y++)
            {
                for (var x = -PaintRadius; x <= PaintRadius; x++)
                {
                    var onBorder = Mathf.Abs(x) == PaintRadius || Mathf.Abs(y) == PaintRadius;
                    TileBase cell = fill;
                    if (onBorder && border != null)
                    {
                        cell = border;
                    }
                    else if (variantIndex >= 2 && ((x + y + variantIndex) & 1) == 0)
                    {
                        var alt = LoadTile(variantIndex % 2 == 0 ? "Ground TestRoom" : "Ground G1_E");
                        if (alt != null)
                        {
                            cell = alt;
                        }
                    }

                    tilemap.SetTile(new Vector3Int(x, y, 0), cell);
                }
            }

            EditorUtility.SetDirty(tilemap);
        }

        private static TileBase LoadTile(string tileName)
        {
            return AssetDatabase.LoadAssetAtPath<TileBase>($"{ArtTilesDir}/{tileName}.asset");
        }
    }
}
#endif
