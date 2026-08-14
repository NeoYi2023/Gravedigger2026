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

            SetGmButtonLabel(_acquireDigRingButton, "获得铁铲");
            SetGmButtonLabel(_spendDigRingCommonExpButton, "划入铁铲升级");
            SetGmButtonLabel(_acquireMinerLampButton, "获得矿灯");
            SetGmButtonLabel(_spendMinerLampCommonExpButton, "划入矿灯升级");
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
