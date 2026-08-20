#if UNITY_EDITOR
using System.IO;
using Gravedigger2026.Gameplay.Shop;
using Gravedigger2026.Meta;
using Gravedigger2026.UI;
using UnityEditor;
using UnityEngine;

namespace Gravedigger2026.Editor.Shop
{
    /// <summary>
    /// Builds ShopStageRoot Prefab + Catalog and wires MetaShellRoot (UI-026 / D-075).
    /// </summary>
    public static class ShopAssetBuilder
    {
        private const string PrefabDir = "Assets/Prefabs/Shop";
        private const string SettingsDir = "Assets/Settings/Shop";
        public const string PrefabPath = PrefabDir + "/ShopStageRoot.prefab";
        private const string CatalogPath = SettingsDir + "/ShopPrefabCatalog.asset";
        private const string MetaRootPath = "Assets/Prefabs/Meta/MetaShellRoot.prefab";

        [InitializeOnLoadMethod]
        private static void AutoGenerateIfMissing()
        {
            EditorApplication.delayCall += () =>
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode)
                {
                    return;
                }

                var missing = AssetDatabase.LoadAssetAtPath<ShopPrefabCatalog>(CatalogPath) == null
                              || AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) == null;
                if (missing)
                {
                    GenerateAll();
                }
            };
        }

        [MenuItem("Gravedigger2026/Shop/Generate Shop Prefab + Catalog")]
        public static void GenerateAll()
        {
            EnsureFolders();

            var temp = new GameObject("ShopStageRoot");
            var view = temp.AddComponent<ShopStageRootView>();
            view.BuildFullscreenHierarchy();
            PrefabUtility.SaveAsPrefabAsset(temp, PrefabPath);
            Object.DestroyImmediate(temp);

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            var catalog = AssetDatabase.LoadAssetAtPath<ShopPrefabCatalog>(CatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<ShopPrefabCatalog>();
                AssetDatabase.CreateAsset(catalog, CatalogPath);
            }

            catalog.EditorSet(prefab);
            EditorUtility.SetDirty(catalog);

            WireMetaShell(catalog);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[ShopAssetBuilder] Generated ShopStageRoot + Catalog and wired MetaShellRoot.");
        }

        private static void WireMetaShell(ShopPrefabCatalog catalog)
        {
            var meta = AssetDatabase.LoadAssetAtPath<GameObject>(MetaRootPath);
            if (meta == null)
            {
                Debug.LogWarning("[ShopAssetBuilder] MetaShellRoot missing — run Meta shell builder first.");
                return;
            }

            var contents = PrefabUtility.LoadPrefabContents(MetaRootPath);
            var controller = contents.GetComponent<MetaShellController>();
            if (controller != null)
            {
                var so = new SerializedObject(controller);
                var catalogProp = so.FindProperty("_shopPrefabCatalog");
                if (catalogProp != null)
                {
                    catalogProp.objectReferenceValue = catalog;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }
                else
                {
                    Debug.LogWarning("[ShopAssetBuilder] MetaShellController._shopPrefabCatalog not found.");
                }
            }

            PrefabUtility.SaveAsPrefabAsset(contents, MetaRootPath);
            PrefabUtility.UnloadPrefabContents(contents);
        }

        private static void EnsureFolders()
        {
            EnsureFolder("Assets/Prefabs");
            EnsureFolder(PrefabDir);
            EnsureFolder("Assets/Settings");
            EnsureFolder(SettingsDir);
            EnsureFolder("Assets/Editor");
            EnsureFolder("Assets/Editor/Shop");
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            var parent = Path.GetDirectoryName(path)?.Replace("\\", "/");
            var name = Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
            {
                EnsureFolder(parent);
            }

            if (!string.IsNullOrEmpty(parent) && !string.IsNullOrEmpty(name))
            {
                AssetDatabase.CreateFolder(parent, name);
            }
        }
    }
}
#endif
