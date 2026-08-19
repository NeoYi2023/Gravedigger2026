using Gravedigger2026.Core.AutoManufacture;
using Gravedigger2026.Core.Config;
using Gravedigger2026.Gameplay.AutoManufacture;
using UnityEngine;
using UnityEngine.UI;

namespace Gravedigger2026.UI
{
    /// <summary>
    /// UI-023 / D-068 / D-072: InSaveShell MagicBook slots modal with shared BookRow + LMB drag TrySwap + delete.
    /// </summary>
    public sealed class MagicBookSlotsPanelView : MonoBehaviour
    {
        private const float DeleteBelowSlotGap = 8f;

        [SerializeField] private GameObject _root;
        [SerializeField] private Text _titleText;
        [SerializeField] private Button _closeButton;
        [SerializeField] private Transform _bookRowHost;
        [SerializeField] private BookRowView _bookRow;
        [SerializeField] private Button _deleteButton;
        [SerializeField] private RectTransform _deleteButtonRect;

        private SpecialEquipSlotsService _equipSlots;
        private ConfigCsvRepository _configs;
        private ConfirmDialogView _confirmDialog;
        private bool _changedSubscribed;
        private int _selectedSlotIndex = -1;
        private bool _deleteVisible;

        public event System.Action Closed;

        public Transform BookRowHost => _bookRowHost;

        private void Awake()
        {
            // Prefab starts inactive. Do NOT SetActive(false) on self/_root here — see ToolsPanelView.
            if (_closeButton != null)
            {
                _closeButton.onClick.AddListener(HandleCloseClicked);
            }

            EnsureDeleteButton();
            if (_deleteButton != null)
            {
                _deleteButton.onClick.AddListener(HandleDeleteClicked);
            }

            EnsureBookRow();
        }

        private void OnEnable()
        {
            SubscribeChanged();
            RefreshBooks();
        }

        private void OnDisable()
        {
            UnsubscribeChanged();
            HideDeleteButton();
        }

        private void OnDestroy()
        {
            UnsubscribeChanged();
            if (_deleteButton != null)
            {
                _deleteButton.onClick.RemoveListener(HandleDeleteClicked);
            }
        }

        public bool IsOpen => _root != null && _root.activeSelf;

        public void Bind(
            SpecialEquipSlotsService equipSlots,
            ConfigCsvRepository configs,
            ConfirmDialogView confirmDialog = null)
        {
            UnsubscribeChanged();
            _equipSlots = equipSlots;
            _configs = configs;
            _confirmDialog = confirmDialog;
            EnsureBookRow();
            WireSlotCallbacks();
            if (_bookRow != null)
            {
                _bookRow.SetAllowReorder(true);
                _bookRow.Bind(_equipSlots, _configs);
            }

            if (isActiveAndEnabled)
            {
                SubscribeChanged();
                RefreshBooks();
            }
        }

        public void Show()
        {
            EnsureBookRow();
            if (_root != null)
            {
                _root.transform.SetAsLastSibling();
                _root.SetActive(true);
            }

            RefreshBooks();
        }

        public void Hide()
        {
            HideDeleteButton();
            if (_root != null)
            {
                _root.SetActive(false);
            }
        }

        public void EnsureBookRow()
        {
            if (_root == null)
            {
                _root = gameObject;
            }

            if (_bookRowHost == null)
            {
                var box = transform.Find("Box");
                var host = box != null ? box.Find("BookRowHost") : null;
                if (host != null)
                {
                    _bookRowHost = host;
                }
            }

            if (_bookRow == null && _bookRowHost != null)
            {
                _bookRow = _bookRowHost.GetComponentInChildren<BookRowView>(true);
            }

            if (_bookRow == null && _bookRowHost != null)
            {
                _bookRow = BookRowView.CreateHierarchy(_bookRowHost);
                StretchToHost(_bookRow.GetComponent<RectTransform>());
            }

            if (_bookRow != null)
            {
                _bookRow.SetAllowReorder(true);
            }

            EnsureDeleteButton();
            WireSlotCallbacks();
        }

        private void WireSlotCallbacks()
        {
            if (_bookRow == null)
            {
                return;
            }

            _bookRow.SetSlotInteractionCallbacks(HandleSlotClicked, HideDeleteButton);
        }

        private void HandleCloseClicked()
        {
            Hide();
            Closed?.Invoke();
        }

        private void HandleSlotClicked(int slotIndex)
        {
            if (_equipSlots == null)
            {
                HideDeleteButton();
                return;
            }

            var bookId = _equipSlots.GetSlot(slotIndex);
            if (string.IsNullOrEmpty(bookId))
            {
                HideDeleteButton();
                return;
            }

            if (_deleteVisible && _selectedSlotIndex == slotIndex)
            {
                HideDeleteButton();
                return;
            }

            _selectedSlotIndex = slotIndex;
            ShowDeleteButtonUnderSlot(slotIndex);
        }

        private void HandleDeleteClicked()
        {
            if (_selectedSlotIndex < 0 || _equipSlots == null)
            {
                return;
            }

            var bookId = _equipSlots.GetSlot(_selectedSlotIndex);
            if (string.IsNullOrEmpty(bookId))
            {
                HideDeleteButton();
                return;
            }

            var displayName = ResolveDisplayName(bookId);
            if (_confirmDialog == null)
            {
                ConfirmUnequip(_selectedSlotIndex);
                return;
            }

            var index = _selectedSlotIndex;
            _confirmDialog.Show(
                $"确认删除「{displayName}」？此操作不可恢复。",
                () => ConfirmUnequip(index));
        }

