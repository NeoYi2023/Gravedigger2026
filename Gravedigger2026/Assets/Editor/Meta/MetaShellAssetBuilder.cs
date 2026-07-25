#if UNITY_EDITOR
using System.IO;
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
    /// </summary>
    public static class MetaShellAssetBuilder
    {
        private const string PrefabMetaDir = "Assets/Prefabs/Meta";
        private const string PrefabUiDir = "Assets/Prefabs/UI";
        private const string RootPrefabPath = PrefabMetaDir + "/MetaShellRoot.prefab";
        private const string BootScenePath = "Assets/Scenes/Boot.unity";
        private const string RegenPrefsKey = "Gravedigger2026.MetaShell.Regen.v0330a";

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
                var needsLevelUiRegen = !EditorPrefs.GetBool(RegenPrefsKey, false);
                if (missing || needsLevelUiRegen)
                {
                    GenerateAll();
                    EditorPrefs.SetBool(RegenPrefsKey, true);
                }
            };
        }

        [MenuItem("Gravedigger2026/Meta/Generate Meta Shell Prefabs + Boot Scene")]
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

            var canvasGo = CreateCanvas(root.transform, "MetaCanvas");
            var saveSelect = BuildSaveSelect(canvasGo.transform);
            var inSaveShell = BuildInSaveShell(canvasGo.transform);
            var confirm = BuildConfirmDialog(canvasGo.transform);
            var toast = BuildToast(canvasGo.transform);

            AssignControllerRefs(controller, saveSelect, inSaveShell, confirm, toast);

            var eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            eventSystem.transform.SetParent(root.transform, false);

            return root;
        }

        private static void AssignControllerRefs(
            MetaShellController controller,
            SaveSelectView saveSelect,
            InSaveShellView inSaveShell,
            ConfirmDialogView confirm,
            ToastView toast)
        {
            var so = new SerializedObject(controller);
            so.FindProperty("_saveSelectView").objectReferenceValue = saveSelect;
            so.FindProperty("_inSaveShellView").objectReferenceValue = inSaveShell;
            so.FindProperty("_confirmDialog").objectReferenceValue = confirm;
            so.FindProperty("_toastView").objectReferenceValue = toast;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static GameObject CreateCanvas(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            go.transform.SetParent(parent, false);
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            return go;
        }

        private static SaveSelectView BuildSaveSelect(Transform parent)
        {
            var root = CreatePanel(parent, "SaveSelectPanel", new Color(0.12f, 0.14f, 0.18f, 0.96f));
            StretchFull(root.GetComponent<RectTransform>());

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
            var slotsProp = so.FindProperty("_slotViews");
            slotsProp.arraySize = 3;
            for (var i = 0; i < 3; i++)
            {
                slotsProp.GetArrayElementAtIndex(i).objectReferenceValue = slotViews[i];
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            root.SetActive(true);
            return view;
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
            Place(stateLabel.GetComponent<RectTransform>(), new Vector2(0.5f, 0.78f), new Vector2(0.5f, 0.78f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(900f, 40f));

            var stageInfo = CreateText(root.transform, "StageInfoLabel", "关卡：未运行", 20, TextAnchor.MiddleCenter);
            Place(stageInfo.GetComponent<RectTransform>(), new Vector2(0.5f, 0.72f), new Vector2(0.5f, 0.72f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1100f, 36f));

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

            var debugBtn = CreateButton(root.transform, "DebugCycleStateButton", "Debug：切下一态", new Color(0.55f, 0.45f, 0.20f, 1f));
            Place(debugBtn.GetComponent<RectTransform>(), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-24f, 24f), new Vector2(200f, 48f));

            var advanceBtn = CreateButton(root.transform, "DebugAdvanceStageButton", "Debug：推进阶段", new Color(0.45f, 0.50f, 0.25f, 1f));
            Place(advanceBtn.GetComponent<RectTransform>(), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-240f, 24f), new Vector2(200f, 48f));

            var toolsPanel = BuildToolsPanel(root.transform);

            var view = root.AddComponent<InSaveShellView>();
            var so = new SerializedObject(view);
            so.FindProperty("_root").objectReferenceValue = root;
            so.FindProperty("_backdropImage").objectReferenceValue = root.GetComponent<Image>();
            so.FindProperty("_slotLabel").objectReferenceValue = slotLabel;
            so.FindProperty("_toolsButton").objectReferenceValue = toolsBtn.GetComponent<Button>();
            so.FindProperty("_backToSaveSelectButton").objectReferenceValue = backBtn.GetComponent<Button>();
            so.FindProperty("_debugCycleStateButton").objectReferenceValue = debugBtn.GetComponent<Button>();
            so.FindProperty("_debugAdvanceStageButton").objectReferenceValue = advanceBtn.GetComponent<Button>();
            so.FindProperty("_toolsPanel").objectReferenceValue = toolsPanel;
            so.FindProperty("_placeholderView").objectReferenceValue = placeholder;
            so.ApplyModifiedPropertiesWithoutUndo();

            root.SetActive(false);
            return view;
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
            Place(go.GetComponent<RectTransform>(), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-24f, -80f), new Vector2(280f, 220f));

            var title = CreateText(go.transform, "Title", "工具面板", 24, TextAnchor.UpperCenter);
            Place(title.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -12f), new Vector2(240f, 36f));

            var settings = CreateButton(go.transform, "SettingsButton", "设置", new Color(0.30f, 0.40f, 0.55f, 1f));
            Place(settings.GetComponent<RectTransform>(), new Vector2(0.5f, 0.55f), new Vector2(0.5f, 0.55f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(200f, 44f));

            var level = CreateButton(go.transform, "LevelButton", "关卡", new Color(0.30f, 0.40f, 0.55f, 1f));
            Place(level.GetComponent<RectTransform>(), new Vector2(0.5f, 0.32f), new Vector2(0.5f, 0.32f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(200f, 44f));

            var close = CreateButton(go.transform, "CloseButton", "关闭", new Color(0.40f, 0.40f, 0.42f, 1f));
            Place(close.GetComponent<RectTransform>(), new Vector2(0.5f, 0.12f), new Vector2(0.5f, 0.12f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(200f, 40f));

            var view = go.AddComponent<ToolsPanelView>();
            var so = new SerializedObject(view);
            so.FindProperty("_root").objectReferenceValue = go;
            so.FindProperty("_settingsButton").objectReferenceValue = settings.GetComponent<Button>();
            so.FindProperty("_levelButton").objectReferenceValue = level.GetComponent<Button>();
            so.FindProperty("_closeButton").objectReferenceValue = close.GetComponent<Button>();
            so.ApplyModifiedPropertiesWithoutUndo();
            go.SetActive(false);
            return view;
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

        private static ToastView BuildToast(Transform parent)
        {
            var root = CreatePanel(parent, "Toast", new Color(0.05f, 0.05f, 0.08f, 0.88f));
            Place(root.GetComponent<RectTransform>(), new Vector2(0.5f, 0.08f), new Vector2(0.5f, 0.08f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(640f, 56f));
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
