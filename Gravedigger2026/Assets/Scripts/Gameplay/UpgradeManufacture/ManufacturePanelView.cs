using System;
using System.Collections;
using System.Collections.Generic;
using Gravedigger2026.Gameplay.Defend;
using UnityEngine;
using UnityEngine.UI;

namespace Gravedigger2026.Gameplay.UpgradeManufacture
{
    /// <summary>
    /// Full-screen manufacture UI: center slot ring, bottom inventory drag bar, visual preview gate.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public sealed class ManufacturePanelView : MonoBehaviour
    {
        public const float InventorySlotSize = 80f;
        public const float BodySlotSize = 72f;
        public const float GemSlotSize = 36f;
        private const float SlotSpacing = 8f;
        private const float DecideThresholdPx = 8f;

        [SerializeField] private RectTransform _inventoryContent;
        [SerializeField] private Button _inventoryRowTemplate;
        [SerializeField] private ScrollRect _inventoryScroll;
        [SerializeField] private RectTransform _inventoryBarRoot;
        [SerializeField] private Button[] _slotCells = new Button[15];
        [SerializeField] private Text _previewText;
        [SerializeField] private Text _poolText;
        [SerializeField] private Button _grantKitButton;
        [SerializeField] private Button _clearSlotsButton;
        [SerializeField] private Button _manufactureButton;
        [SerializeField] private Image _placeholderImage;
        [SerializeField] private RawImage _previewRawImage;
        [SerializeField] private Transform _previewModelAnchor;
        [SerializeField] private Camera _previewCamera;
        [SerializeField] private RectTransform _dragGhost;

        private readonly List<Button> _inventoryCells = new List<Button>();
        private readonly List<string> _inventoryItemIds = new List<string>();
        private readonly string[] _slotLabels = new string[15];

        private enum PointerMode
        {
            Idle,
            Pressed,
            Scrolling,
            DraggingInventory,
            DraggingSlot
        }

        private PointerMode _mode;
        private Vector2 _pressScreen;
        private Vector2 _lastScreen;
        private int _pressedInventoryIndex = -1;
        private int _pressedSlotIndex = -1;
        private string _dragItemId;
        private Canvas _canvas;
        private GameObject _previewInstance;
        private string _previewAppearanceId;
        private Coroutine _previewAnimRoutine;
        private RenderTexture _previewRt;

        public event Action<string> ItemPlaceRequested;
        public event Action<int, string> ItemPlaceAtRequested;
        public event Action<int> SlotClearRequested;
        public event Action GrantKitRequested;
        public event Action ClearSlotsRequested;
        public event Action ManufactureRequested;

        private void Awake()
        {
            _canvas = GetComponentInParent<Canvas>();
            EnsureInventoryScroll();
            if (_slotCells == null || _slotCells.Length != 15)
            {
                _slotCells = new Button[15];
            }

            if (_dragGhost != null)
            {
                _dragGhost.gameObject.SetActive(false);
            }

            SetupPreviewRenderTexture();
        }

        private void OnDestroy()
        {
            ClearPreviewModel();
            if (_previewRt != null)
            {
                if (_previewCamera != null)
                {
                    _previewCamera.targetTexture = null;
                }

                _previewRt.Release();
                Destroy(_previewRt);
                _previewRt = null;
            }
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

            EndDrag();
        }

        private void Update()
        {
            if (!isActiveAndEnabled)
            {
                return;
            }

            var mouse = (Vector2)Input.mousePosition;
            if (Input.GetMouseButtonDown(0))
            {
                TryBeginPress(mouse);
                return;
            }

            if (_mode == PointerMode.Idle)
            {
                return;
            }

            if (Input.GetMouseButton(0))
            {
                UpdatePress(mouse);
                return;
            }

            FinishPress(mouse);
        }

        public void SetInventoryLines(IReadOnlyList<string> labels, IReadOnlyList<string> itemIds)
        {
            EnsureInventoryCellCount(labels.Count);
            _inventoryItemIds.Clear();
            for (var i = 0; i < _inventoryCells.Count; i++)
            {
                var cell = _inventoryCells[i];
                if (i >= labels.Count)
                {
                    cell.gameObject.SetActive(false);
                    continue;
                }

                var itemId = i < itemIds.Count ? itemIds[i] : string.Empty;
                _inventoryItemIds.Add(itemId);
                cell.gameObject.SetActive(true);
                SetCellLabel(cell, labels[i]);
                cell.onClick.RemoveAllListeners();
            }

            RefreshInventoryContentWidth();
        }

        public void SetSlotLines(IReadOnlyList<string> labels)
        {
            for (var i = 0; i < 15; i++)
            {
                var cell = _slotCells != null && i < _slotCells.Length ? _slotCells[i] : null;
                if (cell == null)
                {
                    continue;
                }

                var label = i < labels.Count ? labels[i] : string.Empty;
                _slotLabels[i] = label;
                cell.gameObject.SetActive(true);
                SetCellLabel(cell, ShortSlotLabel(label));
                cell.onClick.RemoveAllListeners();
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

        /// <summary>
        /// Visual appearance gate: show placeholder or instantiate trial appearance Prefab.
        /// </summary>
        public void SetWarriorVisualPreview(bool showAppearance, GameObject appearancePrefab, string appearanceId)
        {
            if (!showAppearance || appearancePrefab == null)
            {
                ClearPreviewModel();
                SetPlaceholderVisible(true);
                return;
            }

            if (_previewInstance != null
                && string.Equals(_previewAppearanceId, appearanceId, StringComparison.Ordinal))
            {
                SetPlaceholderVisible(false);
                return;
            }

            ClearPreviewModel();
            SetPlaceholderVisible(false);

            if (_previewModelAnchor == null)
            {
                Debug.LogWarning("[UM Manufacture] Preview model anchor missing.");
                SetPlaceholderVisible(true);
                return;
            }

            _previewAppearanceId = appearanceId;
            _previewInstance = Instantiate(appearancePrefab, _previewModelAnchor);
            _previewInstance.name = $"UmPreview_{appearanceId}";
            _previewInstance.transform.localPosition = Vector3.zero;
            _previewInstance.transform.localRotation = Quaternion.identity;
            _previewInstance.transform.localScale = Vector3.one;

            var anim = _previewInstance.GetComponent<WarriorAnimView>();
            if (anim == null)
            {
                anim = _previewInstance.GetComponentInChildren<WarriorAnimView>();
            }

            if (anim == null)
            {
                anim = _previewInstance.AddComponent<WarriorAnimView>();
            }

            if (_previewAnimRoutine != null)
            {
                StopCoroutine(_previewAnimRoutine);
            }

            _previewAnimRoutine = StartCoroutine(PlayAttackThenIdle(anim));
        }

        private IEnumerator PlayAttackThenIdle(WarriorAnimView anim)
        {
            if (anim == null)
            {
                yield break;
            }

            anim.ResetToIdle();
            yield return null;
            anim.PlayAttack();
            yield return new WaitForSeconds(0.85f);
            anim.ResetToIdle();
            _previewAnimRoutine = null;
        }

        private void ClearPreviewModel()
        {
            if (_previewAnimRoutine != null)
            {
                StopCoroutine(_previewAnimRoutine);
                _previewAnimRoutine = null;
            }

            if (_previewInstance != null)
            {
                Destroy(_previewInstance);
                _previewInstance = null;
            }

            _previewAppearanceId = null;
        }

        private void SetPlaceholderVisible(bool visible)
        {
            if (_placeholderImage != null)
            {
                _placeholderImage.enabled = visible;
                _placeholderImage.gameObject.SetActive(true);
            }

            if (_previewRawImage != null)
            {
                _previewRawImage.enabled = !visible;
            }
        }

        private void SetupPreviewRenderTexture()
        {
            if (_previewCamera == null || _previewRawImage == null)
            {
                return;
            }

            _previewRt = new RenderTexture(256, 256, 16);
            _previewRt.Create();
            _previewCamera.targetTexture = _previewRt;
            _previewRawImage.texture = _previewRt;
            _previewRawImage.enabled = false;
        }

        private void TryBeginPress(Vector2 mouse)
        {
            _pressScreen = mouse;
            _lastScreen = mouse;
            _pressedInventoryIndex = FindInventoryIndexAt(mouse);
            _pressedSlotIndex = FindSlotIndexAt(mouse);

            if (_pressedInventoryIndex >= 0)
            {
                _mode = PointerMode.Pressed;
                return;
            }

            if (_pressedSlotIndex >= 0 && !IsSlotEmptyLabel(_slotLabels[_pressedSlotIndex]))
            {
                _mode = PointerMode.Pressed;
                return;
            }

            _mode = PointerMode.Idle;
        }

        private void UpdatePress(Vector2 mouse)
        {
            var delta = mouse - _lastScreen;
            _lastScreen = mouse;

            if (_mode == PointerMode.Pressed)
            {
                var total = mouse - _pressScreen;
                if (total.sqrMagnitude < DecideThresholdPx * DecideThresholdPx)
                {
                    return;
                }

                if (_pressedInventoryIndex >= 0)
                {
                    if (Mathf.Abs(total.x) >= Mathf.Abs(total.y))
                    {
                        _mode = PointerMode.Scrolling;
                    }
                    else if (total.y > 0f)
                    {
                        BeginInventoryDrag();
                    }
                    else
                    {
                        _mode = PointerMode.Scrolling;
                    }
                }
                else if (_pressedSlotIndex >= 0)
                {
                    BeginSlotDrag();
                }
            }

            if (_mode == PointerMode.Scrolling)
            {
                ApplyInventoryScroll(delta.x);
                return;
            }

            if (_mode == PointerMode.DraggingInventory || _mode == PointerMode.DraggingSlot)
            {
                UpdateGhost(mouse);
            }
        }

        private void FinishPress(Vector2 mouse)
        {
            if (_mode == PointerMode.DraggingInventory)
            {
                var slotIndex = FindSlotIndexAt(mouse);
                if (slotIndex >= 0 && !string.IsNullOrEmpty(_dragItemId))
                {
                    ItemPlaceAtRequested?.Invoke(slotIndex, _dragItemId);
                }
                else if (!string.IsNullOrEmpty(_dragItemId))
                {
                    ItemPlaceRequested?.Invoke(_dragItemId);
                }
            }
            else if (_mode == PointerMode.DraggingSlot)
            {
                var overInventory = IsOverInventory(mouse);
                var overSlot = FindSlotIndexAt(mouse);
                if (overInventory || overSlot < 0)
                {
                    SlotClearRequested?.Invoke(_pressedSlotIndex);
                }
            }
            else if (_mode == PointerMode.Pressed)
            {
                if (_pressedSlotIndex >= 0)
                {
                    SlotClearRequested?.Invoke(_pressedSlotIndex);
                }
            }

            EndDrag();
        }

        private void BeginInventoryDrag()
        {
            if (_pressedInventoryIndex < 0 || _pressedInventoryIndex >= _inventoryItemIds.Count)
            {
                _mode = PointerMode.Idle;
                return;
            }

            _dragItemId = _inventoryItemIds[_pressedInventoryIndex];
            if (string.IsNullOrEmpty(_dragItemId))
            {
                _mode = PointerMode.Idle;
                return;
            }

            _mode = PointerMode.DraggingInventory;
            ShowGhost(GetCellLabel(_inventoryCells[_pressedInventoryIndex]));
        }

        private void BeginSlotDrag()
        {
            if (_pressedSlotIndex < 0)
            {
                _mode = PointerMode.Idle;
                return;
            }

            _mode = PointerMode.DraggingSlot;
            ShowGhost(ShortSlotLabel(_slotLabels[_pressedSlotIndex]));
        }

        private void EndDrag()
        {
            _mode = PointerMode.Idle;
            _pressedInventoryIndex = -1;
            _pressedSlotIndex = -1;
            _dragItemId = null;
            if (_dragGhost != null)
            {
                _dragGhost.gameObject.SetActive(false);
            }
        }

        private void ShowGhost(string label)
        {
            if (_dragGhost == null)
            {
                return;
            }

            _dragGhost.gameObject.SetActive(true);
            var text = _dragGhost.GetComponentInChildren<Text>(true);
            if (text != null)
            {
                text.text = label ?? string.Empty;
            }

            UpdateGhost(Input.mousePosition);
        }

        private void UpdateGhost(Vector2 screen)
        {
            if (_dragGhost == null)
            {
                return;
            }

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _dragGhost.parent as RectTransform,
                screen,
                _canvas != null && _canvas.renderMode != RenderMode.ScreenSpaceOverlay ? _canvas.worldCamera : null,
                out var local);
            _dragGhost.anchoredPosition = local;
        }

        private void ApplyInventoryScroll(float deltaX)
        {
            if (_inventoryScroll == null || _inventoryContent == null)
            {
                return;
            }

            var contentW = _inventoryContent.rect.width;
            var viewW = _inventoryScroll.viewport != null
                ? _inventoryScroll.viewport.rect.width
                : ((RectTransform)_inventoryScroll.transform).rect.width;
            var overflow = Mathf.Max(0f, contentW - viewW);
            if (overflow <= 0.01f)
            {
                return;
            }

            var pos = _inventoryContent.anchoredPosition;
            pos.x = Mathf.Clamp(pos.x + deltaX, -overflow, 0f);
            _inventoryContent.anchoredPosition = pos;
        }

        private void EnsureInventoryScroll()
        {
            if (_inventoryScroll != null)
            {
                _inventoryScroll.enabled = false;
            }
        }

        private void EnsureInventoryCellCount(int required)
        {
            if (_inventoryRowTemplate == null || _inventoryContent == null)
            {
                return;
            }

            while (_inventoryCells.Count < required)
            {
                var clone = Instantiate(_inventoryRowTemplate, _inventoryContent);
                HardenSquareCell(clone, InventorySlotSize);
                clone.gameObject.SetActive(true);
                _inventoryCells.Add(clone);
            }
        }

        private void RefreshInventoryContentWidth()
        {
            if (_inventoryContent == null)
            {
                return;
            }

            var active = 0;
            for (var i = 0; i < _inventoryCells.Count; i++)
            {
                if (_inventoryCells[i] != null && _inventoryCells[i].gameObject.activeSelf)
                {
                    active++;
                }
            }

            var width = active * (InventorySlotSize + SlotSpacing) + SlotSpacing;
            _inventoryContent.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
        }

        private static void HardenSquareCell(Button cell, float size)
        {
            if (cell == null)
            {
                return;
            }

            var le = cell.GetComponent<LayoutElement>();
            if (le == null)
            {
                le = cell.gameObject.AddComponent<LayoutElement>();
            }

            le.preferredWidth = size;
            le.preferredHeight = size;
            le.minWidth = size;
            le.minHeight = size;
            le.flexibleWidth = 0f;
            le.flexibleHeight = 0f;

            var text = cell.GetComponentInChildren<Text>(true);
            if (text != null)
            {
                text.fontSize = size <= GemSlotSize + 1f ? 10 : 12;
                text.alignment = TextAnchor.MiddleCenter;
                text.horizontalOverflow = HorizontalWrapMode.Wrap;
                text.verticalOverflow = VerticalWrapMode.Truncate;
            }
        }

        private int FindInventoryIndexAt(Vector2 screen)
        {
            for (var i = 0; i < _inventoryCells.Count; i++)
            {
                var cell = _inventoryCells[i];
                if (cell == null || !cell.gameObject.activeInHierarchy)
                {
                    continue;
                }

                if (RectContainsScreen(cell.GetComponent<RectTransform>(), screen))
                {
                    return i;
                }
            }

            return -1;
        }

        private int FindSlotIndexAt(Vector2 screen)
        {
            if (_slotCells == null)
            {
                return -1;
            }

            for (var i = 0; i < _slotCells.Length; i++)
            {
                var cell = _slotCells[i];
                if (cell == null || !cell.gameObject.activeInHierarchy)
                {
                    continue;
                }

                if (RectContainsScreen(cell.GetComponent<RectTransform>(), screen))
                {
                    return i;
                }
            }

            return -1;
        }

        private bool IsOverInventory(Vector2 screen)
        {
            var root = _inventoryBarRoot != null ? _inventoryBarRoot : _inventoryContent;
            return root != null && RectContainsScreen(root, screen);
        }

        private bool RectContainsScreen(RectTransform rt, Vector2 screen)
        {
            if (rt == null)
            {
                return false;
            }

            var cam = _canvas != null && _canvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? _canvas.worldCamera
                : null;
            return RectTransformUtility.RectangleContainsScreenPoint(rt, screen, cam);
        }

        private static bool IsSlotEmptyLabel(string label)
        {
            return string.IsNullOrEmpty(label) || label.Contains("（空）") || label.Contains("(empty)");
        }

        private static string ShortSlotLabel(string label)
        {
            if (string.IsNullOrEmpty(label))
            {
                return string.Empty;
            }

            var colon = label.IndexOf('：');
            if (colon < 0)
            {
                colon = label.IndexOf(':');
            }

            if (colon < 0)
            {
                return label;
            }

            var kind = label.Substring(0, colon);
            var rest = label.Substring(colon + 1).Trim();
            if (rest.Contains("空"))
            {
                return kind;
            }

            return kind + "\n" + rest;
        }

        private static void SetCellLabel(Button cell, string label)
        {
            var text = cell.GetComponentInChildren<Text>(true);
            if (text != null)
            {
                text.text = label ?? string.Empty;
            }
        }

        private static string GetCellLabel(Button cell)
        {
            var text = cell != null ? cell.GetComponentInChildren<Text>(true) : null;
            return text != null ? text.text : string.Empty;
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
