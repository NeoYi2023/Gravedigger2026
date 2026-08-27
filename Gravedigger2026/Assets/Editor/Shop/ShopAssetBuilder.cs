#if UNITY_EDITOR
using System.IO;
using Gravedigger2026.Gameplay.Shop;
using Gravedigger2026.Meta;
using Gravedigger2026.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

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
        private const string BackgroundSpritePath = "Assets/Art/UI/Meta/Title/Title_Shop_1.png";

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

            EnsureShopBackground();

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

        /// <summary>
        /// Surgical patch: UI-026 Background (Title_Shop_1 + AspectRatioFitter EnvelopeParent).
        /// </summary>
        [MenuItem("Gravedigger2026/Shop/Ensure Shop Background (UI-026)")]
        public static void EnsureShopBackground()
        {
            var presentation = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (presentation == null)
            {
                Debug.LogWarning("[ShopAssetBuilder] ShopStageRoot missing; run Generate Shop Prefab + Catalog.");
                return;
            }

            var root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                EnsureBackgroundOnRoot(root.transform);
                EnsureDimAndTransparentBox(root.transform);
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                Debug.Log("[ShopAssetBuilder] Ensured Background on ShopStageRoot.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        /// <summary>Batchmode: -executeMethod Gravedigger2026.Editor.Shop.ShopAssetBuilder.EnsureShopBackgroundBatch</summary>
        public static void EnsureShopBackgroundBatch()
        {
            EnsureShopBackground();
        }

        private static void EnsureBackgroundOnRoot(Transform root)
        {
            if (root == null)
            {
                return;
            }

            var backgroundGo = ShopStageRootView.CreateBackground(root);
            ApplyBackgroundSprite(backgroundGo.GetComponent<Image>());
        }

        private static void EnsureDimAndTransparentBox(Transform root)
        {
            if (root == null)
            {
                return;
            }

            var backdrop = root.Find("ShopBackdrop");
            if (backdrop != null)
            {
                backdrop.SetSiblingIndex(1);
                var image = backdrop.GetComponent<Image>();
                if (image == null)
                {
                    image = backdrop.gameObject.AddComponent<Image>();
                }

                image.color = new Color(0f, 0f, 0f, 0.55f);
                image.raycastTarget = true;
            }

            var box = root.Find("ShopBox");
            if (box != null)
            {
                box.SetSiblingIndex(2);
                var image = box.GetComponent<Image>();
                if (image == null)
                {
                    image = box.gameObject.AddComponent<Image>();
                }

                image.color = new Color(0f, 0f, 0f, 0f);
                image.raycastTarget = true;
            }
        }

        private static void ApplyBackgroundSprite(Image image)
        {
            if (image == null)
            {
                return;
            }

            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(BackgroundSpritePath);
            if (sprite == null)
            {
                Debug.LogWarning(
                    $"[ShopAssetBuilder] Background sprite missing at {BackgroundSpritePath}. " +
                    "Place Title_Shop_1.png then re-run Ensure Shop Background.");
                return;
            }

            image.sprite = sprite;
            image.color = Color.white;
            image.type = Image.Type.Simple;
            image.preserveAspect = false;
            image.raycastTarget = false;

            var aspect = image.GetComponent<AspectRatioFitter>();
            if (aspect == null)
            {
                aspect = image.gameObject.AddComponent<AspectRatioFitter>();
            }

            aspect.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            aspect.aspectRatio = sprite.rect.height > 0.01f
                ? sprite.rect.width / sprite.rect.height
                : 16f / 9f;
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
