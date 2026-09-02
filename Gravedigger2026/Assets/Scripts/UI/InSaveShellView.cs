using System;
using System.Collections.Generic;
using Gravedigger2026.Core;
using Gravedigger2026.Core.AutoManufacture;
using Gravedigger2026.Core.Config;
using Gravedigger2026.Core.Level;
using Gravedigger2026.Core.ProtagonistEquipment;
using Gravedigger2026.Gameplay.Pathing;
using UnityEngine;
using UnityEngine.UI;

namespace Gravedigger2026.UI
{
    public sealed class InSaveShellView : MonoBehaviour
    {
        [SerializeField] private GameObject _root;
        [SerializeField] private Image _backdropImage;
        [SerializeField] private Text _slotLabel;
        [SerializeField] private Button _toolsButton;
        [SerializeField] private Button _backToSaveSelectButton;
        [SerializeField] private Button _equipmentButton;
        [SerializeField] private Button _magicBookButton;
        [SerializeField] private Button _shopButton;
        [SerializeField] private Button _debugCycleStateButton;
        [SerializeField] private Button _debugAdvanceStageButton;
        [SerializeField] private Button _debugWarriorTaskLabelButton;
        [SerializeField] private ToolsPanelView _toolsPanel;
        [SerializeField] private DifficultySelectHostView _difficultySelectHost;
        [SerializeField] private LevelSelectPanelView _levelSelectPanel;
        [SerializeField] private GmGrantListPanelView _gmGrantListPanel;
        [SerializeField] private GmAddSoldierPanelView _gmAddSoldierPanel;
        [SerializeField] private EquipmentWarehousePanelView _equipmentWarehousePanel;
        [SerializeField] private MagicBookSlotsPanelView _magicBookSlotsPanel;
        [SerializeField] private GameplayStatePlaceholderView _placeholderView;

        private Color _backdropDefault = Color.white;
        private bool _backdropDefaultCached;
        private Text _warriorTaskLabelButtonText;

        public event Action ToolsToggleRequested;
        public event Action ToolsClosed;
        public event Action BackToSaveSelectRequested;
        public event Action EquipmentRequested;
        public event Action MagicBookRequested;
        public event Action ShopRequested;
        public event Action DebugCycleStateRequested;
        public event Action DebugAdvanceStageRequested;
        public event Action SettingsRequested;
        public event Action LevelRequested;
        public event Action GrantProtagonistEquipmentRequested;
        public event Action GrantMagicBookRequested;
        public event Action GrantAddSoldierRequested;
        public event Action<string> LevelSelectPicked;
        public event Action LevelSelectClosed;
        public event Action LockedDifficultyClicked;
        public event Action<string> GmGrantItemPicked;
        public event Action<int> GmGrantLevelPicked;
        public event Action GmGrantListClosed;
        public event Action GmAddSoldierAddClicked;
        public event Action GmAddSoldierClosed;
        public event Action EquipmentWarehouseClosed;
        public event Action MagicBookSlotsClosed;

        private void Awake()
        {
            CacheBackdropDefaultFromImage();

            if (_toolsButton != null)
            {
                _toolsButton.onClick.AddListener(() => ToolsToggleRequested?.Invoke());
            }

            if (_backToSaveSelectButton != null)
            {
                _backToSaveSelectButton.onClick.AddListener(() => BackToSaveSelectRequested?.Invoke());
            }

            if (_equipmentButton != null)
            {
                _equipmentButton.onClick.AddListener(() => EquipmentRequested?.Invoke());
            }

            if (_magicBookButton != null)
            {
                _magicBookButton.onClick.AddListener(() => MagicBookRequested?.Invoke());
            }

            if (_debugCycleStateButton != null)
            {
                _debugCycleStateButton.onClick.AddListener(() => DebugCycleStateRequested?.Invoke());
            }

            if (_debugAdvanceStageButton != null)
            {
                _debugAdvanceStageButton.onClick.AddListener(() => DebugAdvanceStageRequested?.Invoke());
            }

            EnsureWarriorTaskLabelToggleButton();
            EnsureGmGrantListPanel();
            EnsureGmAddSoldierPanel();
            EnsureEquipmentWarehouseList();
            EnsureMagicBookRow();
            EnsureShopButton();
            EnsureDifficultySelectHost();
            if (_shopButton != null)
            {
                _shopButton.onClick.RemoveAllListeners();
                _shopButton.onClick.AddListener(() => ShopRequested?.Invoke());
            }

            if (_difficultySelectHost != null)
            {
                _difficultySelectHost.LockedDifficultyClicked += () => LockedDifficultyClicked?.Invoke();
            }

            if (_debugWarriorTaskLabelButton != null)
            {
                _debugWarriorTaskLabelButton.onClick.AddListener(HandleWarriorTaskLabelToggleClicked);
            }

            WarriorTaskLabelSettings.EnabledChanged += HandleWarriorTaskLabelEnabledChanged;
            RefreshWarriorTaskLabelButtonCaption();

            if (_toolsPanel != null)
            {
                _toolsPanel.SettingsClicked += () => SettingsRequested?.Invoke();
                _toolsPanel.LevelClicked += () => LevelRequested?.Invoke();
                _toolsPanel.GrantProtagonistEquipmentClicked +=
                    () => GrantProtagonistEquipmentRequested?.Invoke();
                _toolsPanel.GrantMagicBookClicked += () => GrantMagicBookRequested?.Invoke();
                _toolsPanel.GrantAddSoldierClicked += () => GrantAddSoldierRequested?.Invoke();
                _toolsPanel.Closed += () => ToolsClosed?.Invoke();
            }

            if (_levelSelectPanel != null)
            {
                _levelSelectPanel.LevelPicked += id => LevelSelectPicked?.Invoke(id);
                _levelSelectPanel.Closed += () => LevelSelectClosed?.Invoke();
            }

            if (_gmGrantListPanel != null)
            {
                _gmGrantListPanel.ItemPicked += id => GmGrantItemPicked?.Invoke(id);
                _gmGrantListPanel.LevelPicked += level => GmGrantLevelPicked?.Invoke(level);
                _gmGrantListPanel.Closed += () => GmGrantListClosed?.Invoke();
            }

            if (_gmAddSoldierPanel != null)
            {
                _gmAddSoldierPanel.AddClicked += () => GmAddSoldierAddClicked?.Invoke();
                _gmAddSoldierPanel.Closed += () => GmAddSoldierClosed?.Invoke();
            }

            if (_equipmentWarehousePanel != null)
            {
                _equipmentWarehousePanel.Closed += () => EquipmentWarehouseClosed?.Invoke();
            }

            if (_magicBookSlotsPanel != null)
            {
                _magicBookSlotsPanel.Closed += () => MagicBookSlotsClosed?.Invoke();
            }
        }