        private void ConfirmUnequip(int slotIndex)
        {
            if (_equipSlots == null)
            {
                return;
            }

            if (_equipSlots.TryUnequip(slotIndex, out _))
            {
                HideDeleteButton();
            }
        }

        private string ResolveDisplayName(string bookId)
        {
            if (_configs != null && _configs.TryGetMagicBook(bookId, out var row) && row != null
                && !string.IsNullOrEmpty(row.DisplayName))
            {
                return row.DisplayName;
            }

            return bookId;
        }

        private void ShowDeleteButtonUnderSlot(int slotIndex)
        {
            EnsureDeleteButton();
            if (_deleteButton == null || _deleteButtonRect == null || _bookRow == null)
            {
                return;
            }

            var slots = _bookRow.Slots;
            if (slots == null || slotIndex < 0 || slotIndex >= slots.Length)
            {
                return;
            }

            var slotView = slots[slotIndex];
            if (slotView == null)
            {
                return;
            }

            var slotRt = slotView.RectTransform;
            var parentRt = _deleteButtonRect.parent as RectTransform;
            if (slotRt == null || parentRt == null)
            {
                return;
            }

            var canvas = parentRt.GetComponentInParent<Canvas>();
            var cam = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? canvas.worldCamera
                : null;

            var corners = new Vector3[4];
            slotRt.GetWorldCorners(corners);
            var bottomCenter = (corners[0] + corners[3]) * 0.5f;
            var screenPoint = RectTransformUtility.WorldToScreenPoint(cam, bottomCenter);
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    parentRt,
                    screenPoint,
                    cam,
                    out var local))
            {
                return;
            }

            var halfHeight = _deleteButtonRect.rect.height * 0.5f;
            _deleteButtonRect.anchoredPosition = local + new Vector2(0f, -(DeleteBelowSlotGap + halfHeight));
            _deleteButton.gameObject.SetActive(true);
            _deleteButton.transform.SetAsLastSibling();
            _deleteVisible = true;
        }

        private void HideDeleteButton()
        {
            _selectedSlotIndex = -1;
            _deleteVisible = false;
            if (_deleteButton != null)
            {
                _deleteButton.gameObject.SetActive(false);
            }
        }

        private void EnsureDeleteButton()
        {
            if (_deleteButton != null)
            {
                if (_deleteButtonRect == null)
                {
                    _deleteButtonRect = _deleteButton.GetComponent<RectTransform>();
                }

                return;
            }

            var box = transform.Find("Box");
            if (box == null)
            {
                return;
            }

            var existing = box.Find("DeleteButton");
            if (existing != null)
            {
                _deleteButton = existing.GetComponent<Button>();
                _deleteButtonRect = existing as RectTransform;
                return;
            }

            var go = new GameObject(
                "DeleteButton",
                typeof(RectTransform),
                typeof(Image),
                typeof(Button));
            go.transform.SetParent(box, false);

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(120f, 36f);

            var image = go.GetComponent<Image>();
            image.color = new Color(0.65f, 0.28f, 0.28f, 1f);
            image.raycastTarget = true;

            var labelGo = new GameObject("Label", typeof(RectTransform), typeof(Text));
            labelGo.transform.SetParent(go.transform, false);
            var labelRt = labelGo.GetComponent<RectTransform>();
            StretchFull(labelRt);
            var label = labelGo.GetComponent<Text>();
            label.text = "删除";
            label.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            label.fontSize = 20;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Color.white;
            label.raycastTarget = false;

            _deleteButton = go.GetComponent<Button>();
            _deleteButtonRect = rt;
            go.SetActive(false);
        }

        private void SubscribeChanged()
        {
            if (_changedSubscribed || _equipSlots == null)
            {
                return;
            }

            _equipSlots.Changed += HandleSlotsChanged;
            _changedSubscribed = true;
        }

        private void UnsubscribeChanged()
        {
            if (!_changedSubscribed || _equipSlots == null)
            {
                _changedSubscribed = false;
                return;
            }

            _equipSlots.Changed -= HandleSlotsChanged;
            _changedSubscribed = false;
        }

        private void HandleSlotsChanged()
        {
            if (_deleteVisible && _selectedSlotIndex >= 0 && _equipSlots != null
                && string.IsNullOrEmpty(_equipSlots.GetSlot(_selectedSlotIndex)))
            {
                HideDeleteButton();
            }

            RefreshBooks();
        }

        private void RefreshBooks()
        {
            EnsureBookRow();
            if (_bookRow == null)
            {
                return;
            }

            _bookRow.SetAllowReorder(true);
            WireSlotCallbacks();
            if (_equipSlots != null || _configs != null)
            {
                _bookRow.Bind(_equipSlots, _configs);
            }
            else
            {
                _bookRow.Refresh();
            }

            if (_deleteVisible && _selectedSlotIndex >= 0)
            {
                ShowDeleteButtonUnderSlot(_selectedSlotIndex);
            }
        }

        private static void StretchToHost(RectTransform rt)
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

        private static void StretchFull(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
    }
}
