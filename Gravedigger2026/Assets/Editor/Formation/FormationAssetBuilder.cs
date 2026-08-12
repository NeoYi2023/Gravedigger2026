#if UNITY_EDITOR
using Gravedigger2026.Gameplay.Formation;
using Gravedigger2026.Meta;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Gravedigger2026.Editor.Formation
{
    /// <summary>
    /// Builds FormationEditorRoot Prefab + Catalog and wires MetaShell (SPEC_03 D-032).
    /// </summary>
    public static class FormationAssetBuilder
    {
        private const string PrefabDir = "Assets/Prefabs/Formation";
        private const string SettingsDir = "Assets/Settings/Formation";
        private const string CatalogPath = SettingsDir + "/FormationPrefabCatalog.asset";
        private const string EditorRootPath = PrefabDir + "/FormationEditorRoot.prefab";
        private const string EditorRootMode2Path = PrefabDir + "/FormationEditorRoot_Mode2.prefab";
        private const string MetaRootPath = "Assets/Prefabs/Meta/MetaShellRoot.prefab";
        private const string RegenPrefsKey = "Gravedigger2026.FormationAssets.Regen.v0791";

        [InitializeOnLoadMethod]
        private static void AutoGenerateIfMissing()
        {
            EditorApplication.delayCall += () =>
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode)
                {
                    return;
                }

                var missing = AssetDatabase.LoadAssetAtPath<FormationPrefabCatalog>(CatalogPath) == null
                              || AssetDatabase.LoadAssetAtPath<GameObject>(EditorRootPath) == null
                              || AssetDatabase.LoadAssetAtPath<GameObject>(EditorRootMode2Path) == null;
                var needsRegen = !EditorPrefs.GetBool(RegenPrefsKey, false);
                if (missing || needsRegen)
                {
                    GenerateAll();
                    EditorPrefs.SetBool(RegenPrefsKey, true);
                }
            };
        }

        [MenuItem("Gravedigger2026/Formation/Generate FormationEditor Prefab + Catalog")]
        public static void GenerateAll()
        {
            EnsureFolders();

            var rootGo = BuildEditorRoot();
            PrefabUtility.SaveAsPrefabAsset(rootGo, EditorRootPath);
            Object.DestroyImmediate(rootGo);
            var rootPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(EditorRootPath);
            var rootMode2Prefab = BuildAndSaveMode2EditorRoot();

            var catalog = AssetDatabase.LoadAssetAtPath<FormationPrefabCatalog>(CatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<FormationPrefabCatalog>();
                AssetDatabase.CreateAsset(catalog, CatalogPath);
            }

            catalog.EditorSet(rootPrefab);
            catalog.EditorSetMode2(rootMode2Prefab);
            EditorUtility.SetDirty(catalog);

            WireMetaShell(catalog);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log(
                "[FormationAssetBuilder] Generated FormationEditorRoot (Mode1+Mode2) + Catalog and wired MetaShellRoot.");
        }

        private static GameObject BuildAndSaveMode2EditorRoot()
        {
            var contents = PrefabUtility.LoadPrefabContents(EditorRootPath);
            contents.name = "FormationEditorRoot_Mode2";
            var hud = FindDeep(contents.transform, "ControlPowerText");
            if (hud != null)
            {
                hud.gameObject.SetActive(false);
            }
            else
            {
                Debug.LogWarning("[FormationAssetBuilder] ControlPowerText not found when building Mode2 EditorRoot.");
            }

            EnsureMode2CompleteButton(contents);

            PrefabUtility.SaveAsPrefabAsset(contents, EditorRootMode2Path);
            PrefabUtility.UnloadPrefabContents(contents);
            return AssetDatabase.LoadAssetAtPath<GameObject>(EditorRootMode2Path);
        }

        /// <summary>
        /// Mode2 only: Complete above SoldierBar, right edge (SPEC_03 §3.11 Mode2 / D-053).
        /// </summary>
        private static void EnsureMode2CompleteButton(GameObject editorRoot)
        {
            var canvas = FindDeep(editorRoot.transform, "FormationCanvas");
            if (canvas == null)
            {
                Debug.LogWarning("[FormationAssetBuilder] FormationCanvas missing; cannot add Mode2 CompleteButton.");
                return;
            }

            var existing = FindDeep(editorRoot.transform, "CompleteButton");
            GameObject completeGo;
            if (existing != null)
            {
                completeGo = existing.gameObject;
            }
            else
            {
                completeGo = CreateUiButton(
                    canvas,
                    "CompleteButton",
                    "完成 / 进入下一阶段",
                    new Color(0.28f, 0.48f, 0.36f, 1f));
            }

            // SoldierBar height 112; sit just above it on the right near screen edge.
            Place(
                completeGo.GetComponent<RectTransform>(),
                new Vector2(1f, 0f),
                new Vector2(1f, 0f),
                new Vector2(1f, 0f),
                new Vector2(-24f, 124f),
                new Vector2(260f, 48f));
            completeGo.SetActive(true);

            var controller = editorRoot.GetComponent<FormationEditorController>();
            if (controller == null)
            {
                Debug.LogWarning("[FormationAssetBuilder] FormationEditorController missing on Mode2 root.");
                return;
            }

            var cso = new SerializedObject(controller);
            var prop = cso.FindProperty("_completeButton");
            if (prop != null)
            {
                prop.objectReferenceValue = completeGo.GetComponent<Button>();
                cso.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static Transform FindDeep(Transform root, string name)
        {
            if (root == null)
            {
                return null;
            }

            if (root.name == name)
            {
                return root;
            }

            for (var i = 0; i < root.childCount; i++)
            {
                var found = FindDeep(root.GetChild(i), name);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        private static void WireMetaShell(FormationPrefabCatalog catalog)
        {
            var meta = AssetDatabase.LoadAssetAtPath<GameObject>(MetaRootPath);
            if (meta == null)
            {
                Debug.LogWarning("[FormationAssetBuilder] MetaShellRoot missing.");
                return;
            }

            var contents = PrefabUtility.LoadPrefabContents(MetaRootPath);
            var controller = contents.GetComponent<MetaShellController>();
            if (controller != null)
            {
                var so = new SerializedObject(controller);
                var prop = so.FindProperty("_formationPrefabCatalog");
                if (prop != null)
                {
                    prop.objectReferenceValue = catalog;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }
            }

            PrefabUtility.SaveAsPrefabAsset(contents, MetaRootPath);
            PrefabUtility.UnloadPrefabContents(contents);
        }

        private static GameObject BuildEditorRoot()
        {
            var root = new GameObject("FormationEditorRoot");
            var controller = root.AddComponent<FormationEditorController>();

            var world = new GameObject("WorldRoot");
            world.transform.SetParent(root.transform, false);

            var camGo = new GameObject("FormationCamera", typeof(Camera));
            camGo.transform.SetParent(root.transform, false);
            var cam = camGo.GetComponent<Camera>();
            cam.orthographic = true;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.12f, 0.14f, 0.18f, 1f);
            cam.depth = 20;

            var canvasGo = new GameObject("FormationCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(root.transform, false);
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 70;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            var controlText = CreateUiText(canvasGo.transform, "ControlPowerText", "0 / 0", 28, TextAnchor.UpperLeft);
            Place(controlText.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(24f, -24f), new Vector2(360f, 48f));

            var returnBtn = CreateUiButton(canvasGo.transform, "ReturnButton", "返回", new Color(0.35f, 0.4f, 0.5f, 1f));
            Place(returnBtn.GetComponent<RectTransform>(), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(-24f, -24f), new Vector2(140f, 48f));

            var startBtn = CreateUiButton(canvasGo.transform, "StartBattleButton", "开战", new Color(0.55f, 0.32f, 0.28f, 1f));
            Place(startBtn.GetComponent<RectTransform>(), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(-24f, -24f), new Vector2(140f, 48f));

            var barPanel = CreateUiPanel(canvasGo.transform, "SoldierBar", new Color(0.1f, 0.11f, 0.14f, 0.92f));
            var barRt = barPanel.GetComponent<RectTransform>();
            barRt.anchorMin = new Vector2(0f, 0f);
            barRt.anchorMax = new Vector2(1f, 0f);
            barRt.pivot = new Vector2(0.5f, 0f);
            barRt.anchoredPosition = new Vector2(0f, 0f);
            barRt.sizeDelta = new Vector2(0f, 112f);

            var scrollGo = new GameObject("Scroll", typeof(RectTransform), typeof(ScrollRect), typeof(Image));
            scrollGo.transform.SetParent(barPanel.transform, false);
            var scrollRt = scrollGo.GetComponent<RectTransform>();
            Stretch(scrollRt);
            scrollRt.offsetMin = new Vector2(12f, 8f);
            scrollRt.offsetMax = new Vector2(-12f, -8f);
            var scrollImg = scrollGo.GetComponent<Image>();
            scrollImg.color = new Color(0.14f, 0.15f, 0.18f, 0.5f);
            scrollImg.raycastTarget = true;
            var scroll = scrollGo.GetComponent<ScrollRect>();
            scroll.horizontal = true;
            scroll.vertical = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 24f;

            var viewportGo = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(RectMask2D));
            viewportGo.transform.SetParent(scrollGo.transform, false);
            var viewportRt = viewportGo.GetComponent<RectTransform>();
            Stretch(viewportRt);
            var viewportImg = viewportGo.GetComponent<Image>();
            viewportImg.color = new Color(1f, 1f, 1f, 0.02f);
            viewportImg.raycastTarget = true;

            var contentGo = new GameObject("Content", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            contentGo.transform.SetParent(viewportGo.transform, false);
            var contentRt = contentGo.GetComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0f, 0.5f);
            contentRt.anchorMax = new Vector2(0f, 0.5f);
            contentRt.pivot = new Vector2(0f, 0.5f);
            contentRt.anchoredPosition = Vector2.zero;
            contentRt.sizeDelta = new Vector2(80f, 80f);
            var hlg = contentGo.GetComponent<HorizontalLayoutGroup>();
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.spacing = 8f;
            hlg.padding = new RectOffset(4, 4, 0, 0);
            hlg.childControlWidth = false;
            hlg.childControlHeight = false;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;
            scroll.content = contentRt;
            scroll.viewport = viewportRt;

            var slotGo = CreateSlotTemplate(contentGo.transform);
            var slotView = slotGo.GetComponent<FormationSoldierSlotView>();

            var barView = barPanel.AddComponent<FormationSoldierBarView>();

            // PointerCatcher removed: bar drives input via Update (see FormationSoldierBarView).
            var barSo = new SerializedObject(barView);
            barSo.FindProperty("_barRoot").objectReferenceValue = barRt;
            barSo.FindProperty("_scrollRect").objectReferenceValue = scroll;
            barSo.FindProperty("_content").objectReferenceValue = contentRt;
            barSo.FindProperty("_slotTemplate").objectReferenceValue = slotView;
            barSo.ApplyModifiedPropertiesWithoutUndo();

            var ghostGo = new GameObject("DragGhost", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
            ghostGo.transform.SetParent(canvasGo.transform, false);
            var ghostRt = ghostGo.GetComponent<RectTransform>();
            ghostRt.sizeDelta = new Vector2(80f, 80f);
            var ghostImg = ghostGo.GetComponent<Image>();
            ghostImg.raycastTarget = false;
            ghostImg.preserveAspect = true;
            ghostGo.GetComponent<CanvasGroup>().blocksRaycasts = false;
            ghostGo.SetActive(false);

            var preview = root.AddComponent<FormationBattlefieldPreview>();

            var cso = new SerializedObject(controller);
            cso.FindProperty("_worldRoot").objectReferenceValue = world.transform;
            cso.FindProperty("_editorCamera").objectReferenceValue = cam;
            cso.FindProperty("_soldierBar").objectReferenceValue = barView;
            cso.FindProperty("_battlefieldPreview").objectReferenceValue = preview;
            cso.FindProperty("_controlPowerText").objectReferenceValue = controlText;
            cso.FindProperty("_returnButton").objectReferenceValue = returnBtn.GetComponent<Button>();
            cso.FindProperty("_startBattleButton").objectReferenceValue = startBtn.GetComponent<Button>();
            cso.FindProperty("_dragGhost").objectReferenceValue = ghostRt;
            cso.FindProperty("_dragGhostImage").objectReferenceValue = ghostImg;
            cso.ApplyModifiedPropertiesWithoutUndo();

            return root;
        }

        private static GameObject CreateSlotTemplate(Transform parent)
        {
            var go = new GameObject(
                "SoldierSlotTemplate",
                typeof(RectTransform),
                typeof(Image),
                typeof(LayoutElement),
                typeof(FormationSoldierSlotView));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(80f, 80f);
            var bg = go.GetComponent<Image>();
            bg.color = new Color(0.22f, 0.24f, 0.3f, 1f);
            bg.raycastTarget = true;
            var layout = go.GetComponent<LayoutElement>();
            layout.preferredWidth = 80f;
            layout.preferredHeight = 80f;
            layout.minWidth = 80f;
            layout.minHeight = 80f;

            var thumbGo = new GameObject("Thumbnail", typeof(RectTransform), typeof(Image));
            thumbGo.transform.SetParent(go.transform, false);
            var thumbRt = thumbGo.GetComponent<RectTransform>();
            Stretch(thumbRt);
            thumbRt.offsetMin = new Vector2(4f, 16f);
            thumbRt.offsetMax = new Vector2(-4f, -4f);
            var thumb = thumbGo.GetComponent<Image>();
            thumb.preserveAspect = true;
            thumb.raycastTarget = false;

            var label = CreateUiText(go.transform, "Label", "Id", 11, TextAnchor.LowerCenter);
            var labelRt = label.GetComponent<RectTransform>();
            labelRt.anchorMin = new Vector2(0f, 0f);
            labelRt.anchorMax = new Vector2(1f, 0f);
            labelRt.pivot = new Vector2(0.5f, 0f);
            labelRt.anchoredPosition = new Vector2(0f, 2f);
            labelRt.sizeDelta = new Vector2(0f, 14f);
            label.raycastTarget = false;

            var view = go.GetComponent<FormationSoldierSlotView>();
            var so = new SerializedObject(view);
            so.FindProperty("_thumbnail").objectReferenceValue = thumb;
            so.FindProperty("_label").objectReferenceValue = label;
            so.FindProperty("_background").objectReferenceValue = bg;
            so.ApplyModifiedPropertiesWithoutUndo();

            go.SetActive(false);
            return go;
        }

        private static void EnsureFolders()
        {
            EnsureFolder("Assets/Prefabs", "Formation");
            EnsureFolder("Assets/Settings", "Formation");
        }

        private static void EnsureFolder(string parent, string name)
        {
            var path = parent + "/" + name;
            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder(parent, name);
            }
        }

        private static GameObject CreateUiPanel(Transform parent, string name, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = color;
            return go;
        }

        private static Text CreateUiText(Transform parent, string name, string text, int size, TextAnchor anchor)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var t = go.GetComponent<Text>();
            t.text = text;
            t.fontSize = size;
            t.alignment = anchor;
            t.color = Color.white;
            t.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            return t;
        }

        private static GameObject CreateUiButton(Transform parent, string name, string label, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = color;
            var text = CreateUiText(go.transform, "Text", label, 20, TextAnchor.MiddleCenter);
            Stretch(text.GetComponent<RectTransform>());
            return go;
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static void Place(
            RectTransform rt,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 pivot,
            Vector2 anchoredPos,
            Vector2 size)
        {
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;
        }
    }
}
#endif