        private void OnDestroy()
        {
            WarriorTaskLabelSettings.EnabledChanged -= HandleWarriorTaskLabelEnabledChanged;
        }

        public void Show(int slotIndex)
        {
            if (_slotLabel != null)
            {
                _slotLabel.text = $"进档壳 — 槽 {slotIndex + 1}";
            }

            // Activate first so Awake can cache Prefab Image color before we rewrite it.
            if (_root != null)
            {
                _root.SetActive(true);
            }

            SetShellBackdropVisible(true);
        }

        public void Hide()
        {
            if (_toolsPanel != null)
            {
                _toolsPanel.Hide();
            }

            HideLevelSelectPanel();
            HideDifficultySelectHost();
            HideGmGrantListPanel();
            HideGmAddSoldierPanel();
            HideEquipmentWarehousePanel();
            HideMagicBookSlotsPanel();

            if (_root != null)
            {
                _root.SetActive(false);
            }
        }

        public void ToggleToolsPanel()
        {
            if (_toolsPanel != null)
            {
                _toolsPanel.Toggle();
            }
        }

        public void HideToolsPanel()
        {
            if (_toolsPanel != null)
            {
                _toolsPanel.Hide();
            }
        }

        public void ShowLevelSelectPanel(IReadOnlyList<string> levelIds)
        {
            EnsureDifficultySelectHost();
            // Hide Dig/UM/Defend placeholders, but keep shell backdrop Image visible (hub chrome).
            SuppressModePanelsKeepBackdrop();
            if (_difficultySelectHost != null)
            {
                _difficultySelectHost.ShowExpandedNormal();
            }

            if (_levelSelectPanel != null)
            {
                _levelSelectPanel.ConfigureHubEmbedded(true);
                _levelSelectPanel.Show(levelIds);
            }
        }

        public void HideLevelSelectPanel()
        {
            if (_levelSelectPanel != null)
            {
                _levelSelectPanel.Hide();
            }
        }

        public void ShowDifficultySelectHost()
        {
            EnsureDifficultySelectHost();
            SuppressModePanelsKeepBackdrop();
            if (_difficultySelectHost != null)
            {
                _difficultySelectHost.ShowExpandedNormal();
            }
        }

        public void HideDifficultySelectHost()
        {
            if (_difficultySelectHost != null)
            {
                _difficultySelectHost.Hide();
            }
        }

        public void ShowGmGrantListPanel(string title, IReadOnlyList<GmGrantListItem> items)
        {
            if (_gmGrantListPanel != null)
            {
                _gmGrantListPanel.Show(title, items);
            }
        }

        public void HideGmGrantListPanel()
        {
            if (_gmGrantListPanel != null)
            {
                _gmGrantListPanel.Hide();
            }
        }

        public void ShowGmGrantLevelPicker(string title, IReadOnlyList<int> levels)
        {
            if (_gmGrantListPanel != null)
            {
                _gmGrantListPanel.ShowLevelPicker(title, levels);
            }
        }

        public bool HasGmGrantListPanel => _gmGrantListPanel != null;

        public void ShowGmAddSoldierPanel(
            IReadOnlyList<GmDropdownOption> classes,
            IReadOnlyList<GmDropdownOption> races)
        {
            EnsureGmAddSoldierPanel();
            if (_gmAddSoldierPanel != null)
            {
                _gmAddSoldierPanel.Show(classes, races);
            }
        }

