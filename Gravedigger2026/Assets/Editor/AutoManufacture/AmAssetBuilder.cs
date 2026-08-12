#if UNITY_EDITOR
using Gravedigger2026.Gameplay.AutoManufacture;
using UnityEditor;
using UnityEngine;

namespace Gravedigger2026.Editor.AutoManufacture
{
    /// <summary>
    /// Builds AutoManufacturePresentationRoot Prefab + Catalog (UI-016 / D-055).
    /// </summary>
    public static class AmAssetBuilder
    {
        private const string PrefabDir = "Assets/Prefabs/AutoManufacture";
        private const string SettingsDir = "Assets/Settings/AutoManufacture";
        private const string PrefabPath = PrefabDir + "/AutoManufacturePresentationRoot.prefab";
        private const string CatalogPath = SettingsDir + "/AutoManufacturePrefabCatalog.asset";
        private const string RegenPrefsKey = "Gravedigger2026.AmAssets.Regen.v0790";

        [InitializeOnLoadMethod]
        private static void AutoGenerateIfMissing()
        {
            EditorApplication.delayCall += () =>
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode)
                {
                    return;
                }

                var missing = AssetDatabase.LoadAssetAtPath<AutoManufacturePrefabCatalog>(CatalogPath) == null
                              || AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) == null;
                // Only create when assets are missing — never overwrite hand-tuned Prefab on domain reload.
                if (missing)
                {
                    GenerateAll();
                    EditorPrefs.SetBool(RegenPrefsKey, true);
                }
            };
        }

        [MenuItem("Gravedigger2026/AutoManufacture/Generate Presentation Prefab + Catalog")]
        public static void GenerateAll()
        {
            EnsureFolders();

            var temp = new GameObject("AmBuildTemp");
            var built = AutoManufacturePresentationController.Build(temp.transform);
            built.transform.SetParent(null, false);
            Object.DestroyImmediate(temp);

            PrefabUtility.SaveAsPrefabAsset(built.gameObject, PrefabPath);
            Object.DestroyImmediate(built.gameObject);

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            var catalog = AssetDatabase.LoadAssetAtPath<AutoManufacturePrefabCatalog>(CatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<AutoManufacturePrefabCatalog>();
                AssetDatabase.CreateAsset(catalog, CatalogPath);
            }

            catalog.EditorSet(prefab);
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[AmAssetBuilder] Generated AutoManufacturePresentationRoot + Catalog.");
        }

        private static void EnsureFolders()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
            {
                AssetDatabase.CreateFolder("Assets", "Prefabs");
            }

            if (!AssetDatabase.IsValidFolder(PrefabDir))
            {
                AssetDatabase.CreateFolder("Assets/Prefabs", "AutoManufacture");
            }

            if (!AssetDatabase.IsValidFolder("Assets/Settings"))
            {
                AssetDatabase.CreateFolder("Assets", "Settings");
            }

            if (!AssetDatabase.IsValidFolder(SettingsDir))
            {
                AssetDatabase.CreateFolder("Assets/Settings", "AutoManufacture");
            }
        }
    }
}
#endif
