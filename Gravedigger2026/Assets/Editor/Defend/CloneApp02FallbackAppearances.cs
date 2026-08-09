#if UNITY_EDITOR
using System.Collections.Generic;
using Gravedigger2026.Core.Config;
using Gravedigger2026.Gameplay.Defend;
using Gravedigger2026.Gameplay.UpgradeManufacture;
using UnityEditor;
using UnityEngine;

namespace Gravedigger2026.Editor.Defend
{
    /// <summary>
    /// Clones App_02 warrior Prefab to App_90..App_99 (shared art refs) and refreshes warrior catalog bindings only.
    /// Does not call DefendAssetBuilder.GenerateAll (avoids wiping PushMap map bindings).
    /// </summary>
    public static class CloneApp02FallbackAppearances
    {
        private const string PrefabDir = "Assets/Prefabs/Defend/Warriors";
        private const string SourcePath = PrefabDir + "/App_02.prefab";
        private const string DefendCatalogPath = "Assets/Settings/Defend/DefendPrefabCatalog.asset";
        private const string UmCatalogPath = "Assets/Settings/UpgradeManufacture/UpgradeManufacturePrefabCatalog.asset";
        private const string AppearanceCsv = "Manufacture_BodyAppearanceConfig.csv";
        private const string MenuPath = "Gravedigger2026/Defend/Clone App_02 Fallbacks App_90-99";

        [MenuItem(MenuPath)]
        public static void CloneAndRefreshCatalogs()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(SourcePath) == null)
            {
                Debug.LogError($"[CloneApp02FallbackAppearances] Missing source Prefab: {SourcePath}");
                return;
            }

            var cloned = 0;
            for (var i = 90; i <= 99; i++)
            {
                var id = $"App_{i}";
                var dest = $"{PrefabDir}/{id}.prefab";
                if (AssetDatabase.LoadAssetAtPath<GameObject>(dest) != null)
                {
                    AssetDatabase.DeleteAsset(dest);
                }

                if (!AssetDatabase.CopyAsset(SourcePath, dest))
                {
                    Debug.LogError($"[CloneApp02FallbackAppearances] CopyAsset failed: {dest}");
                    continue;
                }

                RenamePrefabRoot(dest, id);
                cloned++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            RefreshWarriorCatalogBindings();

            Debug.Log(
                $"[CloneApp02FallbackAppearances] Cloned {cloned}/10 Prefabs from App_02 → App_90..App_99; warrior catalogs refreshed.");
        }

        /// <summary>Batch entry for Unity -executeMethod.</summary>
        public static void CloneAndRefreshCatalogsBatch()
        {
            CloneAndRefreshCatalogs();
        }

        private static void RefreshWarriorCatalogBindings()
        {
            var entries = BuildWarriorAppearanceEntries();

            var defend = AssetDatabase.LoadAssetAtPath<DefendPrefabCatalog>(DefendCatalogPath);
            if (defend != null)
            {
                var so = new SerializedObject(defend);
                var list = so.FindProperty("_warriorAppearances");
                list.ClearArray();
                for (var i = 0; i < entries.Count; i++)
                {
                    list.InsertArrayElementAtIndex(i);
                    var elem = list.GetArrayElementAtIndex(i);
                    elem.FindPropertyRelative("AppearanceId").stringValue = entries[i].AppearanceId;
                    elem.FindPropertyRelative("Prefab").objectReferenceValue = entries[i].Prefab;
                }

                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(defend);
            }
            else
            {
                Debug.LogWarning($"[CloneApp02FallbackAppearances] Missing {DefendCatalogPath}");
            }

            var um = AssetDatabase.LoadAssetAtPath<UpgradeManufacturePrefabCatalog>(UmCatalogPath);
            if (um != null)
            {
                var umEntries = new UpgradeManufacturePrefabCatalog.WarriorAppearanceEntry[entries.Count];
                for (var i = 0; i < entries.Count; i++)
                {
                    umEntries[i] = new UpgradeManufacturePrefabCatalog.WarriorAppearanceEntry
                    {
                        AppearanceId = entries[i].AppearanceId,
                        Prefab = entries[i].Prefab
                    };
                }

                um.EditorSetWarriorAppearances(umEntries);
                EditorUtility.SetDirty(um);
            }
            else
            {
                Debug.LogWarning($"[CloneApp02FallbackAppearances] Missing {UmCatalogPath}");
            }

            AssetDatabase.SaveAssets();
        }

        private static List<DefendPrefabCatalog.WarriorAppearanceEntry> BuildWarriorAppearanceEntries()
        {
            var entries = new List<DefendPrefabCatalog.WarriorAppearanceEntry>();
            var csvPath = CsvPathResolver.ResolveExistingFile(AppearanceCsv);
            if (csvPath == null)
            {
                Debug.LogWarning($"[CloneApp02FallbackAppearances] {AppearanceCsv} not found.");
                return entries;
            }

            var rows = SimpleCsv.ReadRows(csvPath);
            for (var i = 0; i < rows.Count; i++)
            {
                if (!rows[i].TryGetValue("AppearanceId", out var appearanceId) || string.IsNullOrEmpty(appearanceId))
                {
                    continue;
                }

                var path = $"{PrefabDir}/{appearanceId}.prefab";
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null)
                {
                    Debug.LogWarning($"[CloneApp02FallbackAppearances] Missing Prefab: {path}");
                    continue;
                }

                entries.Add(new DefendPrefabCatalog.WarriorAppearanceEntry
                {
                    AppearanceId = appearanceId,
                    Prefab = prefab
                });
            }

            return entries;
        }

        private static void RenamePrefabRoot(string prefabPath, string rootName)
        {
            var root = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                root.name = rootName;
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }
    }
}
#endif
