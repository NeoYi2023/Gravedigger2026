using System;
using UnityEngine;
using UnityEngine.UI;

namespace Gravedigger2026.Gameplay.Dig
{
    public sealed class DigHudView : MonoBehaviour
    {
        private const float PortraitSize = 60f;

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

        private void OnEnable()
        {
            EnsurePortraitFrame();
            EnsureGmButtons();
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
            EnsurePortraitFrame();
            EnsureGmButtons();
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

            SetGmButtonsVisible(true);
        }

        public void Hide()
        {
            SetGmButtonsVisible(false);
            if (_root != null)
            {
                _root.SetActive(false);
            }
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
            EnsureGmButtons();
            if (_equipWarriorEnhanceButton != null)
            {
                _equipWarriorEnhanceButton.gameObject.SetActive(visible);
            }
        }

        private void SetGmButtonsVisible(bool visible)
        {
            if (_addGravesButton != null)
            {
                _addGravesButton.gameObject.SetActive(visible);
            }

            if (_addBodyPartsButton != null)
            {
                _addBodyPartsButton.gameObject.SetActive(visible);
            }

            if (_acquireDigRingButton != null)
            {
                _acquireDigRingButton.gameObject.SetActive(visible);
            }

            if (_grantEquipCommonExpButton != null)
            {
                _grantEquipCommonExpButton.gameObject.SetActive(visible);
            }

            if (_spendDigRingCommonExpButton != null)
            {
                _spendDigRingCommonExpButton.gameObject.SetActive(visible);
            }

            if (_acquireMinerLampButton != null)
            {
                _acquireMinerLampButton.gameObject.SetActive(visible);
            }

            if (_spendMinerLampCommonExpButton != null)
            {
                _spendMinerLampCommonExpButton.gameObject.SetActive(visible);
            }

            if (_acquireExplosivesButton != null)
            {
                _acquireExplosivesButton.gameObject.SetActive(visible);
            }

            if (_spendExplosivesCommonExpButton != null)
            {
                _spendExplosivesCommonExpButton.gameObject.SetActive(visible);
            }

            if (_acquireLightningButton != null)
            {
                _acquireLightningButton.gameObject.SetActive(visible);
            }

            if (_spendLightningCommonExpButton != null)
            {
                _spendLightningCommonExpButton.gameObject.SetActive(visible);
            }

            if (_acquireDetectorButton != null)
            {
                _acquireDetectorButton.gameObject.SetActive(visible);
            }

            if (_spendDetectorCommonExpButton != null)
            {
                _spendDetectorCommonExpButton.gameObject.SetActive(visible);
            }

            if (_acquireHumanTokenButton != null)
            {
                _acquireHumanTokenButton.gameObject.SetActive(visible);
            }

            if (_spendHumanTokenCommonExpButton != null)
            {
                _spendHumanTokenCommonExpButton.gameObject.SetActive(visible);
            }

            if (_acquireElfTokenButton != null)
            {
                _acquireElfTokenButton.gameObject.SetActive(visible);
            }

            if (_spendElfTokenCommonExpButton != null)
            {
                _spendElfTokenCommonExpButton.gameObject.SetActive(visible);
            }

            if (_acquireOrcTokenButton != null)
            {
                _acquireOrcTokenButton.gameObject.SetActive(visible);
            }

            if (_spendOrcTokenCommonExpButton != null)
            {
                _spendOrcTokenCommonExpButton.gameObject.SetActive(visible);
            }

            if (_equipWarriorEnhanceButton != null && !visible)
            {
                _equipWarriorEnhanceButton.gameObject.SetActive(false);
            }
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

        private void EnsureGmButtons()
        {
            var parent = _root != null ? _root.transform : transform;
            if (_addGravesButton == null)
            {
                _addGravesButton = FindOrCreateGmButton(parent, "GmAddGravesButton", "增加坟墓", new Vector2(-24f, -86f));
            }

            if (_addBodyPartsButton == null)
            {
                _addBodyPartsButton = FindOrCreateGmButton(parent, "GmAddBodyPartsButton", "增加躯体材料", new Vector2(-24f, -138f));
            }

            if (_equipWarriorEnhanceButton == null)
            {
                _equipWarriorEnhanceButton = FindOrCreateGmButton(
                    parent,
                    "GmEquipWarriorEnhanceButton",
                    "装备战士强化",
                    new Vector2(-24f, -190f));
                _equipWarriorEnhanceButton.gameObject.SetActive(false);
            }

            if (_acquireDigRingButton == null)
            {
                _acquireDigRingButton = FindOrCreateGmButton(
                    parent,
                    "GmAcquireDigRingButton",
                    "获得铁铲",
                    new Vector2(-24f, -242f));
            }

            if (_grantEquipCommonExpButton == null)
            {
                _grantEquipCommonExpButton = FindOrCreateGmButton(
                    parent,
                    "GmGrantEquipCommonExpButton",
                    "装备公共经验+50",
                    new Vector2(-24f, -294f));
            }

            if (_spendDigRingCommonExpButton == null)
            {
                _spendDigRingCommonExpButton = FindOrCreateGmButton(
                    parent,
                    "GmSpendDigRingCommonExpButton",
                    "划入铁铲升级",
                    new Vector2(-24f, -346f));
            }

            if (_acquireMinerLampButton == null)
            {
                _acquireMinerLampButton = FindOrCreateGmButton(
                    parent,
                    "GmAcquireMinerLampButton",
                    "获得矿灯",
                    new Vector2(-24f, -398f));
            }

            if (_spendMinerLampCommonExpButton == null)
            {
                _spendMinerLampCommonExpButton = FindOrCreateGmButton(
                    parent,
                    "GmSpendMinerLampCommonExpButton",
                    "划入矿灯升级",
                    new Vector2(-24f, -450f));
            }

            if (_acquireExplosivesButton == null)
            {
                _acquireExplosivesButton = FindOrCreateGmButton(
                    parent,
                    "GmAcquireExplosivesButton",
                    "获得炸药",
                    new Vector2(-24f, -502f));
            }

            if (_spendExplosivesCommonExpButton == null)
            {
                _spendExplosivesCommonExpButton = FindOrCreateGmButton(
                    parent,
                    "GmSpendExplosivesCommonExpButton",
                    "划入炸药升级",
                    new Vector2(-24f, -554f));
            }

            if (_acquireLightningButton == null)
            {
                _acquireLightningButton = FindOrCreateGmButton(
                    parent,
                    "GmAcquireLightningButton",
                    "获得引雷",
                    new Vector2(-24f, -606f));
            }

            if (_spendLightningCommonExpButton == null)
            {
                _spendLightningCommonExpButton = FindOrCreateGmButton(
                    parent,
                    "GmSpendLightningCommonExpButton",
                    "划入引雷升级",
                    new Vector2(-24f, -658f));
            }

            if (_acquireDetectorButton == null)
            {
                _acquireDetectorButton = FindOrCreateGmButton(
                    parent,
                    "GmAcquireDetectorButton",
                    "获得探测器",
                    new Vector2(-24f, -710f));
            }

            if (_spendDetectorCommonExpButton == null)
            {
                _spendDetectorCommonExpButton = FindOrCreateGmButton(
                    parent,
                    "GmSpendDetectorCommonExpButton",
                    "划入探测器升级",
                    new Vector2(-24f, -762f));
            }

            if (_acquireHumanTokenButton == null)
            {
                _acquireHumanTokenButton = FindOrCreateGmButton(
                    parent,
                    "GmAcquireHumanTokenButton",
                    "获得人类信物",
                    new Vector2(-24f, -814f));
            }

            if (_spendHumanTokenCommonExpButton == null)
            {
                _spendHumanTokenCommonExpButton = FindOrCreateGmButton(
                    parent,
                    "GmSpendHumanTokenCommonExpButton",
                    "划入人类信物升级",
                    new Vector2(-24f, -866f));
            }

            if (_acquireElfTokenButton == null)
            {
                _acquireElfTokenButton = FindOrCreateGmButton(
                    parent,
                    "GmAcquireElfTokenButton",
                    "获得精灵信物",
                    new Vector2(-24f, -918f));
            }

            if (_spendElfTokenCommonExpButton == null)
            {
                _spendElfTokenCommonExpButton = FindOrCreateGmButton(
                    parent,
                    "GmSpendElfTokenCommonExpButton",
                    "划入精灵信物升级",
                    new Vector2(-24f, -970f));
            }

            if (_acquireOrcTokenButton == null)
            {
                _acquireOrcTokenButton = FindOrCreateGmButton(
                    parent,
                    "GmAcquireOrcTokenButton",
                    "获得兽人信物",
                    new Vector2(-24f, -1022f));
            }

            if (_spendOrcTokenCommonExpButton == null)
            {
                _spendOrcTokenCommonExpButton = FindOrCreateGmButton(
                    parent,
                    "GmSpendOrcTokenCommonExpButton",
                    "划入兽人信物升级",
                    new Vector2(-24f, -1074f));
            }

            SetGmButtonLabel(_acquireDigRingButton, "获得铁铲");
            SetGmButtonLabel(_spendDigRingCommonExpButton, "划入铁铲升级");
            SetGmButtonLabel(_acquireMinerLampButton, "获得矿灯");
            SetGmButtonLabel(_spendMinerLampCommonExpButton, "划入矿灯升级");
            SetGmButtonLabel(_acquireExplosivesButton, "获得炸药");
            SetGmButtonLabel(_spendExplosivesCommonExpButton, "划入炸药升级");
            SetGmButtonLabel(_acquireLightningButton, "获得引雷");
            SetGmButtonLabel(_spendLightningCommonExpButton, "划入引雷升级");
            SetGmButtonLabel(_acquireDetectorButton, "获得探测器");
            SetGmButtonLabel(_spendDetectorCommonExpButton, "划入探测器升级");
            SetGmButtonLabel(_acquireHumanTokenButton, "获得人类信物");
            SetGmButtonLabel(_spendHumanTokenCommonExpButton, "划入人类信物升级");
            SetGmButtonLabel(_acquireElfTokenButton, "获得精灵信物");
            SetGmButtonLabel(_spendElfTokenCommonExpButton, "划入精灵信物升级");
            SetGmButtonLabel(_acquireOrcTokenButton, "获得兽人信物");
            SetGmButtonLabel(_spendOrcTokenCommonExpButton, "划入兽人信物升级");
        }

        private static Button FindOrCreateGmButton(Transform parent, string name, string label, Vector2 anchoredPos)
        {
            var existing = parent.Find(name);
            if (existing != null)
            {
                var existingBtn = existing.GetComponent<Button>();
                if (existingBtn != null)
                {
                    SetGmButtonLabel(existingBtn, label);
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
            rt.sizeDelta = new Vector2(180f, 40f);

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
