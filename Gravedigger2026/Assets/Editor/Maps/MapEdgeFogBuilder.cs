#if UNITY_EDITOR
using Gravedigger2026.Gameplay.Dig;
using Gravedigger2026.Gameplay.Maps;
using UnityEditor;
using UnityEngine;

namespace Gravedigger2026.Editor.Maps
{
    /// <summary>
    /// Ensures MapEdgeFog on map Prefabs (SPEC_04 §13 / ME-01).
    /// World-space SpriteRenderer (Fog_1), RotX 90°, sized from DigMapBounds.
    /// Does not touch WalkSurface / NavMesh / AirWall / CameraFogService.
    /// </summary>
    public static class MapEdgeFogBuilder
    {
        public const string PrefabMapsDir = "Assets/Prefabs/Maps";
        public const string FogSpritePath = "Assets/Art/Maps/Fogs/Fog_1.png";

        private const string OneShotPrefsKey = "Gravedigger2026.MapEdgeFog.Ensure.v1";
        private const float DefaultSizeMul = 2.4f;
        private const float DefaultHeightY = 0.02f;

        private static readonly string[] MapPrefabNames =
        {
            "Ground_01", "Ground_02", "Ground_03", "Ground_04", "Ground_05",
            "PushMap_Demo_01", "PushMap_Demo_02", "PushMap_Demo_03"
        };

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
                if (EnsureMapEdgeFogOnAllMaps())
                {
                    EditorPrefs.SetBool(OneShotPrefsKey, true);
                    Debug.Log("[MapEdgeFogBuilder] One-shot MapEdgeFog ensure complete.");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[MapEdgeFogBuilder] AutoEnsureOnce failed: {ex}");
            }
        }

        [MenuItem("Gravedigger2026/Maps/Ensure Map Edge Fog")]
        public static void MenuEnsure()
        {
            EnsureMapEdgeFogOnAllMaps();
        }

        /// <summary>Batchmode: -executeMethod Gravedigger2026.Editor.Maps.MapEdgeFogBuilder.EnsureMapEdgeFogBatch</summary>
        public static void EnsureMapEdgeFogBatch()
        {
            var ok = EnsureMapEdgeFogOnAllMaps();
            EditorApplication.Exit(ok ? 0 : 1);
        }

        public static bool EnsureMapEdgeFogOnAllMaps()
        {
            var fogSprite = AssetDatabase.LoadAssetAtPath<Sprite>(FogSpritePath);
            if (fogSprite == null)
            {
                Debug.LogError($"[MapEdgeFogBuilder] Missing fog sprite: {FogSpritePath}");
                return false;
            }

            var anyOk = false;
            for (var i = 0; i < MapPrefabNames.Length; i++)
            {
                var path = $"{PrefabMapsDir}/{MapPrefabNames[i]}.prefab";
                if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null)
                {
                    Debug.LogWarning($"[MapEdgeFogBuilder] Skip missing Prefab: {path}");
                    continue;
                }

                if (EnsureOnPrefab(path, fogSprite))
                {
                    anyOk = true;
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[MapEdgeFogBuilder] Done. sprite={FogSpritePath}");
            return anyOk;
        }

        private static bool EnsureOnPrefab(string prefabPath, Sprite fogSprite)
        {
            var root = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                var bounds = root.GetComponent<DigMapBounds>();
                if (bounds == null)
                {
                    Debug.LogWarning($"[MapEdgeFogBuilder] {prefabPath}: no DigMapBounds; using Demo half-extents.");
                }

                var fogTf = root.transform.Find(MapEdgeFogView.ChildName);
                GameObject fogGo;
                if (fogTf == null)
                {
                    fogGo = new GameObject(MapEdgeFogView.ChildName);
                    fogGo.transform.SetParent(root.transform, false);
                }
                else
                {
                    fogGo = fogTf.gameObject;
                }

                var view = fogGo.GetComponent<MapEdgeFogView>();
                if (view == null)
                {
                    view = fogGo.AddComponent<MapEdgeFogView>();
                }

                if (fogGo.GetComponent<SpriteRenderer>() == null)
                {
                    fogGo.AddComponent<SpriteRenderer>();
                }

                var isNew = fogTf == null;

                if (isNew)
                {
                    view.Configure(
                        fogSprite,
                        Color.white,
                        DefaultSizeMul,
                        DefaultHeightY,
                        MapEdgeFogView.DefaultSortingOrder,
                        bounds,
                        fitToBounds: true,
                        keepAutoFit: false);
                }
                else
                {
                    // Keep authored pose / SizeMul / Color; only refresh material path via ApplyVisuals.
                    view.SetAutoFitToBounds(false);
                    view.ApplyVisuals();
                }

                var sr = fogGo.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    // Serialize built-in Sprites-Default (fileID 10754); null → magenta error color.
                    var builtin = AssetDatabase.GetBuiltinExtraResource<Material>("Sprites-Default.mat");
                    if (builtin != null)
                    {
                        sr.sharedMaterial = builtin;
                    }
                }

                EditorUtility.SetDirty(fogGo);
                EditorUtility.SetDirty(view);
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                return true;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }
    }
}
#endif