        public void HideGmAddSoldierPanel()
        {
            if (_gmAddSoldierPanel != null)
            {
                _gmAddSoldierPanel.Hide();
            }
        }

        public bool HasGmAddSoldierPanel => _gmAddSoldierPanel != null;

        public void BindEquipmentWarehouse(ProtagonistEquipmentService equipment, ConfigCsvRepository configs)
        {
            if (_equipmentWarehousePanel != null)
            {
                _equipmentWarehousePanel.EnsureRuntimeUi();
                _equipmentWarehousePanel.Bind(equipment, configs);
            }
        }

        public void BindMagicBookSlots(
            SpecialEquipSlotsService slots,
            ConfigCsvRepository configs,
            ConfirmDialogView confirmDialog = null)
        {
            if (_magicBookSlotsPanel != null)
            {
                _magicBookSlotsPanel.EnsureBookRow();
                _magicBookSlotsPanel.Bind(slots, configs, confirmDialog);
            }
        }

        public void ShowEquipmentWarehousePanel()
        {
            if (_equipmentWarehousePanel != null)
            {
                _equipmentWarehousePanel.Show();
            }
        }

        public void HideEquipmentWarehousePanel()
        {
            if (_equipmentWarehousePanel != null)
            {
                _equipmentWarehousePanel.Hide();
            }
        }

        public void ShowMagicBookSlotsPanel()
        {
            if (_magicBookSlotsPanel != null)
            {
                _magicBookSlotsPanel.Show();
            }
        }

        public void HideMagicBookSlotsPanel()
        {
            if (_magicBookSlotsPanel != null)
            {
                _magicBookSlotsPanel.Hide();
            }
        }

        /// <summary>True when any InSaveShell modal that should hide DigFogCanvas is open.</summary>
        public bool IsAnyMetaOverlayBlockingFog
        {
            get
            {
                if (_toolsPanel != null && _toolsPanel.IsOpen)
                {
                    return true;
                }

                if (_levelSelectPanel != null && _levelSelectPanel.IsOpen)
                {
                    return true;
                }

                if (_difficultySelectHost != null && _difficultySelectHost.IsOpen)
                {
                    return true;
                }

                if (_gmGrantListPanel != null && _gmGrantListPanel.IsOpen)
                {
                    return true;
                }

                if (_gmAddSoldierPanel != null && _gmAddSoldierPanel.IsOpen)
                {
                    return true;
                }

                if (_equipmentWarehousePanel != null && _equipmentWarehousePanel.IsOpen)
                {
                    return true;
                }

                if (_magicBookSlotsPanel != null && _magicBookSlotsPanel.IsOpen)
                {
                    return true;
                }

                return false;
            }
        }

        public bool TryGetGmAddSoldierSelection(
            out string classId,
            out string raceId,
            out int count,
            out bool autoDeploy)
        {
            classId = null;
            raceId = null;
            count = 1;
            autoDeploy = true;
            return _gmAddSoldierPanel != null
                   && _gmAddSoldierPanel.TryGetSelection(out classId, out raceId, out count, out autoDeploy);
        }

        public void SetModePanelsSuppressed(bool suppressed)
        {
            if (_placeholderView != null)
            {
                _placeholderView.SetModePanelsSuppressed(suppressed);
            }

            // Stage presentation needs a clear camera view; hide shell backdrop.
            SetShellBackdropVisible(!suppressed);
        }

        /// <summary>
        /// Hub (DifficultySelect + LevelSelect): suppress mode placeholders without clearing shell bg.
        /// </summary>
        private void SuppressModePanelsKeepBackdrop()
        {
            if (_placeholderView != null)
            {
                _placeholderView.SetModePanelsSuppressed(true);
            }

            SetShellBackdropVisible(true);
        }

        public void SetShellBackdropVisible(bool visible)
        {
            if (_backdropImage == null)
            {
                return;
            }

            // Panel starts inactive: Awake may not have run when MetaShell calls this first.
            // Cache Prefab color before any write, or hardcoded dark alpha would tint the sprite.
            CacheBackdropDefaultFromImage();

            var c = _backdropDefault;
            if (!visible)
            {
                c.a = 0f;
            }

            _backdropImage.color = c;
            _backdropImage.raycastTarget = visible;
        }

        private void CacheBackdropDefaultFromImage()
        {
            if (_backdropDefaultCached || _backdropImage == null)
            {
                return;
            }

            _backdropDefault = _backdropImage.color;
            _backdropDefaultCached = true;
        }

        public void ShowGameplayState(GameplayState state)
        {
            if (_placeholderView != null)
            {
                _placeholderView.ShowState(state);
            }
        }

        public void ShowStageInfo(LevelStageContext context)
        {
            if (_placeholderView != null)
            {
                _placeholderView.ShowStageInfo(context);
            }
        }

        private void HandleWarriorTaskLabelToggleClicked()
        {
            WarriorTaskLabelSettings.Toggle();
        }

        private void HandleWarriorTaskLabelEnabledChanged(bool _)
        {
            RefreshWarriorTaskLabelButtonCaption();
        }

