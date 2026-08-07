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
        [SerializeField] private RectTransform _poolContent;
        [SerializeField] private PoolSoldierFrameView _poolFrameTemplate;
        [SerializeField] private ScrollRect _poolScroll;
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
        private readonly List<PoolSoldierFrameView> _poolFrames = new List<PoolSoldierFrameView>();
        private string _selectedPoolWarriorId;

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
        public event Action<string> PoolRemakeRequested;

        private void Awake()
        {
            _canvas = GetComponentInParent<Canvas>();
            EnsureInventoryScroll();
            EnsurePoolUi();
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
            ClearPoolFrames();
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
            // Legacy no-op kept for compile safety during transition; use RebuildPool.
        }

        public void RebuildPool(IReadOnlyList<PoolSoldierEntry> entries)
        {
            EnsurePoolTemplateHidden();
            ClearPoolFrames();

            if (entries == null || entries.Count == 0 || _poolContent == null || _poolFrameTemplate == null)
            {
                _selectedPoolWarriorId = null;
                return;
            }

            var selectionStillValid = false;
            for (var i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                if (entry == null || string.IsNullOrEmpty(entry.WarriorId))
                {
                    continue;
                }

                var frame = Instantiate(_poolFrameTemplate, _poolContent);
                frame.gameObject.SetActive(true);
                frame.name = $"PoolFrame_{entry.WarriorId}";
                var canRemake = entry.CanRemake;
                var summary = entry.Summary;
                frame.Bind(entry.WarriorId, summary, canRemake);
                frame.Selected += HandlePoolFrameSelected;
                frame.RemakeRequested += HandlePoolRemakeRequested;
                _poolFrames.Add(frame);

                if (string.Equals(entry.WarriorId, _selectedPoolWarriorId, StringComparison.Ordinal))
                {
                    selectionStillValid = true;
                }
            }

            if (!selectionStillValid)
            {
                _selectedPoolWarriorId = null;
            }

            ApplyPoolSelection();
        }

        public void ClearPoolSelection()
        {
            _selectedPoolWarriorId = null;
            ApplyPoolSelection();
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

        private void HandlePoolFrameSelected(string warriorId)
        {
            _selectedPoolWarriorId = warriorId;
            ApplyPoolSelection();
        }

        private void HandlePoolRemakeRequested(string warriorId)
        {
            PoolRemakeRequested?.Invoke(warriorId);
        }

        private void ApplyPoolSelection()
        {
            for (var i = 0; i < _poolFrames.Count; i++)
            {
                var frame = _poolFrames[i];
                if (frame == null)
                {
                    continue;
                }

                frame.SetSelected(string.Equals(frame.WarriorId, _selectedPoolWarriorId, StringComparison.Ordinal));
            }
        }

        private void ClearPoolFrames()
        {
            for (var i = 0; i < _poolFrames.Count; i++)
            {
                var frame = _poolFrames[i];
                if (frame == null)
                {
                    continue;
                }

                frame.Selected -= HandlePoolFrameSelected;
                frame.RemakeRequested -= HandlePoolRemakeRequested;
                Destroy(frame.gameObject);
            }

            _poolFrames.Clear();
        }

        private void EnsurePoolTemplateHidden()
        {
            if (_poolFrameTemplate != null)
            {
                _poolFrameTemplate.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// Builds Pool ScrollRect + frame template at runtime when Prefab still has legacy PoolText.
        /// </summary>
        private void EnsurePoolUi()
        {
            if (_poolContent != null && _poolFrameTemplate != null)
            {
                EnsurePoolTemplateHidden();
                return;
            }

            var poolPanel = transform.Find("PoolPanel");
            if (poolPanel == null)
            {
                return;
            }

            var legacyText = poolPanel.Find("PoolText");
            if (legacyText != null)
            {
                legacyText.gameObject.SetActive(false);
            }

            var existingHeader = poolPanel.Find("PoolHeader");
            if (existingHeader == null)
            {
                var headerGo = new GameObject("PoolHeader", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
                headerGo.transform.SetParent(poolPanel, false);
                var headerRt = headerGo.GetComponent<RectTransform>();
                headerRt.anchorMin = new Vector2(0f, 0.92f);
                headerRt.anchorMax = new Vector2(1f, 1f);
                headerRt.offsetMin = new Vector2(2f, 2f);
                headerRt.offsetMax = new Vector2(-2f, -2f);
                var headerText = headerGo.GetComponent<Text>();
                headerText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                headerText.fontSize = 14;
                headerText.alignment = TextAnchor.MiddleCenter;
                headerText.color = Color.white;
                headerText.text = "士兵池";
                headerText.raycastTarget = false;
            }

            var scrollRoot = poolPanel.Find("PoolScrollRoot");
            if (scrollRoot == null)
            {
                var scrollRootGo = new GameObject("PoolScrollRoot", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                scrollRootGo.transform.SetParent(poolPanel, false);
                var scrollRootRt = scrollRootGo.GetComponent<RectTransform>();
                scrollRootRt.anchorMin = new Vector2(0f, 0f);
                scrollRootRt.anchorMax = new Vector2(1f, 0.92f);
                scrollRootRt.offsetMin = new Vector2(2f, 2f);
                scrollRootRt.offsetMax = new Vector2(-2f, -2f);
                scrollRootGo.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.05f);
                scrollRoot = scrollRootGo.transform;
            }

            if (_poolScroll == null)
            {
                _poolScroll = scrollRoot.GetComponentInChildren<ScrollRect>(true);
            }

            if (_poolScroll == null)
            {
                var scrollGo = new GameObject("Scroll", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
                scrollGo.transform.SetParent(scrollRoot, false);
                var scrollRt = scrollGo.GetComponent<RectTransform>();
                scrollRt.anchorMin = Vector2.zero;
                scrollRt.anchorMax = Vector2.one;
                scrollRt.offsetMin = Vector2.zero;
                scrollRt.offsetMax = Vector2.zero;
                scrollGo.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.02f);
                _poolScroll = scrollGo.GetComponent<ScrollRect>();
                _poolScroll.horizontal = false;
                _poolScroll.vertical = true;
                _poolScroll.movementType = ScrollRect.MovementType.Clamped;
                _poolScroll.scrollSensitivity = 28f;

                var viewportGo = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(RectMask2D));
                viewportGo.transform.SetParent(scrollGo.transform, false);
                var viewportRt = viewportGo.GetComponent<RectTransform>();
                viewportRt.anchorMin = Vector2.zero;
                viewportRt.anchorMax = Vector2.one;
                viewportRt.offsetMin = Vector2.zero;
                viewportRt.offsetMax = Vector2.zero;
                viewportGo.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.02f);

                var contentGo = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup),
                    typeof(ContentSizeFitter));
                contentGo.transform.SetParent(viewportGo.transform, false);
                _poolContent = contentGo.GetComponent<RectTransform>();
                _poolContent.anchorMin = new Vector2(0f, 1f);
                _poolContent.anchorMax = new Vector2(1f, 1f);
                _poolContent.pivot = new Vector2(0.5f, 1f);
                _poolContent.anchoredPosition = Vector2.zero;
                _poolContent.sizeDelta = Vector2.zero;

                var layout = contentGo.GetComponent<VerticalLayoutGroup>();
                layout.childAlignment = TextAnchor.UpperCenter;
                layout.spacing = 6f;
                layout.padding = new RectOffset(6, 6, 6, 6);
                layout.childControlHeight = true;
                layout.childControlWidth = true;
                layout.childForceExpandHeight = false;
                layout.childForceExpandWidth = true;

                var fitter = contentGo.GetComponent<ContentSizeFitter>();
                fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
                fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

                _poolScroll.content = _poolContent;
                _poolScroll.viewport = viewportRt;
            }
            else if (_poolContent == null)
            {
                _poolContent = _poolScroll.content;
            }

            if (_poolFrameTemplate == null && _poolContent != null)
            {
                _poolFrameTemplate = BuildRuntimePoolFrameTemplate(_poolContent);
            }

            EnsurePoolTemplateHidden();
        }

        private static PoolSoldierFrameView BuildRuntimePoolFrameTemplate(Transform content)
        {
            var go = new GameObject("PoolSoldierFrameTemplate", typeof(RectTransform), typeof(Image), typeof(Button),
                typeof(LayoutElement), typeof(PoolSoldierFrameView));
            go.transform.SetParent(content, false);
            var bg = go.GetComponent<Image>();
            bg.color = new Color(0.2f, 0.24f, 0.22f, 0.95f);
            var le = go.GetComponent<LayoutElement>();
            le.minHeight = 72f;
            le.preferredHeight = 72f;
            le.flexibleWidth = 1f;

            var summaryGo = new GameObject("Summary", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            summaryGo.transform.SetParent(go.transform, false);
            var summaryRt = summaryGo.GetComponent<RectTransform>();
            summaryRt.anchorMin = new Vector2(0.04f, 0.35f);
            summaryRt.anchorMax = new Vector2(0.96f, 0.95f);
            summaryRt.offsetMin = Vector2.zero;
            summaryRt.offsetMax = Vector2.zero;
            var summary = summaryGo.GetComponent<Text>();
            summary.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            summary.fontSize = 12;
            summary.alignment = TextAnchor.UpperLeft;
            summary.color = Color.white;
            summary.text = "W_001";
            summary.raycastTarget = false;

            var remakeGo = new GameObject("RemakeButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image),
                typeof(Button));
            remakeGo.transform.SetParent(go.transform, false);
            var remakeRt = remakeGo.GetComponent<RectTransform>();
            remakeRt.anchorMin = new Vector2(0.15f, 0.05f);
            remakeRt.anchorMax = new Vector2(0.85f, 0.38f);
            remakeRt.offsetMin = Vector2.zero;
            remakeRt.offsetMax = Vector2.zero;
            remakeGo.GetComponent<Image>().color = new Color(0.32f, 0.5f, 0.36f, 1f);
            var remakeLabelGo = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            remakeLabelGo.transform.SetParent(remakeGo.transform, false);
            var remakeLabelRt = remakeLabelGo.GetComponent<RectTransform>();
            remakeLabelRt.anchorMin = Vector2.zero;
            remakeLabelRt.anchorMax = Vector2.one;
            remakeLabelRt.offsetMin = Vector2.zero;
            remakeLabelRt.offsetMax = Vector2.zero;
            var remakeLabel = remakeLabelGo.GetComponent<Text>();
            remakeLabel.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            remakeLabel.fontSize = 12;
            remakeLabel.alignment = TextAnchor.MiddleCenter;
            remakeLabel.color = Color.white;
            remakeLabel.text = "再造1个";
            remakeLabel.raycastTarget = false;
            remakeGo.SetActive(false);

            var view = go.GetComponent<PoolSoldierFrameView>();
            // SerializeField not set at runtime — wire via reflection-free public Bind path uses private fields.
            // Assign via Serialized-like fields using a small runtime wire helper.
            view.RuntimeWire(go.GetComponent<Button>(), summary, remakeGo.GetComponent<Button>(), bg);
            go.SetActive(false);
            return view;
        }
    }

    /// <summary>View model for one PoolPanel soldier frame.</summary>
    public sealed class PoolSoldierEntry
    {
        public string WarriorId;
        public string Summary;
        public bool CanRemake;
    }
}
