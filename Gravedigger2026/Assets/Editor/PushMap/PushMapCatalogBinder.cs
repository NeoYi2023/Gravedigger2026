#if UNITY_EDITOR
using Gravedigger2026.Gameplay.Defend;
using UnityEditor;
using UnityEngine;

namespace Gravedigger2026.Editor.PushMap
{
    /// <summary>
    /// Binds the sample PushMap map into DefendPrefabCatalog.Maps so PushMapStageController
    /// can resolve it at runtime (PM-03 Approach A). Does not rewrite existing Ground_* entries.
    /// </summary>
    public static class PushMapCatalogBinder
    {
        private const string CatalogPath = "Assets/Settings/Defend/DefendPrefabCatalog.asset";

        [MenuItem("Gravedigger2026/PushMap/Ensure Catalog Map Binding")]
        public static void EnsureCatalogMapBindingMenu()
        {
            if (EnsureCatalogMapBinding())
            {
                Debug.Log(
                    $"[PushMapCatalogBinder] Bound '{PushMapSampleMapBuilder.SampleMapId}' into {CatalogPath}.");
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

            var prefabPath = $"{PushMapSampleMapBuilder.PrefabMapsDir}/{PushMapSampleMapBuilder.SampleMapId}.prefab";
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                Debug.LogError($"[PushMapCatalogBinder] Map prefab missing: {prefabPath}");
                return false;
            }

            if (catalog.TryGetMap(PushMapSampleMapBuilder.SampleMapId, out var existing) && existing == prefab)
            {
                return true;
            }

            var so = new SerializedObject(catalog);
            var mapsProp = so.FindProperty("_maps");
            if (mapsProp == null || !mapsProp.isArray)
            {
                Debug.LogError("[PushMapCatalogBinder] DefendPrefabCatalog._maps not found.");
                return false;
            }

            var mapIdPropPath = nameof(DefendPrefabCatalog.MapEntry.MapId);
            var prefabPropPath = nameof(DefendPrefabCatalog.MapEntry.Prefab);

            for (var i = 0; i < mapsProp.arraySize; i++)
            {
                var entry = mapsProp.GetArrayElementAtIndex(i);
                var idProp = entry.FindPropertyRelative(mapIdPropPath);
                if (idProp != null && idProp.stringValue == PushMapSampleMapBuilder.SampleMapId)
                {
                    entry.FindPropertyRelative(prefabPropPath).objectReferenceValue = prefab;
                    so.ApplyModifiedPropertiesWithoutUndo();
                    EditorUtility.SetDirty(catalog);
                    AssetDatabase.SaveAssets();
                    return true;
                }
            }

            mapsProp.InsertArrayElementAtIndex(mapsProp.arraySize);
            var newEntry = mapsProp.GetArrayElementAtIndex(mapsProp.arraySize - 1);
            newEntry.FindPropertyRelative(mapIdPropPath).stringValue = PushMapSampleMapBuilder.SampleMapId;
            newEntry.FindPropertyRelative(prefabPropPath).objectReferenceValue = prefab;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            return true;
        }
    }
}
#endif
