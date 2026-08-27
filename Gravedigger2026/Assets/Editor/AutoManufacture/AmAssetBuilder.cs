#if UNITY_EDITOR
using Gravedigger2026.Gameplay.AutoManufacture;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Gravedigger2026.Editor.AutoManufacture
{
    /// <summary>
    /// Builds AutoManufacturePresentationRoot Prefab + Catalog (UI-016 / D-055)
    /// and shared BookRow.prefab (UI-023 / D-068).
    /// </summary>
    public static class AmAssetBuilder
    {
        private const string PrefabDir = "Assets/Prefabs/AutoManufacture";
        private const string SettingsDir = "Assets/Settings/AutoManufacture";
        public const string PrefabPath = PrefabDir + "/AutoManufacturePresentationRoot.prefab";
        public const string BookRowPrefabPath = PrefabDir + "/BookRow.prefab";
        private const string CatalogPath = SettingsDir + "/AutoManufacturePrefabCatalog.asset";
        private const string BackgroundSpritePath = "Assets/Art/UI/Meta/Title/Title_AutoManufacture_1.png";
        private const string RegenPrefsKey = "Gravedigger2026.AmAssets.Regen.v0790";
        private const string BookRowNestPrefsKey = "Gravedigger2026.AmAssets.BookRowNest.v08272";
        private const string BackgroundPrefsKey = "Gravedigger2026.AmAssets.Background.v08323";

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
                    EditorPrefs.SetBool(BookRowNestPrefsKey, true);
                    EditorPrefs.SetBool(BackgroundPrefsKey, true);
                    return;
                }

                if (!EditorPrefs.GetBool(BookRowNestPrefsKey, false)
                    || AssetDatabase.LoadAssetAtPath<GameObject>(BookRowPrefabPath) == null)
                {
                    NestBookRowIntoExistingPresentation();
                    EditorPrefs.SetBool(BookRowNestPrefsKey, true);
                }

                if (!EditorPrefs.GetBool(BackgroundPrefsKey, false))
                {
                    EnsurePresentationBackground();
                    EditorPrefs.SetBool(BackgroundPrefsKey, true);
                }
            };
        }

        [MenuItem("Gravedigger2026/AutoManufacture/Generate Presentation Prefab + Catalog")]
        public static void GenerateAll()
        {
            EnsureFolders();
            EnsureBookRowPrefab();

            var bookRowPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BookRowPrefabPath);
            var temp = new GameObject("AmBuildTemp");
            var built = AutoManufacturePresentationController.Build(temp.transform);
            built.transform.SetParent(null, false);
            Object.DestroyImmediate(temp);

            ApplyBackgroundSprite(built.transform.Find("Background")?.GetComponent<Image>());
            ReplaceInlineBookRowWithPrefab(built.gameObject, bookRowPrefab, preserveRect: false);
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
            EditorPrefs.SetBool(BackgroundPrefsKey, true);
            Debug.Log("[AmAssetBuilder] Generated AutoManufacturePresentationRoot + Catalog + BookRow.");
        }

        [MenuItem("Gravedigger2026/AutoManufacture/Generate BookRow Prefab (UI-023)")]
        public static GameObject EnsureBookRowPrefab()
        {
            EnsureFolders();
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(BookRowPrefabPath);
            if (existing != null)
            {
                return existing;
            }

            return SaveBookRowPrefab();
        }

        [MenuItem("Gravedigger2026/AutoManufacture/Nest BookRow into Presentation Root")]
        public static void NestBookRowIntoExistingPresentation()
        {
            var bookRowPrefab = EnsureBookRowPrefab();
            if (bookRowPrefab == null)
            {
                bookRowPrefab = SaveBookRowPrefab();
            }

            var presentation = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (presentation == null)
            {
                Debug.LogWarning("[AmAssetBuilder] AutoManufacturePresentationRoot missing; run Generate Presentation.");
                return;
            }

            var root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                ReplaceInlineBookRowWithPrefab(root, bookRowPrefab, preserveRect: true);
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                Debug.Log("[AmAssetBuilder] Nested BookRow.prefab into AutoManufacturePresentationRoot.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }

            EditorPrefs.SetBool(BookRowNestPrefsKey, true);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        /// <summary>
        /// Surgical patch: UI-016 Background (Title_AutoManufacture_1 + AspectRatioFitter EnvelopeParent).
        /// Does not regenerate the whole presentation Prefab.
        /// </summary>
        [MenuItem("Gravedigger2026/AutoManufacture/Ensure Presentation Background (UI-016)")]
        public static void EnsurePresentationBackground()
        {
            var presentation = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (presentation == null)
            {
                Debug.LogWarning("[AmAssetBuilder] AutoManufacturePresentationRoot missing; run Generate Presentation.");
                return;
            }

            var root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                EnsureBackgroundOnRoot(root.transform);
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                Debug.Log("[AmAssetBuilder] Ensured Background on AutoManufacturePresentationRoot.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }

            EditorPrefs.SetBool(BackgroundPrefsKey, true);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        /// <summary>Batchmode: -executeMethod Gravedigger2026.Editor.AutoManufacture.AmAssetBuilder.NestBookRowBatch</summary>
        public static void NestBookRowBatch()
        {
            NestBookRowIntoExistingPresentation();
        }

        /// <summary>Batchmode: -executeMethod Gravedigger2026.Editor.AutoManufacture.AmAssetBuilder.EnsurePresentationBackgroundBatch</summary>
        public static void EnsurePresentationBackgroundBatch()
        {
            EnsurePresentationBackground();
        }

        private static void EnsureBackgroundOnRoot(Transform root)
        {
            if (root == null)
            {
                return;
            }

            var existing = root.Find("Background");
            GameObject backgroundGo;
            if (existing == null)
            {
                backgroundGo = AutoManufacturePresentationController.CreateBackground(root);
            }
            else
            {
                backgroundGo = existing.gameObject;
                backgroundGo.transform.SetAsFirstSibling();
                var aspect = backgroundGo.GetComponent<AspectRatioFitter>();
                if (aspect == null)
                {
                    aspect = backgroundGo.AddComponent<AspectRatioFitter>();
                }

                aspect.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
                aspect.aspectRatio = 16f / 9f;

                var rt = backgroundGo.GetComponent<RectTransform>();
                if (rt != null)
                {
                    rt.anchorMin = new Vector2(0.5f, 0.5f);
                    rt.anchorMax = new Vector2(0.5f, 0.5f);
                    rt.pivot = new Vector2(0.5f, 0.5f);
                    rt.anchoredPosition = Vector2.zero;
                }
            }

            ApplyBackgroundSprite(backgroundGo.GetComponent<Image>());
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
                    $"[AmAssetBuilder] Background sprite missing at {BackgroundSpritePath}. " +
                    "Place Title_AutoManufacture_1.png then re-run Ensure Presentation Background.");
                return;
            }

            image.sprite = sprite;
            image.color = Color.white;
            image.type = Image.Type.Simple;
            image.preserveAspect = false;
            image.raycastTarget = false;
        }

        private static GameObject SaveBookRowPrefab()
        {
            var temp = new GameObject("BookRowBuildTemp", typeof(RectTransform));
            var row = BookRowView.CreateHierarchy(temp.transform);
            row.transform.SetParent(null, false);
            Object.DestroyImmediate(temp);
            row.SetAllowReorder(false);
            PrefabUtility.SaveAsPrefabAsset(row.gameObject, BookRowPrefabPath);
            Object.DestroyImmediate(row.gameObject);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[AmAssetBuilder] Generated BookRow.prefab.");
            return AssetDatabase.LoadAssetAtPath<GameObject>(BookRowPrefabPath);
        }

        private static void ReplaceInlineBookRowWithPrefab(
            GameObject presentationRoot,
            GameObject bookRowPrefab,
            bool preserveRect)
        {
            if (presentationRoot == null || bookRowPrefab == null)
            {
                return;
            }

            var controller = presentationRoot.GetComponent<AutoManufacturePresentationController>();
            var existing = presentationRoot.transform.Find("BookRow");
            Vector2 anchored = new Vector2(0f, 220f);
            Vector2 size = new Vector2(BookRowView.RowWidth, AutoMfgMagicBookSlotView.SlotHeight);
            Vector2 anchorMin = new Vector2(0.5f, 0.5f);
            Vector2 anchorMax = new Vector2(0.5f, 0.5f);
            Vector2 pivot = new Vector2(0.5f, 0.5f);
            var sibling = 1;

            if (existing != null)
            {
                var existingRt = existing as RectTransform;
                if (preserveRect && existingRt != null)
                {
                    anchored = existingRt.anchoredPosition;
                    size = existingRt.sizeDelta;
                    anchorMin = existingRt.anchorMin;
                    anchorMax = existingRt.anchorMax;
                    pivot = existingRt.pivot;
                }

                sibling = existing.GetSiblingIndex();
                var source = PrefabUtility.GetCorrespondingObjectFromSource(existing.gameObject);
                if (source == bookRowPrefab)
                {
                    WirePresentationBookRow(controller, existing.GetComponent<BookRowView>(), existingRt);
                    return;
                }

                Object.DestroyImmediate(existing.gameObject);
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(bookRowPrefab, presentationRoot.transform);
            instance.name = "BookRow";
            instance.transform.SetSiblingIndex(sibling);
            var rt = instance.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchorMin = anchorMin;
                rt.anchorMax = anchorMax;
                rt.pivot = pivot;
                rt.anchoredPosition = anchored;
                rt.sizeDelta = size;
            }

            var view = instance.GetComponent<BookRowView>();
            if (view != null)
            {
                view.SetAllowReorder(false);
            }

            WirePresentationBookRow(controller, view, rt);
        }

        private static void WirePresentationBookRow(
            AutoManufacturePresentationController controller,
            BookRowView view,
            RectTransform bookRt)
        {
            if (controller == null)
            {
                return;
            }

            var so = new SerializedObject(controller);
            var bookRowProp = so.FindProperty("_bookRow");
            if (bookRowProp != null)
            {
                bookRowProp.objectReferenceValue = bookRt;
            }

            var bookRowViewProp = so.FindProperty("_bookRowView");
            if (bookRowViewProp != null)
            {
                bookRowViewProp.objectReferenceValue = view;
            }

            var slotsProp = so.FindProperty("_bookSlots");
            if (slotsProp != null && view != null && view.Slots != null)
            {
                slotsProp.arraySize = view.Slots.Length;
                for (var i = 0; i < view.Slots.Length; i++)
                {
                    slotsProp.GetArrayElementAtIndex(i).objectReferenceValue = view.Slots[i];
                }
            }

            so.ApplyModifiedPropertiesWithoutUndo();
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
