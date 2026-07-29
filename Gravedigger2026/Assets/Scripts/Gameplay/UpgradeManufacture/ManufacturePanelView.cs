using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Gravedigger2026.Gameplay.UpgradeManufacture
{
    /// <summary>
    /// Rough manufacture panel UI (SPEC_03 UI-010 / D-031): inventory rows, slot rows, preview, actions.
    /// Clicking an inventory row places the item; clicking a filled slot row clears it.
    /// </summary>
    public sealed class ManufacturePanelView : MonoBehaviour
    {
        private const float RowHeight = 18f;

        [SerializeField] private RectTransform _inventoryContent;
        [SerializeField] private Button _inventoryRowTemplate;
        [SerializeField] private RectTransform _slotContent;
        [SerializeField] private Button _slotRowTemplate;
        [SerializeField] private Text _previewText;
        [SerializeField] private Text _poolText;
        [SerializeField] private Button _grantKitButton;
        [SerializeField] private Button _clearSlotsButton;
        [SerializeField] private Button _manufactureButton;

        private readonly List<Button> _inventoryRows = new List<Button>();
        private readonly List<Button> _slotRows = new List<Button>();

        public event Action<string> ItemPlaceRequested;
        public event Action<int> SlotClearRequested;
        public event Action GrantKitRequested;
        public event Action ClearSlotsRequested;
        public event Action ManufactureRequested;

        private void Awake()
        {
            // Legacy Prefabs stretched Content into a fixed panel; many kit rows crushed height to ~0
            // (blank labels). Heal once at runtime so existing assets work without a full rebuild.
            _inventoryContent = EnsureVerticalScrollColumn(_inventoryContent);
            _slotContent = EnsureVerticalScrollColumn(_slotContent);
            HardenRowTemplate(_inventoryRowTemplate);
            HardenRowTemplate(_slotRowTemplate);
        }

        private void OnEnable()
        {
            if (_grantKitButton != null)
            {
                _grantKitButton.onClick.AddListener(HandleGrantKit);
            }

            if (_clearSlotsButton != null)
            {
                _clearSlotsButton.onClick.AddListener(HandleClearSlots);
            }

            if (_manufactureButton != null)
            {
                _manufactureButton.onClick.AddListener(HandleManufacture);
            }
        }

        private void OnDisable()
        {
            if (_grantKitButton != null)
            {
                _grantKitButton.onClick.RemoveListener(HandleGrantKit);
            }

            if (_clearSlotsButton != null)
            {
                _clearSlotsButton.onClick.RemoveListener(HandleClearSlots);
            }

            if (_manufactureButton != null)
            {
                _manufactureButton.onClick.RemoveListener(HandleManufacture);
            }
        }

        public void SetInventoryLines(IReadOnlyList<string> labels, IReadOnlyList<string> itemIds)
        {
            EnsureRowCount(_inventoryRows, _inventoryRowTemplate, _inventoryContent, labels.Count);
            for (var i = 0; i < _inventoryRows.Count; i++)
            {
                var row = _inventoryRows[i];
                if (i >= labels.Count)
                {
                    row.gameObject.SetActive(false);
                    continue;
                }

                var itemId = itemIds[i];
                row.gameObject.SetActive(true);
                SetRowLabel(row, labels[i]);
                row.onClick.RemoveAllListeners();
                row.onClick.AddListener(() => ItemPlaceRequested?.Invoke(itemId));
            }
        }

        public void SetSlotLines(IReadOnlyList<string> labels)
        {
            EnsureRowCount(_slotRows, _slotRowTemplate, _slotContent, labels.Count);
            for (var i = 0; i < _slotRows.Count; i++)
            {
                var row = _slotRows[i];
                if (i >= labels.Count)
                {
                    row.gameObject.SetActive(false);
                    continue;
                }

                var index = i;
                row.gameObject.SetActive(true);
                SetRowLabel(row, labels[i]);
                row.onClick.RemoveAllListeners();
                row.onClick.AddListener(() => SlotClearRequested?.Invoke(index));
            }
        }

        public void SetPreviewText(string text)
        {
            if (_previewText != null)
            {
                _previewText.text = text ?? string.Empty;
            }
        }

        public void SetPoolText(string text)
        {
            if (_poolText != null)
            {
                _poolText.text = text ?? string.Empty;
            }
        }

        public void SetManufactureInteractable(bool interactable)
        {
            if (_manufactureButton != null)
            {
                _manufactureButton.interactable = interactable;
            }
        }

        private void EnsureRowCount(List<Button> rows, Button template, RectTransform content, int required)
        {
            if (template == null || content == null)
            {
                return;
            }

            while (rows.Count < required)
            {
                var clone = Instantiate(template, content);
                HardenRowTemplate(clone);
                clone.gameObject.SetActive(true);
                rows.Add(clone);
            }
        }

        private static void HardenRowTemplate(Button row)
        {
            if (row == null)
            {
                return;
            }

            var layoutElement = row.GetComponent<LayoutElement>();
            if (layoutElement == null)
            {
                layoutElement = row.gameObject.AddComponent<LayoutElement>();
            }

            layoutElement.preferredHeight = RowHeight;
            layoutElement.minHeight = RowHeight;
        }

        /// <summary>
        /// Upgrades a legacy list Content (stretched inside a fixed column) into Scroll/Viewport/Content
        /// so row preferred heights stay readable when the Debug kit adds many inventory lines.
        /// </summary>
        private static RectTransform EnsureVerticalScrollColumn(RectTransform content)
        {
            if (content == null)
            {
                return null;
            }

            if (content.GetComponentInParent<ScrollRect>() != null)
            {
                HardenListContent(content);
                return content;
            }

            var column = content.parent as RectTransform;
            if (column == null)
            {
                HardenListContent(content);
                return content;
            }

            var columnMask = column.GetComponent<RectMask2D>();
            if (columnMask != null)
            {
                Destroy(columnMask);
            }

            var scrollGo = new GameObject("Scroll", typeof(RectTransform), typeof(ScrollRect), typeof(Image));
            scrollGo.transform.SetParent(column, false);
            scrollGo.transform.SetSiblingIndex(content.GetSiblingIndex());
            var scrollRt = scrollGo.GetComponent<RectTransform>();
            StretchFill(scrollRt, 2f);
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
            StretchFill(viewportRt, 0f);
            var viewportImg = viewportGo.GetComponent<Image>();
            viewportImg.color = new Color(1f, 1f, 1f, 0.02f);
            viewportImg.raycastTarget = true;

            content.SetParent(viewportGo.transform, false);
            HardenListContent(content);

            scroll.content = content;
            scroll.viewport = viewportRt;
            return content;
        }

        private static void HardenListContent(RectTransform content)
        {
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = Vector2.zero;

            var layout = content.GetComponent<VerticalLayoutGroup>();
            if (layout == null)
            {
                layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            }

            layout.childAlignment = TextAnchor.UpperLeft;
            layout.spacing = 1f;
            layout.padding = new RectOffset(2, 2, 2, 2);
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;

            var fitter = content.GetComponent<ContentSizeFitter>();
            if (fitter == null)
            {
                fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            }

            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        private static void StretchFill(RectTransform rt, float padding)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(padding, padding);
            rt.offsetMax = new Vector2(-padding, -padding);
        }

        private static void SetRowLabel(Button row, string label)
        {
            var text = row.GetComponentInChildren<Text>(true);
            if (text != null)
            {
                text.text = label ?? string.Empty;
            }
        }

        private void HandleGrantKit()
        {
            GrantKitRequested?.Invoke();
        }

        private void HandleClearSlots()
        {
            ClearSlotsRequested?.Invoke();
        }

        private void HandleManufacture()
        {
            ManufactureRequested?.Invoke();
        }
    }
}