        private void RefreshWarriorTaskLabelButtonCaption()
        {
            if (_warriorTaskLabelButtonText == null && _debugWarriorTaskLabelButton != null)
            {
                _warriorTaskLabelButtonText = _debugWarriorTaskLabelButton.GetComponentInChildren<Text>(true);
            }

            if (_warriorTaskLabelButtonText != null)
            {
                _warriorTaskLabelButtonText.text = WarriorTaskLabelSettings.Enabled
                    ? "士兵任务:开"
                    : "士兵任务:关";
            }
        }

        private void EnsureWarriorTaskLabelToggleButton()
        {
            if (_debugWarriorTaskLabelButton != null)
            {
                return;
            }

            if (_debugAdvanceStageButton == null)
            {
                return;
            }

            var template = _debugAdvanceStageButton.gameObject;
            var clone = Instantiate(template, template.transform.parent);
            clone.name = "DebugWarriorTaskLabelButton";

            var rect = clone.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.anchoredPosition = new Vector2(-460f, rect.anchoredPosition.y);
            }

            var image = clone.GetComponent<Image>();
            if (image != null)
            {
                image.color = new Color(0.25f, 0.45f, 0.55f, 1f);
            }

            _debugWarriorTaskLabelButton = clone.GetComponent<Button>();
            if (_debugWarriorTaskLabelButton != null)
            {
                _debugWarriorTaskLabelButton.onClick.RemoveAllListeners();
            }

            _warriorTaskLabelButtonText = clone.GetComponentInChildren<Text>(true);
        }

        private void EnsureEquipmentWarehouseList()
        {
            if (_equipmentWarehousePanel != null)
            {
                _equipmentWarehousePanel.EnsureRuntimeUi();
            }
        }

        private void EnsureMagicBookRow()
        {
            if (_magicBookSlotsPanel != null)
            {
                _magicBookSlotsPanel.EnsureBookRow();
            }
        }

        private void EnsureShopButton()
        {
            if (_shopButton != null)
            {
                return;
            }

            if (_equipmentButton == null)
            {
                return;
            }

            // SS-04：运行时克隆“装备”按钮作为“商店”入口，避免 prefab 修改耦合。
            var template = _equipmentButton.gameObject;
            var clone = Instantiate(template, template.transform.parent);
            clone.name = "ShopButton";
            clone.SetActive(true);

            var btn = clone.GetComponent<Button>();
            if (btn == null)
            {
                DestroyImmediate(clone);
                return;
            }

            btn.onClick.RemoveAllListeners();

            var label = clone.GetComponentInChildren<Text>(true);
            if (label != null)
            {
                label.text = "商店";
            }

            var rt = clone.GetComponent<RectTransform>();
            var eqRt = _equipmentButton.GetComponent<RectTransform>();
            if (rt != null && eqRt != null)
            {
                rt.anchoredPosition = new Vector2(eqRt.anchoredPosition.x, eqRt.anchoredPosition.y + eqRt.sizeDelta.y + 8f);
            }

            var img = clone.GetComponent<Image>();
            if (img != null)
            {
                img.color = new Color(0.25f, 0.45f, 0.55f, 1f);
            }

            _shopButton = btn;
        }

        private void EnsureDifficultySelectHost()
        {
            var parent = _root != null ? _root.transform : transform;
            var existing = parent.Find("DifficultySelectHost");
            GameObject hostGo;
            if (existing != null)
            {
                hostGo = existing.gameObject;
                _difficultySelectHost = hostGo.GetComponent<DifficultySelectHostView>();
                if (_difficultySelectHost == null)
                {
                    _difficultySelectHost = hostGo.AddComponent<DifficultySelectHostView>();
                }

                if (_difficultySelectHost.NormalLevelHost == null)
                {
                    BuildDifficultyScrollRuntime(hostGo);
                }
            }
            else
            {
                hostGo = new GameObject("DifficultySelectHost", typeof(RectTransform));
                hostGo.transform.SetParent(parent, false);
                var hostRt = hostGo.GetComponent<RectTransform>();
                hostRt.anchorMin = new Vector2(0.08f, 0.12f);
                hostRt.anchorMax = new Vector2(0.92f, 0.88f);
                hostRt.offsetMin = Vector2.zero;
                hostRt.offsetMax = Vector2.zero;

                _difficultySelectHost = hostGo.AddComponent<DifficultySelectHostView>();
                BuildDifficultyScrollRuntime(hostGo);
            }

            EmbedLevelSelectInNormalColumn();
            hostGo.SetActive(false);
        }

