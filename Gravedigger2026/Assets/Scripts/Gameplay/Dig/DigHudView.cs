using System;
using UnityEngine;
using UnityEngine.UI;

namespace Gravedigger2026.Gameplay.Dig
{
    public sealed class DigHudView : MonoBehaviour
    {
        private const float PortraitSize = 60f;
        private const float GmButtonHeight = 40f;
        private const float GmButtonGap = 8f;
        private const float GmButtonStep = GmButtonHeight + GmButtonGap;

        [SerializeField] private GameObject _root;
        [SerializeField] private Text _timerText;
        [SerializeField] private Text _warehouseText;
        [SerializeField] private Button _addGravesButton;
        [SerializeField] private Button _addBodyPartsButton;
        [SerializeField] private Button _equipWarriorEnhanceButton;
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
        public event Action EquipWarriorEnhanceRequested;
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

        private void OnEnable()
        {
            EnsureCanvasLayers();
            EnsurePortraitFrame();
            EnsureGmMenu();
            Wire(_gmToggleButton, HandleGmToggle);
            Wire(_addGravesButton, HandleAddGraves);
            Wire(_addBodyPartsButton, HandleAddBodyParts);
            Wire(_equipWarriorEnhanceButton, HandleEquipWarriorEnhance);
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
            Unwire(_equipWarriorEnhanceButton, HandleEquipWarriorEnhance);
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

        private void HandleAddGraves()
        {
            AddGravesRequested?.Invoke();
        }

        private void HandleAddBodyParts()
        {
            AddBodyPartsRequested?.Invoke();
        }

        private void HandleEquipWarriorEnhance()
        {
            EquipWarriorEnhanceRequested?.Invoke();
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

        public void SetWarriorEnhanceGmVisible(bool visible)
        {
            EnsureGmMenu();
            if (_equipWarriorEnhanceButton != null)
            {
                _equipWarriorEnhanceButton.gameObject.SetActive(visible);
            }
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
                _gmToggleButton = FindOrCreateGmButton(parent, "GmToggleButton", "GM", new Vector2(-24f, -86f), new Vector2(80f, GmButtonHeight));
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
                    panelRt.sizeDelta = new Vector2(200f, 780f);
                    _gmMenuPanel = panelGo;
                }

                _gmMenuPanel.SetActive(false);
            }

            var menuParent = _gmMenuPanel.transform;
            ReparentGmButton(menuParent, ref _addGravesButton, "GmAddGravesButton", "增加坟墓", 0);
            ReparentGmButton(menuParent, ref _addBodyPartsButton, "GmAddBodyPartsButton", "增加躯体材料", 1);
            ReparentGmButton(menuParent, ref _equipWarriorEnhanceButton, "GmEquipWarriorEnhanceButton", "装备战士强化", 2);
            if (_equipWarriorEnhanceButton != null)
            {
                _equipWarriorEnhanceButton.gameObject.SetActive(false);
            }
            ReparentGmButton(menuParent, ref _acquireDigRingButton, "GmAcquireDigRingButton", "获得铁铲", 3);
            ReparentGmButton(menuParent, ref _grantEquipCommonExpButton, "GmGrantEquipCommonExpButton", "装备公共经验+50", 4);
            ReparentGmButton(menuParent, ref _spendDigRingCommonExpButton, "GmSpendDigRingCommonExpButton", "划入铁铲升级", 5);
            ReparentGmButton(menuParent, ref _acquireMinerLampButton, "GmAcquireMinerLampButton", "获得矿灯", 6);
            ReparentGmButton(menuParent, ref _spendMinerLampCommonExpButton, "GmSpendMinerLampCommonExpButton", "划入矿灯升级", 7);
            ReparentGmButton(menuParent, ref _acquireExplosivesButton, "GmAcquireExplosivesButton", "获得炸药", 8);
            ReparentGmButton(menuParent, ref _spendExplosivesCommonExpButton, "GmSpendExplosivesCommonExpButton", "划入炸药升级", 9);
            ReparentGmButton(menuParent, ref _acquireLightningButton, "GmAcquireLightningButton", "获得引雷", 10);
            ReparentGmButton(menuParent, ref _spendLightningCommonExpButton, "GmSpendLightningCommonExpButton", "划入引雷升级", 11);
            ReparentGmButton(menuParent, ref _acquireDetectorButton, "GmAcquireDetectorButton", "获得探测器", 12);
            ReparentGmButton(menuParent, ref _spendDetectorCommonExpButton, "GmSpendDetectorCommonExpButton", "划入探测器升级", 13);
            ReparentGmButton(menuParent, ref _acquireHumanTokenButton, "GmAcquireHumanTokenButton", "获得人类信物", 14);
            ReparentGmButton(menuParent, ref _spendHumanTokenCommonExpButton, "GmSpendHumanTokenCommonExpButton", "划入人类信物升级", 15);
            ReparentGmButton(menuParent, ref _acquireElfTokenButton, "GmAcquireElfTokenButton", "获得精灵信物", 16);
            ReparentGmButton(menuParent, ref _spendElfTokenCommonExpButton, "GmSpendElfTokenCommonExpButton", "划入精灵信物升级", 17);
            ReparentGmButton(menuParent, ref _acquireOrcTokenButton, "GmAcquireOrcTokenButton", "获得兽人信物", 18);
            ReparentGmButton(menuParent, ref _spendOrcTokenCommonExpButton, "GmSpendOrcTokenCommonExpButton", "划入兽人信物升级", 19);
        }

        private void ReparentGmButton(Transform menuParent, ref Button button, string name, string label, int index)
        {
            if (button == null)
            {
                button = FindOrCreateGmButton(menuParent, name, label, new Vector2(0f, -index * GmButtonStep), new Vector2(180f, GmButtonHeight));
                return;
            }

            button.transform.SetParent(menuParent, false);
            PlaceGmMenuButton(button.GetComponent<RectTransform>(), index);
            SetGmButtonLabel(button, label);
        }

        private static void PlaceGmMenuButton(RectTransform rt, int index)
        {
            if (rt == null)
            {
                return;
            }

            rt.anchorMin = new Vector2(1f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(1f, 1f);
            rt.anchoredPosition = new Vector2(0f, -index * GmButtonStep);
            rt.sizeDelta = new Vector2(180f, GmButtonHeight);
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
            var existing = parent.Find(name);
            if (existing != null)
            {
                var existingBtn = existing.GetComponent<Button>();
                if (existingBtn != null)
                {
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
            text.fontSize = 20;
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
