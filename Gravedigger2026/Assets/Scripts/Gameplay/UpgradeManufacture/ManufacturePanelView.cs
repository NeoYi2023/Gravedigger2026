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
                clone.gameObject.SetActive(true);
                rows.Add(clone);
            }
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