        private void BuildDifficultyScrollRuntime(GameObject hostGo)
        {
            var panel = hostGo.transform.parent;
            if (_levelSelectPanel == null)
            {
                _levelSelectPanel = hostGo.GetComponentInChildren<LevelSelectPanelView>(true);
            }

            if (_levelSelectPanel == null && panel != null)
            {
                _levelSelectPanel = panel.GetComponentInChildren<LevelSelectPanelView>(true);
            }

            // Detach LevelSelect if nested under obsolete MapHost / Columns.
            if (_levelSelectPanel != null && panel != null)
            {
                _levelSelectPanel.transform.SetParent(panel, false);
            }

            for (var i = hostGo.transform.childCount - 1; i >= 0; i--)
            {
                UnityEngine.Object.DestroyImmediate(hostGo.transform.GetChild(i).gameObject);
            }

            var scrollGo = new GameObject("ColumnsScroll", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
            scrollGo.transform.SetParent(hostGo.transform, false);
            Stretch(scrollGo.GetComponent<RectTransform>());
            var scrollImg = scrollGo.GetComponent<Image>();
            scrollImg.color = new Color(0f, 0f, 0f, 0.01f);
            scrollImg.raycastTarget = true;

            var viewportGo = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
            viewportGo.transform.SetParent(scrollGo.transform, false);
            Stretch(viewportGo.GetComponent<RectTransform>());
            var vpImg = viewportGo.GetComponent<Image>();
            vpImg.color = Color.white;
            vpImg.raycastTarget = true;
            viewportGo.GetComponent<Mask>().showMaskGraphic = false;

            var contentGo = new GameObject("Content", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            contentGo.transform.SetParent(viewportGo.transform, false);
            var contentRt = contentGo.GetComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0f, 0f);
            contentRt.anchorMax = new Vector2(0f, 1f);
            contentRt.pivot = new Vector2(0f, 0.5f);
            contentRt.anchoredPosition = Vector2.zero;
            contentRt.sizeDelta = new Vector2(0f, 0f);
            var hlg = contentGo.GetComponent<HorizontalLayoutGroup>();
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = true;
            hlg.spacing = 0f;
            hlg.padding = new RectOffset(0, 0, 0, 0);

            var scroll = scrollGo.GetComponent<ScrollRect>();
            scroll.horizontal = true;
            scroll.vertical = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.viewport = viewportGo.GetComponent<RectTransform>();
            scroll.content = contentRt;
            scroll.inertia = true;
            scroll.scrollSensitivity = 40f;

            var normal = CreateDifficultyColumn(contentRt, "NormalColumn", "普通难度",
                new Color(0.55f, 0.78f, 0.55f, 1f), withLevelHost: true);
            var hard = CreateDifficultyColumn(contentRt, "HardColumn", "困难难度",
                new Color(0.85f, 0.75f, 0.35f, 1f), withLevelHost: false);
            var hell = CreateDifficultyColumn(contentRt, "HellColumn", "地狱难度",
                new Color(0.90f, 0.55f, 0.35f, 1f), withLevelHost: false);

            var levelHost = normal.transform.Find("LevelHost") as RectTransform;
            _difficultySelectHost.BindRuntime(
                hostGo,
                scroll,
                contentRt,
                normal.GetComponent<RectTransform>(),
                hard.GetComponent<RectTransform>(),
                hell.GetComponent<RectTransform>(),
                levelHost,
                normal.GetComponent<Button>(),
                hard.GetComponent<Button>(),
                hell.GetComponent<Button>(),
                normal.transform.Find("Label")?.GetComponent<Text>(),
                hard.transform.Find("Label")?.GetComponent<Text>(),
                hell.transform.Find("Label")?.GetComponent<Text>());
        }

        private void EmbedLevelSelectInNormalColumn()
        {
            if (_difficultySelectHost == null || _levelSelectPanel == null)
            {
                return;
            }

            var levelHost = _difficultySelectHost.NormalLevelHost;
            if (levelHost == null)
            {
                return;
            }

            var levelGo = _levelSelectPanel.gameObject;
            if (levelGo.transform.parent != levelHost)
            {
                levelGo.transform.SetParent(levelHost, false);
            }

            var rt = levelGo.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = new Vector2(8f, 8f);
                rt.offsetMax = new Vector2(-8f, -8f);
            }

            var backdrop = levelGo.GetComponent<Image>();
            if (backdrop != null)
            {
                backdrop.color = new Color(0.12f, 0.14f, 0.18f, 0.92f);
            }

            EnsureLevelEnterButton(levelGo.transform);
            _levelSelectPanel.ConfigureHubEmbedded(true);
        }

        private void EnsureLevelEnterButton(Transform levelRoot)
        {
            if (_levelSelectPanel == null)
            {
                return;
            }

            var box = levelRoot.Find("Box");
            var parent = box != null ? box : levelRoot;
            var enterTf = parent.Find("EnterButton");
            Button enterBtn;
            if (enterTf == null)
            {
                var enterGo = new GameObject("EnterButton", typeof(RectTransform), typeof(Image), typeof(Button));
                enterGo.transform.SetParent(parent, false);
                var ert = enterGo.GetComponent<RectTransform>();
                ert.anchorMin = new Vector2(0.5f, 0f);
                ert.anchorMax = new Vector2(0.5f, 0f);
                ert.pivot = new Vector2(0.5f, 0f);
                ert.anchoredPosition = new Vector2(0f, 16f);
                ert.sizeDelta = new Vector2(200f, 48f);
                enterGo.GetComponent<Image>().color = new Color(0.25f, 0.45f, 0.85f, 1f);
                var label = CreateUiText(enterGo.transform, "Label", "进入", 22, TextAnchor.MiddleCenter);
                Stretch(label.rectTransform);
                label.color = Color.white;
                enterBtn = enterGo.GetComponent<Button>();
            }
            else
            {
                enterBtn = enterTf.GetComponent<Button>();
            }

            var close = parent.Find("CloseButton")?.GetComponent<Button>();
            var title = parent.Find("Title")?.GetComponent<Text>();
            var content = parent.Find("LevelScroll/Viewport/Content");
            var rowTemplate = content != null ? content.Find("LevelRowTemplate")?.gameObject : null;
            var emptyHint = parent.Find("EmptyHint")?.GetComponent<Text>();
            var backdrop = levelRoot.GetComponent<Image>();
            _levelSelectPanel.BindRuntime(
                levelRoot.gameObject,
                title,
                content,
                rowTemplate,
                close,
                emptyHint,
                enterBtn,
                backdrop);
        }

