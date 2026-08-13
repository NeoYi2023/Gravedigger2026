#if UNITY_EDITOR
using Gravedigger2026.Gameplay.Defend;
using UnityEditor;
using UnityEngine;

namespace Gravedigger2026.Editor.PushMap
{
    /// <summary>
    /// Binds PushMap_Demo_* maps into DefendPrefabCatalog.Maps so PushMapStageController
    /// can resolve them at runtime (PM-03 Approach A). Does not rewrite existing Ground_* entries.
    /// Does not regenerate Demo_02/03 (author-owned); only EnsureSampleMapPrefab for Demo_01.
    /// </summary>
    public static class PushMapCatalogBinder
    {
        private const string CatalogPath = "Assets/Settings/Defend/DefendPrefabCatalog.asset";

        private static readonly string[] CatalogMapIds =
        {
            "PushMap_Demo_01",
            "PushMap_Demo_02",
            "PushMap_Demo_03"
        };

        [MenuItem("Gravedigger2026/PushMap/Ensure Catalog Map Binding")]
        public static void EnsureCatalogMapBindingMenu()
        {
            if (EnsureCatalogMapBinding())
            {
                Debug.Log(
                    $"[PushMapCatalogBinder] Bound {string.Join(", ", CatalogMapIds)} into {CatalogPath}.");
            }
        }

        /// <summary>Batchmode entry: -executeMethod Gravedigger2026.Editor.PushMap.PushMapCatalogBinder.EnsureCatalogMapBindingBatch</summary>
        public static void EnsureCatalogMapBindingBatch()
        {
            if (!EnsureCatalogMapBinding())
            {
                EditorApplication.Exit(1);
                return;
            }

            EditorApplication.Exit(0);
        }

        public static bool EnsureCatalogMapBinding()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<DefendPrefabCatalog>(CatalogPath);
            if (catalog == null)
            {
                Debug.LogError(
                    $"[PushMapCatalogBinder] Catalog missing: {CatalogPath} — run Gravedigger2026/Defend build first.");
                return false;
            }

            if (!PushMapSampleMapBuilder.EnsureSampleMapPrefab())
            {
                return false;
            }

            var so = new SerializedObject(catalog);
            var mapsProp = so.FindProperty("_maps");
            if (mapsProp == null || !mapsProp.isArray)
            {
                Debug.LogError("[PushMapCatalogBinder] DefendPrefabCatalog._maps not found.");
                return false;
            }

            var bound = 0;
            for (var i = 0; i < CatalogMapIds.Length; i++)
            {
                var mapId = CatalogMapIds[i];
                var prefabPath = $"{PushMapSampleMapBuilder.PrefabMapsDir}/{mapId}.prefab";
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                if (prefab == null)
                {
                    Debug.LogWarning($"[PushMapCatalogBinder] Map prefab missing: {prefabPath}");
                    continue;
                }

                UpsertMapEntry(mapsProp, mapId, prefab);
                bound++;
            }

            if (bound == 0)
            {
                Debug.LogError("[PushMapCatalogBinder] No PushMap_Demo_* prefabs bound.");
                return false;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            return true;
        }

        private static void UpsertMapEntry(SerializedProperty mapsProp, string mapId, GameObject prefab)
        {
            var mapIdPropPath = nameof(DefendPrefabCatalog.MapEntry.MapId);
            var prefabPropPath = nameof(DefendPrefabCatalog.MapEntry.Prefab);

            for (var i = 0; i < mapsProp.arraySize; i++)
            {
                var entry = mapsProp.GetArrayElementAtIndex(i);
                var idProp = entry.FindPropertyRelative(mapIdPropPath);
                if (idProp != null && idProp.stringValue == mapId)
                {
                    entry.FindPropertyRelative(prefabPropPath).objectReferenceValue = prefab;
                    return;
                }
            }

            mapsProp.InsertArrayElementAtIndex(mapsProp.arraySize);
            var newEntry = mapsProp.GetArrayElementAtIndex(mapsProp.arraySize - 1);
            newEntry.FindPropertyRelative(mapIdPropPath).stringValue = mapId;
            newEntry.FindPropertyRelative(prefabPropPath).objectReferenceValue = prefab;
        }
    }
}
#endif
