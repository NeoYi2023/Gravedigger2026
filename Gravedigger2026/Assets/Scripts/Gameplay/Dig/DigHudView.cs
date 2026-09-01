using System;
using System.Collections.Generic;
using UnityEngine;
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

        [SerializeField] private GameObject _root;
        [SerializeField] private Text _timerText;
        [SerializeField] private Text _warehouseText;
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

        private void OnEnable()
        {
            EnsureCanvasLayers();
            EnsurePortraitFrame();
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
            if (_warehouseText != null)
            {
                _warehouseText.text = summary ?? string.Empty;
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
            if (_warehouseText == null)
            {
                return;
            }

            var whRt = _warehouseText.GetComponent<RectTransform>();
            if (whRt != null && whRt.anchoredPosition.y > -90f)
            {
                whRt.anchoredPosition = new Vector2(whRt.anchoredPosition.x, -90f);
            }
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
