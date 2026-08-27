#if UNITY_EDITOR
using System.IO;
using Gravedigger2026.Editor.AutoManufacture;
using Gravedigger2026.Gameplay.AutoManufacture;
using Gravedigger2026.Gameplay.Dig;
using Gravedigger2026.Meta;
using Gravedigger2026.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Gravedigger2026.Editor.Meta
{
    /// <summary>
    /// Builds Meta shell Prefabs + Boot scene (Approach A). Auto-runs once if assets missing.
    /// EM-01: Ensure InSaveEquipMagicBookPanels (UI-022/023).
    /// EM-02: Ensure EquipmentWarehouse list (UI-022 / D-067).
    /// EM-03: Nest shared BookRow into MagicBookSlotsPanel (UI-023 / D-068).
    /// D-072: MagicBookSlotsPanel floating DeleteButton + TryUnequip confirm.
    /// </summary>
    public static class MetaShellAssetBuilder
    {
        private const string PrefabMetaDir = "Assets/Prefabs/Meta";
        private const string PrefabUiDir = "Assets/Prefabs/UI";
        private const string RootPrefabPath = PrefabMetaDir + "/MetaShellRoot.prefab";
        private const string TitleMenuPrefabPath = PrefabMetaDir + "/TitleMenuPanel.prefab";
        private const string TitleSettingsPrefabPath = PrefabMetaDir + "/TitleSettingsPanel.prefab";
        private const string InSaveShellPrefabPath = PrefabMetaDir + "/InSaveShellPanel.prefab";
        private const string TitleBackgroundSpritePath = "Assets/Art/UI/Meta/Title/Title_Background_Static.png";
        private const string TitleGameNameSpritePath = "Assets/Art/UI/Meta/Title/Title_GameName.png";
        private const string LevelMapSpritePath = "Assets/Art/UI/Meta/Title/Level_Map_1.jpg";
        private const string DifficultyItem1SpritePath = "Assets/Art/UI/Meta/Title/Item_Difficulty_1.png";
        private const string DifficultyItem2SpritePath = "Assets/Art/UI/Meta/Title/Item_Difficulty_2.png";
        private const string BootScenePath = "Assets/Scenes/Boot.unity";
        private const string RegenPrefsKey = "Gravedigger2026.MetaShell.Regen.v0792_levelSelect";
        private const string TitleMenuRegenPrefsKey = "Gravedigger2026.MetaShell.Regen.v08321_titleGameName";
        private const string TitleSettingsRegenPrefsKey = "Gravedigger2026.MetaShell.Regen.v08322_titleSettings";
        private const string InSaveDifficultyHubRegenPrefsKey = "Gravedigger2026.MetaShell.Regen.v08326_insaveDifficultyHub";
        private const string GmGrantRegenPrefsKey = "Gravedigger2026.MetaShell.Regen.v08217_gmGrant";
        private const string GmGrantLevelPickerRegenPrefsKey = "Gravedigger2026.MetaShell.Regen.v08297_gmGrantLevelPicker";
        private const string GmAddSoldierRegenPrefsKey = "Gravedigger2026.MetaShell.Regen.v08239_gmAddSoldier";
        private const string EquipMagicBookRegenPrefsKey = "Gravedigger2026.MetaShell.Regen.v08270_equipMagicBook";
        private const string EquipWarehouseListRegenPrefsKey = "Gravedigger2026.MetaShell.Regen.v08271_equipWarehouseList";
        private const string MagicBookBookRowRegenPrefsKey = "Gravedigger2026.MetaShell.Regen.v08272_magicBookRow";
        private const int InSaveModalSortingOrder = 100;

        [InitializeOnLoadMethod]
        private static void AutoGenerateIfMissing()
        {
            EditorApplication.delayCall += () =>
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode)
                {
                    return;
                }

                var missing = AssetDatabase.LoadAssetAtPath<GameObject>(RootPrefabPath) == null;
                if (missing)
                {
                    GenerateAll();
                    EditorPrefs.SetBool(RegenPrefsKey, true);
                    return;
                }

                var needsLevelUiRegen = !EditorPrefs.GetBool(RegenPrefsKey, false);
                if (needsLevelUiRegen)
                {
                    EnsureLevelSelectPanelOnExistingPrefab();
                    EditorPrefs.SetBool(RegenPrefsKey, true);
                }

                var needsGmGrantRegen = !EditorPrefs.GetBool(GmGrantRegenPrefsKey, false);
                if (needsGmGrantRegen)
                {
                    EnsureGmGrantListPanelOnExistingPrefab();
                    EditorPrefs.SetBool(GmGrantRegenPrefsKey, true);
                }

                var needsGmGrantLevelPickerRegen = !EditorPrefs.GetBool(GmGrantLevelPickerRegenPrefsKey, false);
                if (needsGmGrantLevelPickerRegen)
                {
                    EnsureGmGrantListPanelOnExistingPrefab();
                    EditorPrefs.SetBool(GmGrantLevelPickerRegenPrefsKey, true);
                }

                var needsGmAddSoldierRegen = !EditorPrefs.GetBool(GmAddSoldierRegenPrefsKey, false);
                if (needsGmAddSoldierRegen)
                {
                    EnsureGmAddSoldierPanelOnExistingPrefab();
                    EditorPrefs.SetBool(GmAddSoldierRegenPrefsKey, true);
                }

                var needsEquipMagicBookRegen = !EditorPrefs.GetBool(EquipMagicBookRegenPrefsKey, false);
                if (needsEquipMagicBookRegen)
                {
                    EnsureInSaveEquipMagicBookPanelsOnExistingPrefab();
                    EditorPrefs.SetBool(EquipMagicBookRegenPrefsKey, true);
                }

                var needsEquipWarehouseListRegen = !EditorPrefs.GetBool(EquipWarehouseListRegenPrefsKey, false);
                if (needsEquipWarehouseListRegen)
                {
                    EnsureEquipmentWarehouseListOnExistingPrefab();
                    EditorPrefs.SetBool(EquipWarehouseListRegenPrefsKey, true);
                }

                var needsMagicBookRowRegen = !EditorPrefs.GetBool(MagicBookBookRowRegenPrefsKey, false);
                if (needsMagicBookRowRegen)
                {
                    EnsureMagicBookBookRowOnExistingPrefab();
                    EditorPrefs.SetBool(MagicBookBookRowRegenPrefsKey, true);
                }

                var needsTitleMenuRegen = !EditorPrefs.GetBool(TitleMenuRegenPrefsKey, false);
                if (needsTitleMenuRegen)
                {
                    EnsureTitleMenuOnExistingPrefab();
                    EditorPrefs.SetBool(TitleMenuRegenPrefsKey, true);
                }

                var needsTitleSettingsRegen = !EditorPrefs.GetBool(TitleSettingsRegenPrefsKey, false);
                if (needsTitleSettingsRegen)
                {
                    EnsureTitleSettingsPanelOnExistingPrefab();
                    EditorPrefs.SetBool(TitleSettingsRegenPrefsKey, true);
                }

                var needsInSaveDifficultyHub = !EditorPrefs.GetBool(InSaveDifficultyHubRegenPrefsKey, false);
                if (needsInSaveDifficultyHub)
                {
                    EnsureInSaveShellPanelOnExistingPrefab();
                    EditorPrefs.SetBool(InSaveDifficultyHubRegenPrefsKey, true);
                }
            };
        }

        [MenuItem("Gravedigger2026/Meta/Ensure TitleMenu (UI-027)")]
        public static void EnsureTitleMenuMenu()
        {
            EnsureTitleMenuOnExistingPrefab();
            EditorPrefs.SetBool(TitleMenuRegenPrefsKey, true);
        }

        [MenuItem("Gravedigger2026/Meta/Ensure TitleSettingsPanel (UI-028)")]
        public static void EnsureTitleSettingsPanelMenu()
        {
            EnsureTitleSettingsPanelOnExistingPrefab();
            EditorPrefs.SetBool(TitleSettingsRegenPrefsKey, true);
        }

        [MenuItem("Gravedigger2026/Meta/Ensure InSaveShellPanel (UI-029)")]
        public static void EnsureInSaveShellPanelMenu()
        {
            EnsureInSaveShellPanelOnExistingPrefab();
            EditorPrefs.SetBool(InSaveDifficultyHubRegenPrefsKey, true);
        }

        /// <summary>Batchmode: -executeMethod Gravedigger2026.Editor.Meta.MetaShellAssetBuilder.EnsureInSaveShellPanelBatch</summary>
        public static void EnsureInSaveShellPanelBatch()
        {
            EnsureInSaveShellPanelMenu();
        }

        /// <summary>Batchmode: -executeMethod Gravedigger2026.Editor.Meta.MetaShellAssetBuilder.EnsureTitleSettingsPanelBatch</summary>
        public static void EnsureTitleSettingsPanelBatch()
        {
            EnsureTitleSettingsPanelMenu();
        }

        /// <summary>Batchmode: -executeMethod Gravedigger2026.Editor.Meta.MetaShellAssetBuilder.EnsureTitleMenuBatch</summary>
        public static void EnsureTitleMenuBatch()
        {
            EnsureTitleMenuMenu();
        }

        /// <summary>
        /// Surgical patch: TitleScreenBackground + TitleMenuPanel; SaveSelect transparent + Back button.
        /// </summary>
        public static void EnsureTitleMenuOnExistingPrefab()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RootPrefabPath);
            if (prefab == null)
            {
                Debug.LogWarning("[MetaShellAssetBuilder] MetaShellRoot missing; run full Generate.");
                return;
            }

            var root = PrefabUtility.LoadPrefabContents(RootPrefabPath);
            try
            {
                var controller = root.GetComponent<MetaShellController>();
                var canvas = root.transform.Find("MetaCanvas");
                if (canvas == null)
                {
                    Debug.LogError("[MetaShellAssetBuilder] MetaCanvas not found on MetaShellRoot.");
                    return;
                }

                var background = canvas.Find("TitleScreenBackground");
                if (background == null)
                {
                    BuildTitleScreenBackground(canvas);
                }
                else
                {
                    ApplyTitleBackgroundSprite(background.GetComponent<Image>());
                }

                if (canvas.GetComponentInChildren<TitleMenuView>(true) == null)
                {
                    BuildTitleMenu(canvas);
                }
                else
                {
                    var titleMenu = canvas.Find("TitleMenuPanel");
                    if (titleMenu != null)
                    {
                        EnsureTitleGameName(titleMenu);
                    }
                }

                EnsureStandaloneTitleMenuGameName();

                var saveSelect = canvas.GetComponentInChildren<SaveSelectView>(true);
                if (saveSelect != null)
                {
                    PatchSaveSelectPanel(saveSelect);
                    saveSelect.gameObject.SetActive(false);
                }

                if (controller != null)
                {
                    WireTitleScreenRefs(controller, canvas);
                }

                PrefabUtility.SaveAsPrefabAsset(root, RootPrefabPath);
                Debug.Log("[MetaShellAssetBuilder] TitleMenu (UI-027) patched onto MetaShellRoot.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        /// <summary>
        /// Surgical patch: TitleSettingsPanel (UI-028) under MetaCanvas + wire MetaShellController.
        /// </summary>
        public static void EnsureTitleSettingsPanelOnExistingPrefab()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RootPrefabPath);
            if (prefab == null)
            {
                Debug.LogWarning("[MetaShellAssetBuilder] MetaShellRoot missing; run full Generate.");
                return;
            }

            var root = PrefabUtility.LoadPrefabContents(RootPrefabPath);
            try
            {
                var controller = root.GetComponent<MetaShellController>();
                var canvas = root.transform.Find("MetaCanvas");
                if (canvas == null)
                {
                    Debug.LogError("[MetaShellAssetBuilder] MetaCanvas not found on MetaShellRoot.");
                    return;
                }

                TitleSettingsPanelView settingsView;
                var existing = canvas.GetComponentInChildren<TitleSettingsPanelView>(true);
                if (existing != null)
                {
                    Object.DestroyImmediate(existing.gameObject);
                }

                var stale = canvas.Find("TitleSettingsPanel");
                if (stale != null)
                {
                    Object.DestroyImmediate(stale.gameObject);
                }

                settingsView = BuildTitleSettingsPanel(canvas);

                if (controller != null)
                {
                    var so = new SerializedObject(controller);
                    so.FindProperty("_titleSettingsPanelView").objectReferenceValue = settingsView;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }

                PrefabUtility.SaveAsPrefabAsset(root, RootPrefabPath);
                Debug.Log("[MetaShellAssetBuilder] TitleSettingsPanel (UI-028) patched onto MetaShellRoot.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        /// <summary>
        /// Surgical patch: DifficultySelectHost (UI-029) + standalone InSaveShellPanel.prefab.
        /// </summary>
        public static void EnsureInSaveShellPanelOnExistingPrefab()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RootPrefabPath);
            if (prefab == null)
            {
                Debug.LogWarning("[MetaShellAssetBuilder] MetaShellRoot missing; run full Generate.");
                return;
            }

            if (!Directory.Exists(PrefabMetaDir))
            {
                Directory.CreateDirectory(PrefabMetaDir);
            }

            var root = PrefabUtility.LoadPrefabContents(RootPrefabPath);
            try
            {
                var controller = root.GetComponent<MetaShellController>();
                var inSave = root.GetComponentInChildren<InSaveShellView>(true);
                if (inSave == null)
                {
                    Debug.LogError("[MetaShellAssetBuilder] InSaveShellView not found on MetaShellRoot.");
                    return;
                }

                PatchDifficultySelectHost(inSave);
                PrefabUtility.SaveAsPrefabAsset(root, RootPrefabPath);

                var panelGo = inSave.gameObject;
                var standalone = Object.Instantiate(panelGo);
                standalone.name = "InSaveShellPanel";
                PrefabUtility.SaveAsPrefabAsset(standalone, InSaveShellPrefabPath);
                Object.DestroyImmediate(standalone);

                Debug.Log("[MetaShellAssetBuilder] InSaveShellPanel (UI-029) ensured + standalone Prefab saved.");

                if (controller != null)
                {
                    var so = new SerializedObject(controller);
                    so.FindProperty("_inSaveShellView").objectReferenceValue = inSave;
                    so.ApplyModifiedPropertiesWithoutUndo();
                    PrefabUtility.SaveAsPrefabAsset(root, RootPrefabPath);
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void PatchDifficultySelectHost(InSaveShellView inSave)
        {
            var panel = inSave.transform;
            var hostTf = panel.Find("DifficultySelectHost");
            DifficultySelectHostView hostView;
            RectTransform mapHostRt;
            if (hostTf == null)
            {
                var hostGo = new GameObject("DifficultySelectHost", typeof(RectTransform));
                hostGo.transform.SetParent(panel, false);
                var hostRt = hostGo.GetComponent<RectTransform>();
                hostRt.anchorMin = new Vector2(0.08f, 0.12f);
                hostRt.anchorMax = new Vector2(0.92f, 0.88f);
                hostRt.offsetMin = Vector2.zero;
                hostRt.offsetMax = Vector2.zero;
                hostGo.transform.SetSiblingIndex(Mathf.Min(3, panel.childCount - 1));

                var columns = new GameObject("Columns", typeof(RectTransform));
                columns.transform.SetParent(hostGo.transform, false);
                StretchFull(columns.GetComponent<RectTransform>());

                var normal = CreateDifficultyColumnEditor(columns.transform, "NormalColumn", "普通难度",
                    new Color(0.55f, 0.78f, 0.55f, 1f), DifficultyItem1SpritePath);
                var hard = CreateDifficultyColumnEditor(columns.transform, "HardColumn", "困难难度",
                    new Color(0.85f, 0.75f, 0.35f, 1f), DifficultyItem2SpritePath);
                var hell = CreateDifficultyColumnEditor(columns.transform, "HellColumn", "地狱难度",
                    new Color(0.90f, 0.55f, 0.35f, 1f), null);

                var mapHostGo = new GameObject("MapHost", typeof(RectTransform), typeof(Image));
                mapHostGo.transform.SetParent(hostGo.transform, false);
                mapHostRt = mapHostGo.GetComponent<RectTransform>();
                mapHostRt.anchorMin = new Vector2(0.18f, 0f);
                mapHostRt.anchorMax = new Vector2(0.82f, 1f);
                mapHostRt.offsetMin = Vector2.zero;
                mapHostRt.offsetMax = Vector2.zero;
                var mapImg = mapHostGo.GetComponent<Image>();
                mapImg.color = new Color(1f, 1f, 1f, 0.96f);
                var mapSprite = AssetDatabase.LoadAssetAtPath<Sprite>(LevelMapSpritePath);
                if (mapSprite != null)
                {
                    mapImg.sprite = mapSprite;
                }

                hostView = hostGo.AddComponent<DifficultySelectHostView>();
                var hso = new SerializedObject(hostView);
                hso.FindProperty("_root").objectReferenceValue = hostGo;
                hso.FindProperty("_columnsRoot").objectReferenceValue = columns.GetComponent<RectTransform>();
                hso.FindProperty("_normalButton").objectReferenceValue = normal.GetComponent<Button>();
                hso.FindProperty("_hardButton").objectReferenceValue = hard.GetComponent<Button>();
                hso.FindProperty("_hellButton").objectReferenceValue = hell.GetComponent<Button>();
                hso.FindProperty("_normalImage").objectReferenceValue = normal.GetComponent<Image>();
                hso.FindProperty("_hardImage").objectReferenceValue = hard.GetComponent<Image>();
                hso.FindProperty("_hellImage").objectReferenceValue = hell.GetComponent<Image>();
                hso.FindProperty("_normalLabel").objectReferenceValue =
                    normal.transform.Find("Label")?.GetComponent<Text>();
                hso.FindProperty("_hardLabel").objectReferenceValue =
                    hard.transform.Find("Label")?.GetComponent<Text>();
                hso.FindProperty("_hellLabel").objectReferenceValue =
                    hell.transform.Find("Label")?.GetComponent<Text>();
                hso.FindProperty("_mapHost").objectReferenceValue = mapHostRt;
                hso.FindProperty("_mapHostRoot").objectReferenceValue = mapHostGo;
                hso.ApplyModifiedPropertiesWithoutUndo();
                hostGo.SetActive(false);
            }
            else
            {
                hostView = hostTf.GetComponent<DifficultySelectHostView>();
                mapHostRt = hostTf.Find("MapHost") as RectTransform;
            }

            var levelSelect = panel.GetComponentInChildren<LevelSelectPanelView>(true);
            if (levelSelect != null && mapHostRt != null)
            {
                var levelGo = levelSelect.gameObject;
                levelGo.transform.SetParent(mapHostRt, false);
                var lrt = levelGo.GetComponent<RectTransform>();
                StretchFull(lrt);
                lrt.offsetMin = new Vector2(12f, 12f);
                lrt.offsetMax = new Vector2(-12f, -12f);

                var box = levelGo.transform.Find("Box");
                var parent = box != null ? box : levelGo.transform;
                EnsureEnterButtonOnLevelSelect(parent);

                var so = new SerializedObject(levelSelect);
                so.FindProperty("_hubEmbedded").boolValue = true;
                so.FindProperty("_backdropImage").objectReferenceValue = levelGo.GetComponent<Image>();
                var enter = parent.Find("EnterButton")?.GetComponent<Button>();
                if (enter != null)
                {
                    so.FindProperty("_enterButton").objectReferenceValue = enter;
                }

                so.ApplyModifiedPropertiesWithoutUndo();
            }

            var viewSo = new SerializedObject(inSave);
            viewSo.FindProperty("_difficultySelectHost").objectReferenceValue = hostView;
            if (levelSelect != null)
            {
                viewSo.FindProperty("_levelSelectPanel").objectReferenceValue = levelSelect;
            }

            viewSo.ApplyModifiedPropertiesWithoutUndo();
        }

        private static GameObject CreateDifficultyColumnEditor(
            Transform parent,
            string name,
            string label,
            Color color,
            string spritePath)
        {
            var go = CreateButton(parent, name, label, color);
            var le = go.GetComponent<LayoutElement>();
            if (le != null)
            {
                Object.DestroyImmediate(le);
            }

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.offsetMin = new Vector2(8f, 8f);
            rt.offsetMax = new Vector2(-8f, -8f);
            if (!string.IsNullOrEmpty(spritePath))
            {
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
                if (sprite != null)
                {
                    go.GetComponent<Image>().sprite = sprite;
                    go.GetComponent<Image>().type = Image.Type.Simple;
                }
            }

            var labelText = go.transform.Find("Label")?.GetComponent<Text>();
            if (labelText != null)
            {
                labelText.fontSize = 28;
            }

            return go;
        }

        private static void EnsureEnterButtonOnLevelSelect(Transform parent)
        {
            if (parent.Find("EnterButton") != null)
            {
                return;
            }

            var enter = CreateButton(parent, "EnterButton", "进入", new Color(0.25f, 0.45f, 0.85f, 1f));
            Place(
                enter.GetComponent<RectTransform>(),
                new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f),
                new Vector2(0f, 16f),
                new Vector2(200f, 48f));
        }

        [MenuItem("Gravedigger2026/Meta/Ensure LevelSelectPanel (UI-008)")]
        public static void EnsureLevelSelectPanelMenu()
        {
            EnsureLevelSelectPanelOnExistingPrefab();
            EditorPrefs.SetBool(RegenPrefsKey, true);
        }

        /// <summary>
        /// Surgical patch: add LevelSelectPanel under InSaveShell without rebuilding MetaShellRoot.
        /// </summary>
        public static void EnsureLevelSelectPanelOnExistingPrefab()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RootPrefabPath);
            if (prefab == null)
            {
                Debug.LogWarning("[MetaShellAssetBuilder] MetaShellRoot missing; run full Generate.");
                return;
            }

            var root = PrefabUtility.LoadPrefabContents(RootPrefabPath);
            try
            {
                var inSave = root.GetComponentInChildren<InSaveShellView>(true);
                if (inSave == null)
                {
                    Debug.LogError("[MetaShellAssetBuilder] InSaveShellView not found on MetaShellRoot.");
                    return;
                }

                var so = new SerializedObject(inSave);
                var panelProp = so.FindProperty("_levelSelectPanel");
                if (panelProp != null && panelProp.objectReferenceValue != null)
                {
                    Debug.Log("[MetaShellAssetBuilder] LevelSelectPanel already wired.");
                    return;
                }

                var existing = inSave.transform.Find("LevelSelectPanel");
                if (existing != null)
                {
                    Object.DestroyImmediate(existing.gameObject);
                }

                var levelSelect = BuildLevelSelectPanel(inSave.transform);
                if (panelProp != null)
                {
                    panelProp.objectReferenceValue = levelSelect;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }

                PrefabUtility.SaveAsPrefabAsset(root, RootPrefabPath);
                Debug.Log("[MetaShellAssetBuilder] LevelSelectPanel patched onto MetaShellRoot.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        [MenuItem("Gravedigger2026/Meta/Ensure GmGrantListPanel (UI-019)")]
        public static void EnsureGmGrantListPanelMenu()
        {
            EnsureGmGrantListPanelOnExistingPrefab();
            EditorPrefs.SetBool(GmGrantRegenPrefsKey, true);
            EditorPrefs.SetBool(GmGrantLevelPickerRegenPrefsKey, true);
        }

        /// <summary>Batchmode: -executeMethod Gravedigger2026.Editor.Meta.MetaShellAssetBuilder.EnsureGmGrantListPanelBatch</summary>
        public static void EnsureGmGrantListPanelBatch()
        {
            EnsureGmGrantListPanelMenu();
        }

        /// <summary>
        /// Surgical patch: ToolsPanel GM entries + GmGrantListPanel under InSaveShell.
        /// </summary>
        public static void EnsureGmGrantListPanelOnExistingPrefab()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RootPrefabPath);
            if (prefab == null)
            {
                Debug.LogWarning("[MetaShellAssetBuilder] MetaShellRoot missing; run full Generate.");
                return;
            }

            var root = PrefabUtility.LoadPrefabContents(RootPrefabPath);
            try
            {
                var inSave = root.GetComponentInChildren<InSaveShellView>(true);
                if (inSave == null)
                {
                    Debug.LogError("[MetaShellAssetBuilder] InSaveShellView not found on MetaShellRoot.");
                    return;
                }

                var tools = inSave.GetComponentInChildren<ToolsPanelView>(true);
                if (tools != null)
                {
                    PatchToolsPanelGrantButtons(tools);
                }

                var so = new SerializedObject(inSave);
                var panelProp = so.FindProperty("_gmGrantListPanel");
                GmGrantListPanelView grantPanel = null;
                if (panelProp != null && panelProp.objectReferenceValue != null)
                {
                    grantPanel = panelProp.objectReferenceValue as GmGrantListPanelView;
                }

                if (grantPanel == null)
                {
                    var existing = inSave.transform.Find("GmGrantListPanel");
                    if (existing != null)
                    {
                        Object.DestroyImmediate(existing.gameObject);
                    }

                    grantPanel = BuildGmGrantListPanel(inSave.transform);
                    if (panelProp != null)
                    {
                        panelProp.objectReferenceValue = grantPanel;
                        so.ApplyModifiedPropertiesWithoutUndo();
                    }
                }
                else
                {
                    AttachLevelPicker(grantPanel);
                }

                PrefabUtility.SaveAsPrefabAsset(root, RootPrefabPath);
                Debug.Log("[MetaShellAssetBuilder] GmGrantListPanel + ToolsPanel GM buttons patched onto MetaShellRoot.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        [MenuItem("Gravedigger2026/Meta/Ensure GmAddSoldierPanel (UI-020)")]
        public static void EnsureGmAddSoldierPanelMenu()
        {
            EnsureGmAddSoldierPanelOnExistingPrefab();
            EditorPrefs.SetBool(GmAddSoldierRegenPrefsKey, true);
        }

        public static void EnsureGmAddSoldierPanelOnExistingPrefab()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RootPrefabPath);
            if (prefab == null)
            {
                Debug.LogWarning("[MetaShellAssetBuilder] MetaShellRoot missing; run full Generate.");
                return;
            }

            var root = PrefabUtility.LoadPrefabContents(RootPrefabPath);
            try
            {
                var inSave = root.GetComponentInChildren<InSaveShellView>(true);
                if (inSave == null)
                {
                    Debug.LogError("[MetaShellAssetBuilder] InSaveShellView not found on MetaShellRoot.");
                    return;
                }

                var tools = inSave.GetComponentInChildren<ToolsPanelView>(true);
                if (tools != null)
                {
                    PatchToolsPanelGrantButtons(tools);
                }

                var so = new SerializedObject(inSave);
                var panelProp = so.FindProperty("_gmAddSoldierPanel");
                // Runtime EnsureGmAddSoldierPanel builds UI if missing; clear stale refs so Awake rebuilds.
                if (panelProp != null && panelProp.objectReferenceValue == null)
                {
                    // leave null — InSaveShellView.EnsureGmAddSoldierPanel creates at runtime
                }

                PrefabUtility.SaveAsPrefabAsset(root, RootPrefabPath);
                Debug.Log("[MetaShellAssetBuilder] ToolsPanel「添加士兵」patched onto MetaShellRoot (UI-020).");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        [MenuItem("Gravedigger2026/Meta/Ensure InSaveEquipMagicBookPanels (UI-022/023)")]
        public static void EnsureInSaveEquipMagicBookPanelsMenu()
        {
            EnsureInSaveEquipMagicBookPanelsOnExistingPrefab();
            EditorPrefs.SetBool(EquipMagicBookRegenPrefsKey, true);
            EnsureEquipmentWarehouseListOnExistingPrefab();
            EditorPrefs.SetBool(EquipWarehouseListRegenPrefsKey, true);
        }

        /// <summary>Batchmode: -executeMethod Gravedigger2026.Editor.Meta.MetaShellAssetBuilder.EnsureInSaveEquipMagicBookPanelsBatch</summary>
        public static void EnsureInSaveEquipMagicBookPanelsBatch()
        {
            EnsureInSaveEquipMagicBookPanelsMenu();
        }

        /// <summary>
        /// Surgical patch: bottom-left Equipment / MagicBook buttons + two modal shells.
        /// Does not rebuild MetaShellRoot.
        /// </summary>
        public static void EnsureInSaveEquipMagicBookPanelsOnExistingPrefab()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RootPrefabPath);
            if (prefab == null)
            {
                Debug.LogWarning("[MetaShellAssetBuilder] MetaShellRoot missing; run full Generate.");
                return;
            }

            var root = PrefabUtility.LoadPrefabContents(RootPrefabPath);
            try
            {
                var inSave = root.GetComponentInChildren<InSaveShellView>(true);
                if (inSave == null)
                {
                    Debug.LogError("[MetaShellAssetBuilder] InSaveShellView not found on MetaShellRoot.");
                    return;
                }

                PatchInSaveEquipMagicBookButtons(inSave.transform, out var equipmentBtn, out var magicBookBtn);

                var so = new SerializedObject(inSave);
                var equipmentBtnProp = so.FindProperty("_equipmentButton");
                if (equipmentBtnProp != null && equipmentBtn != null)
                {
                    equipmentBtnProp.objectReferenceValue = equipmentBtn.GetComponent<Button>();
                }

                var magicBookBtnProp = so.FindProperty("_magicBookButton");
                if (magicBookBtnProp != null && magicBookBtn != null)
                {
                    magicBookBtnProp.objectReferenceValue = magicBookBtn.GetComponent<Button>();
                }

                var equipPanelProp = so.FindProperty("_equipmentWarehousePanel");
                EquipmentWarehousePanelView equipPanel = null;
                if (equipPanelProp != null)
                {
                    equipPanel = equipPanelProp.objectReferenceValue as EquipmentWarehousePanelView;
                }

                if (equipPanel == null)
                {
                    var existing = inSave.transform.Find("EquipmentWarehousePanel");
                    if (existing != null)
                    {
                        Object.DestroyImmediate(existing.gameObject);
                    }

                    equipPanel = BuildEquipmentWarehousePanel(inSave.transform);
                    if (equipPanelProp != null)
                    {
                        equipPanelProp.objectReferenceValue = equipPanel;
                    }
                }

                var magicPanelProp = so.FindProperty("_magicBookSlotsPanel");
                MagicBookSlotsPanelView magicPanel = null;
                if (magicPanelProp != null)
                {
                    magicPanel = magicPanelProp.objectReferenceValue as MagicBookSlotsPanelView;
                }

                if (magicPanel == null)
                {
                    var existing = inSave.transform.Find("MagicBookSlotsPanel");
                    if (existing != null)
                    {
                        Object.DestroyImmediate(existing.gameObject);
                    }

                    magicPanel = BuildMagicBookSlotsPanel(inSave.transform);
                    if (magicPanelProp != null)
                    {
                        magicPanelProp.objectReferenceValue = magicPanel;
                    }
                }
                else
                {
                    var box = magicPanel.transform.Find("Box");
                    if (box != null)
                    {
                        EnsureMagicBookDeleteButton(box, magicPanel);
                    }
                }

                so.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, RootPrefabPath);
                Debug.Log("[MetaShellAssetBuilder] InSave Equipment/MagicBook buttons + modal shells patched onto MetaShellRoot (UI-022/023).");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        [MenuItem("Gravedigger2026/Meta/Ensure EquipmentWarehouseList (UI-022)")]
        public static void EnsureEquipmentWarehouseListMenu()
        {
            EnsureEquipmentWarehouseListOnExistingPrefab();
            EditorPrefs.SetBool(EquipWarehouseListRegenPrefsKey, true);
        }

        /// <summary>Batchmode: -executeMethod Gravedigger2026.Editor.Meta.MetaShellAssetBuilder.EnsureEquipmentWarehouseListBatch</summary>
        public static void EnsureEquipmentWarehouseListBatch()
        {
            EnsureEquipmentWarehouseListMenu();
        }

        [MenuItem("Gravedigger2026/Meta/Ensure MagicBook BookRow (UI-023)")]
        public static void EnsureMagicBookBookRowMenu()
        {
            EnsureMagicBookBookRowOnExistingPrefab();
            EditorPrefs.SetBool(MagicBookBookRowRegenPrefsKey, true);
        }

        /// <summary>Batchmode: -executeMethod Gravedigger2026.Editor.Meta.MetaShellAssetBuilder.EnsureMagicBookBookRowBatch</summary>
        public static void EnsureMagicBookBookRowBatch()
        {
            AmAssetBuilder.NestBookRowIntoExistingPresentation();
            EnsureMagicBookBookRowOnExistingPrefab();
        }

        /// <summary>
        /// Surgical patch: nest shared BookRow.prefab under MagicBookSlotsPanel/BookRowHost.
        /// </summary>
        public static void EnsureMagicBookBookRowOnExistingPrefab()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RootPrefabPath);
            if (prefab == null)
            {
                Debug.LogWarning("[MetaShellAssetBuilder] MetaShellRoot missing; run full Generate.");
                return;
            }

            var bookRowPrefab = AmAssetBuilder.EnsureBookRowPrefab();
            if (bookRowPrefab == null)
            {
                Debug.LogWarning("[MetaShellAssetBuilder] BookRow.prefab missing; run AmAssetBuilder Generate BookRow.");
                return;
            }

            var root = PrefabUtility.LoadPrefabContents(RootPrefabPath);
            try
            {
                var inSave = root.GetComponentInChildren<InSaveShellView>(true);
                if (inSave == null)
                {
                    Debug.LogError("[MetaShellAssetBuilder] InSaveShellView not found on MetaShellRoot.");
                    return;
                }

                var magicPanel = inSave.GetComponentInChildren<MagicBookSlotsPanelView>(true);
                if (magicPanel == null)
                {
                    var existing = inSave.transform.Find("MagicBookSlotsPanel");
                    if (existing != null)
                    {
                        Object.DestroyImmediate(existing.gameObject);
                    }

                    magicPanel = BuildMagicBookSlotsPanel(inSave.transform);
                    var so = new SerializedObject(inSave);
                    var magicPanelProp = so.FindProperty("_magicBookSlotsPanel");
                    if (magicPanelProp != null)
                    {
                        magicPanelProp.objectReferenceValue = magicPanel;
                    }

                    so.ApplyModifiedPropertiesWithoutUndo();
                }

                NestBookRowIntoMagicPanel(magicPanel, bookRowPrefab);
                PrefabUtility.SaveAsPrefabAsset(root, RootPrefabPath);
                Debug.Log("[MetaShellAssetBuilder] Nested BookRow.prefab into MagicBookSlotsPanel (UI-023).");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }

            EditorPrefs.SetBool(MagicBookBookRowRegenPrefsKey, true);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        /// <summary>
        /// Surgical patch: EquipScroll + row template + empty hint on EquipmentWarehousePanel.
        /// Does not rebuild MetaShellRoot.
        /// </summary>
        public static void EnsureEquipmentWarehouseListOnExistingPrefab()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RootPrefabPath);
            if (prefab == null)
            {
                Debug.LogWarning("[MetaShellAssetBuilder] MetaShellRoot missing; run full Generate.");
                return;
            }

            var root = PrefabUtility.LoadPrefabContents(RootPrefabPath);
            try
            {
                var inSave = root.GetComponentInChildren<InSaveShellView>(true);
                if (inSave == null)
                {
                    Debug.LogError("[MetaShellAssetBuilder] InSaveShellView not found on MetaShellRoot.");
                    return;
                }

                var so = new SerializedObject(inSave);
                var equipPanelProp = so.FindProperty("_equipmentWarehousePanel");
                EquipmentWarehousePanelView equipPanel = null;
                if (equipPanelProp != null)
                {
                    equipPanel = equipPanelProp.objectReferenceValue as EquipmentWarehousePanelView;
                }

                if (equipPanel == null)
                {
                    var existing = inSave.transform.Find("EquipmentWarehousePanel");
                    if (existing != null)
                    {
                        Object.DestroyImmediate(existing.gameObject);
                    }

                    equipPanel = BuildEquipmentWarehousePanel(inSave.transform);
                    if (equipPanelProp != null)
                    {
                        equipPanelProp.objectReferenceValue = equipPanel;
                    }
                }
                else
                {
                    PatchEquipmentWarehouseList(equipPanel);
                }

                so.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, RootPrefabPath);
                Debug.Log("[MetaShellAssetBuilder] EquipmentWarehousePanel scroll list patched onto MetaShellRoot (UI-022 / D-067).");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        public static void GenerateAll()
        {
            EnsureFolders();

            var root = BuildMetaShellRoot();
            PrefabUtility.SaveAsPrefabAsset(root, RootPrefabPath);
            Object.DestroyImmediate(root);

            CreateBootScene();
            AddBootToBuildSettings();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[MetaShellAssetBuilder] Generated Meta Prefabs and Boot scene.");

            // Dig vertical (D-020) Prefabs + MetaShell Dig wiring.
            Gravedigger2026.Editor.Dig.DigAssetBuilder.GenerateAll();
            // TechTree canvas (UI-012) Prefab + Settings wiring.
            Gravedigger2026.Editor.Tech.TechTreeAssetBuilder.GenerateAll();
        }

        private static void EnsureFolders()
        {
            EnsureFolder("Assets/Prefabs");
            EnsureFolder(PrefabMetaDir);
            EnsureFolder(PrefabUiDir);
            EnsureFolder("Assets/Scenes");
            EnsureFolder("Assets/Scripts");
            EnsureFolder("Assets/Scripts/Core");
            EnsureFolder("Assets/Scripts/Core/Config");
            EnsureFolder("Assets/Scripts/Core/Level");
            EnsureFolder("Assets/Scripts/Meta");
            EnsureFolder("Assets/Scripts/UI");
            EnsureFolder("Assets/Editor");
            EnsureFolder("Assets/Editor/Meta");
            EnsureFolder("Assets/Art/UI/Meta/Title");
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

        private static GameObject BuildMetaShellRoot()
        {
            var root = new GameObject("MetaShellRoot");
            var controller = root.AddComponent<MetaShellController>();

            var canvasGo = CreateCanvas(root.transform, "MetaCanvas", DigUiLayering.MetaShellCanvasOrder);
            var titleBackground = BuildTitleScreenBackground(canvasGo.transform);
            var titleMenu = BuildTitleMenu(canvasGo.transform);
            var titleSettings = BuildTitleSettingsPanel(canvasGo.transform);
            var saveSelect = BuildSaveSelect(canvasGo.transform);
            var inSaveShell = BuildInSaveShell(canvasGo.transform);
            var confirm = BuildConfirmDialog(canvasGo.transform);
            var campaignModeSelect = BuildCampaignModeSelect(canvasGo.transform);
            var toast = BuildToast(canvasGo.transform);

            AssignControllerRefs(
                controller,
                saveSelect,
                titleMenu,
                titleSettings,
                titleBackground,
                inSaveShell,
                confirm,
                campaignModeSelect,
                toast);

            var eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            eventSystem.transform.SetParent(root.transform, false);

            return root;
        }

        private static void AssignControllerRefs(
            MetaShellController controller,
            SaveSelectView saveSelect,
            TitleMenuView titleMenu,
            TitleSettingsPanelView titleSettings,
            GameObject titleBackground,
            InSaveShellView inSaveShell,
            ConfirmDialogView confirm,
            CampaignModeSelectView campaignModeSelect,
            ToastView toast)
        {
            var so = new SerializedObject(controller);
            so.FindProperty("_saveSelectView").objectReferenceValue = saveSelect;
            so.FindProperty("_titleMenuView").objectReferenceValue = titleMenu;
            so.FindProperty("_titleSettingsPanelView").objectReferenceValue = titleSettings;
            so.FindProperty("_titleScreenBackground").objectReferenceValue = titleBackground;
            so.FindProperty("_inSaveShellView").objectReferenceValue = inSaveShell;
            so.FindProperty("_confirmDialog").objectReferenceValue = confirm;
            so.FindProperty("_campaignModeSelect").objectReferenceValue = campaignModeSelect;
            so.FindProperty("_toastView").objectReferenceValue = toast;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void WireTitleScreenRefs(MetaShellController controller, Transform canvas)
        {
            var so = new SerializedObject(controller);
            so.FindProperty("_titleMenuView").objectReferenceValue =
                canvas.GetComponentInChildren<TitleMenuView>(true);
            so.FindProperty("_titleScreenBackground").objectReferenceValue =
                canvas.Find("TitleScreenBackground")?.gameObject;
            so.FindProperty("_saveSelectView").objectReferenceValue =
                canvas.GetComponentInChildren<SaveSelectView>(true);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static GameObject BuildTitleScreenBackground(Transform parent)
        {
            var go = CreatePanel(parent, "TitleScreenBackground", new Color(0.12f, 0.14f, 0.18f, 1f));
            StretchFull(go.GetComponent<RectTransform>());
            go.transform.SetAsFirstSibling();
            ApplyTitleBackgroundSprite(go.GetComponent<Image>());
            go.SetActive(true);
            return go;
        }

        private static void ApplyTitleBackgroundSprite(Image image)
        {
            if (image == null)
            {
                return;
            }

            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(TitleBackgroundSpritePath);
            if (sprite == null)
            {
                Debug.LogWarning(
                    $"[MetaShellAssetBuilder] Title background sprite missing at {TitleBackgroundSpritePath}. " +
                    "Place Title_Background_Static.png then re-run Ensure TitleMenu (UI-027).");
                return;
            }

            image.sprite = sprite;
            image.color = Color.white;
            image.type = Image.Type.Simple;
            image.preserveAspect = false;
        }

        /// <summary>
        /// Ensures TitleMenuPanel/GameName (top-center Title_GameName.png; raycastTarget=0).
        /// </summary>
        private static void EnsureTitleGameName(Transform titleMenuRoot)
        {
            if (titleMenuRoot == null)
            {
                return;
            }

            var existing = titleMenuRoot.Find("GameName");
            GameObject go;
            if (existing == null)
            {
                go = new GameObject("GameName", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                go.transform.SetParent(titleMenuRoot, false);
                go.transform.SetAsFirstSibling();
            }
            else
            {
                go = existing.gameObject;
            }

            var rt = go.GetComponent<RectTransform>();
            Place(rt, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -40f), new Vector2(380f, 178f));

            var image = go.GetComponent<Image>();
            ApplyTitleGameNameSprite(image);
        }

        private static void ApplyTitleGameNameSprite(Image image)
        {
            if (image == null)
            {
                return;
            }

            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(TitleGameNameSpritePath);
            if (sprite == null)
            {
                Debug.LogWarning(
                    $"[MetaShellAssetBuilder] Title game-name sprite missing at {TitleGameNameSpritePath}. " +
                    "Place Title_GameName.png then re-run Ensure TitleMenu (UI-027).");
                return;
            }

            image.sprite = sprite;
            image.color = Color.white;
            image.type = Image.Type.Simple;
            image.preserveAspect = true;
            image.raycastTarget = false;
        }

        private static void EnsureStandaloneTitleMenuGameName()
        {
            var standalone = AssetDatabase.LoadAssetAtPath<GameObject>(TitleMenuPrefabPath);
            if (standalone == null)
            {
                return;
            }

            var root = PrefabUtility.LoadPrefabContents(TitleMenuPrefabPath);
            try
            {
                EnsureTitleGameName(root.transform);
                PrefabUtility.SaveAsPrefabAsset(root, TitleMenuPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static TitleMenuView BuildTitleMenu(Transform parent)
        {
            var existing = parent.Find("TitleMenuPanel");
            if (existing != null)
            {
                Object.DestroyImmediate(existing.gameObject);
            }

            var root = CreateContentRoot(parent, "TitleMenuPanel");
            StretchFull(root.GetComponent<RectTransform>());

            EnsureTitleGameName(root.transform);

            var buttonsParent = new GameObject("Buttons", typeof(RectTransform));
            buttonsParent.transform.SetParent(root.transform, false);
            var buttonsRt = buttonsParent.GetComponent<RectTransform>();
            Place(buttonsRt, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                new Vector2(-80f, 0f), new Vector2(280f, 280f));

            var layout = buttonsParent.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 12f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            var primary = CreateButton(buttonsParent.transform, "PrimaryButton", "开始游戏",
                new Color(0.25f, 0.55f, 0.35f, 1f));
            var primaryLe = primary.GetComponent<LayoutElement>();
            primaryLe.preferredHeight = 56f;
            primaryLe.minHeight = 56f;
            var loadSave = CreateButton(buttonsParent.transform, "LoadSaveButton", "读取存档",
                new Color(0.30f, 0.40f, 0.55f, 1f));
            var settings = CreateButton(buttonsParent.transform, "SettingsButton", "设置",
                new Color(0.30f, 0.40f, 0.55f, 1f));
            var credits = CreateButton(buttonsParent.transform, "CreditsButton", "开发者介绍",
                new Color(0.30f, 0.40f, 0.55f, 1f));

            var version = CreateText(root.transform, "VersionText", "版本 v0.0.0", 20, TextAnchor.LowerRight);
            version.color = new Color(0.75f, 0.75f, 0.78f, 1f);
            var versionRt = version.GetComponent<RectTransform>();
            Place(versionRt, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f),
                new Vector2(-24f, 24f), new Vector2(320f, 32f));

            var view = root.AddComponent<TitleMenuView>();
            var so = new SerializedObject(view);
            so.FindProperty("_root").objectReferenceValue = root;
            so.FindProperty("_primaryButton").objectReferenceValue = primary.GetComponent<Button>();
            so.FindProperty("_primaryButtonLabel").objectReferenceValue =
                primary.transform.Find("Label")?.GetComponent<Text>();
            so.FindProperty("_loadSaveButton").objectReferenceValue = loadSave.GetComponent<Button>();
            so.FindProperty("_settingsButton").objectReferenceValue = settings.GetComponent<Button>();
            so.FindProperty("_creditsButton").objectReferenceValue = credits.GetComponent<Button>();
            so.FindProperty("_versionText").objectReferenceValue = version;
            so.ApplyModifiedPropertiesWithoutUndo();

            root.SetActive(true);

            var prefabRoot = Object.Instantiate(root);
            prefabRoot.name = "TitleMenuPanel";
            PrefabUtility.SaveAsPrefabAsset(prefabRoot, TitleMenuPrefabPath);
            Object.DestroyImmediate(prefabRoot);

            return view;
        }

        private static TitleSettingsPanelView BuildTitleSettingsPanel(Transform parent)
        {
            var existing = parent.Find("TitleSettingsPanel");
            if (existing != null)
            {
                Object.DestroyImmediate(existing.gameObject);
            }

            var root = CreatePanel(parent, "TitleSettingsPanel", new Color(0f, 0f, 0f, 0.55f));
            StretchFull(root.GetComponent<RectTransform>());
            var canvas = root.AddComponent<Canvas>();
            canvas.overrideSorting = true;
            canvas.sortingOrder = InSaveModalSortingOrder;
            root.AddComponent<GraphicRaycaster>();

            var box = CreatePanel(root.transform, "Box", new Color(0.16f, 0.18f, 0.22f, 1f));
            Place(box.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(640f, 620f));

            var title = CreateText(box.transform, "Title", "设置", 28, TextAnchor.MiddleCenter);
            Place(title.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -16f), new Vector2(560f, 40f));

            var displayTabBtn = CreateButton(box.transform, "DisplayTabButton", "显示", new Color(0.35f, 0.55f, 0.38f, 1f));
            Place(displayTabBtn.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(24f, -64f), new Vector2(120f, 40f));
            Object.DestroyImmediate(displayTabBtn.GetComponent<LayoutElement>());

            var displayRoot = CreatePanel(box.transform, "DisplayTab", new Color(0.12f, 0.13f, 0.16f, 1f));
            Place(displayRoot.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, -10f), new Vector2(580f, 420f));

            var resLabel = CreateText(displayRoot.transform, "ResolutionLabel", "分辨率", 22, TextAnchor.MiddleLeft);
            Place(resLabel.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(16f, -12f), new Vector2(200f, 32f));

            var scrollGo = new GameObject("ResolutionScroll", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
            scrollGo.transform.SetParent(displayRoot.transform, false);
            var scrollRt = scrollGo.GetComponent<RectTransform>();
            Place(scrollRt, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -48f), new Vector2(548f, 220f));
            scrollGo.GetComponent<Image>().color = new Color(0.10f, 0.11f, 0.14f, 1f);

            var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
            viewport.transform.SetParent(scrollGo.transform, false);
            StretchFull(viewport.GetComponent<RectTransform>());
            viewport.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.02f);
            viewport.GetComponent<Mask>().showMaskGraphic = false;

            var content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            content.transform.SetParent(viewport.transform, false);
            var contentRt = content.GetComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0f, 1f);
            contentRt.anchorMax = new Vector2(1f, 1f);
            contentRt.pivot = new Vector2(0.5f, 1f);
            contentRt.anchoredPosition = Vector2.zero;
            contentRt.sizeDelta = Vector2.zero;
            var vlg = content.GetComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(10, 10, 10, 10);
            vlg.spacing = 8f;
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            var fitter = content.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var scroll = scrollGo.GetComponent<ScrollRect>();
            scroll.viewport = viewport.GetComponent<RectTransform>();
            scroll.content = contentRt;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;

            var rowTemplate = CreateButton(content.transform, "ResolutionRowTemplate", "1920 × 1080",
                new Color(0.22f, 0.28f, 0.36f, 1f));
            var rowLe = rowTemplate.GetComponent<LayoutElement>();
            rowLe.minHeight = 40f;
            rowLe.preferredHeight = 40f;
            rowTemplate.SetActive(false);

            var modeLabel = CreateText(displayRoot.transform, "ModeLabel", "显示模式", 22, TextAnchor.MiddleLeft);
            Place(modeLabel.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f),
                new Vector2(16f, 120f), new Vector2(200f, 32f));

            var modeWindowed = CreateButton(displayRoot.transform, "ModeWindowedButton", "窗口",
                new Color(0.28f, 0.38f, 0.52f, 1f));
            Place(modeWindowed.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f),
                new Vector2(16f, 64f), new Vector2(170f, 44f));
            Object.DestroyImmediate(modeWindowed.GetComponent<LayoutElement>());

            var modeBorderless = CreateButton(displayRoot.transform, "ModeBorderlessButton", "无边框全屏",
                new Color(0.28f, 0.38f, 0.52f, 1f));
            Place(modeBorderless.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 64f), new Vector2(170f, 44f));
            Object.DestroyImmediate(modeBorderless.GetComponent<LayoutElement>());

            var modeExclusive = CreateButton(displayRoot.transform, "ModeExclusiveButton", "独占全屏",
                new Color(0.28f, 0.38f, 0.52f, 1f));
            Place(modeExclusive.GetComponent<RectTransform>(), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f),
                new Vector2(-16f, 64f), new Vector2(170f, 44f));
            Object.DestroyImmediate(modeExclusive.GetComponent<LayoutElement>());

            var apply = CreateButton(displayRoot.transform, "ApplyButton", "应用", new Color(0.25f, 0.55f, 0.35f, 1f));
            Place(apply.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 12f), new Vector2(160f, 44f));
            Object.DestroyImmediate(apply.GetComponent<LayoutElement>());

            var displayTab = displayRoot.AddComponent<DisplaySettingsTabView>();
            var displaySo = new SerializedObject(displayTab);
            displaySo.FindProperty("_root").objectReferenceValue = displayRoot;
            displaySo.FindProperty("_resolutionListContent").objectReferenceValue = content.transform;
            displaySo.FindProperty("_resolutionRowTemplate").objectReferenceValue = rowTemplate;
            displaySo.FindProperty("_modeWindowedButton").objectReferenceValue = modeWindowed.GetComponent<Button>();
            displaySo.FindProperty("_modeBorderlessButton").objectReferenceValue = modeBorderless.GetComponent<Button>();
            displaySo.FindProperty("_modeExclusiveButton").objectReferenceValue = modeExclusive.GetComponent<Button>();
            displaySo.FindProperty("_applyButton").objectReferenceValue = apply.GetComponent<Button>();
            displaySo.ApplyModifiedPropertiesWithoutUndo();

            var close = CreateButton(box.transform, "CloseButton", "关闭", new Color(0.40f, 0.40f, 0.42f, 1f));
            Place(close.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 16f), new Vector2(180f, 44f));
            Object.DestroyImmediate(close.GetComponent<LayoutElement>());

            var view = root.AddComponent<TitleSettingsPanelView>();
            var so = new SerializedObject(view);
            so.FindProperty("_root").objectReferenceValue = root;
            so.FindProperty("_displayTabButton").objectReferenceValue = displayTabBtn.GetComponent<Button>();
            so.FindProperty("_displayTab").objectReferenceValue = displayTab;
            so.FindProperty("_closeButton").objectReferenceValue = close.GetComponent<Button>();
            so.ApplyModifiedPropertiesWithoutUndo();

            root.SetActive(false);

            var prefabRoot = Object.Instantiate(root);
            prefabRoot.name = "TitleSettingsPanel";
            PrefabUtility.SaveAsPrefabAsset(prefabRoot, TitleSettingsPrefabPath);
            Object.DestroyImmediate(prefabRoot);

            return view;
        }

        private static void PatchSaveSelectPanel(SaveSelectView saveSelect)
        {
            var panel = saveSelect.transform;
            var panelImage = panel.GetComponent<Image>();
            if (panelImage != null)
            {
                Object.DestroyImmediate(panelImage);
            }

            var back = panel.Find("BackButton");
            Button backButton;
            if (back == null)
            {
                var backGo = CreateButton(panel, "BackButton", "返回", new Color(0.35f, 0.38f, 0.42f, 1f));
                var backRt = backGo.GetComponent<RectTransform>();
                Place(backRt, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                    new Vector2(24f, -24f), new Vector2(120f, 48f));
                backButton = backGo.GetComponent<Button>();
            }
            else
            {
                backButton = back.GetComponent<Button>();
            }

            var so = new SerializedObject(saveSelect);
            so.FindProperty("_root").objectReferenceValue = panel.gameObject;
            so.FindProperty("_backButton").objectReferenceValue = backButton;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static GameObject CreateCanvas(Transform parent, string name, int sortingOrder = 0)
        {
            var go = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            go.transform.SetParent(parent, false);
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            return go;
        }

        private static SaveSelectView BuildSaveSelect(Transform parent)
        {
            var root = CreateContentRoot(parent, "SaveSelectPanel");
            StretchFull(root.GetComponent<RectTransform>());

            var backGo = CreateButton(root.transform, "BackButton", "返回", new Color(0.35f, 0.38f, 0.42f, 1f));
            var backRt = backGo.GetComponent<RectTransform>();
            Place(backRt, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(24f, -24f), new Vector2(120f, 48f));

            var title = CreateText(root.transform, "Title", "存档选择", 42, TextAnchor.MiddleCenter);
            var titleRt = title.GetComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0.5f, 1f);
            titleRt.anchorMax = new Vector2(0.5f, 1f);
            titleRt.pivot = new Vector2(0.5f, 1f);
            titleRt.anchoredPosition = new Vector2(0f, -40f);
            titleRt.sizeDelta = new Vector2(600f, 60f);

            var slotsParent = new GameObject("Slots", typeof(RectTransform));
            slotsParent.transform.SetParent(root.transform, false);
            var slotsRt = slotsParent.GetComponent<RectTransform>();
            slotsRt.anchorMin = new Vector2(0.5f, 0.5f);
            slotsRt.anchorMax = new Vector2(0.5f, 0.5f);
            slotsRt.sizeDelta = new Vector2(900f, 420f);
            slotsRt.anchoredPosition = Vector2.zero;

            var layout = slotsParent.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 24f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;

            var slotViews = new SaveSlotView[3];
            for (var i = 0; i < 3; i++)
            {
                slotViews[i] = BuildSaveSlot(slotsParent.transform, i);
            }

            var view = root.AddComponent<SaveSelectView>();
            var so = new SerializedObject(view);
            so.FindProperty("_root").objectReferenceValue = root;
            so.FindProperty("_backButton").objectReferenceValue = backGo.GetComponent<Button>();
            var slotsProp = so.FindProperty("_slotViews");
            slotsProp.arraySize = 3;
            for (var i = 0; i < 3; i++)
            {
                slotsProp.GetArrayElementAtIndex(i).objectReferenceValue = slotViews[i];
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            root.SetActive(false);
            return view;
        }

        private static GameObject CreateContentRoot(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        private static SaveSlotView BuildSaveSlot(Transform parent, int index)
        {
            var go = CreatePanel(parent, $"SaveSlot_{index}", new Color(0.22f, 0.26f, 0.32f, 1f));
            var vlg = go.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(16, 16, 16, 16);
            vlg.spacing = 12f;
            vlg.childAlignment = TextAnchor.MiddleCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true;

            var title = CreateText(go.transform, "Title", $"存档槽 {index + 1}", 28, TextAnchor.MiddleCenter);
            var status = CreateText(go.transform, "Status", "状态：空", 22, TextAnchor.MiddleCenter);
            var primary = CreateButton(go.transform, "PrimaryButton", "新建", new Color(0.25f, 0.55f, 0.35f, 1f));
            var delete = CreateButton(go.transform, "DeleteButton", "删除", new Color(0.65f, 0.28f, 0.28f, 1f));

            var view = go.AddComponent<SaveSlotView>();
            var so = new SerializedObject(view);
            so.FindProperty("_slotIndex").intValue = index;
            so.FindProperty("_titleText").objectReferenceValue = title;
            so.FindProperty("_statusText").objectReferenceValue = status;
            so.FindProperty("_primaryButton").objectReferenceValue = primary.GetComponent<Button>();
            so.FindProperty("_deleteButton").objectReferenceValue = delete.GetComponent<Button>();
            so.FindProperty("_primaryButtonLabel").objectReferenceValue = primary.GetComponentInChildren<Text>();
            so.ApplyModifiedPropertiesWithoutUndo();
            return view;
        }

        private static InSaveShellView BuildInSaveShell(Transform parent)
        {
            var root = CreatePanel(parent, "InSaveShellPanel", new Color(0.10f, 0.12f, 0.16f, 0.96f));
            StretchFull(root.GetComponent<RectTransform>());

            var slotLabel = CreateText(root.transform, "SlotLabel", "进档壳", 30, TextAnchor.UpperLeft);
            Place(slotLabel.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(24f, -20f), new Vector2(480f, 40f));

            var stateLabel = CreateText(root.transform, "StateLabel", "当前玩法：挖坟（Dig）", 26, TextAnchor.MiddleCenter);
            Place(stateLabel.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -16f), new Vector2(900f, 40f));

            var stageInfo = CreateText(root.transform, "StageInfoLabel", "关卡：未运行", 20, TextAnchor.MiddleCenter);
            Place(stageInfo.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -52f), new Vector2(1100f, 36f));

            var dig = CreatePlaceholderPanel(root.transform, "DigPlaceholder", "挖坟占位（Dig）", new Color(0.20f, 0.35f, 0.28f, 0.9f));
            var um = CreatePlaceholderPanel(root.transform, "UpgradeManufacturePlaceholder", "升级与制造占位（UpgradeManufacture）", new Color(0.28f, 0.28f, 0.42f, 0.9f));
            var defend = CreatePlaceholderPanel(root.transform, "DefendPlaceholder", "防守占位（Defend）", new Color(0.42f, 0.26f, 0.22f, 0.9f));

            var placeholder = root.AddComponent<GameplayStatePlaceholderView>();
            var pso = new SerializedObject(placeholder);
            pso.FindProperty("_digPanel").objectReferenceValue = dig;
            pso.FindProperty("_upgradeManufacturePanel").objectReferenceValue = um;
            pso.FindProperty("_defendPanel").objectReferenceValue = defend;
            pso.FindProperty("_stateLabel").objectReferenceValue = stateLabel;
            pso.FindProperty("_stageInfoLabel").objectReferenceValue = stageInfo;
            pso.ApplyModifiedPropertiesWithoutUndo();

            var toolsBtn = CreateButton(root.transform, "ToolsButton", "工具", new Color(0.30f, 0.45f, 0.70f, 1f));
            Place(toolsBtn.GetComponent<RectTransform>(), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-24f, -20f), new Vector2(140f, 48f));

            var backBtn = CreateButton(root.transform, "BackButton", "返回存档", new Color(0.35f, 0.35f, 0.40f, 1f));
            Place(backBtn.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(24f, 24f), new Vector2(160f, 48f));

            PatchInSaveEquipMagicBookButtons(root.transform, out var equipmentBtn, out var magicBookBtn);

            var debugBtn = CreateButton(root.transform, "DebugCycleStateButton", "Debug：切下一态", new Color(0.55f, 0.45f, 0.20f, 1f));
            Place(debugBtn.GetComponent<RectTransform>(), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-24f, 24f), new Vector2(200f, 48f));

            var advanceBtn = CreateButton(root.transform, "DebugAdvanceStageButton", "Debug：推进阶段", new Color(0.45f, 0.50f, 0.25f, 1f));
            Place(advanceBtn.GetComponent<RectTransform>(), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-240f, 24f), new Vector2(200f, 48f));

            var toolsPanel = BuildToolsPanel(root.transform);
            var levelSelect = BuildLevelSelectPanel(root.transform);
            var gmGrant = BuildGmGrantListPanel(root.transform);
            var equipmentWarehouse = BuildEquipmentWarehousePanel(root.transform);
            var magicBookSlots = BuildMagicBookSlotsPanel(root.transform);

            var view = root.AddComponent<InSaveShellView>();
            var so = new SerializedObject(view);
            so.FindProperty("_root").objectReferenceValue = root;
            so.FindProperty("_backdropImage").objectReferenceValue = root.GetComponent<Image>();
            so.FindProperty("_slotLabel").objectReferenceValue = slotLabel;
            so.FindProperty("_toolsButton").objectReferenceValue = toolsBtn.GetComponent<Button>();
            so.FindProperty("_backToSaveSelectButton").objectReferenceValue = backBtn.GetComponent<Button>();
            so.FindProperty("_equipmentButton").objectReferenceValue = equipmentBtn.GetComponent<Button>();
            so.FindProperty("_magicBookButton").objectReferenceValue = magicBookBtn.GetComponent<Button>();
            so.FindProperty("_debugCycleStateButton").objectReferenceValue = debugBtn.GetComponent<Button>();
            so.FindProperty("_debugAdvanceStageButton").objectReferenceValue = advanceBtn.GetComponent<Button>();
            so.FindProperty("_toolsPanel").objectReferenceValue = toolsPanel;
            so.FindProperty("_levelSelectPanel").objectReferenceValue = levelSelect;
            so.FindProperty("_gmGrantListPanel").objectReferenceValue = gmGrant;
            so.FindProperty("_equipmentWarehousePanel").objectReferenceValue = equipmentWarehouse;
            so.FindProperty("_magicBookSlotsPanel").objectReferenceValue = magicBookSlots;
            so.FindProperty("_placeholderView").objectReferenceValue = placeholder;
            so.ApplyModifiedPropertiesWithoutUndo();

            PatchDifficultySelectHost(view);

            root.SetActive(false);

            var standalone = Object.Instantiate(root);
            standalone.name = "InSaveShellPanel";
            PrefabUtility.SaveAsPrefabAsset(standalone, InSaveShellPrefabPath);
            Object.DestroyImmediate(standalone);

            return view;
        }

        private static LevelSelectPanelView BuildLevelSelectPanel(Transform parent)
        {
            var root = CreatePanel(parent, "LevelSelectPanel", new Color(0f, 0f, 0f, 0.55f));
            StretchFull(root.GetComponent<RectTransform>());

            var box = CreatePanel(root.transform, "Box", new Color(0.16f, 0.18f, 0.22f, 1f));
            Place(box.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(520f, 560f));

            var title = CreateText(box.transform, "Title", "关卡选择", 28, TextAnchor.MiddleCenter);
            Place(title.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -16f), new Vector2(460f, 40f));

            var scrollGo = new GameObject("LevelScroll", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
            scrollGo.transform.SetParent(box.transform, false);
            var scrollRt = scrollGo.GetComponent<RectTransform>();
            Place(scrollRt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 10f), new Vector2(460f, 400f));
            scrollGo.GetComponent<Image>().color = new Color(0.10f, 0.11f, 0.14f, 1f);

            var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
            viewport.transform.SetParent(scrollGo.transform, false);
            StretchFull(viewport.GetComponent<RectTransform>());
            viewport.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.02f);
            viewport.GetComponent<Mask>().showMaskGraphic = false;

            var content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            content.transform.SetParent(viewport.transform, false);
            var contentRt = content.GetComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0f, 1f);
            contentRt.anchorMax = new Vector2(1f, 1f);
            contentRt.pivot = new Vector2(0.5f, 1f);
            contentRt.anchoredPosition = Vector2.zero;
            contentRt.sizeDelta = new Vector2(0f, 0f);
            var vlg = content.GetComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(12, 12, 12, 12);
            vlg.spacing = 10f;
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            var fitter = content.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var scroll = scrollGo.GetComponent<ScrollRect>();
            scroll.viewport = viewport.GetComponent<RectTransform>();
            scroll.content = contentRt;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;

            var rowTemplate = CreateButton(content.transform, "LevelRowTemplate", "Level_XX", new Color(0.28f, 0.38f, 0.52f, 1f));
            var rowLe = rowTemplate.GetComponent<LayoutElement>();
            if (rowLe == null)
            {
                rowLe = rowTemplate.AddComponent<LayoutElement>();
            }

            rowLe.minHeight = 48f;
            rowLe.preferredHeight = 48f;
            rowTemplate.SetActive(false);

            var emptyHint = CreateText(box.transform, "EmptyHint", "当前模式无可用关卡", 22, TextAnchor.MiddleCenter);
            Place(emptyHint.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 10f), new Vector2(420f, 60f));
            emptyHint.gameObject.SetActive(false);

            var close = CreateButton(box.transform, "CloseButton", "关闭", new Color(0.40f, 0.40f, 0.42f, 1f));
            Place(close.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 20f), new Vector2(180f, 44f));

            var enter = CreateButton(box.transform, "EnterButton", "进入", new Color(0.25f, 0.45f, 0.85f, 1f));
            Place(enter.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 20f), new Vector2(200f, 48f));
            enter.SetActive(false);

            var view = root.AddComponent<LevelSelectPanelView>();
            var so = new SerializedObject(view);
            so.FindProperty("_root").objectReferenceValue = root;
            so.FindProperty("_titleText").objectReferenceValue = title;
            so.FindProperty("_levelListContent").objectReferenceValue = content.transform;
            so.FindProperty("_levelRowTemplate").objectReferenceValue = rowTemplate;
            so.FindProperty("_closeButton").objectReferenceValue = close.GetComponent<Button>();
            so.FindProperty("_enterButton").objectReferenceValue = enter.GetComponent<Button>();
            so.FindProperty("_emptyHintText").objectReferenceValue = emptyHint;
            so.FindProperty("_backdropImage").objectReferenceValue = root.GetComponent<Image>();
            so.FindProperty("_hubEmbedded").boolValue = false;
            so.ApplyModifiedPropertiesWithoutUndo();
            root.SetActive(false);
            return view;
        }

        private static void PatchInSaveEquipMagicBookButtons(
            Transform inSaveRoot,
            out GameObject equipmentBtn,
            out GameObject magicBookBtn)
        {
            var backBtn = inSaveRoot.Find("BackButton");
            if (backBtn != null)
            {
                Place(
                    backBtn.GetComponent<RectTransform>(),
                    new Vector2(0f, 0f),
                    new Vector2(0f, 0f),
                    new Vector2(0f, 0f),
                    new Vector2(24f, 24f),
                    new Vector2(160f, 48f));
            }

            equipmentBtn = inSaveRoot.Find("EquipmentButton")?.gameObject;
            if (equipmentBtn == null)
            {
                equipmentBtn = CreateButton(
                    inSaveRoot,
                    "EquipmentButton",
                    "装备",
                    new Color(0.30f, 0.48f, 0.42f, 1f));
            }

            Place(
                equipmentBtn.GetComponent<RectTransform>(),
                new Vector2(0f, 0f),
                new Vector2(0f, 0f),
                new Vector2(0f, 0f),
                new Vector2(24f, 136f),
                new Vector2(160f, 48f));

            magicBookBtn = inSaveRoot.Find("MagicBookButton")?.gameObject;
            if (magicBookBtn == null)
            {
                magicBookBtn = CreateButton(
                    inSaveRoot,
                    "MagicBookButton",
                    "魔法书",
                    new Color(0.38f, 0.36f, 0.52f, 1f));
            }

            Place(
                magicBookBtn.GetComponent<RectTransform>(),
                new Vector2(0f, 0f),
                new Vector2(0f, 0f),
                new Vector2(0f, 0f),
                new Vector2(24f, 80f),
                new Vector2(160f, 48f));
        }

        private static void ApplyModalCanvasSorting(GameObject root)
        {
            var canvas = root.GetComponent<Canvas>();
            if (canvas == null)
            {
                canvas = root.AddComponent<Canvas>();
            }

            canvas.overrideSorting = true;
            canvas.sortingOrder = InSaveModalSortingOrder;
            if (root.GetComponent<GraphicRaycaster>() == null)
            {
                root.AddComponent<GraphicRaycaster>();
            }
        }

        private static EquipmentWarehousePanelView BuildEquipmentWarehousePanel(Transform parent)
        {
            var root = CreatePanel(parent, "EquipmentWarehousePanel", new Color(0f, 0f, 0f, 0.55f));
            StretchFull(root.GetComponent<RectTransform>());
            ApplyModalCanvasSorting(root);

            var box = CreatePanel(root.transform, "Box", new Color(0.16f, 0.18f, 0.22f, 1f));
            Place(box.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(520f, 560f));

            var title = CreateText(box.transform, "Title", "装备", 28, TextAnchor.MiddleCenter);
            Place(title.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -16f), new Vector2(460f, 40f));

            BuildEquipWarehouseListOnBox(box.transform, out var listContent, out var rowTemplate, out var emptyHint);

            var close = CreateButton(box.transform, "CloseButton", "关闭", new Color(0.40f, 0.40f, 0.42f, 1f));
            Place(close.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 20f), new Vector2(180f, 44f));

            var view = root.AddComponent<EquipmentWarehousePanelView>();
            WireEquipmentWarehousePanel(view, root, title, close.GetComponent<Button>(), listContent, rowTemplate, emptyHint);
            root.SetActive(false);
            return view;
        }

        private static void PatchEquipmentWarehouseList(EquipmentWarehousePanelView view)
        {
            if (view == null)
            {
                return;
            }

            var box = view.transform.Find("Box");
            if (box == null)
            {
                return;
            }

            var so = new SerializedObject(view);
            var listProp = so.FindProperty("_listContent");
            var rowProp = so.FindProperty("_rowTemplate");
            var emptyProp = so.FindProperty("_emptyHintText");
            var alreadyWired = listProp != null && listProp.objectReferenceValue != null
                               && rowProp != null && rowProp.objectReferenceValue != null
                               && emptyProp != null && emptyProp.objectReferenceValue != null
                               && box.Find("EquipScroll") != null;
            if (alreadyWired)
            {
                return;
            }

            BuildEquipWarehouseListOnBox(box, out var listContent, out var rowTemplate, out var emptyHint);
            if (listProp != null)
            {
                listProp.objectReferenceValue = listContent;
            }

            if (rowProp != null)
            {
                rowProp.objectReferenceValue = rowTemplate;
            }

            if (emptyProp != null)
            {
                emptyProp.objectReferenceValue = emptyHint;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void BuildEquipWarehouseListOnBox(
            Transform box,
            out Transform listContent,
            out GameObject rowTemplate,
            out Text emptyHint)
        {
            var existingScroll = box.Find("EquipScroll");
            if (existingScroll != null)
            {
                Object.DestroyImmediate(existingScroll.gameObject);
            }

            var existingEmpty = box.Find("EmptyHint");
            if (existingEmpty != null)
            {
                Object.DestroyImmediate(existingEmpty.gameObject);
            }

            var scrollGo = new GameObject("EquipScroll", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
            scrollGo.transform.SetParent(box, false);
            var scrollRt = scrollGo.GetComponent<RectTransform>();
            Place(scrollRt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 10f), new Vector2(460f, 400f));
            scrollGo.GetComponent<Image>().color = new Color(0.10f, 0.11f, 0.14f, 1f);

            var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
            viewport.transform.SetParent(scrollGo.transform, false);
            StretchFull(viewport.GetComponent<RectTransform>());
            viewport.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.02f);
            viewport.GetComponent<Mask>().showMaskGraphic = false;

            var content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            content.transform.SetParent(viewport.transform, false);
            var contentRt = content.GetComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0f, 1f);
            contentRt.anchorMax = new Vector2(1f, 1f);
            contentRt.pivot = new Vector2(0.5f, 1f);
            contentRt.anchoredPosition = Vector2.zero;
            contentRt.sizeDelta = new Vector2(0f, 0f);
            var vlg = content.GetComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(12, 12, 12, 12);
            vlg.spacing = 10f;
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            var fitter = content.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var scroll = scrollGo.GetComponent<ScrollRect>();
            scroll.viewport = viewport.GetComponent<RectTransform>();
            scroll.content = contentRt;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;

            rowTemplate = CreateEquipRowTemplate(content.transform);
            listContent = content.transform;

            emptyHint = CreateText(box, "EmptyHint", "尚未拥有装备", 22, TextAnchor.MiddleCenter);
            Place(emptyHint.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 10f), new Vector2(420f, 60f));
            emptyHint.gameObject.SetActive(false);
        }

        private static GameObject CreateEquipRowTemplate(Transform content)
        {
            var go = CreatePanel(content, "EquipRowTemplate", new Color(0.28f, 0.38f, 0.52f, 1f));
            var le = go.GetComponent<LayoutElement>();
            if (le == null)
            {
                le = go.AddComponent<LayoutElement>();
            }

            le.minHeight = 96f;
            le.preferredHeight = 96f;

            var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            iconGo.transform.SetParent(go.transform, false);
            Place(iconGo.GetComponent<RectTransform>(), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(12f, 0f), new Vector2(64f, 64f));
            var icon = iconGo.GetComponent<Image>();
            icon.color = Color.white;
            icon.preserveAspect = true;
            icon.raycastTarget = false;

            var title = CreateText(go.transform, "Title", "Name Lv.1", 20, TextAnchor.MiddleLeft);
            StretchFull(title.GetComponent<RectTransform>());
            title.GetComponent<RectTransform>().offsetMin = new Vector2(84f, 56f);
            title.GetComponent<RectTransform>().offsetMax = new Vector2(-12f, -8f);
            title.horizontalOverflow = HorizontalWrapMode.Overflow;
            title.raycastTarget = false;

            var desc = CreateText(go.transform, "Description", "Description", 16, TextAnchor.UpperLeft);
            StretchFull(desc.GetComponent<RectTransform>());
            desc.GetComponent<RectTransform>().offsetMin = new Vector2(84f, 8f);
            desc.GetComponent<RectTransform>().offsetMax = new Vector2(-12f, -40f);
            desc.horizontalOverflow = HorizontalWrapMode.Wrap;
            desc.verticalOverflow = VerticalWrapMode.Overflow;
            desc.raycastTarget = false;

            go.SetActive(false);
            return go;
        }

        private static void WireEquipmentWarehousePanel(
            EquipmentWarehousePanelView view,
            GameObject root,
            Text title,
            Button close,
            Transform listContent,
            GameObject rowTemplate,
            Text emptyHint)
        {
            var so = new SerializedObject(view);
            so.FindProperty("_root").objectReferenceValue = root;
            so.FindProperty("_titleText").objectReferenceValue = title;
            so.FindProperty("_closeButton").objectReferenceValue = close;
            so.FindProperty("_listContent").objectReferenceValue = listContent;
            so.FindProperty("_rowTemplate").objectReferenceValue = rowTemplate;
            so.FindProperty("_emptyHintText").objectReferenceValue = emptyHint;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static MagicBookSlotsPanelView BuildMagicBookSlotsPanel(Transform parent)
        {
            var root = CreatePanel(parent, "MagicBookSlotsPanel", new Color(0f, 0f, 0f, 0.55f));
            StretchFull(root.GetComponent<RectTransform>());
            ApplyModalCanvasSorting(root);

            var box = CreatePanel(root.transform, "Box", new Color(0.16f, 0.18f, 0.22f, 1f));
            Place(box.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(800f, 400f));

            var title = CreateText(box.transform, "Title", "魔法书", 28, TextAnchor.MiddleCenter);
            Place(title.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -16f), new Vector2(740f, 40f));

            var host = new GameObject("BookRowHost", typeof(RectTransform));
            host.transform.SetParent(box.transform, false);
            Place(
                host.GetComponent<RectTransform>(),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0f, 10f),
                new Vector2(BookRowView.RowWidth, AutoMfgMagicBookSlotView.SlotHeight));

            var close = CreateButton(box.transform, "CloseButton", "关闭", new Color(0.40f, 0.40f, 0.42f, 1f));
            Place(close.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 20f), new Vector2(180f, 44f));

            var view = root.AddComponent<MagicBookSlotsPanelView>();
            EnsureMagicBookDeleteButton(box.transform, view);
            BookRowView nestedRow = null;
            var bookRowPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(AmAssetBuilder.BookRowPrefabPath);
            if (bookRowPrefab != null)
            {
                nestedRow = NestBookRowUnderHost(host.transform, bookRowPrefab);
            }

            var so = new SerializedObject(view);
            so.FindProperty("_root").objectReferenceValue = root;
            so.FindProperty("_titleText").objectReferenceValue = title;
            so.FindProperty("_closeButton").objectReferenceValue = close.GetComponent<Button>();
            so.FindProperty("_bookRowHost").objectReferenceValue = host.transform;
            var bookRowProp = so.FindProperty("_bookRow");
            if (bookRowProp != null)
            {
                bookRowProp.objectReferenceValue = nestedRow;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            root.SetActive(false);
            return view;
        }

        [MenuItem("Gravedigger2026/Meta/Ensure MagicBook DeleteButton (UI-023 / D-072)")]
        public static void EnsureMagicBookDeleteButtonMenu()
        {
            EnsureInSaveEquipMagicBookPanelsOnExistingPrefab();
        }

        private static void EnsureMagicBookDeleteButton(Transform box, MagicBookSlotsPanelView view)
        {
            if (box == null || view == null)
            {
                return;
            }

            var existing = box.Find("DeleteButton");
            GameObject deleteGo;
            if (existing != null)
            {
                deleteGo = existing.gameObject;
            }
            else
            {
                deleteGo = CreateButton(box, "DeleteButton", "删除", new Color(0.65f, 0.28f, 0.28f, 1f));
                Place(
                    deleteGo.GetComponent<RectTransform>(),
                    new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f),
                    Vector2.zero,
                    new Vector2(120f, 36f));
            }

            deleteGo.SetActive(false);

            var so = new SerializedObject(view);
            var deleteBtnProp = so.FindProperty("_deleteButton");
            if (deleteBtnProp != null)
            {
                deleteBtnProp.objectReferenceValue = deleteGo.GetComponent<Button>();
            }

            var deleteRtProp = so.FindProperty("_deleteButtonRect");
            if (deleteRtProp != null)
            {
                deleteRtProp.objectReferenceValue = deleteGo.GetComponent<RectTransform>();
            }

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void NestBookRowIntoMagicPanel(MagicBookSlotsPanelView panel, GameObject bookRowPrefab)
        {
            if (panel == null || bookRowPrefab == null)
            {
                return;
            }

            var so = new SerializedObject(panel);
            var hostProp = so.FindProperty("_bookRowHost");
            Transform host = hostProp != null ? hostProp.objectReferenceValue as Transform : null;
            if (host == null)
            {
                var box = panel.transform.Find("Box");
                host = box != null ? box.Find("BookRowHost") : null;
            }

            if (host == null)
            {
                return;
            }

            var hostRt = host as RectTransform;
            if (hostRt != null)
            {
                hostRt.sizeDelta = new Vector2(BookRowView.RowWidth, AutoMfgMagicBookSlotView.SlotHeight);
            }

            var nested = NestBookRowUnderHost(host, bookRowPrefab);
            if (hostProp != null)
            {
                hostProp.objectReferenceValue = host;
            }

            var bookRowProp = so.FindProperty("_bookRow");
            if (bookRowProp != null)
            {
                bookRowProp.objectReferenceValue = nested;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static BookRowView NestBookRowUnderHost(Transform host, GameObject bookRowPrefab)
        {
            var existing = host.GetComponentInChildren<BookRowView>(true);
            if (existing != null)
            {
                var source = PrefabUtility.GetCorrespondingObjectFromSource(existing.gameObject);
                if (source == bookRowPrefab)
                {
                    PlaceNestedBookRow(existing.GetComponent<RectTransform>());
                    return existing;
                }

                Object.DestroyImmediate(existing.gameObject);
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(bookRowPrefab, host);
            instance.name = "BookRow";
            var rt = instance.GetComponent<RectTransform>();
            PlaceNestedBookRow(rt);
            return instance.GetComponent<BookRowView>();
        }

        private static void PlaceNestedBookRow(RectTransform rt)
        {
            if (rt == null)
            {
                return;
            }

            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(BookRowView.RowWidth, AutoMfgMagicBookSlotView.SlotHeight);
        }

        private static GameObject CreatePlaceholderPanel(Transform parent, string name, string label, Color color)
        {
            var go = CreatePanel(parent, name, color);
            Place(go.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -20f), new Vector2(720f, 280f));
            CreateText(go.transform, "Label", label, 32, TextAnchor.MiddleCenter);
            go.SetActive(false);
            return go;
        }

        private static ToolsPanelView BuildToolsPanel(Transform parent)
        {
            var go = CreatePanel(parent, "ToolsPanel", new Color(0.08f, 0.09f, 0.12f, 0.95f));
            Place(go.GetComponent<RectTransform>(), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-24f, -80f), new Vector2(280f, 430f));

            var title = CreateText(go.transform, "Title", "工具面板", 24, TextAnchor.UpperCenter);
            Place(title.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -12f), new Vector2(240f, 36f));

            var settings = CreateButton(go.transform, "SettingsButton", "设置", new Color(0.30f, 0.40f, 0.55f, 1f));
            Place(settings.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -60f), new Vector2(200f, 44f));

            var level = CreateButton(go.transform, "LevelButton", "关卡", new Color(0.30f, 0.40f, 0.55f, 1f));
            Place(level.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -112f), new Vector2(200f, 44f));

            var grantEquip = CreateButton(go.transform, "GrantProtagonistEquipmentButton", "增加主角装备", new Color(0.30f, 0.48f, 0.42f, 1f));
            Place(grantEquip.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -164f), new Vector2(200f, 44f));

            var grantBook = CreateButton(go.transform, "GrantMagicBookButton", "增加魔法书", new Color(0.38f, 0.36f, 0.52f, 1f));
            Place(grantBook.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -216f), new Vector2(200f, 44f));

            var grantSoldier = CreateButton(go.transform, "GrantAddSoldierButton", "添加士兵", new Color(0.48f, 0.40f, 0.28f, 1f));
            Place(grantSoldier.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -268f), new Vector2(200f, 44f));

            var close = CreateButton(go.transform, "CloseButton", "关闭", new Color(0.40f, 0.40f, 0.42f, 1f));
            Place(close.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -324f), new Vector2(200f, 40f));

            var view = go.AddComponent<ToolsPanelView>();
            WireToolsPanelView(view, go, settings, level, grantEquip, grantBook, grantSoldier, close);
            go.SetActive(false);
            return view;
        }

        private static void PatchToolsPanelGrantButtons(ToolsPanelView view)
        {
            var go = view.gameObject;
            Place(go.GetComponent<RectTransform>(), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-24f, -80f), new Vector2(280f, 430f));

            var settings = go.transform.Find("SettingsButton")?.gameObject;
            var level = go.transform.Find("LevelButton")?.gameObject;
            var close = go.transform.Find("CloseButton")?.gameObject;
            var grantEquip = go.transform.Find("GrantProtagonistEquipmentButton")?.gameObject;
            var grantBook = go.transform.Find("GrantMagicBookButton")?.gameObject;
            var grantSoldier = go.transform.Find("GrantAddSoldierButton")?.gameObject;

            if (settings != null)
            {
                Place(settings.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -60f), new Vector2(200f, 44f));
            }

            if (level != null)
            {
                Place(level.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -112f), new Vector2(200f, 44f));
            }

            if (grantEquip == null)
            {
                grantEquip = CreateButton(go.transform, "GrantProtagonistEquipmentButton", "增加主角装备", new Color(0.30f, 0.48f, 0.42f, 1f));
            }

            Place(grantEquip.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -164f), new Vector2(200f, 44f));

            if (grantBook == null)
            {
                grantBook = CreateButton(go.transform, "GrantMagicBookButton", "增加魔法书", new Color(0.38f, 0.36f, 0.52f, 1f));
            }

            Place(grantBook.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -216f), new Vector2(200f, 44f));

            if (grantSoldier == null)
            {
                grantSoldier = CreateButton(go.transform, "GrantAddSoldierButton", "添加士兵", new Color(0.48f, 0.40f, 0.28f, 1f));
            }

            Place(grantSoldier.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -268f), new Vector2(200f, 44f));

            if (close != null)
            {
                Place(close.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -324f), new Vector2(200f, 40f));
            }

            WireToolsPanelView(view, go, settings, level, grantEquip, grantBook, grantSoldier, close);
        }

        private static void WireToolsPanelView(
            ToolsPanelView view,
            GameObject root,
            GameObject settings,
            GameObject level,
            GameObject grantEquip,
            GameObject grantBook,
            GameObject grantSoldier,
            GameObject close)
        {
            var so = new SerializedObject(view);
            so.FindProperty("_root").objectReferenceValue = root;
            if (settings != null)
            {
                so.FindProperty("_settingsButton").objectReferenceValue = settings.GetComponent<Button>();
            }

            if (level != null)
            {
                so.FindProperty("_levelButton").objectReferenceValue = level.GetComponent<Button>();
            }

            var grantEquipProp = so.FindProperty("_grantProtagonistEquipmentButton");
            if (grantEquipProp != null && grantEquip != null)
            {
                grantEquipProp.objectReferenceValue = grantEquip.GetComponent<Button>();
            }

            var grantBookProp = so.FindProperty("_grantMagicBookButton");
            if (grantBookProp != null && grantBook != null)
            {
                grantBookProp.objectReferenceValue = grantBook.GetComponent<Button>();
            }

            var grantSoldierProp = so.FindProperty("_grantAddSoldierButton");
            if (grantSoldierProp != null && grantSoldier != null)
            {
                grantSoldierProp.objectReferenceValue = grantSoldier.GetComponent<Button>();
            }

            if (close != null)
            {
                so.FindProperty("_closeButton").objectReferenceValue = close.GetComponent<Button>();
            }

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static GmGrantListPanelView BuildGmGrantListPanel(Transform parent)
        {
            var root = CreatePanel(parent, "GmGrantListPanel", new Color(0f, 0f, 0f, 0.55f));
            StretchFull(root.GetComponent<RectTransform>());

            var box = CreatePanel(root.transform, "Box", new Color(0.16f, 0.18f, 0.22f, 1f));
            Place(box.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(520f, 560f));

            var title = CreateText(box.transform, "Title", "发放", 28, TextAnchor.MiddleCenter);
            Place(title.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -16f), new Vector2(460f, 40f));

            var scrollGo = new GameObject("GrantScroll", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
            scrollGo.transform.SetParent(box.transform, false);
            var scrollRt = scrollGo.GetComponent<RectTransform>();
            Place(scrollRt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 10f), new Vector2(460f, 400f));
            scrollGo.GetComponent<Image>().color = new Color(0.10f, 0.11f, 0.14f, 1f);

            var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
            viewport.transform.SetParent(scrollGo.transform, false);
            StretchFull(viewport.GetComponent<RectTransform>());
            viewport.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.02f);
            viewport.GetComponent<Mask>().showMaskGraphic = false;

            var content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            content.transform.SetParent(viewport.transform, false);
            var contentRt = content.GetComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0f, 1f);
            contentRt.anchorMax = new Vector2(1f, 1f);
            contentRt.pivot = new Vector2(0.5f, 1f);
            contentRt.anchoredPosition = Vector2.zero;
            contentRt.sizeDelta = new Vector2(0f, 0f);
            var vlg = content.GetComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(12, 12, 12, 12);
            vlg.spacing = 10f;
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            var fitter = content.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var scroll = scrollGo.GetComponent<ScrollRect>();
            scroll.viewport = viewport.GetComponent<RectTransform>();
            scroll.content = contentRt;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;

            var rowTemplate = CreateButton(content.transform, "GrantRowTemplate", "Item", new Color(0.28f, 0.38f, 0.52f, 1f));
            var rowLe = rowTemplate.GetComponent<LayoutElement>();
            if (rowLe == null)
            {
                rowLe = rowTemplate.AddComponent<LayoutElement>();
            }

            rowLe.minHeight = 48f;
            rowLe.preferredHeight = 48f;
            rowTemplate.SetActive(false);

            var emptyHint = CreateText(box.transform, "EmptyHint", "当前模式无可用项", 22, TextAnchor.MiddleCenter);
            Place(emptyHint.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 10f), new Vector2(420f, 60f));
            emptyHint.gameObject.SetActive(false);

            var close = CreateButton(box.transform, "CloseButton", "关闭", new Color(0.40f, 0.40f, 0.42f, 1f));
            Place(close.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 20f), new Vector2(180f, 44f));

            var view = root.AddComponent<GmGrantListPanelView>();
            var so = new SerializedObject(view);
            so.FindProperty("_root").objectReferenceValue = root;
            so.FindProperty("_titleText").objectReferenceValue = title;
            so.FindProperty("_listContent").objectReferenceValue = content.transform;
            so.FindProperty("_rowTemplate").objectReferenceValue = rowTemplate;
            so.FindProperty("_closeButton").objectReferenceValue = close.GetComponent<Button>();
            so.FindProperty("_emptyHintText").objectReferenceValue = emptyHint;
            so.ApplyModifiedPropertiesWithoutUndo();
            AttachLevelPicker(view);
            root.SetActive(false);
            return view;
        }

        private static void AttachLevelPicker(GmGrantListPanelView view)
        {
            if (view == null)
            {
                return;
            }

            var so = new SerializedObject(view);
            var pickerProp = so.FindProperty("_levelPickerRoot");
            if (pickerProp != null && pickerProp.objectReferenceValue != null)
            {
                return;
            }

            var host = view.transform;
            var existing = host.Find("LevelPicker");
            if (existing != null)
            {
                Object.DestroyImmediate(existing.gameObject);
            }

            var picker = CreatePanel(host, "LevelPicker", new Color(0f, 0f, 0f, 0.55f));
            StretchFull(picker.GetComponent<RectTransform>());
            var canvas = picker.AddComponent<Canvas>();
            canvas.overrideSorting = true;
            canvas.sortingOrder = 111;
            picker.AddComponent<GraphicRaycaster>();

            var box = CreatePanel(picker.transform, "Box", new Color(0.16f, 0.18f, 0.22f, 1f));
            Place(box.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(360f, 420f));

            var title = CreateText(box.transform, "Title", "选择等级", 24, TextAnchor.MiddleCenter);
            Place(title.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -16f), new Vector2(320f, 36f));

            var scrollGo = new GameObject("LevelScroll", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
            scrollGo.transform.SetParent(box.transform, false);
            var scrollRt = scrollGo.GetComponent<RectTransform>();
            Place(scrollRt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 16f), new Vector2(300f, 280f));
            scrollGo.GetComponent<Image>().color = new Color(0.10f, 0.11f, 0.14f, 1f);

            var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
            viewport.transform.SetParent(scrollGo.transform, false);
            StretchFull(viewport.GetComponent<RectTransform>());
            viewport.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.02f);
            viewport.GetComponent<Mask>().showMaskGraphic = false;

            var content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            content.transform.SetParent(viewport.transform, false);
            var contentRt = content.GetComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0f, 1f);
            contentRt.anchorMax = new Vector2(1f, 1f);
            contentRt.pivot = new Vector2(0.5f, 1f);
            contentRt.anchoredPosition = Vector2.zero;
            contentRt.sizeDelta = new Vector2(0f, 0f);
            var vlg = content.GetComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(8, 8, 8, 8);
            vlg.spacing = 8f;
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            var fitter = content.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var scroll = scrollGo.GetComponent<ScrollRect>();
            scroll.viewport = viewport.GetComponent<RectTransform>();
            scroll.content = contentRt;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;

            var close = CreateButton(box.transform, "CloseButton", "关闭", new Color(0.40f, 0.40f, 0.42f, 1f));
            Place(close.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 16f), new Vector2(140f, 40f));

            so.Update();
            so.FindProperty("_levelPickerRoot").objectReferenceValue = picker;
            so.FindProperty("_levelPickerTitleText").objectReferenceValue = title;
            so.FindProperty("_levelPickerContent").objectReferenceValue = content.transform;
            so.FindProperty("_levelPickerCloseButton").objectReferenceValue = close.GetComponent<Button>();
            so.ApplyModifiedPropertiesWithoutUndo();
            picker.SetActive(false);
        }

        private static ConfirmDialogView BuildConfirmDialog(Transform parent)
        {
            var root = CreatePanel(parent, "ConfirmDialog", new Color(0f, 0f, 0f, 0.55f));
            StretchFull(root.GetComponent<RectTransform>());

            var box = CreatePanel(root.transform, "Box", new Color(0.18f, 0.20f, 0.24f, 1f));
            Place(box.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(520f, 240f));

            var message = CreateText(box.transform, "Message", "确认？", 24, TextAnchor.MiddleCenter);
            Place(message.GetComponent<RectTransform>(), new Vector2(0.5f, 0.65f), new Vector2(0.5f, 0.65f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(460f, 80f));

            var confirm = CreateButton(box.transform, "ConfirmButton", "确认", new Color(0.65f, 0.28f, 0.28f, 1f));
            Place(confirm.GetComponent<RectTransform>(), new Vector2(0.30f, 0.22f), new Vector2(0.30f, 0.22f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(140f, 44f));

            var cancel = CreateButton(box.transform, "CancelButton", "取消", new Color(0.35f, 0.38f, 0.42f, 1f));
            Place(cancel.GetComponent<RectTransform>(), new Vector2(0.70f, 0.22f), new Vector2(0.70f, 0.22f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(140f, 44f));

            var view = root.AddComponent<ConfirmDialogView>();
            var so = new SerializedObject(view);
            so.FindProperty("_root").objectReferenceValue = root;
            so.FindProperty("_messageText").objectReferenceValue = message;
            so.FindProperty("_confirmButton").objectReferenceValue = confirm.GetComponent<Button>();
            so.FindProperty("_cancelButton").objectReferenceValue = cancel.GetComponent<Button>();
            so.ApplyModifiedPropertiesWithoutUndo();
            root.SetActive(false);
            return view;
        }

        private static CampaignModeSelectView BuildCampaignModeSelect(Transform parent)
        {
            var root = CreatePanel(parent, "CampaignModeSelect", new Color(0f, 0f, 0f, 0.55f));
            StretchFull(root.GetComponent<RectTransform>());

            var box = CreatePanel(root.transform, "Box", new Color(0.18f, 0.20f, 0.24f, 1f));
            Place(box.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(560f, 280f));

            var message = CreateText(box.transform, "Message", "选择玩法模式", 24, TextAnchor.MiddleCenter);
            Place(message.GetComponent<RectTransform>(), new Vector2(0.5f, 0.78f), new Vector2(0.5f, 0.78f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(500f, 60f));

            var mode1 = CreateButton(box.transform, "Mode1Button", "模式1", new Color(0.28f, 0.45f, 0.65f, 1f));
            Place(mode1.GetComponent<RectTransform>(), new Vector2(0.28f, 0.42f), new Vector2(0.28f, 0.42f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(150f, 48f));

            var mode2 = CreateButton(box.transform, "Mode2Button", "模式2", new Color(0.35f, 0.55f, 0.38f, 1f));
            Place(mode2.GetComponent<RectTransform>(), new Vector2(0.72f, 0.42f), new Vector2(0.72f, 0.42f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(150f, 48f));

            var cancel = CreateButton(box.transform, "CancelButton", "取消", new Color(0.35f, 0.38f, 0.42f, 1f));
            Place(cancel.GetComponent<RectTransform>(), new Vector2(0.5f, 0.18f), new Vector2(0.5f, 0.18f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(140f, 44f));

            var view = root.AddComponent<CampaignModeSelectView>();
            var so = new SerializedObject(view);
            so.FindProperty("_root").objectReferenceValue = root;
            so.FindProperty("_messageText").objectReferenceValue = message;
            so.FindProperty("_mode1Button").objectReferenceValue = mode1.GetComponent<Button>();
            so.FindProperty("_mode2Button").objectReferenceValue = mode2.GetComponent<Button>();
            so.FindProperty("_cancelButton").objectReferenceValue = cancel.GetComponent<Button>();
            so.ApplyModifiedPropertiesWithoutUndo();
            root.SetActive(false);
            return view;
        }

        private static ToastView BuildToast(Transform parent)
        {
            var root = CreatePanel(parent, "Toast", new Color(0.05f, 0.05f, 0.08f, 0.88f));
            Place(root.GetComponent<RectTransform>(), new Vector2(0.5f, 0.08f), new Vector2(0.5f, 0.08f), new Vector2(0.5f, 0.5f), new Vector2(0f, 828f), new Vector2(640f, 56f));
            var text = CreateText(root.transform, "Message", string.Empty, 22, TextAnchor.MiddleCenter);

            // Keep Toast GO active (CanvasGroup hides it). Inactive host cannot StartCoroutine.
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
            so.FindProperty("_visibleSeconds").floatValue = 1.6f;
            so.ApplyModifiedPropertiesWithoutUndo();
            root.SetActive(true);
            return view;
        }

        private static GameObject CreatePanel(Transform parent, string name, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = color;
            return go;
        }

        private static Text CreateText(Transform parent, string name, string content, int fontSize, TextAnchor alignment)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            go.transform.SetParent(parent, false);
            var text = go.GetComponent<Text>();
            text.text = content;
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = Color.white;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            StretchFull(go.GetComponent<RectTransform>());
            return text;
        }

        private static GameObject CreateButton(Transform parent, string name, string label, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = color;
            var le = go.AddComponent<LayoutElement>();
            le.minHeight = 44f;
            le.preferredHeight = 44f;

            var text = CreateText(go.transform, "Label", label, 22, TextAnchor.MiddleCenter);
            text.raycastTarget = false;
            return go;
        }

        private static void StretchFull(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static void Place(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPos, Vector2 size)
        {
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;
        }

        private static void CreateBootScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            var rootPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(RootPrefabPath);
            if (rootPrefab != null)
            {
                PrefabUtility.InstantiatePrefab(rootPrefab);
            }

            // Keep default Main Camera / Directional Light from DefaultGameObjects.
            EditorSceneManager.SaveScene(scene, BootScenePath);
        }

        private static void AddBootToBuildSettings()
        {
            var boot = new EditorBuildSettingsScene(BootScenePath, true);
            var scenes = EditorBuildSettings.scenes;
            var found = false;
            for (var i = 0; i < scenes.Length; i++)
            {
                if (scenes[i].path == BootScenePath)
                {
                    scenes[i] = boot;
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                var list = new EditorBuildSettingsScene[scenes.Length + 1];
                list[0] = boot;
                for (var i = 0; i < scenes.Length; i++)
                {
                    list[i + 1] = scenes[i];
                }

                EditorBuildSettings.scenes = list;
            }
            else
            {
                // Ensure Boot is first.
                if (scenes.Length > 0 && scenes[0].path != BootScenePath)
                {
                    var reordered = new EditorBuildSettingsScene[scenes.Length];
                    reordered[0] = boot;
                    var w = 1;
                    for (var i = 0; i < scenes.Length; i++)
                    {
                        if (scenes[i].path == BootScenePath)
                        {
                            continue;
                        }

                        reordered[w++] = scenes[i];
                    }

                    EditorBuildSettings.scenes = reordered;
                }
                else
                {
                    EditorBuildSettings.scenes = scenes;
                }
            }
        }
    }
}
#endif
