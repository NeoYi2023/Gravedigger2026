#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using Gravedigger2026.Core.Config;
using Gravedigger2026.Gameplay.UpgradeManufacture;
using Gravedigger2026.Meta;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Gravedigger2026.Editor.UpgradeManufacture
{
    /// <summary>
    /// Builds UM StageRoot Prefab / Catalog / temp warrior appearance Prefabs and wires MetaShellRoot
    /// (Approach A / D-030 + D-031 + D-032).
    /// </summary>
    public static class UmAssetBuilder
    {
        private const string PrefabUmDir = "Assets/Prefabs/UpgradeManufacture";
        private const string PrefabWarriorsDir = "Assets/Prefabs/Defend/Warriors";
        private const string SettingsUmDir = "Assets/Settings/UpgradeManufacture";
        private const string CatalogPath = SettingsUmDir + "/UpgradeManufacturePrefabCatalog.asset";
        private const string StageRootPath = PrefabUmDir + "/UpgradeManufactureStageRoot.prefab";
        private const string MetaRootPath = "Assets/Prefabs/Meta/MetaShellRoot.prefab";
        private const string AppearanceCsv = "Manufacture_BodyAppearanceConfig.csv";
        private const string RegenPrefsKey = "Gravedigger2026.UmAssets.Regen.v0480";

        [InitializeOnLoadMethod]
        private static void AutoGenerateIfMissing()
        {
            EditorApplication.delayCall += () =>
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode)
                {
                    return;
                }

                var missing = AssetDatabase.LoadAssetAtPath<UpgradeManufacturePrefabCatalog>(CatalogPath) == null
                              || AssetDatabase.LoadAssetAtPath<GameObject>(StageRootPath) == null
                              || !AssetDatabase.IsValidFolder(PrefabWarriorsDir);
                var needsRegen = !EditorPrefs.GetBool(RegenPrefsKey, false);
                if (missing || needsRegen)
                {
                    GenerateAll();
                    EditorPrefs.SetBool(RegenPrefsKey, true);
                }
            };
        }

        [MenuItem("Gravedigger2026/UpgradeManufacture/Generate UM Prefabs + Catalog")]
        public static void GenerateAll()
        {
            EnsureFolders();

            var appearanceEntries = BuildWarriorAppearancePrefabs();

            var stageGo = BuildStageRoot();
            PrefabUtility.SaveAsPrefabAsset(stageGo, StageRootPath);
            Object.DestroyImmediate(stageGo);
            var stagePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(StageRootPath);

            var catalog = AssetDatabase.LoadAssetAtPath<UpgradeManufacturePrefabCatalog>(CatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<UpgradeManufacturePrefabCatalog>();
                AssetDatabase.CreateAsset(catalog, CatalogPath);
            }

            catalog.EditorSet(stagePrefab);
            catalog.EditorSetWarriorAppearances(appearanceEntries);
            EditorUtility.SetDirty(catalog);

            var stageContents = PrefabUtility.LoadPrefabContents(StageRootPath);
            var controller = stageContents.GetComponent<UpgradeManufactureStageController>();
            if (controller != null)
            {
                var so = new SerializedObject(controller);
                so.FindProperty("_catalog").objectReferenceValue = catalog;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            PrefabUtility.SaveAsPrefabAsset(stageContents, StageRootPath);
            PrefabUtility.UnloadPrefabContents(stageContents);

            WireMetaShell(catalog);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log(
                $"[UmAssetBuilder] Generated UM Prefabs, Catalog ({appearanceEntries.Length} warrior appearances), and wired MetaShellRoot.");
        }

        /// <summary>
        /// Temp capsule Prefabs at Assets/Prefabs/Defend/Warriors/{AppearanceId}.prefab (SPEC_04 §13 / §15).
        /// </summary>
        private static UpgradeManufacturePrefabCatalog.WarriorAppearanceEntry[] BuildWarriorAppearancePrefabs()
        {
            var entries = new List<UpgradeManufacturePrefabCatalog.WarriorAppearanceEntry>();
            var csvPath = CsvPathResolver.ResolveExistingFile(AppearanceCsv);
            if (csvPath == null)
            {
                Debug.LogWarning($"[UmAssetBuilder] {AppearanceCsv} not found — skipped warrior appearance Prefabs.");
                return entries.ToArray();
            }

            var rows = SimpleCsv.ReadRows(csvPath);
            for (var i = 0; i < rows.Count; i++)
            {
                if (!rows[i].TryGetValue("AppearanceId", out var appearanceId) || string.IsNullOrEmpty(appearanceId))
                {
                    continue;
                }

                var path = $"{PrefabWarriorsDir}/{appearanceId}.prefab";
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null)
                {
                    var temp = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                    temp.name = appearanceId;
                    temp.transform.localScale = new Vector3(0.7f, 0.9f, 0.7f);
                    PrefabUtility.SaveAsPrefabAsset(temp, path);
                    Object.DestroyImmediate(temp);
                    prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                }

                entries.Add(new UpgradeManufacturePrefabCatalog.WarriorAppearanceEntry
                {
                    AppearanceId = appearanceId,
                    Prefab = prefab
                });
            }

            return entries.ToArray();
        }

        private static void WireMetaShell(UpgradeManufacturePrefabCatalog catalog)
        {
            var meta = AssetDatabase.LoadAssetAtPath<GameObject>(MetaRootPath);
            if (meta == null)
            {
                Debug.LogWarning("[UmAssetBuilder] MetaShellRoot missing — run Meta shell builder first.");
                return;
            }

            var contents = PrefabUtility.LoadPrefabContents(MetaRootPath);
            var controller = contents.GetComponent<MetaShellController>();
            var umParent = contents.transform.Find("UmWorldParent");
            if (umParent == null)
            {
                var umParentGo = new GameObject("UmWorldParent");
                umParentGo.transform.SetParent(contents.transform, false);
                umParent = umParentGo.transform;
            }

            if (controller != null)
            {
                var so = new SerializedObject(controller);
                so.FindProperty("_umPrefabCatalog").objectReferenceValue = catalog;
                so.FindProperty("_umWorldParent").objectReferenceValue = umParent;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            PrefabUtility.SaveAsPrefabAsset(contents, MetaRootPath);
            PrefabUtility.UnloadPrefabContents(contents);
        }

        private static GameObject BuildStageRoot()
        {
            var root = new GameObject("UpgradeManufactureStageRoot");
            var controller = root.AddComponent<UpgradeManufactureStageController>();

            var canvasGo = new GameObject("UmCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(root.transform, false);
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 55;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            var panelRoot = CreateUiPanel(canvasGo.transform, "UmRoot", new Color(0.08f, 0.09f, 0.14f, 0.92f));
            Stretch(panelRoot.GetComponent<RectTransform>());

            var title = CreateUiText(panelRoot.transform, "Title", "升级与制造（UpgradeManufacture）", 30, TextAnchor.UpperCenter);
            Place(title.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -18f), new Vector2(900f, 42f));

            // Two panels: Upgrade / Manufacture (formation via 布阵 button)
            var upgradeZone = CreateUiPanel(panelRoot.transform, "UpgradeZone", new Color(0.18f, 0.22f, 0.32f, 0.95f));
            StretchFill(upgradeZone.GetComponent<RectTransform>(), new Vector2(0f, 0.18f), new Vector2(0.49f, 0.88f), 8f);

            var manufactureZone = CreateUiPanel(panelRoot.transform, "ManufactureZone", new Color(0.2f, 0.2f, 0.26f, 0.95f));
            StretchFill(manufactureZone.GetComponent<RectTransform>(), new Vector2(0.51f, 0.18f), new Vector2(1f, 0.88f), 8f);
            var manufactureView = BuildManufactureZone(manufactureZone.transform);

            var status = CreateUiText(upgradeZone.transform, "Status", "升级区", 20, TextAnchor.UpperLeft);
            StretchFill(status.GetComponent<RectTransform>(), new Vector2(0.04f, 0.38f), new Vector2(0.96f, 0.96f), 0f);

            var inject100 = CreateUiButton(upgradeZone.transform, "Inject100", "+100 经验", new Color(0.32f, 0.48f, 0.72f, 1f));
            Place(inject100.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f), new Vector2(-90f, 70f), new Vector2(160f, 40f));

            var inject500 = CreateUiButton(upgradeZone.transform, "Inject500", "+500 经验", new Color(0.32f, 0.55f, 0.42f, 1f));
            Place(inject500.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f), new Vector2(90f, 70f), new Vector2(160f, 40f));

            var complete = CreateUiButton(panelRoot.transform, "CompleteButton", "完成 / 进入下一阶段",
                new Color(0.45f, 0.35f, 0.22f, 1f));
            Place(complete.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f), new Vector2(-100f, 28f), new Vector2(320f, 52f));

            var formationBtn = CreateUiButton(panelRoot.transform, "FormationButton", "布阵",
                new Color(0.28f, 0.42f, 0.55f, 1f));
            Place(formationBtn.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f), new Vector2(160f, 28f), new Vector2(140f, 52f));

            var upgradeView = panelRoot.AddComponent<UpgradePanelView>();
            var vso = new SerializedObject(upgradeView);
            vso.FindProperty("_root").objectReferenceValue = panelRoot;
            vso.FindProperty("_statusText").objectReferenceValue = status;
            vso.FindProperty("_inject100Button").objectReferenceValue = inject100.GetComponent<Button>();
            vso.FindProperty("_inject500Button").objectReferenceValue = inject500.GetComponent<Button>();
            vso.FindProperty("_completeButton").objectReferenceValue = complete.GetComponent<Button>();
            vso.FindProperty("_formationButton").objectReferenceValue = formationBtn.GetComponent<Button>();
            vso.ApplyModifiedPropertiesWithoutUndo();

            var cso = new SerializedObject(controller);
            cso.FindProperty("_upgradePanel").objectReferenceValue = upgradeView;
            cso.FindProperty("_manufacturePanel").objectReferenceValue = manufactureView;
            cso.FindProperty("_mainUiRoot").objectReferenceValue = panelRoot;
            cso.FindProperty("_formationPanel").objectReferenceValue = null;
            cso.ApplyModifiedPropertiesWithoutUndo();

            panelRoot.SetActive(false);
            return root;
        }

        private static FormationPanelView BuildFormationZoneUnused(Transform zone)
        {
            // Kept for reference; UM formation is FormationEditorRoot (v0.48).
            var header = CreateUiText(zone, "Header", "布阵区（已迁移至独立编辑器）", 15,
                TextAnchor.UpperCenter);
            StretchFill(header.GetComponent<RectTransform>(), new Vector2(0.02f, 0.94f), new Vector2(0.98f, 1f), 0f);

            var poolContent = CreateListColumn(zone, "PoolColumn",
                new Vector2(0.02f, 0.42f), new Vector2(0.49f, 0.93f));
            var poolTemplate = CreateRowTemplate(poolContent, "PoolRowTemplate");

            var formationContent = CreateListColumn(zone, "FormationColumn",
                new Vector2(0.51f, 0.42f), new Vector2(0.98f, 0.93f));
            var formationTemplate = CreateRowTemplate(formationContent, "FormationRowTemplate");

            var statusPanel = CreateUiPanel(zone, "StatusPanel", new Color(0.13f, 0.14f, 0.18f, 0.95f));
            StretchFill(statusPanel.GetComponent<RectTransform>(), new Vector2(0.02f, 0.18f), new Vector2(0.98f, 0.41f), 0f);
            var statusText = CreateUiText(statusPanel.transform, "StatusText", "布阵区", 13, TextAnchor.UpperLeft);
            StretchFill(statusText.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, 4f);

            var undeploy = CreateUiButton(zone, "UndeployButton", "下阵", new Color(0.5f, 0.3f, 0.3f, 1f));
            StretchFill(undeploy.GetComponent<RectTransform>(), new Vector2(0.02f, 0.01f), new Vector2(0.22f, 0.16f), 2f);
            SetButtonFontSize(undeploy, 14);

            var negX = CreateUiButton(zone, "NudgeNegX", "−X", new Color(0.3f, 0.4f, 0.55f, 1f));
            StretchFill(negX.GetComponent<RectTransform>(), new Vector2(0.23f, 0.01f), new Vector2(0.40f, 0.16f), 2f);
            SetButtonFontSize(negX, 14);

            var posX = CreateUiButton(zone, "NudgePosX", "+X", new Color(0.3f, 0.4f, 0.55f, 1f));
            StretchFill(posX.GetComponent<RectTransform>(), new Vector2(0.41f, 0.01f), new Vector2(0.58f, 0.16f), 2f);
            SetButtonFontSize(posX, 14);

            var negZ = CreateUiButton(zone, "NudgeNegZ", "−Z", new Color(0.3f, 0.45f, 0.4f, 1f));
            StretchFill(negZ.GetComponent<RectTransform>(), new Vector2(0.59f, 0.01f), new Vector2(0.76f, 0.16f), 2f);
            SetButtonFontSize(negZ, 14);

            var posZ = CreateUiButton(zone, "NudgePosZ", "+Z", new Color(0.3f, 0.45f, 0.4f, 1f));
            StretchFill(posZ.GetComponent<RectTransform>(), new Vector2(0.77f, 0.01f), new Vector2(0.98f, 0.16f), 2f);
            SetButtonFontSize(posZ, 14);

            var view = zone.gameObject.AddComponent<FormationPanelView>();
            var so = new SerializedObject(view);
            so.FindProperty("_poolContent").objectReferenceValue = poolContent;
            so.FindProperty("_poolRowTemplate").objectReferenceValue = poolTemplate;
            so.FindProperty("_formationContent").objectReferenceValue = formationContent;
            so.FindProperty("_formationRowTemplate").objectReferenceValue = formationTemplate;
            so.FindProperty("_statusText").objectReferenceValue = statusText;
            so.FindProperty("_undeployButton").objectReferenceValue = undeploy.GetComponent<Button>();
            so.FindProperty("_nudgeNegXButton").objectReferenceValue = negX.GetComponent<Button>();
            so.FindProperty("_nudgePosXButton").objectReferenceValue = posX.GetComponent<Button>();
            so.FindProperty("_nudgeNegZButton").objectReferenceValue = negZ.GetComponent<Button>();
            so.FindProperty("_nudgePosZButton").objectReferenceValue = posZ.GetComponent<Button>();
            so.ApplyModifiedPropertiesWithoutUndo();
            return view;
        }

        private static FormationPanelView BuildFormationZone(Transform zone)
        {
            return BuildFormationZoneUnused(zone);
        }

        private static ManufacturePanelView BuildManufactureZone(Transform zone)
        {
            var header = CreateUiText(zone, "Header", "制造区（严格槽位 · 点击库存放入 / 点击槽位取出）", 15,
                TextAnchor.UpperCenter);
            StretchFill(header.GetComponent<RectTransform>(), new Vector2(0.02f, 0.94f), new Vector2(0.98f, 1f), 0f);

            var inventoryContent = CreateListColumn(zone, "InventoryColumn",
                new Vector2(0.02f, 0.56f), new Vector2(0.49f, 0.93f));
            var inventoryTemplate = CreateRowTemplate(inventoryContent, "InventoryRowTemplate");

            var slotContent = CreateListColumn(zone, "SlotColumn",
                new Vector2(0.51f, 0.56f), new Vector2(0.98f, 0.93f));
            var slotTemplate = CreateRowTemplate(slotContent, "SlotRowTemplate");

            var previewPanel = CreateUiPanel(zone, "PreviewPanel", new Color(0.13f, 0.14f, 0.18f, 0.95f));
            StretchFill(previewPanel.GetComponent<RectTransform>(), new Vector2(0.02f, 0.26f), new Vector2(0.98f, 0.55f), 0f);
            var previewText = CreateUiText(previewPanel.transform, "PreviewText", "制造区预览", 13, TextAnchor.UpperLeft);
            StretchFill(previewText.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, 4f);

            var poolPanel = CreateUiPanel(zone, "PoolPanel", new Color(0.13f, 0.16f, 0.14f, 0.95f));
            StretchFill(poolPanel.GetComponent<RectTransform>(), new Vector2(0.02f, 0.1f), new Vector2(0.98f, 0.25f), 0f);
            var poolText = CreateUiText(poolPanel.transform, "PoolText", "士兵池：空", 13, TextAnchor.UpperLeft);
            StretchFill(poolText.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, 4f);

            var grantKit = CreateUiButton(zone, "GrantKitButton", "注入制造套件(Debug)", new Color(0.3f, 0.42f, 0.6f, 1f));
            StretchFill(grantKit.GetComponent<RectTransform>(), new Vector2(0.02f, 0.01f), new Vector2(0.36f, 0.09f), 2f);
            SetButtonFontSize(grantKit, 14);

            var clearSlots = CreateUiButton(zone, "ClearSlotsButton", "清空槽位", new Color(0.45f, 0.3f, 0.3f, 1f));
            StretchFill(clearSlots.GetComponent<RectTransform>(), new Vector2(0.37f, 0.01f), new Vector2(0.63f, 0.09f), 2f);
            SetButtonFontSize(clearSlots, 14);

            var manufacture = CreateUiButton(zone, "ManufactureButton", "制造", new Color(0.3f, 0.55f, 0.38f, 1f));
            StretchFill(manufacture.GetComponent<RectTransform>(), new Vector2(0.64f, 0.01f), new Vector2(0.98f, 0.09f), 2f);
            SetButtonFontSize(manufacture, 16);

            var view = zone.gameObject.AddComponent<ManufacturePanelView>();
            var so = new SerializedObject(view);
            so.FindProperty("_inventoryContent").objectReferenceValue = inventoryContent;
            so.FindProperty("_inventoryRowTemplate").objectReferenceValue = inventoryTemplate;
            so.FindProperty("_slotContent").objectReferenceValue = slotContent;
            so.FindProperty("_slotRowTemplate").objectReferenceValue = slotTemplate;
            so.FindProperty("_previewText").objectReferenceValue = previewText;
            so.FindProperty("_poolText").objectReferenceValue = poolText;
            so.FindProperty("_grantKitButton").objectReferenceValue = grantKit.GetComponent<Button>();
            so.FindProperty("_clearSlotsButton").objectReferenceValue = clearSlots.GetComponent<Button>();
            so.FindProperty("_manufactureButton").objectReferenceValue = manufacture.GetComponent<Button>();
            so.ApplyModifiedPropertiesWithoutUndo();
            return view;
        }

        private static RectTransform CreateListColumn(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax)
        {
            var panel = CreateUiPanel(parent, name, new Color(0.12f, 0.13f, 0.17f, 0.95f));
            StretchFill(panel.GetComponent<RectTransform>(), anchorMin, anchorMax, 0f);

            var scrollGo = new GameObject("Scroll", typeof(RectTransform), typeof(ScrollRect), typeof(Image));
            scrollGo.transform.SetParent(panel.transform, false);
            var scrollRt = scrollGo.GetComponent<RectTransform>();
            StretchFill(scrollRt, Vector2.zero, Vector2.one, 2f);
            var scrollImg = scrollGo.GetComponent<Image>();
            scrollImg.color = new Color(1f, 1f, 1f, 0.02f);
            scrollImg.raycastTarget = true;
            var scroll = scrollGo.GetComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 24f;

            var viewportGo = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(RectMask2D));
            viewportGo.transform.SetParent(scrollGo.transform, false);
            var viewportRt = viewportGo.GetComponent<RectTransform>();
            StretchFill(viewportRt, Vector2.zero, Vector2.one, 0f);
            var viewportImg = viewportGo.GetComponent<Image>();
            viewportImg.color = new Color(1f, 1f, 1f, 0.02f);
            viewportImg.raycastTarget = true;

            var contentGo = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup),
                typeof(ContentSizeFitter));
            contentGo.transform.SetParent(viewportGo.transform, false);
            var content = contentGo.GetComponent<RectTransform>();
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = Vector2.zero;

            var layout = contentGo.GetComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.spacing = 1f;
            layout.padding = new RectOffset(2, 2, 2, 2);
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;

            var fitter = contentGo.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll.content = content;
            scroll.viewport = viewportRt;
            return content;
        }

        private static Button CreateRowTemplate(Transform content, string name)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            go.transform.SetParent(content, false);
            go.GetComponent<Image>().color = new Color(0.24f, 0.27f, 0.34f, 0.95f);
            var layoutElement = go.GetComponent<LayoutElement>();
            layoutElement.preferredHeight = 18f;
            layoutElement.minHeight = 18f;

            var text = CreateUiText(go.transform, "Label", "row", 11, TextAnchor.MiddleLeft);
            var rect = text.GetComponent<RectTransform>();
            Stretch(rect);
            rect.offsetMin = new Vector2(4f, 0f);
            rect.offsetMax = new Vector2(-4f, 0f);
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;

            go.SetActive(false);
            return go.GetComponent<Button>();
        }

        private static void SetButtonFontSize(GameObject button, int fontSize)
        {
            var text = button.GetComponentInChildren<Text>(true);
            if (text != null)
            {
                text.fontSize = fontSize;
            }
        }

        private static void EnsureFolders()
        {
            EnsureFolder("Assets/Prefabs");
            EnsureFolder(PrefabUmDir);
            EnsureFolder("Assets/Prefabs/Defend");
            EnsureFolder(PrefabWarriorsDir);
            EnsureFolder("Assets/Settings");
            EnsureFolder(SettingsUmDir);
            EnsureFolder("Assets/Editor");
            EnsureFolder("Assets/Editor/UpgradeManufacture");
            EnsureFolder("Assets/Scripts/Gameplay");
            EnsureFolder("Assets/Scripts/Gameplay/UpgradeManufacture");
            EnsureFolder("Assets/Scripts/Core/UpgradeManufacture");
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

            AssetDatabase.CreateFolder(parent, name);
        }

        private static GameObject CreateUiPanel(Transform parent, string name, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = color;
            return go;
        }

        private static Text CreateUiText(Transform parent, string name, string content, int fontSize, TextAnchor anchor)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var text = go.GetComponent<Text>();
            text.text = content;
            text.fontSize = fontSize;
            text.alignment = anchor;
            text.color = Color.white;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            return text;
        }

        private static GameObject CreateUiButton(Transform parent, string name, string label, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = color;
            var text = CreateUiText(go.transform, "Label", label, 20, TextAnchor.MiddleCenter);
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

        private static void StretchFill(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax, float padding)
        {
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = new Vector2(padding, padding);
            rt.offsetMax = new Vector2(-padding, -padding);
        }

        private static void Place(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
            Vector2 anchoredPos, Vector2 size)
        {
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.anchoredPosition = anchoredPos;
            if (size.sqrMagnitude > 0.01f)
            {
                rt.sizeDelta = size;
            }
        }
    }
}
#endif
