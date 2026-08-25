#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Gravedigger2026.Editor.Maps
{
    /// <summary>
    /// Ensures FlowingWater Tilemap layers on map Prefabs (SPEC_04 §13 / MW-02).
    /// Water = Chunk + Water.mat; Foam = Individual + Foam.mat; paints a small Demo pond.
    /// Does not touch WalkSurface / NavMesh / AirWall / EngageZone.
    /// </summary>
    public static class MapFlowingWaterBuilder
    {
        public const string PrefabMapsDir = "Assets/Prefabs/Maps";
        public const string WaterMatPath = "Assets/Art/Maps/Shaders/Water/Water.mat";
        public const string FoamMatPath = "Assets/Art/Maps/Shaders/Water/Foam.mat";
        public const string WaterMaskTilePath = "Assets/Art/Maps/Tiles/BLACK TILE.asset";
        public const string RipplesDir = "Assets/Art/Maps/Environment/Animated tiles";

        private const string OneShotPrefsKey = "Gravedigger2026.MapFlowingWater.Ensure.v1";
        private const string GridChildName = "GroundTilemap";
        private const string WaterLayerName = "Water";
        private const string FoamLayerName = "Foam";

        /// <summary>Below existing ground orders (lowest authored ≈ -3).</summary>
        private const int WaterSortingOrder = -4;

        /// <summary>Above Water; aligns with lowest ground stack for shoreline rim.</summary>
        private const int FoamSortingOrder = -3;

        private static readonly string[] MapPrefabNames =
        {
            "Ground_01", "Ground_02", "Ground_03", "Ground_04", "Ground_05",
            "PushMap_Demo_01", "PushMap_Demo_02", "PushMap_Demo_03"
        };

        // Demo pond center / manhattan radius (iso cell coords under GroundTilemap).
        private const int PondOriginX = -4;
        private const int PondOriginY = 3;
        private const int PondRadius = 2;

        [InitializeOnLoadMethod]
        private static void AutoEnsureOnce()
        {
            EditorApplication.delayCall += TryAutoEnsure;
        }

        private static void TryAutoEnsure()
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
                if (EnsureFlowingWaterOnAllMaps(forceRepaint: false))
                {
                    EditorPrefs.SetBool(OneShotPrefsKey, true);
                    Debug.Log("[MapFlowingWaterBuilder] One-shot Water/Foam ensure complete.");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[MapFlowingWaterBuilder] AutoEnsureOnce failed: {ex}");
            }
        }

        [MenuItem("Gravedigger2026/Maps/Ensure Flowing Water Layers (preserve paint)")]
        public static void MenuEnsurePreservePaint()
        {
            EnsureFlowingWaterOnAllMaps(forceRepaint: false);
        }

        [MenuItem("Gravedigger2026/Maps/Ensure Flowing Water Layers (force Demo pond)")]
        public static void MenuEnsureForceDemoPond()
        {
            EnsureFlowingWaterOnAllMaps(forceRepaint: true);
        }

        /// <summary>Batchmode: -executeMethod Gravedigger2026.Editor.Maps.MapFlowingWaterBuilder.EnsureFlowingWaterBatch</summary>
        public static void EnsureFlowingWaterBatch()
        {
            var ok = EnsureFlowingWaterOnAllMaps(forceRepaint: true);
            EditorApplication.Exit(ok ? 0 : 1);
        }

        public static bool EnsureFlowingWaterOnAllMaps(bool forceRepaint)
        {
            var waterMat = AssetDatabase.LoadAssetAtPath<Material>(WaterMatPath);
            var foamMat = AssetDatabase.LoadAssetAtPath<Material>(FoamMatPath);
            if (waterMat == null || foamMat == null)
            {
                Debug.LogError(
                    $"[MapFlowingWaterBuilder] Missing materials. Expected {WaterMatPath} and {FoamMatPath} (run MW-01 first).");
                return false;
            }

            var mask = AssetDatabase.LoadAssetAtPath<TileBase>(WaterMaskTilePath);
            if (mask == null)
            {
                Debug.LogError($"[MapFlowingWaterBuilder] Water mask tile missing: {WaterMaskTilePath}");
                return false;
            }

            var ripples = LoadWaterRippleTiles();
            if (ripples.Length == 0)
            {
                Debug.LogError($"[MapFlowingWaterBuilder] No WaterRipples tiles under {RipplesDir}");
                return false;
            }

            var anyOk = false;
            for (var i = 0; i < MapPrefabNames.Length; i++)
            {
                var path = $"{PrefabMapsDir}/{MapPrefabNames[i]}.prefab";
                if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null)
                {
                    Debug.LogWarning($"[MapFlowingWaterBuilder] Skip missing Prefab: {path}");
                    continue;
                }

                if (EnsureOnPrefab(path, waterMat, foamMat, mask, ripples, forceRepaint))
                {
                    anyOk = true;
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log(
                $"[MapFlowingWaterBuilder] Done. forceRepaint={forceRepaint}; materials={WaterMatPath}, {FoamMatPath}");
            return anyOk;
        }

        private static bool EnsureOnPrefab(
            string prefabPath,
            Material waterMat,
            Material foamMat,
            TileBase mask,
            TileBase[] ripples,
            bool forceRepaint)
        {
            var root = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                var gridTf = root.transform.Find(GridChildName);
                if (gridTf == null)
                {
                    Debug.LogError($"[MapFlowingWaterBuilder] {prefabPath}: missing child '{GridChildName}'");
                    return false;
                }

                if (gridTf.GetComponent<Grid>() == null)
                {
                    Debug.LogError($"[MapFlowingWaterBuilder] {prefabPath}: '{GridChildName}' has no Grid");
                    return false;
                }

                var water = EnsureLayer(gridTf, WaterLayerName, waterMat, TilemapRenderer.Mode.Chunk,
                    WaterSortingOrder);
                var foam = EnsureLayer(gridTf, FoamLayerName, foamMat, TilemapRenderer.Mode.Individual,
                    FoamSortingOrder);

                PaintDemoPond(water, foam, mask, ripples, forceRepaint);

                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                return true;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static Tilemap EnsureLayer(
            Transform grid,
            string layerName,
            Material material,
            TilemapRenderer.Mode mode,
            int sortingOrder)
        {
            var tf = grid.Find(layerName);
            GameObject go;
            if (tf == null)
            {
                go = new GameObject(layerName);
                go.transform.SetParent(grid, false);
                go.transform.localPosition = Vector3.zero;
                go.transform.localRotation = Quaternion.identity;
                go.transform.localScale = Vector3.one;
            }
            else
            {
                go = tf.gameObject;
            }

            var tilemap = go.GetComponent<Tilemap>();
            if (tilemap == null)
            {
                tilemap = go.AddComponent<Tilemap>();
            }

            var renderer = go.GetComponent<TilemapRenderer>();
            if (renderer == null)
            {
                renderer = go.AddComponent<TilemapRenderer>();
            }

            renderer.sortOrder = TilemapRenderer.SortOrder.TopRight;
            renderer.mode = mode;
            renderer.sortingOrder = sortingOrder;
            renderer.sharedMaterial = material;
            EditorUtility.SetDirty(go);
            return tilemap;
        }

        private static void PaintDemoPond(
            Tilemap water,
            Tilemap foam,
            TileBase mask,
            TileBase[] ripples,
            bool forceRepaint)
        {
            if (!forceRepaint && water.GetUsedTilesCount() > 0)
            {
                return;
            }

            water.ClearAllTiles();
            foam.ClearAllTiles();

            var rippleIdx = 0;
            for (var y = -PondRadius; y <= PondRadius; y++)
            {
                for (var x = -PondRadius; x <= PondRadius; x++)
                {
                    if (Mathf.Abs(x) + Mathf.Abs(y) > PondRadius)
                    {
                        continue;
                    }

                    var cell = new Vector3Int(PondOriginX + x, PondOriginY + y, 0);
                    water.SetTile(cell, mask);
                    foam.SetTile(cell, ripples[rippleIdx % ripples.Length]);
                    rippleIdx++;
                }
            }

            EditorUtility.SetDirty(water);
            EditorUtility.SetDirty(foam);
        }

        private static TileBase[] LoadWaterRippleTiles()
        {
            var list = new List<TileBase>(13);
            for (var i = 1; i <= 13; i++)
            {
                var path = $"{RipplesDir}/WaterRipples {i}.asset";
                var tile = AssetDatabase.LoadAssetAtPath<TileBase>(path);
                if (tile != null)
                {
                    list.Add(tile);
                }
                else
                {
                    Debug.LogWarning($"[MapFlowingWaterBuilder] Missing ripple tile: {path}");
                }
            }

            return list.ToArray();
        }
    }
}
#endif
