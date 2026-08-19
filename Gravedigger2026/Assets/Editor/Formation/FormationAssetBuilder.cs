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
        private const string RegenPrefsKey = "Gravedigger2026.FormationAssets.Regen.v08247";

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

        [MenuItem("Gravedigger2026/Formation/Patch Return Button Bottom-Right (D-064)")]
        public static void PatchReturnButtonBottomRightMenu()
        {
            PatchReturnButtonBottomRight();
        }

        public static void PatchReturnButtonBottomRight()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(EditorRootPath) == null)
            {
                GenerateAll();
                return;
            }

            var mode1 = PrefabUtility.LoadPrefabContents(EditorRootPath);
            EnsureMode1ReturnBottomRight(mode1);
            PrefabUtility.SaveAsPrefabAsset(mode1, EditorRootPath);
            PrefabUtility.UnloadPrefabContents(mode1);

            if (AssetDatabase.LoadAssetAtPath<GameObject>(EditorRootMode2Path) != null)
            {
                var mode2 = PrefabUtility.LoadPrefabContents(EditorRootMode2Path);
                EnsureMode2CompleteButton(mode2);
                EnsureMode2StartBattleAboveComplete(mode2);
                EnsureMode2ReturnAboveComplete(mode2);
                EnsureMode2SoldierHoverTooltip(mode2);
                PrefabUtility.SaveAsPrefabAsset(mode2, EditorRootMode2Path);
                PrefabUtility.UnloadPrefabContents(mode2);
            }
            else
            {
                BuildAndSaveMode2EditorRoot();
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[FormationAssetBuilder] Patched ReturnButton bottom-right (D-064).");
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
            EnsureMode2StartBattleAboveComplete(contents);
            EnsureMode2ReturnAboveComplete(contents);
            EnsureMode2SoldierHoverTooltip(contents);
            EnsureFormationBondHud(FindDeep(contents.transform, "FormationCanvas"), contents.GetComponent<FormationEditorController>());

            PrefabUtility.SaveAsPrefabAsset(contents, EditorRootMode2Path);
            PrefabUtility.UnloadPrefabContents(contents);
            return AssetDatabase.LoadAssetAtPath<GameObject>(EditorRootMode2Path);
        }

        [MenuItem("Gravedigger2026/Formation/Patch Mode2 Soldier Hover Tooltip (D-065)")]
        public static void PatchMode2SoldierHoverTooltipMenu()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(EditorRootMode2Path) == null)
            {
                GenerateAll();
                return;
            }

            var mode2 = PrefabUtility.LoadPrefabContents(EditorRootMode2Path);
            EnsureMode2SoldierHoverTooltip(mode2);
            PrefabUtility.SaveAsPrefabAsset(mode2, EditorRootMode2Path);
            PrefabUtility.UnloadPrefabContents(mode2);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[FormationAssetBuilder] Patched Mode2 SoldierHoverTooltip (D-065).");
        }

        [MenuItem("Gravedigger2026/Formation/Patch Formation Bond HUD")]
        public static void PatchFormationBondHudMenu()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(EditorRootPath) == null)
            {
                GenerateAll();
                return;
            }

            var mode1 = PrefabUtility.LoadPrefabContents(EditorRootPath);
            var canvas1 = FindDeep(mode1.transform, "FormationCanvas");
            EnsureFormationBondHud(canvas1, mode1.GetComponent<FormationEditorController>());
            PrefabUtility.SaveAsPrefabAsset(mode1, EditorRootPath);
            PrefabUtility.UnloadPrefabContents(mode1);

            if (AssetDatabase.LoadAssetAtPath<GameObject>(EditorRootMode2Path) != null)
            {
                var mode2 = PrefabUtility.LoadPrefabContents(EditorRootMode2Path);
                EnsureFormationBondHud(FindDeep(mode2.transform, "FormationCanvas"), mode2.GetComponent<FormationEditorController>());
                PrefabUtility.SaveAsPrefabAsset(mode2, EditorRootMode2Path);
                PrefabUtility.UnloadPrefabContents(mode2);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[FormationAssetBuilder] Patched Formation Bond HUD.");
        }

        /// <summary>Top-left bond button + active icon row + detail modal (SPEC_03 §3.17).</summary>
        private static void EnsureFormationBondHud(Transform canvas, FormationEditorController controller)
        {
            if (canvas == null)
            {
                Debug.LogWarning("[FormationAssetBuilder] FormationCanvas missing; cannot add Bond HUD.");
                return;
            }

            var hudRoot = FindDeep(canvas, "BondHudRoot");
            GameObject hudGo;
            if (hudRoot != null)
            {
                hudGo = hudRoot.gameObject;
            }
            else
            {
                hudGo = new GameObject("BondHudRoot", typeof(RectTransform), typeof(FormationBondHudView));
                hudGo.transform.SetParent(canvas, false);
            }

            Place(
                hudGo.GetComponent<RectTransform>(),
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(24f, -72f),
                new Vector2(160f, 400f));

            var viewBtn = FindDeep(hudGo.transform, "ViewBondsButton");
            GameObject viewBtnGo;
            if (viewBtn != null)
            {
                viewBtnGo = viewBtn.gameObject;
            }
            else
            {
                viewBtnGo = CreateUiButton(hudGo.transform, "ViewBondsButton", "查看阵容羁绊",
                    new Color(0.28f, 0.36f, 0.48f, 1f));
                Place(
                    viewBtnGo.GetComponent<RectTransform>(),
                    new Vector2(0f, 1f),
                    new Vector2(0f, 1f),
                    new Vector2(0f, 1f),
                    new Vector2(0f, 0f),
                    new Vector2(160f, 36f));
            }

            var iconRow = FindDeep(hudGo.transform, "ActiveBondIconsRow");
            GameObject iconRowGo;
            if (iconRow != null)
            {
                iconRowGo = iconRow.gameObject;
            }
            else
            {
                iconRowGo = new GameObject(
                    "ActiveBondIconsRow",
                    typeof(RectTransform),
                    typeof(VerticalLayoutGroup),
                    typeof(ContentSizeFitter));
                iconRowGo.transform.SetParent(hudGo.transform, false);
            }

            var legacyHlg = iconRowGo.GetComponent<HorizontalLayoutGroup>();
            if (legacyHlg != null)
            {
                Object.DestroyImmediate(legacyHlg);
            }

            Place(
                iconRowGo.GetComponent<RectTransform>(),
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(0f, -42f),
                new Vector2(40f, 0f));

            var vlg = iconRowGo.GetComponent<VerticalLayoutGroup>();
            if (vlg == null)
            {
                vlg = iconRowGo.AddComponent<VerticalLayoutGroup>();
            }

            vlg.spacing = 6f;
            vlg.childAlignment = TextAnchor.UpperLeft;
            vlg.childControlWidth = false;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = false;
            vlg.childForceExpandHeight = false;
            var fitter = iconRowGo.GetComponent<ContentSizeFitter>();
            if (fitter == null)
            {
                fitter = iconRowGo.AddComponent<ContentSizeFitter>();
            }

            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var detail = FindDeep(canvas, "BondDetailModal");
            GameObject detailGo;
            FormationBondDetailView detailView;
            if (detail != null)
            {
                detailGo = detail.gameObject;
                detailView = detailGo.GetComponent<FormationBondDetailView>();
                if (detailView == null)
                {
                    detailView = detailGo.AddComponent<FormationBondDetailView>();
                }
            }
            else
            {
                detailGo = CreateUiPanel(canvas, "BondDetailModal", new Color(0.08f, 0.1f, 0.14f, 0.96f));
                Place(
                    detailGo.GetComponent<RectTransform>(),
                    new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f),
                    Vector2.zero,
                    new Vector2(720f, 520f));
                detailView = detailGo.AddComponent<FormationBondDetailView>();

                var title = CreateUiText(detailGo.transform, "Title", "阵容羁绊", 24, TextAnchor.UpperCenter);
                Place(title.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
                    new Vector2(0f, -12f), new Vector2(0f, 40f));

                var body = CreateUiText(detailGo.transform, "Body", string.Empty, 16, TextAnchor.UpperLeft);
                Place(body.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f),
                    new Vector2(16f, 56f), new Vector2(-32f, -72f));
                body.horizontalOverflow = HorizontalWrapMode.Wrap;
                body.verticalOverflow = VerticalWrapMode.Overflow;

                var closeBtn = CreateUiButton(detailGo.transform, "CloseButton", "关闭",
                    new Color(0.35f, 0.4f, 0.5f, 1f));
                Place(closeBtn.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                    new Vector2(0.5f, 0f), new Vector2(0f, 28f), new Vector2(140f, 40f));

                var dso = new SerializedObject(detailView);
                dso.FindProperty("_root").objectReferenceValue = detailGo;
                dso.FindProperty("_titleText").objectReferenceValue = title;
                dso.FindProperty("_bodyText").objectReferenceValue = body;
                dso.FindProperty("_closeButton").objectReferenceValue = closeBtn.GetComponent<Button>();
                dso.ApplyModifiedPropertiesWithoutUndo();
                detailGo.SetActive(false);
            }

            var hudView = hudGo.GetComponent<FormationBondHudView>();
            if (hudView == null)
            {
                hudView = hudGo.AddComponent<FormationBondHudView>();
            }

            var hso = new SerializedObject(hudView);
            hso.FindProperty("_viewButton").objectReferenceValue = viewBtnGo.GetComponent<Button>();
            hso.FindProperty("_viewButtonLabel").objectReferenceValue = viewBtnGo.GetComponentInChildren<Text>();
            hso.FindProperty("_iconRow").objectReferenceValue = iconRowGo.GetComponent<RectTransform>();
            hso.FindProperty("_detailView").objectReferenceValue = detailView;
            hso.ApplyModifiedPropertiesWithoutUndo();

            if (controller != null)
            {
                var cso = new SerializedObject(controller);
                cso.FindProperty("_bondHud").objectReferenceValue = hudView;
                cso.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        /// <summary>
        /// Mode2 only: hover tooltip on FormationCanvas (SPEC_03 UI-021 / D-065).
        /// </summary>
        private static void EnsureMode2SoldierHoverTooltip(GameObject editorRoot)
        {
            var canvas = FindDeep(editorRoot.transform, "FormationCanvas");
            if (canvas == null)
            {
                Debug.LogWarning("[FormationAssetBuilder] FormationCanvas missing; cannot add SoldierHoverTooltip.");
                return;
            }

            var existing = FindDeep(editorRoot.transform, "SoldierHoverTooltip");
            GameObject tipGo;
            if (existing != null)
            {
                tipGo = existing.gameObject;
            }
            else
            {
                tipGo = new GameObject(
                    "SoldierHoverTooltip",
                    typeof(RectTransform),
                    typeof(Image),
                    typeof(CanvasGroup),
                    typeof(FormationSoldierHoverTooltipView));
                tipGo.transform.SetParent(canvas, false);
            }

            var rt = tipGo.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.sizeDelta = new Vector2(300f, 236f);
            rt.anchoredPosition = Vector2.zero;

            var image = tipGo.GetComponent<Image>();
            if (image == null)
            {
                image = tipGo.AddComponent<Image>();
            }

            image.color = Color.white;
            image.raycastTarget = false;

            var group = tipGo.GetComponent<CanvasGroup>();
            if (group == null)
            {
                group = tipGo.AddComponent<CanvasGroup>();
            }

            group.blocksRaycasts = false;
            group.interactable = false;

            if (tipGo.GetComponent<FormationSoldierHoverTooltipView>() == null)
            {
                tipGo.AddComponent<FormationSoldierHoverTooltipView>();
            }

            tipGo.SetActive(false);

            var controller = editorRoot.GetComponent<FormationEditorController>();
            if (controller == null)
            {
                Debug.LogWarning("[FormationAssetBuilder] FormationEditorController missing on Mode2 root.");
                return;
            }

            var cso = new SerializedObject(controller);
            var prop = cso.FindProperty("_hoverTooltip");
            if (prop != null)
            {
                prop.objectReferenceValue = tipGo.GetComponent<FormationSoldierHoverTooltipView>();
                cso.ApplyModifiedPropertiesWithoutUndo();
            }
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

        /// <summary>
        /// Mode2 only: StartBattle stacked directly above Complete on bottom-right (SPEC_03 §3.11).
        /// Complete at y=124 h=48; 8px gap → StartBattle y=180.
        /// </summary>
        private static void EnsureMode2StartBattleAboveComplete(GameObject editorRoot)
        {
            var start = FindDeep(editorRoot.transform, "StartBattleButton");
            if (start == null)
            {
                Debug.LogWarning("[FormationAssetBuilder] StartBattleButton missing on Mode2 EditorRoot.");
                return;
            }

            Place(
                start.GetComponent<RectTransform>(),
                new Vector2(1f, 0f),
                new Vector2(1f, 0f),
                new Vector2(1f, 0f),
                new Vector2(-24f, 180f),
                new Vector2(140f, 48f));
        }

        /// <summary>
        /// Mode2: UM Return shares StartBattle slot above Complete (mutually exclusive by mode; D-064).
        /// </summary>
        private static void EnsureMode2ReturnAboveComplete(GameObject editorRoot)
        {
            var ret = FindDeep(editorRoot.transform, "ReturnButton");
            if (ret == null)
            {
                Debug.LogWarning("[FormationAssetBuilder] ReturnButton missing on Mode2 EditorRoot.");
                return;
            }

            Place(
                ret.GetComponent<RectTransform>(),
                new Vector2(1f, 0f),
                new Vector2(1f, 0f),
                new Vector2(1f, 0f),
                new Vector2(-24f, 180f),
                new Vector2(140f, 48f));
        }

        /// <summary>Patch Mode1 Return/StartBattle to bottom-right above SoldierBar (D-064).</summary>
        private static void EnsureMode1ReturnBottomRight(GameObject editorRoot)
        {
            PlaceBottomRightAboveBar(FindDeep(editorRoot.transform, "ReturnButton"), 124f);
            PlaceBottomRightAboveBar(FindDeep(editorRoot.transform, "StartBattleButton"), 124f);
        }

        private static void PlaceBottomRightAboveBar(Transform t, float y)
        {
            if (t == null)
            {
                return;
            }

            Place(
                t.GetComponent<RectTransform>(),
                new Vector2(1f, 0f),
                new Vector2(1f, 0f),
                new Vector2(1f, 0f),
                new Vector2(-24f, y),
                new Vector2(140f, 48f));
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

            EnsureFormationBondHud(canvasGo.transform, controller);

            var returnBtn = CreateUiButton(canvasGo.transform, "ReturnButton", "返回", new Color(0.35f, 0.4f, 0.5f, 1f));
            // Bottom-right above SoldierBar (height 112). Mode2 bumps Return/StartBattle to y=180 above Complete.
            Place(returnBtn.GetComponent<RectTransform>(), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f),
                new Vector2(-24f, 124f), new Vector2(140f, 48f));

            var startBtn = CreateUiButton(canvasGo.transform, "StartBattleButton", "开战", new Color(0.55f, 0.32f, 0.28f, 1f));
            Place(startBtn.GetComponent<RectTransform>(), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f),
                new Vector2(-24f, 124f), new Vector2(140f, 48f));

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
            thumbRt.offsetMin = new Vector2(4f, 24f);
            thumbRt.offsetMax = new Vector2(-4f, -4f);
            var thumb = thumbGo.GetComponent<Image>();
            thumb.preserveAspect = true;
            thumb.raycastTarget = false;

            var label = CreateUiText(go.transform, "Label", "Class", 10, TextAnchor.MiddleCenter);
            var labelRt = label.GetComponent<RectTransform>();
            labelRt.anchorMin = new Vector2(0f, 0f);
            labelRt.anchorMax = new Vector2(1f, 0f);
            labelRt.pivot = new Vector2(0.5f, 0f);
            labelRt.anchoredPosition = new Vector2(0f, 12f);
            labelRt.sizeDelta = new Vector2(0f, 12f);
            label.raycastTarget = false;

            var classLevel = CreateUiText(go.transform, "ClassLevel", "Lv.0", 9, TextAnchor.MiddleCenter);
            var levelRt = classLevel.GetComponent<RectTransform>();
            levelRt.anchorMin = new Vector2(0f, 0f);
            levelRt.anchorMax = new Vector2(1f, 0f);
            levelRt.pivot = new Vector2(0.5f, 0f);
            levelRt.anchoredPosition = new Vector2(0f, 1f);
            levelRt.sizeDelta = new Vector2(0f, 11f);
            classLevel.raycastTarget = false;

            var view = go.GetComponent<FormationSoldierSlotView>();
            var so = new SerializedObject(view);
            so.FindProperty("_thumbnail").objectReferenceValue = thumb;
            so.FindProperty("_label").objectReferenceValue = label;
            so.FindProperty("_classLevelLabel").objectReferenceValue = classLevel;
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