        private static GameObject CreateDifficultyColumn(
            Transform parent,
            string name,
            string label,
            Color color,
            bool withLevelHost)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = color;
            var le = go.GetComponent<LayoutElement>();
            le.minWidth = 400f;
            le.preferredWidth = 400f;
            le.flexibleWidth = 0f;
            le.flexibleHeight = 1f;

            var text = CreateUiText(go.transform, "Label", label, 28, TextAnchor.MiddleCenter);
            var labelRt = text.rectTransform;
            labelRt.anchorMin = new Vector2(0f, 0.88f);
            labelRt.anchorMax = new Vector2(1f, 1f);
            labelRt.offsetMin = Vector2.zero;
            labelRt.offsetMax = Vector2.zero;
            text.color = new Color(0.15f, 0.15f, 0.18f, 1f);

            if (withLevelHost)
            {
                var levelHostGo = new GameObject("LevelHost", typeof(RectTransform));
                levelHostGo.transform.SetParent(go.transform, false);
                var lh = levelHostGo.GetComponent<RectTransform>();
                lh.anchorMin = new Vector2(0f, 0f);
                lh.anchorMax = new Vector2(1f, 0.88f);
                lh.offsetMin = new Vector2(8f, 8f);
                lh.offsetMax = new Vector2(-8f, -4f);
            }

