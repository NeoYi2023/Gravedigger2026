using System;
using System.Collections.Generic;
using Gravedigger2026.Core.Dig;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Gravedigger2026.Gameplay.Dig
{
    public readonly struct GmMagicBookGmEntry
    {
        public readonly string MagicBookId;
        public readonly string Label;

        public GmMagicBookGmEntry(string magicBookId, string label)
        {
            MagicBookId = magicBookId ?? string.Empty;
            Label = string.IsNullOrEmpty(label) ? MagicBookId : label;
        }
    }

    public sealed class DigHudView : MonoBehaviour
    {
        private const float PortraitSize = 60f;
        private const float WarehouseIconSize = 60f;
        private const int WarehouseValueFontSize = 24;
        private const float WarehouseCellGap = 10f;
        private const float WarehouseRowGap = 20f;
        private const float WarehouseValueHeight = 28f;
        private const float WarehouseCellHeight = WarehouseIconSize + WarehouseValueHeight;
        private const float GmButtonHeight = 40f;
        private const float GmButtonGap = 8f;
        private const float GmRowStep = GmButtonHeight + GmButtonGap;
        private const float GmColWidth = 172f;
        private const float GmColGap = 8f;
        private const float GmLayerGap = 16f;
        private const float GmPanelWidth = GmColWidth * 2f + GmColGap;
        private const int DigMagicFixedRows = 2;
        private const int EquipRows = 8;
        private const string MagicBookButtonPrefix = "GmEquipMagicBook_";
        public const string DigWarehouseHoverTipsKey = "DigWarehouseHoverTips";

        [SerializeField] private GameObject _root;
        [SerializeField] private Text _timerText;
        [SerializeField] private Text _warehouseText;
        [SerializeField] private RectTransform _warehouseRoot;
        [SerializeField] private Sprite _spiritIcon;
        [SerializeField] private Sprite _wreckIcon;
        [SerializeField] private Sprite _raceUndeadIcon;
        [SerializeField] private Sprite _raceOrcIcon;
        [SerializeField] private Sprite _raceElfIcon;
        [SerializeField] private Sprite _raceHumanIcon;
        [SerializeField] private Sprite _classWarriorIcon;
        [SerializeField] private Sprite _classArcherIcon;
        [SerializeField] private Sprite _classMageIcon;
        [SerializeField] private Sprite _classAssassinIcon;
        [SerializeField] private Button _addGravesButton;
        [SerializeField] private Button _addBodyPartsButton;
        [SerializeField] private Button _acquireDigRingButton;
        [SerializeField] private Button _grantEquipCommonExpButton;
        [SerializeField] private Button _spendDigRingCommonExpButton;
        [SerializeField] private Button _acquireMinerLampButton;
        [SerializeField] private Button _spendMinerLampCommonExpButton;
        [SerializeField] private Button _acquireExplosivesButton;
        [SerializeField] private Button _spendExplosivesCommonExpButton;
        [SerializeField] private Button _acquireLightningButton;
        [SerializeField] private Button _spendLightningCommonExpButton;
        [SerializeField] private Button _acquireDetectorButton;
        [SerializeField] private Button _spendDetectorCommonExpButton;
        [SerializeField] private Button _acquireHumanTokenButton;
        [SerializeField] private Button _spendHumanTokenCommonExpButton;
        [SerializeField] private Button _acquireElfTokenButton;
        [SerializeField] private Button _spendElfTokenCommonExpButton;
        [SerializeField] private Button _acquireOrcTokenButton;
        [SerializeField] private Button _spendOrcTokenCommonExpButton;
        [SerializeField] private Button _gmToggleButton;
        [SerializeField] private GameObject _gmMenuPanel;
        [SerializeField] private RectTransform _rewardFlyerLayer;
        [SerializeField] private RectTransform _portraitFrame;
        [SerializeField] private Image _portraitImage;
        [SerializeField] private Sprite _portraitSprite;

        public event Action AddGravesRequested;
        public event Action AddBodyPartsRequested;
        public event Action<string> EquipMagicBookRequested;
        public event Action AcquireDigRingRequested;
        public event Action GrantEquipCommonExpRequested;
        public event Action SpendDigRingCommonExpRequested;
        public event Action AcquireMinerLampRequested;
        public event Action SpendMinerLampCommonExpRequested;
        public event Action AcquireExplosivesRequested;
        public event Action SpendExplosivesCommonExpRequested;
        public event Action AcquireLightningRequested;
        public event Action SpendLightningCommonExpRequested;
        public event Action AcquireDetectorRequested;
        public event Action SpendDetectorCommonExpRequested;
        public event Action AcquireHumanTokenRequested;
        public event Action SpendHumanTokenCommonExpRequested;
        public event Action AcquireElfTokenRequested;
        public event Action SpendElfTokenCommonExpRequested;
        public event Action AcquireOrcTokenRequested;
        public event Action SpendOrcTokenCommonExpRequested;

        public RectTransform PortraitFrame => _portraitFrame;
        public RectTransform RewardFlyerLayer
        {
            get
            {
                EnsureCanvasLayers();
                return _rewardFlyerLayer;
            }
        }

        private bool _gmMenuOpen;
        private Transform _digMagicLayer;
        private Transform _equipLayer;
        private int _magicBookRows;
        private readonly List<Button> _magicBookButtons = new List<Button>();
        private readonly List<UnityEngine.Events.UnityAction> _magicBookHandlers =
            new List<UnityEngine.Events.UnityAction>();

        private RectTransform _statsRow1;
        private RectTransform _statsRow2;
        private RectTransform _statsRow3;
        private WarehouseStatCell _spiritCell;
        private WarehouseStatCell _wreckCell;
        private WarehouseStatCell _undeadCell;
        private WarehouseStatCell _orcCell;
        private WarehouseStatCell _elfCell;
        private WarehouseStatCell _humanCell;
        private WarehouseStatCell _warriorCell;
        private WarehouseStatCell _archerCell;
        private WarehouseStatCell _mageCell;
        private WarehouseStatCell _assassinCell;
        private RectTransform _warehouseTipsPanel;
        private Text _warehouseTipsText;
        private string _warehouseTipsCopy = string.Empty;
        private bool _warehouseStatsBuilt;

        private sealed class WarehouseStatCell
        {
            public GameObject Root;
            public Image Icon;
            public Text Value;
        }

        private void OnEnable()
        {
            EnsureCanvasLayers();
            EnsurePortraitFrame();
            EnsureWarehouseStats();
            EnsureGmMenu();
            Wire(_gmToggleButton, HandleGmToggle);
            Wire(_addGravesButton, HandleAddGraves);
            Wire(_addBodyPartsButton, HandleAddBodyParts);
            Wire(_acquireDigRingButton, HandleAcquireDigRing);
            Wire(_grantEquipCommonExpButton, HandleGrantEquipCommonExp);
            Wire(_spendDigRingCommonExpButton, HandleSpendDigRingCommonExp);
            Wire(_acquireMinerLampButton, HandleAcquireMinerLamp);
            Wire(_spendMinerLampCommonExpButton, HandleSpendMinerLampCommonExp);
            Wire(_acquireExplosivesButton, HandleAcquireExplosives);
            Wire(_spendExplosivesCommonExpButton, HandleSpendExplosivesCommonExp);
            Wire(_acquireLightningButton, HandleAcquireLightning);
            Wire(_spendLightningCommonExpButton, HandleSpendLightningCommonExp);
            Wire(_acquireDetectorButton, HandleAcquireDetector);
            Wire(_spendDetectorCommonExpButton, HandleSpendDetectorCommonExp);
            Wire(_acquireHumanTokenButton, HandleAcquireHumanToken);
            Wire(_spendHumanTokenCommonExpButton, HandleSpendHumanTokenCommonExp);
            Wire(_acquireElfTokenButton, HandleAcquireElfToken);
            Wire(_spendElfTokenCommonExpButton, HandleSpendElfTokenCommonExp);
            Wire(_acquireOrcTokenButton, HandleAcquireOrcToken);
            Wire(_spendOrcTokenCommonExpButton, HandleSpendOrcTokenCommonExp);
        }

        private void OnDisable()
        {
            Unwire(_gmToggleButton, HandleGmToggle);
            Unwire(_addGravesButton, HandleAddGraves);
            Unwire(_addBodyPartsButton, HandleAddBodyParts);
            Unwire(_acquireDigRingButton, HandleAcquireDigRing);
            Unwire(_grantEquipCommonExpButton, HandleGrantEquipCommonExp);
            Unwire(_spendDigRingCommonExpButton, HandleSpendDigRingCommonExp);
            Unwire(_acquireMinerLampButton, HandleAcquireMinerLamp);
            Unwire(_spendMinerLampCommonExpButton, HandleSpendMinerLampCommonExp);
            Unwire(_acquireExplosivesButton, HandleAcquireExplosives);
            Unwire(_spendExplosivesCommonExpButton, HandleSpendExplosivesCommonExp);
            Unwire(_acquireLightningButton, HandleAcquireLightning);
            Unwire(_spendLightningCommonExpButton, HandleSpendLightningCommonExp);
            Unwire(_acquireDetectorButton, HandleAcquireDetector);
            Unwire(_spendDetectorCommonExpButton, HandleSpendDetectorCommonExp);
            Unwire(_acquireHumanTokenButton, HandleAcquireHumanToken);
            Unwire(_spendHumanTokenCommonExpButton, HandleSpendHumanTokenCommonExp);
            Unwire(_acquireElfTokenButton, HandleAcquireElfToken);
            Unwire(_spendElfTokenCommonExpButton, HandleSpendElfTokenCommonExp);
            Unwire(_acquireOrcTokenButton, HandleAcquireOrcToken);
            Unwire(_spendOrcTokenCommonExpButton, HandleSpendOrcTokenCommonExp);
        }

        public void Show()
        {
            EnsureCanvasLayers();
            EnsurePortraitFrame();
            EnsureGmMenu();
            if (_root != null)
            {
                _root.SetActive(true);
                var image = _root.GetComponent<Image>();
                if (image != null)
                {
                    image.raycastTarget = false;
                }
            }

            if (_portraitFrame != null)
            {
                _portraitFrame.gameObject.SetActive(true);
            }

            SetGmChromeVisible(true);
            SetGmMenuOpen(false);
        }

        public void Hide()
        {
            SetGmMenuOpen(false);
            SetGmChromeVisible(false);
            if (_root != null)
            {
                _root.SetActive(false);
            }
        }

        /// <summary>Deprecated: fog pulse is owned by CameraFogService.</summary>
        public void SetCameraFogPulseActive(bool active)
        {
            // No-op — DigStageController drives CameraFogService directly.
        }

        /// <summary>
        /// Screen-space center of the Dig HUD portrait (for DigReward fly target).
        /// Overlay canvas: pass null uiCamera.
        /// </summary>
        public bool TryGetPortraitScreenPoint(Camera uiCamera, out Vector2 screenPoint)
        {
            EnsurePortraitFrame();
            screenPoint = default;
            if (_portraitFrame == null)
            {
                return false;
            }

            var corners = new Vector3[4];
            _portraitFrame.GetWorldCorners(corners);
            var worldCenter = (corners[0] + corners[2]) * 0.5f;
            screenPoint = RectTransformUtility.WorldToScreenPoint(uiCamera, worldCenter);
            return true;
        }

        public void SetTimer(float remaining, float total)
        {
            if (_timerText == null)
            {
                return;
            }

            _timerText.text = $"Dig 剩余 {Mathf.CeilToInt(remaining)} / {Mathf.CeilToInt(total)} 秒";
        }

        public void SetWarehouse(string summary)
        {
            // Legacy text API retained for DigAssetBuilder wiring; stats UI supersedes it.
            if (_warehouseText != null)
            {
                _warehouseText.enabled = false;
            }
        }

        public void SetWarehouseTips(string tipsCopy)
        {
            _warehouseTipsCopy = tipsCopy ?? string.Empty;
            if (_warehouseTipsText != null)
            {
                _warehouseTipsText.text = _warehouseTipsCopy;
            }
        }

        public void SetWarehouseStats(DigWarehouseHudStats stats)
        {
            EnsureWarehouseStats();
            if (stats == null)
            {
                stats = new DigWarehouseHudStats();
            }

            ApplyCell(_spiritCell, _spiritIcon, stats.Spirit > 0f, FormatSpirit(stats.Spirit));
            ApplyCell(_wreckCell, _wreckIcon, stats.WreckCount > 0, stats.WreckCount.ToString());
            ApplyCell(_undeadCell, _raceUndeadIcon, stats.UndeadPrimaryHand > 0, stats.UndeadPrimaryHand.ToString());
            ApplyCell(_orcCell, _raceOrcIcon, stats.OrcPrimaryHand > 0, stats.OrcPrimaryHand.ToString());
            ApplyCell(_elfCell, _raceElfIcon, stats.ElfPrimaryHand > 0, stats.ElfPrimaryHand.ToString());
            ApplyCell(_humanCell, _raceHumanIcon, stats.HumanPrimaryHand > 0, stats.HumanPrimaryHand.ToString());
            ApplyCell(_warriorCell, _classWarriorIcon, stats.WarriorPrimaryHand > 0, stats.WarriorPrimaryHand.ToString());
            ApplyCell(_archerCell, _classArcherIcon, stats.ArcherPrimaryHand > 0, stats.ArcherPrimaryHand.ToString());
            ApplyCell(_mageCell, _classMageIcon, stats.MagePrimaryHand > 0, stats.MagePrimaryHand.ToString());
            ApplyCell(_assassinCell, _classAssassinIcon, stats.ThiefPrimaryHand > 0, stats.ThiefPrimaryHand.ToString());
            RelayoutWarehouseRows();
        }

        private static string FormatSpirit(float spirit)
        {
            if (Mathf.Approximately(spirit, Mathf.Round(spirit)))
            {
                return Mathf.RoundToInt(spirit).ToString();
            }

            return spirit.ToString("0.##");
        }

        private static void ApplyCell(WarehouseStatCell cell, Sprite icon, bool visible, string valueText)
        {
            if (cell == null || cell.Root == null)
            {
                return;
            }

            cell.Root.SetActive(visible);
            if (!visible)
            {
                return;
            }

            if (cell.Icon != null)
            {
                cell.Icon.sprite = icon;
                cell.Icon.enabled = icon != null;
            }

            if (cell.Value != null)
            {
                cell.Value.text = valueText ?? string.Empty;
            }
        }

        /// <summary>
        /// Rebuild Mode2 MagicBook GM buttons (two-col under Dig commons). Mode1: visible=false.
        /// </summary>
        public void RebuildMagicBookGmButtons(IReadOnlyList<GmMagicBookGmEntry> entries, bool visible)
        {
            EnsureGmMenu();
            ClearMagicBookButtons();

            if (!visible || entries == null || entries.Count == 0)
            {
                _magicBookRows = 0;
                RefreshGmMenuPanelSize();
                return;
            }

            for (var i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                if (string.IsNullOrEmpty(entry.MagicBookId))
                {
                    continue;
                }

                var row = DigMagicFixedRows + (i / 2);
                var col = i % 2;
                var safeName = MagicBookButtonPrefix + SanitizeButtonName(entry.MagicBookId);
                var button = FindOrCreateGmButton(
                    _digMagicLayer,
                    safeName,
                    entry.Label,
                    ColAnchoredPos(row, col),
                    new Vector2(GmColWidth, GmButtonHeight));
                PlaceGmMenuButton(button.GetComponent<RectTransform>(), row, col);

                var bookId = entry.MagicBookId;
                UnityEngine.Events.UnityAction handler = () => EquipMagicBookRequested?.Invoke(bookId);
                button.onClick.AddListener(handler);
                _magicBookButtons.Add(button);
                _magicBookHandlers.Add(handler);
            }

            _magicBookRows = (_magicBookButtons.Count + 1) / 2;
            RefreshGmMenuPanelSize();
        }

        private void HandleAddGraves()
        {
            AddGravesRequested?.Invoke();
        }

        private void HandleAddBodyParts()
        {
            AddBodyPartsRequested?.Invoke();
        }

        private void HandleAcquireDigRing()
        {
            AcquireDigRingRequested?.Invoke();
        }

        private void HandleGrantEquipCommonExp()
        {
            GrantEquipCommonExpRequested?.Invoke();
        }

        private void HandleSpendDigRingCommonExp()
        {
            SpendDigRingCommonExpRequested?.Invoke();
        }

        private void HandleAcquireMinerLamp()
        {
            AcquireMinerLampRequested?.Invoke();
        }

        private void HandleSpendMinerLampCommonExp()
        {
            SpendMinerLampCommonExpRequested?.Invoke();
        }

        private void HandleAcquireExplosives()
        {
            AcquireExplosivesRequested?.Invoke();
        }

        private void HandleSpendExplosivesCommonExp()
        {
            SpendExplosivesCommonExpRequested?.Invoke();
        }

        private void HandleAcquireLightning()
        {
            AcquireLightningRequested?.Invoke();
        }

        private void HandleSpendLightningCommonExp()
        {
            SpendLightningCommonExpRequested?.Invoke();
        }

        private void HandleAcquireDetector()
        {
            AcquireDetectorRequested?.Invoke();
        }

        private void HandleSpendDetectorCommonExp()
        {
            SpendDetectorCommonExpRequested?.Invoke();
        }

        private void HandleAcquireHumanToken()
        {
            AcquireHumanTokenRequested?.Invoke();
        }

        private void HandleSpendHumanTokenCommonExp()
        {
            SpendHumanTokenCommonExpRequested?.Invoke();
        }

        private void HandleAcquireElfToken()
        {
            AcquireElfTokenRequested?.Invoke();
        }

        private void HandleSpendElfTokenCommonExp()
        {
            SpendElfTokenCommonExpRequested?.Invoke();
        }

        private void HandleAcquireOrcToken()
        {
            AcquireOrcTokenRequested?.Invoke();
        }

        private void HandleSpendOrcTokenCommonExp()
        {
            SpendOrcTokenCommonExpRequested?.Invoke();
        }

        private void HandleGmToggle()
        {
            SetGmMenuOpen(!_gmMenuOpen);
        }

        private void SetGmChromeVisible(bool visible)
        {
            if (_gmToggleButton != null)
            {
                _gmToggleButton.gameObject.SetActive(visible);
            }

            if (!visible && _gmMenuPanel != null)
            {
                _gmMenuPanel.SetActive(false);
                _gmMenuOpen = false;
            }
        }

        private void SetGmMenuOpen(bool open)
        {
            _gmMenuOpen = open;
            if (_gmMenuPanel != null)
            {
                _gmMenuPanel.SetActive(open);
            }
        }

        private void EnsureCanvasLayers()
        {
            var canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                return;
            }

            canvas.sortingOrder = DigUiLayering.HudCanvasOrder;

            var canvasTransform = canvas.transform;

            var flyerLayer = canvasTransform.Find("RewardFlyerLayer") as RectTransform;
            if (flyerLayer == null)
            {
                var layerGo = new GameObject("RewardFlyerLayer", typeof(RectTransform));
                layerGo.transform.SetParent(canvasTransform, false);
                flyerLayer = layerGo.GetComponent<RectTransform>();
                StretchRect(flyerLayer);
                var blocker = layerGo.GetComponent<Image>();
                if (blocker != null)
                {
                    Destroy(blocker);
                }
            }

            flyerLayer.SetAsLastSibling();
            _rewardFlyerLayer = flyerLayer;

            var summaryRoot = canvasTransform.Find("SummaryRoot");
            if (summaryRoot != null)
            {
                summaryRoot.SetAsLastSibling();
            }
            else
            {
                flyerLayer.SetAsLastSibling();
            }
        }

        private static void StretchRect(RectTransform rt)
        {
            if (rt == null)
            {
                return;
            }

            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0.5f);
        }

        private void EnsureGmMenu()
        {
            var parent = _root != null ? _root.transform : transform;
            if (_gmToggleButton == null)
            {
                _gmToggleButton = FindOrCreateGmButton(
                    parent, "GmToggleButton", "GM",
                    new Vector2(-24f, -86f), new Vector2(80f, GmButtonHeight));
            }

            if (_gmMenuPanel == null)
            {
                var existing = parent.Find("GmMenuPanel");
                if (existing != null)
                {
                    _gmMenuPanel = existing.gameObject;
                }
                else
                {
                    var panelGo = new GameObject("GmMenuPanel", typeof(RectTransform));
                    panelGo.transform.SetParent(parent, false);
                    var panelRt = panelGo.GetComponent<RectTransform>();
                    panelRt.anchorMin = new Vector2(1f, 1f);
                    panelRt.anchorMax = new Vector2(1f, 1f);
                    panelRt.pivot = new Vector2(1f, 1f);
                    panelRt.anchoredPosition = new Vector2(-24f, -134f);
                    panelRt.sizeDelta = new Vector2(GmPanelWidth, 780f);
                    _gmMenuPanel = panelGo;
                }

                _gmMenuPanel.SetActive(false);
            }

            var menuParent = _gmMenuPanel.transform;
            DestroyLegacyWarriorEnhanceButton(menuParent);

            _digMagicLayer = EnsureGmLayer(menuParent, "GmLayerDigMagic", 0f);
            _equipLayer = EnsureGmLayer(menuParent, "GmLayerEquip", 0f);

            // Top layer: Dig commons (row0–1) + MagicBooks (row2+)
            ReparentGmButton(_digMagicLayer, ref _addGravesButton, "GmAddGravesButton", "增加坟墓", 0, 0);
            ReparentGmButton(_digMagicLayer, ref _addBodyPartsButton, "GmAddBodyPartsButton", "增加躯体材料", 0, 1);
            ReparentGmButton(
                _digMagicLayer, ref _grantEquipCommonExpButton, "GmGrantEquipCommonExpButton",
                "装备公共经验+50", 1, 0);

            // Bottom layer: grant (col0) / spend (col1) pairs
            ReparentGmButton(_equipLayer, ref _acquireDigRingButton, "GmAcquireDigRingButton", "获得铁铲", 0, 0);
            ReparentGmButton(
                _equipLayer, ref _spendDigRingCommonExpButton, "GmSpendDigRingCommonExpButton",
                "划入铁铲升级", 0, 1);
            ReparentGmButton(_equipLayer, ref _acquireMinerLampButton, "GmAcquireMinerLampButton", "获得矿灯", 1, 0);
            ReparentGmButton(
                _equipLayer, ref _spendMinerLampCommonExpButton, "GmSpendMinerLampCommonExpButton",
                "划入矿灯升级", 1, 1);
            ReparentGmButton(
                _equipLayer, ref _acquireExplosivesButton, "GmAcquireExplosivesButton", "获得炸药", 2, 0);
            ReparentGmButton(
                _equipLayer, ref _spendExplosivesCommonExpButton, "GmSpendExplosivesCommonExpButton",
                "划入炸药升级", 2, 1);
            ReparentGmButton(
                _equipLayer, ref _acquireLightningButton, "GmAcquireLightningButton", "获得引雷", 3, 0);
            ReparentGmButton(
                _equipLayer, ref _spendLightningCommonExpButton, "GmSpendLightningCommonExpButton",
                "划入引雷升级", 3, 1);
            ReparentGmButton(
                _equipLayer, ref _acquireDetectorButton, "GmAcquireDetectorButton", "获得探测器", 4, 0);
            ReparentGmButton(
                _equipLayer, ref _spendDetectorCommonExpButton, "GmSpendDetectorCommonExpButton",
                "划入探测器升级", 4, 1);
            ReparentGmButton(
                _equipLayer, ref _acquireHumanTokenButton, "GmAcquireHumanTokenButton", "获得人类信物", 5, 0);
            ReparentGmButton(
                _equipLayer, ref _spendHumanTokenCommonExpButton, "GmSpendHumanTokenCommonExpButton",
                "划入人类信物升级", 5, 1);
            ReparentGmButton(
                _equipLayer, ref _acquireElfTokenButton, "GmAcquireElfTokenButton", "获得精灵信物", 6, 0);
            ReparentGmButton(
                _equipLayer, ref _spendElfTokenCommonExpButton, "GmSpendElfTokenCommonExpButton",
                "划入精灵信物升级", 6, 1);
            ReparentGmButton(
                _equipLayer, ref _acquireOrcTokenButton, "GmAcquireOrcTokenButton", "获得兽人信物", 7, 0);
            ReparentGmButton(
                _equipLayer, ref _spendOrcTokenCommonExpButton, "GmSpendOrcTokenCommonExpButton",
                "划入兽人信物升级", 7, 1);

            RefreshGmMenuPanelSize();
        }

        private void RefreshGmMenuPanelSize()
        {
            if (_gmMenuPanel == null)
            {
                return;
            }

            var digRows = DigMagicFixedRows + Mathf.Max(0, _magicBookRows);
            var digHeight = digRows * GmRowStep;
            var equipHeight = EquipRows * GmRowStep;
            var totalHeight = digHeight + GmLayerGap + equipHeight;

            var panelRt = _gmMenuPanel.GetComponent<RectTransform>();
            if (panelRt != null)
            {
                panelRt.sizeDelta = new Vector2(GmPanelWidth, totalHeight);
            }

            PlaceGmLayer(_digMagicLayer as RectTransform, 0f, digHeight);
            PlaceGmLayer(_equipLayer as RectTransform, -(digHeight + GmLayerGap), equipHeight);
        }

        private static Transform EnsureGmLayer(Transform menuParent, string name, float anchoredY)
        {
            var existing = menuParent.Find(name);
            if (existing != null)
            {
                return existing;
            }

            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(menuParent, false);
            var rt = go.GetComponent<RectTransform>();
            PlaceGmLayer(rt, anchoredY, 0f);
            return go.transform;
        }

        private static void PlaceGmLayer(RectTransform rt, float anchoredY, float height)
        {
            if (rt == null)
            {
                return;
            }

            rt.anchorMin = new Vector2(1f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(1f, 1f);
            rt.anchoredPosition = new Vector2(0f, anchoredY);
            rt.sizeDelta = new Vector2(GmPanelWidth, height);
        }

        private void ReparentGmButton(
            Transform layer,
            ref Button button,
            string name,
            string label,
            int row,
            int col)
        {
            if (button == null)
            {
                button = FindOrCreateGmButton(
                    layer, name, label, ColAnchoredPos(row, col),
                    new Vector2(GmColWidth, GmButtonHeight));
                // Also migrate if button lived under panel root (legacy prefab).
                if (button.transform.parent != layer)
                {
                    button.transform.SetParent(layer, false);
                }

                PlaceGmMenuButton(button.GetComponent<RectTransform>(), row, col);
                SetGmButtonLabel(button, label);
                return;
            }

            // Prefer serialized ref; if still under old parent, move into layer.
            if (_gmMenuPanel != null && button.transform.IsChildOf(_gmMenuPanel.transform)
                && button.transform.parent != layer)
            {
                button.transform.SetParent(layer, false);
            }
            else if (button.transform.parent != layer)
            {
                var found = FindDeep(layer, name) ?? FindDeep(_gmMenuPanel != null ? _gmMenuPanel.transform : null, name);
                if (found != null)
                {
                    button = found.GetComponent<Button>() ?? button;
                }

                button.transform.SetParent(layer, false);
            }

            PlaceGmMenuButton(button.GetComponent<RectTransform>(), row, col);
            SetGmButtonLabel(button, label);
        }

        private static void PlaceGmMenuButton(RectTransform rt, int row, int col)
        {
            if (rt == null)
            {
                return;
            }

            rt.anchorMin = new Vector2(1f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(1f, 1f);
            rt.anchoredPosition = ColAnchoredPos(row, col);
            rt.sizeDelta = new Vector2(GmColWidth, GmButtonHeight);
        }

        /// <summary>col 0 = left, col 1 = right (right-anchored panel).</summary>
        private static Vector2 ColAnchoredPos(int row, int col)
        {
            var x = col <= 0
                ? -(GmColWidth + GmColGap)
                : 0f;
            return new Vector2(x, -row * GmRowStep);
        }

        private void ClearMagicBookButtons()
        {
            for (var i = 0; i < _magicBookButtons.Count; i++)
            {
                var button = _magicBookButtons[i];
                if (button == null)
                {
                    continue;
                }

                if (i < _magicBookHandlers.Count && _magicBookHandlers[i] != null)
                {
                    button.onClick.RemoveListener(_magicBookHandlers[i]);
                }

                Destroy(button.gameObject);
            }

            _magicBookButtons.Clear();
            _magicBookHandlers.Clear();
        }

        private static void DestroyLegacyWarriorEnhanceButton(Transform menuParent)
        {
            if (menuParent == null)
            {
                return;
            }

            var legacy = FindDeep(menuParent, "GmEquipWarriorEnhanceButton");
            if (legacy != null)
            {
                Destroy(legacy.gameObject);
            }
        }

        private static Transform FindDeep(Transform root, string name)
        {
            if (root == null || string.IsNullOrEmpty(name))
            {
                return null;
            }

            var direct = root.Find(name);
            if (direct != null)
            {
                return direct;
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

        private static string SanitizeButtonName(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return "Unknown";
            }

            return id.Replace('/', '_').Replace('\\', '_').Replace(' ', '_');
        }

        private void EnsurePortraitFrame()
        {
            if (_portraitFrame != null && _portraitImage != null)
            {
                ApplyPortraitSprite();
                NudgeWarehouseBelowPortrait();
                return;
            }

            var parent = _root != null ? _root.transform : transform;
            var existing = parent.Find("ProtagonistPortrait");
            if (existing != null)
            {
                _portraitFrame = existing as RectTransform ?? existing.GetComponent<RectTransform>();
                _portraitImage = existing.Find("Icon")?.GetComponent<Image>()
                    ?? existing.GetComponent<Image>();
                ApplyPortraitSprite();
                NudgeWarehouseBelowPortrait();
                return;
            }

            var frameGo = new GameObject("ProtagonistPortrait", typeof(RectTransform), typeof(Image));
            frameGo.transform.SetParent(parent, false);
            var frameImg = frameGo.GetComponent<Image>();
            frameImg.color = new Color(0.12f, 0.14f, 0.18f, 0.95f);
            frameImg.raycastTarget = false;

            _portraitFrame = frameGo.GetComponent<RectTransform>();
            _portraitFrame.anchorMin = new Vector2(0f, 1f);
            _portraitFrame.anchorMax = new Vector2(0f, 1f);
            _portraitFrame.pivot = new Vector2(0f, 1f);
            _portraitFrame.anchoredPosition = new Vector2(16f, -16f);
            _portraitFrame.sizeDelta = new Vector2(PortraitSize, PortraitSize);

            var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconGo.transform.SetParent(frameGo.transform, false);
            var iconRt = iconGo.GetComponent<RectTransform>();
            iconRt.anchorMin = Vector2.zero;
            iconRt.anchorMax = Vector2.one;
            iconRt.offsetMin = new Vector2(4f, 4f);
            iconRt.offsetMax = new Vector2(-4f, -4f);
            _portraitImage = iconGo.GetComponent<Image>();
            _portraitImage.raycastTarget = false;
            _portraitImage.preserveAspect = true;
            ApplyPortraitSprite();
            NudgeWarehouseBelowPortrait();
        }

        private void NudgeWarehouseBelowPortrait()
        {
            var whRt = _warehouseRoot;
            if (whRt == null && _warehouseText != null)
            {
                whRt = _warehouseText.GetComponent<RectTransform>();
            }

            if (whRt != null && whRt.anchoredPosition.y > -90f)
            {
                whRt.anchoredPosition = new Vector2(whRt.anchoredPosition.x, -90f);
            }
        }

        private void EnsureWarehouseStats()
        {
            if (_warehouseStatsBuilt && _warehouseRoot != null && _statsRow1 != null)
            {
                return;
            }

            var parent = _root != null ? _root.transform : transform;
            if (_warehouseRoot == null)
            {
                var existing = parent.Find("Warehouse");
                if (existing != null)
                {
                    _warehouseRoot = existing as RectTransform ?? existing.GetComponent<RectTransform>();
                }
            }

            if (_warehouseRoot == null)
            {
                var go = new GameObject("Warehouse", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(parent, false);
                _warehouseRoot = go.GetComponent<RectTransform>();
                _warehouseRoot.anchorMin = new Vector2(0f, 1f);
                _warehouseRoot.anchorMax = new Vector2(0f, 1f);
                _warehouseRoot.pivot = new Vector2(0f, 1f);
                _warehouseRoot.anchoredPosition = new Vector2(24f, -90f);
                _warehouseRoot.sizeDelta = new Vector2(420f, 260f);
            }

            // Legacy DigStageRoot used Text on Warehouse; Image cannot be added while Text remains.
            var legacyText = _warehouseRoot.GetComponent<Text>();
            if (legacyText != null)
            {
                if (_warehouseText == legacyText)
                {
                    _warehouseText = null;
                }

                DestroyImmediate(legacyText);
            }
            else if (_warehouseText != null)
            {
                _warehouseText.enabled = false;
                _warehouseText.raycastTarget = false;
            }

            var rootImg = _warehouseRoot.GetComponent<Image>();
            if (rootImg == null)
            {
                rootImg = _warehouseRoot.gameObject.AddComponent<Image>();
            }

            if (rootImg == null)
            {
                Debug.LogError("[DigHudView] Warehouse root Image missing; stats hover disabled.");
                return;
            }

            rootImg.color = Color.clear;
            rootImg.raycastTarget = true;

            ClearWarehouseChildrenExceptLegacyText();
            _statsRow1 = CreateStatsRow(_warehouseRoot, "Row1", 0f);
            _statsRow2 = CreateStatsRow(_warehouseRoot, "Row2", -(WarehouseCellHeight + WarehouseRowGap));
            _statsRow3 = CreateStatsRow(_warehouseRoot, "Row3", -2f * (WarehouseCellHeight + WarehouseRowGap));

            _spiritCell = CreateStatCell(_statsRow1, "Spirit");
            _wreckCell = CreateStatCell(_statsRow1, "Wreck");
            _undeadCell = CreateStatCell(_statsRow2, "RaceUndead");
            _orcCell = CreateStatCell(_statsRow2, "RaceOrc");
            _elfCell = CreateStatCell(_statsRow2, "RaceElf");
            _humanCell = CreateStatCell(_statsRow2, "RaceHuman");
            _warriorCell = CreateStatCell(_statsRow3, "ClassWarrior");
            _archerCell = CreateStatCell(_statsRow3, "ClassArcher");
            _mageCell = CreateStatCell(_statsRow3, "ClassMage");
            _assassinCell = CreateStatCell(_statsRow3, "ClassAssassin");

            EnsureWarehouseTips();
            WireWarehouseHover();
            TryLoadWarehouseIconsFromEditor();
            RelayoutWarehouseRows();
            NudgeWarehouseBelowPortrait();
            _warehouseStatsBuilt = true;
        }

        private void ClearWarehouseChildrenExceptLegacyText()
        {
            if (_warehouseRoot == null)
            {
                return;
            }

            for (var i = _warehouseRoot.childCount - 1; i >= 0; i--)
            {
                var child = _warehouseRoot.GetChild(i);
                if (_warehouseText != null && child == _warehouseText.transform)
                {
                    continue;
                }

                if (Application.isPlaying)
                {
                    Destroy(child.gameObject);
                }
                else
                {
                    DestroyImmediate(child.gameObject);
                }
            }
        }

        private static RectTransform CreateStatsRow(RectTransform parent, string name, float anchoredY)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(0f, anchoredY);
            rt.sizeDelta = new Vector2(420f, WarehouseCellHeight);
            return rt;
        }

        private WarehouseStatCell CreateStatCell(RectTransform row, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(row, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.sizeDelta = new Vector2(WarehouseIconSize, WarehouseCellHeight);

            var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconGo.transform.SetParent(go.transform, false);
            var iconRt = iconGo.GetComponent<RectTransform>();
            iconRt.anchorMin = new Vector2(0.5f, 1f);
            iconRt.anchorMax = new Vector2(0.5f, 1f);
            iconRt.pivot = new Vector2(0.5f, 1f);
            iconRt.anchoredPosition = Vector2.zero;
            iconRt.sizeDelta = new Vector2(WarehouseIconSize, WarehouseIconSize);
            var icon = iconGo.GetComponent<Image>();
            icon.preserveAspect = true;
            icon.raycastTarget = false;

            var valueGo = new GameObject("Value", typeof(RectTransform), typeof(Text));
            valueGo.transform.SetParent(go.transform, false);
            var valueRt = valueGo.GetComponent<RectTransform>();
            valueRt.anchorMin = new Vector2(0.5f, 1f);
            valueRt.anchorMax = new Vector2(0.5f, 1f);
            valueRt.pivot = new Vector2(0.5f, 1f);
            valueRt.anchoredPosition = new Vector2(0f, -WarehouseIconSize);
            valueRt.sizeDelta = new Vector2(WarehouseIconSize + 20f, WarehouseValueHeight);
            var value = valueGo.GetComponent<Text>();
            value.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            value.fontSize = WarehouseValueFontSize;
            value.alignment = TextAnchor.UpperCenter;
            value.color = Color.white;
            value.raycastTarget = false;
            value.horizontalOverflow = HorizontalWrapMode.Overflow;
            value.verticalOverflow = VerticalWrapMode.Overflow;

            go.SetActive(false);
            return new WarehouseStatCell
            {
                Root = go,
                Icon = icon,
                Value = value
            };
        }

        private void RelayoutWarehouseRows()
        {
            LayoutRow(_statsRow1, _spiritCell, _wreckCell);
            LayoutRow(_statsRow2, _undeadCell, _orcCell, _elfCell, _humanCell);
            LayoutRow(_statsRow3, _warriorCell, _archerCell, _mageCell, _assassinCell);

            var rowY = 0f;
            PlaceRowIfVisible(_statsRow1, ref rowY);
            PlaceRowIfVisible(_statsRow2, ref rowY);
            PlaceRowIfVisible(_statsRow3, ref rowY);

            if (_warehouseRoot != null)
            {
                var height = Mathf.Max(WarehouseCellHeight, -rowY + WarehouseCellHeight);
                _warehouseRoot.sizeDelta = new Vector2(420f, height);
            }
        }

        private static void PlaceRowIfVisible(RectTransform row, ref float rowY)
        {
            if (row == null)
            {
                return;
            }

            var any = false;
            for (var i = 0; i < row.childCount; i++)
            {
                if (row.GetChild(i).gameObject.activeSelf)
                {
                    any = true;
                    break;
                }
            }

            row.gameObject.SetActive(any);
            if (!any)
            {
                return;
            }

            row.anchoredPosition = new Vector2(0f, rowY);
            rowY -= WarehouseCellHeight + WarehouseRowGap;
        }

        private static void LayoutRow(RectTransform row, params WarehouseStatCell[] cells)
        {
            if (row == null || cells == null)
            {
                return;
            }

            var x = 0f;
            for (var i = 0; i < cells.Length; i++)
            {
                var cell = cells[i];
                if (cell == null || cell.Root == null || !cell.Root.activeSelf)
                {
                    continue;
                }

                var rt = cell.Root.GetComponent<RectTransform>();
                rt.anchoredPosition = new Vector2(x, 0f);
                x += WarehouseIconSize + WarehouseCellGap;
            }
        }

        private void EnsureWarehouseTips()
        {
            if (_warehouseRoot == null)
            {
                return;
            }

            if (_warehouseTipsPanel != null)
            {
                return;
            }

            var tipsGo = new GameObject("WarehouseTips", typeof(RectTransform), typeof(Image));
            tipsGo.transform.SetParent(_warehouseRoot, false);
            _warehouseTipsPanel = tipsGo.GetComponent<RectTransform>();
            _warehouseTipsPanel.anchorMin = new Vector2(0f, 1f);
            _warehouseTipsPanel.anchorMax = new Vector2(0f, 1f);
            _warehouseTipsPanel.pivot = new Vector2(0f, 1f);
            _warehouseTipsPanel.anchoredPosition = new Vector2(430f, 0f);
            _warehouseTipsPanel.sizeDelta = new Vector2(280f, 72f);
            var tipsBg = tipsGo.GetComponent<Image>();
            tipsBg.color = new Color(0.08f, 0.1f, 0.14f, 0.92f);
            tipsBg.raycastTarget = false;

            var textGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
            textGo.transform.SetParent(tipsGo.transform, false);
            var textRt = textGo.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = new Vector2(8f, 6f);
            textRt.offsetMax = new Vector2(-8f, -6f);
            _warehouseTipsText = textGo.GetComponent<Text>();
            _warehouseTipsText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            _warehouseTipsText.fontSize = 18;
            _warehouseTipsText.alignment = TextAnchor.UpperLeft;
            _warehouseTipsText.color = Color.white;
            _warehouseTipsText.raycastTarget = false;
            _warehouseTipsText.horizontalOverflow = HorizontalWrapMode.Wrap;
            _warehouseTipsText.verticalOverflow = VerticalWrapMode.Overflow;
            _warehouseTipsText.text = _warehouseTipsCopy;
            tipsGo.SetActive(false);
        }

        private void WireWarehouseHover()
        {
            if (_warehouseRoot == null)
            {
                return;
            }

            var trigger = _warehouseRoot.GetComponent<EventTrigger>();
            if (trigger == null)
            {
                trigger = _warehouseRoot.gameObject.AddComponent<EventTrigger>();
            }

            trigger.triggers.Clear();
            AddTrigger(trigger, EventTriggerType.PointerEnter, _ => ShowWarehouseTips(true));
            AddTrigger(trigger, EventTriggerType.PointerExit, _ => ShowWarehouseTips(false));
        }

        private static void AddTrigger(
            EventTrigger trigger,
            EventTriggerType type,
            UnityEngine.Events.UnityAction<BaseEventData> action)
        {
            var entry = new EventTrigger.Entry { eventID = type };
            entry.callback.AddListener(action);
            trigger.triggers.Add(entry);
        }

        private void ShowWarehouseTips(bool show)
        {
            if (_warehouseTipsPanel == null)
            {
                return;
            }

            if (show && string.IsNullOrEmpty(_warehouseTipsCopy))
            {
                _warehouseTipsPanel.gameObject.SetActive(false);
                return;
            }

            if (_warehouseTipsText != null)
            {
                _warehouseTipsText.text = _warehouseTipsCopy;
            }

            _warehouseTipsPanel.gameObject.SetActive(show);
        }

        private void TryLoadWarehouseIconsFromEditor()
        {
#if UNITY_EDITOR
            if (_spiritIcon == null)
            {
                _spiritIcon = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(
                    "Assets/Art/UI/Icons/Currency_Spirit.png");
            }

            if (_wreckIcon == null)
            {
                _wreckIcon = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(
                    "Assets/Art/UI/Icons/WreckWarehouse.png");
            }

            if (_raceUndeadIcon == null)
            {
                _raceUndeadIcon = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(
                    "Assets/Art/UI/Icons/AllRacesIcon_1.png");
            }

            if (_raceOrcIcon == null)
            {
                _raceOrcIcon = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(
                    "Assets/Art/UI/Icons/OrcIcon_1.png");
            }

            if (_raceElfIcon == null)
            {
                _raceElfIcon = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(
                    "Assets/Art/UI/Icons/ElvesIcon_1.png");
            }

            if (_raceHumanIcon == null)
            {
                _raceHumanIcon = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(
                    "Assets/Art/UI/Icons/HumansIcon_1.png");
            }

            if (_classWarriorIcon == null)
            {
                _classWarriorIcon = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(
                    "Assets/Art/UI/Icons/WarriorIcon.png");
            }

            if (_classArcherIcon == null)
            {
                _classArcherIcon = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(
                    "Assets/Art/UI/Icons/ArcherIcon.png");
            }

            if (_classMageIcon == null)
            {
                _classMageIcon = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(
                    "Assets/Art/UI/Icons/MageIcon.png");
            }

            if (_classAssassinIcon == null)
            {
                _classAssassinIcon = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(
                    "Assets/Art/UI/Icons/AssassinIcon.png");
            }
#endif
        }

        private void ApplyPortraitSprite()
        {
            if (_portraitImage == null)
            {
                return;
            }

            if (_portraitSprite != null)
            {
                _portraitImage.sprite = _portraitSprite;
                _portraitImage.color = Color.white;
                return;
            }

            if (_portraitImage.sprite == null)
            {
                _portraitImage.color = new Color(0.55f, 0.62f, 0.78f, 1f);
            }
        }

        private static Button FindOrCreateGmButton(
            Transform parent,
            string name,
            string label,
            Vector2 anchoredPos,
            Vector2 size)
        {
            Transform existing = null;
            if (parent != null)
            {
                existing = parent.Find(name);
                if (existing == null && parent.parent != null)
                {
                    existing = FindDeep(parent.parent, name);
                }
            }

            if (existing != null)
            {
                var existingBtn = existing.GetComponent<Button>();
                if (existingBtn != null)
                {
                    if (existing.parent != parent && parent != null)
                    {
                        existing.SetParent(parent, false);
                    }

                    SetGmButtonLabel(existingBtn, label);
                    var existingRt = existingBtn.GetComponent<RectTransform>();
                    if (existingRt != null)
                    {
                        existingRt.anchoredPosition = anchoredPos;
                        existingRt.sizeDelta = size;
                    }

                    return existingBtn;
                }
            }

            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.color = new Color(0.22f, 0.28f, 0.38f, 0.92f);
            image.raycastTarget = true;

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(1f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(1f, 1f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;

            var textGo = new GameObject("Label", typeof(RectTransform), typeof(Text));
            textGo.transform.SetParent(go.transform, false);
            var textRt = textGo.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = Vector2.zero;
            textRt.offsetMax = Vector2.zero;
            var text = textGo.GetComponent<Text>();
            text.text = label;
            text.fontSize = 18;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.raycastTarget = false;
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");

            return go.GetComponent<Button>();
        }

        private static void SetGmButtonLabel(Button button, string label)
        {
            if (button == null || string.IsNullOrEmpty(label))
            {
                return;
            }

            var text = button.GetComponentInChildren<Text>(true);
            if (text != null)
            {
                text.text = label;
            }
        }

        private static void Wire(Button button, UnityEngine.Events.UnityAction handler)
        {
            if (button != null)
            {
                button.onClick.AddListener(handler);
            }
        }

        private static void Unwire(Button button, UnityEngine.Events.UnityAction handler)
        {
            if (button != null)
            {
                button.onClick.RemoveListener(handler);
            }
        }
    }
}
