#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using Gravedigger2026.Core.Config;
using Gravedigger2026.Gameplay.UpgradeManufacture;
using Gravedigger2026.Meta;
using Gravedigger2026.UI;
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
        private const string StageRootMode2Path = PrefabUmDir + "/UpgradeManufactureStageRoot_Mode2.prefab";
        private const string MetaRootPath = "Assets/Prefabs/Meta/MetaShellRoot.prefab";
        private const string AppearanceCsv = "Manufacture_BodyAppearanceConfig.csv";
        private const string RegenPrefsKey = "Gravedigger2026.UmAssets.Regen.v0780";

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
                              || AssetDatabase.LoadAssetAtPath<GameObject>(StageRootMode2Path) == null
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
            var stageMode2Prefab = BuildAndSaveMode2StageRoot(stagePrefab);

            var catalog = AssetDatabase.LoadAssetAtPath<UpgradeManufacturePrefabCatalog>(CatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<UpgradeManufacturePrefabCatalog>();
                AssetDatabase.CreateAsset(catalog, CatalogPath);
            }

            catalog.EditorSet(stagePrefab);
            catalog.EditorSetMode2(stageMode2Prefab);
            catalog.EditorSetWarriorAppearances(appearanceEntries);
            EditorUtility.SetDirty(catalog);

            WireCatalogOnStageRoot(StageRootPath, catalog);
            WireCatalogOnStageRoot(StageRootMode2Path, catalog);

            WireMetaShell(catalog);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log(
                $"[UmAssetBuilder] Generated UM Prefabs (Mode1+Mode2), Catalog ({appearanceEntries.Length} warrior appearances), and wired MetaShellRoot.");
        }

        private static GameObject BuildAndSaveMode2StageRoot(GameObject mode1Prefab)
        {
            if (mode1Prefab == null)
            {
                return null;
            }

            var contents = PrefabUtility.LoadPrefabContents(StageRootPath);
            contents.name = "UpgradeManufactureStageRoot_Mode2";
            var zone = FindDeep(contents.transform, "ManufactureZone");
            if (zone != null)
            {
                zone.gameObject.SetActive(false);
            }
            else
            {
                Debug.LogWarning("[UmAssetBuilder] ManufactureZone not found when building Mode2 StageRoot.");
            }

            var umRoot = FindDeep(contents.transform, "UmRoot");
            var upgradeView = umRoot != null
                ? umRoot.GetComponent<UpgradePanelView>()
                : contents.GetComponentInChildren<UpgradePanelView>(true);
            if (upgradeView != null && umRoot != null)
            {
                var recordView = ManufactureRecordModalView.Build(umRoot, out var recordButton);
                var vso = new SerializedObject(upgradeView);
                vso.FindProperty("_manufactureRecordButton").objectReferenceValue = recordButton;
                vso.FindProperty("_manufactureRecordModal").objectReferenceValue = recordView;
                vso.ApplyModifiedPropertiesWithoutUndo();
            }
            else
            {
                Debug.LogWarning("[UmAssetBuilder] UmRoot/UpgradePanelView missing when adding Mode2 ManufactureRecord.");
            }

            PrefabUtility.SaveAsPrefabAsset(contents, StageRootMode2Path);
            PrefabUtility.UnloadPrefabContents(contents);
            return AssetDatabase.LoadAssetAtPath<GameObject>(StageRootMode2Path);
        }

        private static void WireCatalogOnStageRoot(string prefabPath, UpgradeManufacturePrefabCatalog catalog)
        {
            var stageContents = PrefabUtility.LoadPrefabContents(prefabPath);
            var controller = stageContents.GetComponent<UpgradeManufactureStageController>();
            if (controller != null)
            {
                var so = new SerializedObject(controller);
                so.FindProperty("_catalog").objectReferenceValue = catalog;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            PrefabUtility.SaveAsPrefabAsset(stageContents, prefabPath);
            PrefabUtility.UnloadPrefabContents(stageContents);
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

            // Off-screen warrior preview world (RenderTexture → RawImage)
            var previewWorld = new GameObject("WarriorPreviewWorld");
            previewWorld.transform.SetParent(root.transform, false);
            previewWorld.transform.position = new Vector3(500f, 0f, 0f);
            var previewAnchor = new GameObject("PreviewAnchor").transform;
            previewAnchor.SetParent(previewWorld.transform, false);
            previewAnchor.localPosition = Vector3.zero;
            var previewCamGo = new GameObject("PreviewCamera", typeof(Camera));
            previewCamGo.transform.SetParent(previewWorld.transform, false);
            previewCamGo.transform.localPosition = new Vector3(0f, 2.2f, -4f);
            previewCamGo.transform.LookAt(previewAnchor);
            var previewCam = previewCamGo.GetComponent<Camera>();
            previewCam.clearFlags = CameraClearFlags.SolidColor;
            previewCam.backgroundColor = new Color(0.12f, 0.14f, 0.18f, 1f);
            previewCam.orthographic = true;
            previewCam.orthographicSize = 1.6f;
            previewCam.nearClipPlane = 0.1f;
            previewCam.farClipPlane = 20f;
            previewCam.depth = -20;

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

            var title = CreateUiText(panelRoot.transform, "Title", "升级与制造（UpgradeManufacture）", 28, TextAnchor.UpperCenter);
            Place(title.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -16f), new Vector2(900f, 40f));

            var gmUpgrade = CreateUiButton(panelRoot.transform, "GmUpgradeButton", "GM升级", new Color(0.35f, 0.45f, 0.7f, 1f));
            Place(gmUpgrade.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(16f, -12f), new Vector2(140f, 44f));
            SetButtonFontSize(gmUpgrade, 18);

            // Full-screen manufacture
            var manufactureZone = CreateUiPanel(panelRoot.transform, "ManufactureZone", new Color(0.16f, 0.17f, 0.22f, 0.95f));
            StretchFill(manufactureZone.GetComponent<RectTransform>(), new Vector2(0f, 0.12f), new Vector2(1f, 0.92f), 8f);
            var manufactureView = BuildManufactureZone(manufactureZone.transform, previewAnchor, previewCam);

            var umTips = BuildUmTips(canvasGo.transform);

            // Upgrade Modal (ConfirmDialog-style)
            var upgradeModal = CreateUiPanel(panelRoot.transform, "UpgradeModal", new Color(0f, 0f, 0f, 0.55f));
            Stretch(upgradeModal.GetComponent<RectTransform>());
            var upgradeBox = CreateUiPanel(upgradeModal.transform, "UpgradeZone", new Color(0.18f, 0.22f, 0.32f, 0.98f));
            Place(upgradeBox.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(640f, 420f));

            var closeX = CreateUiButton(upgradeBox.transform, "CloseButton", "X", new Color(0.55f, 0.28f, 0.28f, 1f));
            Place(closeX.GetComponent<RectTransform>(), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(-12f, -12f), new Vector2(44f, 44f));
            SetButtonFontSize(closeX, 22);

            var status = CreateUiText(upgradeBox.transform, "Status", "升级区", 20, TextAnchor.UpperLeft);
            StretchFill(status.GetComponent<RectTransform>(), new Vector2(0.06f, 0.28f), new Vector2(0.94f, 0.88f), 0f);

            var inject100 = CreateUiButton(upgradeBox.transform, "Inject100", "+100 经验", new Color(0.32f, 0.48f, 0.72f, 1f));
            Place(inject100.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f), new Vector2(-90f, 48f), new Vector2(160f, 40f));

            var inject500 = CreateUiButton(upgradeBox.transform, "Inject500", "+500 经验", new Color(0.32f, 0.55f, 0.42f, 1f));
            Place(inject500.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f), new Vector2(90f, 48f), new Vector2(160f, 40f));

            upgradeModal.SetActive(false);

            var complete = CreateUiButton(panelRoot.transform, "CompleteButton", "完成 / 进入下一阶段",
                new Color(0.45f, 0.35f, 0.22f, 1f));
            Place(complete.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f), new Vector2(-100f, 28f), new Vector2(320f, 48f));

            var formationBtn = CreateUiButton(panelRoot.transform, "FormationButton", "布阵",
                new Color(0.28f, 0.42f, 0.55f, 1f));
            Place(formationBtn.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f), new Vector2(160f, 28f), new Vector2(140f, 48f));

            var upgradeView = panelRoot.AddComponent<UpgradePanelView>();
            var vso = new SerializedObject(upgradeView);
            vso.FindProperty("_root").objectReferenceValue = panelRoot;
            vso.FindProperty("_upgradeModal").objectReferenceValue = upgradeModal;
            vso.FindProperty("_statusText").objectReferenceValue = status;
            vso.FindProperty("_gmUpgradeButton").objectReferenceValue = gmUpgrade.GetComponent<Button>();
            vso.FindProperty("_closeModalButton").objectReferenceValue = closeX.GetComponent<Button>();
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
            cso.FindProperty("_tipsView").objectReferenceValue = umTips;
            cso.ApplyModifiedPropertiesWithoutUndo();

            panelRoot.SetActive(false);
            return root;
        }

        private static ManufacturePanelView BuildManufactureZone(Transform zone, Transform previewAnchor, Camera previewCam)
        {
            // Left PreviewPanel
            var previewPanel = CreateUiPanel(zone, "PreviewPanel", new Color(0.13f, 0.14f, 0.18f, 0.95f));
            StretchFill(previewPanel.GetComponent<RectTransform>(), new Vector2(0.01f, 0.22f), new Vector2(0.22f, 0.98f), 2f);
            var previewText = CreateUiText(previewPanel.transform, "PreviewText", "制造区预览", 13, TextAnchor.UpperLeft);
            StretchFill(previewText.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, 6f);

            // Right PoolPanel — scrollable soldier frames
            var poolPanel = CreateUiPanel(zone, "PoolPanel", new Color(0.13f, 0.16f, 0.14f, 0.95f));
            StretchFill(poolPanel.GetComponent<RectTransform>(), new Vector2(0.78f, 0.22f), new Vector2(0.99f, 0.98f), 2f);
            var poolHeader = CreateUiText(poolPanel.transform, "PoolHeader", "士兵池", 14, TextAnchor.MiddleCenter);
            StretchFill(poolHeader.GetComponent<RectTransform>(), new Vector2(0f, 0.92f), new Vector2(1f, 1f), 2f);
            var poolScrollRoot = CreateUiPanel(poolPanel.transform, "PoolScrollRoot", new Color(0f, 0f, 0f, 0.05f));
            StretchFill(poolScrollRoot.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(1f, 0.92f), 2f);
            var poolContent = CreateVerticalScrollBar(poolScrollRoot.transform, out var poolScroll);
            var poolFrameTemplate = CreatePoolSoldierFrameTemplate(poolContent);

            // Center SlotColumn + soldier preview
            var slotColumn = CreateUiPanel(zone, "SlotColumn", new Color(0.14f, 0.15f, 0.2f, 0.9f));
            StretchFill(slotColumn.GetComponent<RectTransform>(), new Vector2(0.23f, 0.22f), new Vector2(0.77f, 0.98f), 2f);

            var soldierPreview = CreateUiPanel(slotColumn.transform, "SoldierPreview", new Color(0.1f, 0.11f, 0.14f, 0.95f));
            StretchFill(soldierPreview.GetComponent<RectTransform>(), new Vector2(0.32f, 0.42f), new Vector2(0.68f, 0.92f), 4f);

            var placeholder = new GameObject("PlaceholderImage", typeof(RectTransform), typeof(Image));
            placeholder.transform.SetParent(soldierPreview.transform, false);
            Stretch(placeholder.GetComponent<RectTransform>());
            var placeholderImg = placeholder.GetComponent<Image>();
            placeholderImg.color = new Color(0.25f, 0.28f, 0.34f, 1f);

            var rawGo = new GameObject("PreviewRawImage", typeof(RectTransform), typeof(RawImage));
            rawGo.transform.SetParent(soldierPreview.transform, false);
            Stretch(rawGo.GetComponent<RectTransform>());
            var rawImage = rawGo.GetComponent<RawImage>();
            rawImage.color = Color.white;
            rawImage.enabled = false;

            var slotCells = new Button[15];
            // Left: Head(0), Arm1(2), Leg1(4), Wing(14)
            slotCells[0] = CreateSquareSlot(slotColumn.transform, "Slot_Head", new Vector2(0.04f, 0.78f), new Vector2(0.28f, 0.96f), false);
            slotCells[2] = CreateSquareSlot(slotColumn.transform, "Slot_Arm1", new Vector2(0.04f, 0.58f), new Vector2(0.28f, 0.76f), false);
            slotCells[4] = CreateSquareSlot(slotColumn.transform, "Slot_Leg1", new Vector2(0.04f, 0.38f), new Vector2(0.28f, 0.56f), false);
            slotCells[14] = CreateSquareSlot(slotColumn.transform, "Slot_Wing", new Vector2(0.04f, 0.18f), new Vector2(0.28f, 0.36f), false);
            // Right: Torso(1), Arm2(3), Leg2(5), Mount(13)
            slotCells[1] = CreateSquareSlot(slotColumn.transform, "Slot_Torso", new Vector2(0.72f, 0.78f), new Vector2(0.96f, 0.96f), false);
            slotCells[3] = CreateSquareSlot(slotColumn.transform, "Slot_Arm2", new Vector2(0.72f, 0.58f), new Vector2(0.96f, 0.76f), false);
            slotCells[5] = CreateSquareSlot(slotColumn.transform, "Slot_Leg2", new Vector2(0.72f, 0.38f), new Vector2(0.96f, 0.56f), false);
            slotCells[13] = CreateSquareSlot(slotColumn.transform, "Slot_Mount", new Vector2(0.72f, 0.18f), new Vector2(0.96f, 0.36f), false);
            // Soul inside preview bottom
            slotCells[6] = CreateSquareSlot(soldierPreview.transform, "Slot_Soul", new Vector2(0.2f, 0.02f), new Vector2(0.8f, 0.22f), false);
            // Gems half-size below preview
            for (var g = 0; g < 6; g++)
            {
                var x0 = 0.2f + g * 0.1f;
                slotCells[7 + g] = CreateSquareSlot(slotColumn.transform, $"Slot_Gem{g}",
                    new Vector2(x0, 0.02f), new Vector2(x0 + 0.09f, 0.16f), true);
            }

            // Bottom inventory bar
            var inventoryColumn = CreateUiPanel(zone, "InventoryColumn", new Color(0.12f, 0.13f, 0.17f, 0.95f));
            StretchFill(inventoryColumn.GetComponent<RectTransform>(), new Vector2(0.01f, 0.09f), new Vector2(0.99f, 0.21f), 2f);
            var invScrollContent = CreateHorizontalScrollBar(inventoryColumn.transform, out var invScroll);
            var inventoryTemplate = CreateSquareTemplate(invScrollContent, "InventoryRowTemplate", ManufacturePanelView.InventorySlotSize);

            // Action buttons under inventory
            var grantKit = CreateUiButton(zone, "GrantKitButton", "注入制造套件(Debug)", new Color(0.3f, 0.42f, 0.6f, 1f));
            StretchFill(grantKit.GetComponent<RectTransform>(), new Vector2(0.02f, 0.01f), new Vector2(0.36f, 0.08f), 2f);
            SetButtonFontSize(grantKit, 14);

            var clearSlots = CreateUiButton(zone, "ClearSlotsButton", "清空槽位", new Color(0.45f, 0.3f, 0.3f, 1f));
            StretchFill(clearSlots.GetComponent<RectTransform>(), new Vector2(0.37f, 0.01f), new Vector2(0.63f, 0.08f), 2f);
            SetButtonFontSize(clearSlots, 14);

            var manufacture = CreateUiButton(zone, "ManufactureButton", "制造", new Color(0.3f, 0.55f, 0.38f, 1f));
            StretchFill(manufacture.GetComponent<RectTransform>(), new Vector2(0.64f, 0.01f), new Vector2(0.98f, 0.08f), 2f);
            SetButtonFontSize(manufacture, 16);

            // Drag ghost
            var ghost = CreateUiPanel(zone, "DragGhost", new Color(0.4f, 0.55f, 0.75f, 0.85f));
            Place(ghost.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(88f, 88f));
            var ghostLabel = CreateUiText(ghost.transform, "Label", "", 12, TextAnchor.MiddleCenter);
            Stretch(ghostLabel.GetComponent<RectTransform>());
            ghost.SetActive(false);

            var view = zone.gameObject.AddComponent<ManufacturePanelView>();
            var so = new SerializedObject(view);
            so.FindProperty("_inventoryContent").objectReferenceValue = invScrollContent;
            so.FindProperty("_inventoryRowTemplate").objectReferenceValue = inventoryTemplate;
            so.FindProperty("_inventoryScroll").objectReferenceValue = invScroll;
            so.FindProperty("_inventoryBarRoot").objectReferenceValue = inventoryColumn.GetComponent<RectTransform>();
            var slotProp = so.FindProperty("_slotCells");
            slotProp.arraySize = 15;
            for (var i = 0; i < 15; i++)
            {
                slotProp.GetArrayElementAtIndex(i).objectReferenceValue = slotCells[i];
            }

            so.FindProperty("_previewText").objectReferenceValue = previewText;
            so.FindProperty("_poolContent").objectReferenceValue = poolContent;
            so.FindProperty("_poolFrameTemplate").objectReferenceValue = poolFrameTemplate;
            so.FindProperty("_poolScroll").objectReferenceValue = poolScroll;
            so.FindProperty("_grantKitButton").objectReferenceValue = grantKit.GetComponent<Button>();
            so.FindProperty("_clearSlotsButton").objectReferenceValue = clearSlots.GetComponent<Button>();
            so.FindProperty("_manufactureButton").objectReferenceValue = manufacture.GetComponent<Button>();
            so.FindProperty("_placeholderImage").objectReferenceValue = placeholderImg;
            so.FindProperty("_previewRawImage").objectReferenceValue = rawImage;
            so.FindProperty("_previewModelAnchor").objectReferenceValue = previewAnchor;
            so.FindProperty("_previewCamera").objectReferenceValue = previewCam;
            so.FindProperty("_dragGhost").objectReferenceValue = ghost.GetComponent<RectTransform>();
            so.ApplyModifiedPropertiesWithoutUndo();
            return view;
        }

        private static Button CreateSquareSlot(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, bool gem)
        {
            var size = gem ? ManufacturePanelView.GemSlotSize : ManufacturePanelView.BodySlotSize;
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = new Color(0.24f, 0.27f, 0.34f, 0.95f);
            StretchFill(go.GetComponent<RectTransform>(), anchorMin, anchorMax, 2f);
            var le = go.GetComponent<LayoutElement>();
            le.preferredWidth = size;
            le.preferredHeight = size;
            le.minWidth = size;
            le.minHeight = size;
            var text = CreateUiText(go.transform, "Label", name.Replace("Slot_", ""), gem ? 10 : 12, TextAnchor.MiddleCenter);
            Stretch(text.GetComponent<RectTransform>());
            return go.GetComponent<Button>();
        }

        private static RectTransform CreateHorizontalScrollBar(Transform parent, out ScrollRect scroll)
        {
            var scrollGo = new GameObject("Scroll", typeof(RectTransform), typeof(ScrollRect), typeof(Image));
            scrollGo.transform.SetParent(parent, false);
            StretchFill(scrollGo.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, 2f);
            scrollGo.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.02f);
            scroll = scrollGo.GetComponent<ScrollRect>();
            scroll.horizontal = true;
            scroll.vertical = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.enabled = false;

            var viewportGo = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(RectMask2D));
            viewportGo.transform.SetParent(scrollGo.transform, false);
            StretchFill(viewportGo.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, 0f);
            viewportGo.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.02f);

            var contentGo = new GameObject("Content", typeof(RectTransform), typeof(HorizontalLayoutGroup),
                typeof(ContentSizeFitter));
            contentGo.transform.SetParent(viewportGo.transform, false);
            var content = contentGo.GetComponent<RectTransform>();
            content.anchorMin = new Vector2(0f, 0f);
            content.anchorMax = new Vector2(0f, 1f);
            content.pivot = new Vector2(0f, 0.5f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = new Vector2(0f, 0f);

            var layout = contentGo.GetComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.spacing = 8f;
            layout.padding = new RectOffset(8, 8, 4, 4);
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = true;
            layout.childForceExpandWidth = false;

            var fitter = contentGo.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;

            scroll.content = content;
            scroll.viewport = viewportGo.GetComponent<RectTransform>();
            return content;
        }

        private static RectTransform CreateVerticalScrollBar(Transform parent, out ScrollRect scroll)
        {
            var scrollGo = new GameObject("Scroll", typeof(RectTransform), typeof(ScrollRect), typeof(Image));
            scrollGo.transform.SetParent(parent, false);
            StretchFill(scrollGo.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, 2f);
            scrollGo.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.02f);
            scroll = scrollGo.GetComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 28f;

            var viewportGo = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(RectMask2D));
            viewportGo.transform.SetParent(scrollGo.transform, false);
            StretchFill(viewportGo.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, 0f);
            viewportGo.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.02f);

            var contentGo = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup),
                typeof(ContentSizeFitter));
            contentGo.transform.SetParent(viewportGo.transform, false);
            var content = contentGo.GetComponent<RectTransform>();
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = new Vector2(0f, 0f);

            var layout = contentGo.GetComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.spacing = 6f;
            layout.padding = new RectOffset(6, 6, 6, 6);
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;

            var fitter = contentGo.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll.content = content;
            scroll.viewport = viewportGo.GetComponent<RectTransform>();
            return content;
        }

        private static PoolSoldierFrameView CreatePoolSoldierFrameTemplate(Transform content)
        {
            var go = new GameObject("PoolSoldierFrameTemplate", typeof(RectTransform), typeof(Image), typeof(Button),
                typeof(LayoutElement), typeof(PoolSoldierFrameView));
            go.transform.SetParent(content, false);
            var bg = go.GetComponent<Image>();
            bg.color = new Color(0.2f, 0.24f, 0.22f, 0.95f);
            var le = go.GetComponent<LayoutElement>();
            le.minHeight = 72f;
            le.preferredHeight = 72f;
            le.flexibleWidth = 1f;

            var summary = CreateUiText(go.transform, "Summary", "W_001 士兵", 12, TextAnchor.UpperLeft);
            StretchFill(summary.GetComponent<RectTransform>(), new Vector2(0.04f, 0.35f), new Vector2(0.96f, 0.95f), 2f);

            var remake = CreateUiButton(go.transform, "RemakeButton", "再造1个", new Color(0.32f, 0.5f, 0.36f, 1f));
            StretchFill(remake.GetComponent<RectTransform>(), new Vector2(0.15f, 0.05f), new Vector2(0.85f, 0.38f), 2f);
            SetButtonFontSize(remake, 12);
            remake.SetActive(false);

            var view = go.GetComponent<PoolSoldierFrameView>();
            var so = new SerializedObject(view);
            so.FindProperty("_frameButton").objectReferenceValue = go.GetComponent<Button>();
            so.FindProperty("_summaryText").objectReferenceValue = summary;
            so.FindProperty("_remakeButton").objectReferenceValue = remake.GetComponent<Button>();
            so.FindProperty("_background").objectReferenceValue = bg;
            so.ApplyModifiedPropertiesWithoutUndo();
            go.SetActive(false);
            return view;
        }

        private static ToastView BuildUmTips(Transform canvas)
        {
            var root = CreateUiPanel(canvas, "UmTips", new Color(0.05f, 0.05f, 0.08f, 0.9f));
            Place(root.GetComponent<RectTransform>(), new Vector2(0.5f, 0.82f), new Vector2(0.5f, 0.82f),
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(480f, 52f));
            var text = CreateUiText(root.transform, "Message", string.Empty, 22, TextAnchor.MiddleCenter);
            Stretch(text.GetComponent<RectTransform>());

            var cg = root.GetComponent<CanvasGroup>();
            if (cg == null)
            {
                cg = root.AddComponent<CanvasGroup>();
            }

            cg.alpha = 0f;
            cg.interactable = false;
            cg.blocksRaycasts = false;

            var view = root.AddComponent<ToastView>();
            var so = new SerializedObject(view);
            so.FindProperty("_root").objectReferenceValue = root;
            so.FindProperty("_messageText").objectReferenceValue = text;
            so.FindProperty("_visibleSeconds").floatValue = 1f;
            so.ApplyModifiedPropertiesWithoutUndo();
            root.SetActive(true);
            return view;
        }

        private static Button CreateSquareTemplate(Transform content, string name, float size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            go.transform.SetParent(content, false);
            go.GetComponent<Image>().color = new Color(0.24f, 0.27f, 0.34f, 0.95f);
            var le = go.GetComponent<LayoutElement>();
            le.preferredWidth = size;
            le.preferredHeight = size;
            le.minWidth = size;
            le.minHeight = size;
            var text = CreateUiText(go.transform, "Label", "item", 11, TextAnchor.MiddleCenter);
            Stretch(text.GetComponent<RectTransform>());
            go.SetActive(false);
            return go.GetComponent<Button>();
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