            return go;
        }

        private void EnsureGmGrantListPanel()
        {
            if (_gmGrantListPanel != null)
            {
                return;
            }

            if (_levelSelectPanel == null)
            {
                return;
            }

            var template = _levelSelectPanel.gameObject;
            var clone = Instantiate(template, template.transform.parent);
            clone.name = "GmGrantListPanel";
            clone.SetActive(false);

            var oldView = clone.GetComponent<LevelSelectPanelView>();
            if (oldView != null)
            {
                DestroyImmediate(oldView);
            }

            var view = clone.GetComponent<GmGrantListPanelView>();
            if (view == null)
            {
                view = clone.AddComponent<GmGrantListPanelView>();
            }

            var title = clone.transform.Find("Box/Title")?.GetComponent<Text>();
            var content = clone.transform.Find("Box/LevelScroll/Viewport/Content");
            var rowTemplate = content != null ? content.Find("LevelRowTemplate")?.gameObject : null;
            var close = clone.transform.Find("Box/CloseButton")?.GetComponent<Button>();
            var emptyHint = clone.transform.Find("Box/EmptyHint")?.GetComponent<Text>();
            view.BindRuntime(clone, title, content, rowTemplate, close, emptyHint);
            _gmGrantListPanel = view;
        }

        private void EnsureGmAddSoldierPanel()
        {
            if (_gmAddSoldierPanel != null)
            {
                return;
            }

            var parent = _toolsPanel != null ? _toolsPanel.transform.parent : transform;
            var go = new GameObject("GmAddSoldierPanel", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rootRt = go.GetComponent<RectTransform>();
            rootRt.anchorMin = new Vector2(0f, 0.5f);
            rootRt.anchorMax = new Vector2(0f, 0.5f);
            rootRt.pivot = new Vector2(0f, 0.5f);
            rootRt.anchoredPosition = new Vector2(16f, 0f);
            rootRt.sizeDelta = new Vector2(320f, 420f);
            go.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.96f);

            var title = CreateUiText(go.transform, "Title", "添加士兵", 26, TextAnchor.UpperCenter);
            PlaceUi(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -16f), new Vector2(280f, 36f));

            CreateUiText(go.transform, "ClassLabel", "士兵职业", 18, TextAnchor.MiddleLeft);
            PlaceUi(go.transform.Find("ClassLabel").GetComponent<RectTransform>(),
                new Vector2(0.5f, 1f), new Vector2(0f, -64f), new Vector2(280f, 24f));
            var classDd = CreateUiDropdown(go.transform, "ClassDropdown");
            PlaceUi(classDd.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0f, -96f), new Vector2(280f, 36f));

            CreateUiText(go.transform, "RaceLabel", "士兵种族", 18, TextAnchor.MiddleLeft);
            PlaceUi(go.transform.Find("RaceLabel").GetComponent<RectTransform>(),
                new Vector2(0.5f, 1f), new Vector2(0f, -148f), new Vector2(280f, 24f));
            var raceDd = CreateUiDropdown(go.transform, "RaceDropdown");
            PlaceUi(raceDd.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0f, -180f), new Vector2(280f, 36f));

            CreateUiText(go.transform, "CountLabel", "增加数量", 18, TextAnchor.MiddleLeft);
            PlaceUi(go.transform.Find("CountLabel").GetComponent<RectTransform>(),
                new Vector2(0.5f, 1f), new Vector2(0f, -232f), new Vector2(280f, 24f));
            var countInput = CreateUiInput(go.transform, "CountInput", "1");
            PlaceUi(countInput.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0f, -264f), new Vector2(280f, 36f));

            var autoToggle = CreateUiToggle(go.transform, "AutoDeployToggle", "自动上阵", true);
            PlaceUi(autoToggle.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0f, -316f), new Vector2(280f, 32f));

            var close = CreateUiButton(go.transform, "CloseButton", "关闭", new Color(0.92f, 0.92f, 0.92f, 1f));
            PlaceUi(close.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(-70f, 24f), new Vector2(120f, 40f));
            var add = CreateUiButton(go.transform, "AddButton", "添加", new Color(0.25f, 0.55f, 0.95f, 1f));
            PlaceUi(add.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(70f, 24f), new Vector2(120f, 40f));

            var view = go.AddComponent<GmAddSoldierPanelView>();
            view.BindRuntime(
                go,
                classDd,
                raceDd,
                countInput,
                autoToggle,
                close.GetComponent<Button>(),
                add.GetComponent<Button>());
            go.SetActive(false);
            _gmAddSoldierPanel = view;
        }

        private static Text CreateUiText(Transform parent, string name, string text, int size, TextAnchor anchor)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var t = go.GetComponent<Text>();
            t.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            t.fontSize = size;
            t.alignment = anchor;
            t.color = Color.black;
            t.text = text;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            return t;
        }

        private static Dropdown CreateUiDropdown(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Dropdown));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = new Color(0.95f, 0.95f, 0.95f, 1f);
            var dd = go.GetComponent<Dropdown>();

            var labelGo = new GameObject("Label", typeof(RectTransform), typeof(Text));
            labelGo.transform.SetParent(go.transform, false);
            var labelRt = labelGo.GetComponent<RectTransform>();
            Stretch(labelRt);
            labelRt.offsetMin = new Vector2(10f, 2f);
            labelRt.offsetMax = new Vector2(-28f, -2f);
            var label = labelGo.GetComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            label.fontSize = 18;
            label.color = Color.black;
            label.alignment = TextAnchor.MiddleLeft;

            var arrowGo = new GameObject("Arrow", typeof(RectTransform), typeof(Text));
            arrowGo.transform.SetParent(go.transform, false);
            var arrowRt = arrowGo.GetComponent<RectTransform>();
            arrowRt.anchorMin = new Vector2(1f, 0.5f);
            arrowRt.anchorMax = new Vector2(1f, 0.5f);
            arrowRt.pivot = new Vector2(1f, 0.5f);
            arrowRt.anchoredPosition = new Vector2(-8f, 0f);
            arrowRt.sizeDelta = new Vector2(20f, 20f);
            var arrow = arrowGo.GetComponent<Text>();
            arrow.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            arrow.fontSize = 16;
            arrow.alignment = TextAnchor.MiddleCenter;
            arrow.color = Color.black;
            arrow.text = "▼";

            var template = new GameObject("Template", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
            template.transform.SetParent(go.transform, false);
            var templateRt = template.GetComponent<RectTransform>();
            templateRt.anchorMin = new Vector2(0f, 0f);
            templateRt.anchorMax = new Vector2(1f, 0f);
            templateRt.pivot = new Vector2(0.5f, 1f);
            templateRt.anchoredPosition = new Vector2(0f, 2f);
            templateRt.sizeDelta = new Vector2(0f, 150f);
            template.GetComponent<Image>().color = Color.white;

            var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
            viewport.transform.SetParent(template.transform, false);
            Stretch(viewport.GetComponent<RectTransform>());
            viewport.GetComponent<Image>().color = Color.white;
            viewport.GetComponent<Mask>().showMaskGraphic = false;

            var content = new GameObject("Content", typeof(RectTransform));
            content.transform.SetParent(viewport.transform, false);
            var contentRt = content.GetComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0f, 1f);
            contentRt.anchorMax = new Vector2(1f, 1f);
            contentRt.pivot = new Vector2(0.5f, 1f);
            contentRt.sizeDelta = new Vector2(0f, 28f);

            var item = new GameObject("Item", typeof(RectTransform), typeof(Toggle));
            item.transform.SetParent(content.transform, false);
            var itemRt = item.GetComponent<RectTransform>();
            itemRt.anchorMin = new Vector2(0f, 0.5f);
            itemRt.anchorMax = new Vector2(1f, 0.5f);
            itemRt.sizeDelta = new Vector2(0f, 28f);

            var itemBg = new GameObject("Item Background", typeof(RectTransform), typeof(Image));
            itemBg.transform.SetParent(item.transform, false);
            Stretch(itemBg.GetComponent<RectTransform>());
            itemBg.GetComponent<Image>().color = new Color(0.9f, 0.9f, 0.9f, 1f);

            var itemCheck = new GameObject("Item Checkmark", typeof(RectTransform), typeof(Image));
            itemCheck.transform.SetParent(item.transform, false);
            var checkRt = itemCheck.GetComponent<RectTransform>();
            checkRt.anchorMin = new Vector2(0f, 0.5f);
            checkRt.anchorMax = new Vector2(0f, 0.5f);
            checkRt.anchoredPosition = new Vector2(10f, 0f);
            checkRt.sizeDelta = new Vector2(16f, 16f);
            itemCheck.GetComponent<Image>().color = new Color(0.2f, 0.5f, 0.9f, 1f);

            var itemLabel = new GameObject("Item Label", typeof(RectTransform), typeof(Text));
            itemLabel.transform.SetParent(item.transform, false);
            var itemLabelRt = itemLabel.GetComponent<RectTransform>();
            Stretch(itemLabelRt);
            itemLabelRt.offsetMin = new Vector2(28f, 1f);
            itemLabelRt.offsetMax = new Vector2(-8f, -1f);
            var itemLabelText = itemLabel.GetComponent<Text>();
            itemLabelText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            itemLabelText.fontSize = 16;
            itemLabelText.color = Color.black;
            itemLabelText.alignment = TextAnchor.MiddleLeft;

            var itemToggle = item.GetComponent<Toggle>();
            itemToggle.targetGraphic = itemBg.GetComponent<Image>();
            itemToggle.graphic = itemCheck.GetComponent<Image>();

            var scroll = template.GetComponent<ScrollRect>();
            scroll.content = contentRt;
            scroll.viewport = viewport.GetComponent<RectTransform>();
            scroll.horizontal = false;
            scroll.vertical = true;

            dd.targetGraphic = go.GetComponent<Image>();
            dd.captionText = label;
            dd.itemText = itemLabelText;
            dd.template = templateRt;
            template.SetActive(false);
            return dd;
        }

        private static InputField CreateUiInput(Transform parent, string name, string text)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(InputField));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = Color.white;
            var input = go.GetComponent<InputField>();

            var textGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
            textGo.transform.SetParent(go.transform, false);
            var textRt = textGo.GetComponent<RectTransform>();
            Stretch(textRt);
            textRt.offsetMin = new Vector2(10f, 4f);
            textRt.offsetMax = new Vector2(-10f, -4f);
            var t = textGo.GetComponent<Text>();
            t.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            t.fontSize = 18;
            t.color = Color.black;
            t.supportRichText = false;
            input.textComponent = t;
            input.contentType = InputField.ContentType.IntegerNumber;
            input.text = text;
            return input;
        }

        private static Toggle CreateUiToggle(Transform parent, string name, string label, bool on)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Toggle));
            go.transform.SetParent(parent, false);
            var toggle = go.GetComponent<Toggle>();

            var bg = new GameObject("Background", typeof(RectTransform), typeof(Image));
            bg.transform.SetParent(go.transform, false);
            var bgRt = bg.GetComponent<RectTransform>();
            bgRt.anchorMin = new Vector2(0f, 0.5f);
            bgRt.anchorMax = new Vector2(0f, 0.5f);
            bgRt.anchoredPosition = new Vector2(16f, 0f);
            bgRt.sizeDelta = new Vector2(24f, 24f);
            bg.GetComponent<Image>().color = new Color(0.85f, 0.85f, 0.85f, 1f);

            var check = new GameObject("Checkmark", typeof(RectTransform), typeof(Image));
            check.transform.SetParent(bg.transform, false);
            Stretch(check.GetComponent<RectTransform>());
            check.GetComponent<Image>().color = new Color(0.2f, 0.7f, 0.35f, 1f);

            var labelGo = new GameObject("Label", typeof(RectTransform), typeof(Text));
            labelGo.transform.SetParent(go.transform, false);
            var labelRt = labelGo.GetComponent<RectTransform>();
            labelRt.anchorMin = new Vector2(0f, 0f);
            labelRt.anchorMax = new Vector2(1f, 1f);
            labelRt.offsetMin = new Vector2(48f, 0f);
            labelRt.offsetMax = Vector2.zero;
            var labelText = labelGo.GetComponent<Text>();
            labelText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            labelText.fontSize = 18;
            labelText.color = Color.black;
            labelText.alignment = TextAnchor.MiddleLeft;
            labelText.text = label;

            toggle.targetGraphic = bg.GetComponent<Image>();
            toggle.graphic = check.GetComponent<Image>();
            toggle.isOn = on;
            return toggle;
        }

        private static GameObject CreateUiButton(Transform parent, string name, string label, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = color;
            var text = CreateUiText(go.transform, "Text", label, 20, TextAnchor.MiddleCenter);
            Stretch(text.rectTransform);
            return go;
        }

        private static void PlaceUi(RectTransform rt, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = Mathf.Approximately(anchor.y, 0f)
                ? new Vector2(0.5f, 0f)
                : new Vector2(0.5f, 1f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
    }
}
