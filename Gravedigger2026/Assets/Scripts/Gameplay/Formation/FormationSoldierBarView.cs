using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Gravedigger2026.Gameplay.Formation
{
    /// <summary>
    /// Bottom soldier bar. Shows all pool warriors; deployed slots stay visible + highlighted.
    /// Pointer is driven by Input in Update (EventSystem catcher was unreliable on this canvas).
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public sealed class FormationSoldierBarView : MonoBehaviour
    {
        public const float SlotSize = 80f;
        private const float SlotSpacing = 8f;
        private const float DecideThresholdPx = 8f;

        [SerializeField] private RectTransform _barRoot;
        [SerializeField] private ScrollRect _scrollRect;
        [SerializeField] private RectTransform _content;
        [SerializeField] private FormationSoldierSlotView _slotTemplate;

        private readonly List<FormationSoldierSlotView> _slots = new List<FormationSoldierSlotView>();

        private enum PointerMode
        {
            Idle,
            Pressed,
            Scrolling,
            Lifting
        }

        private PointerMode _mode;
        private Vector2 _pressScreen;
        private Vector2 _lastScreen;
        private FormationSoldierSlotView _pressedSlot;
        private bool _liftNotified;
        private Canvas _canvas;

        public RectTransform BarRoot => _barRoot != null ? _barRoot : (RectTransform)transform;

        /// <summary>Upward drag started on a slot (still may be inside bar).</summary>
        public event Action<FormationSoldierSlotView> SlotLiftStarted;

        private void Awake()
        {
            _canvas = GetComponentInParent<Canvas>();
            EnsureScrollHierarchy();
            DisablePointerCatcherRaycast();
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
                if (!ContainsScreenPoint(mouse, null))
                {
                    return;
                }

                _pressScreen = mouse;
                _lastScreen = mouse;
                _pressedSlot = FindSlotAt(mouse) ?? FindNearestSlot(mouse, SlotSize * 1.75f);
                _mode = PointerMode.Pressed;
                _liftNotified = false;
                return;
            }

            if (_mode == PointerMode.Idle)
            {
                return;
            }

            if (Input.GetMouseButton(0))
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

                    if (Mathf.Abs(total.x) >= Mathf.Abs(total.y))
                    {
                        _mode = PointerMode.Scrolling;
                    }
                    else if (total.y > 0f)
                    {
                        BeginLift();
                    }
                    else
                    {
                        _mode = PointerMode.Scrolling;
                    }
                }

                if (_mode == PointerMode.Scrolling)
                {
                    ApplyScrollDelta(delta.x);
                }

                return;
            }

            _mode = PointerMode.Idle;
            _pressedSlot = null;
            _liftNotified = false;
        }

        public void SetSlots(
            IReadOnlyList<string> warriorIds,
            IReadOnlyList<string> displayNames,
            IReadOnlyList<int> classLevels,
            IReadOnlyList<Sprite> thumbnails,
            IReadOnlyList<bool> highlighted)
        {
            EnsureScrollHierarchy();
            DisablePointerCatcherRaycast();

            var count = warriorIds != null ? warriorIds.Count : 0;
            EnsureSlotCount(count);
            for (var i = 0; i < _slots.Count; i++)
            {
                var slot = _slots[i];
                if (i >= count)
                {
                    slot.gameObject.SetActive(false);
                    continue;
                }

                slot.gameObject.SetActive(true);
                var sprite = thumbnails != null && i < thumbnails.Count ? thumbnails[i] : null;
                var hi = highlighted != null && i < highlighted.Count && highlighted[i];
                var displayName = displayNames != null && i < displayNames.Count ? displayNames[i] : null;
                var classLevel = classLevels != null && i < classLevels.Count ? classLevels[i] : 0;
                slot.Bind(warriorIds[i], displayName, classLevel, sprite, hi);
            }

            if (_content != null)
            {
                var width = Mathf.Max(
                    SlotSize,
                    count * (SlotSize + SlotSpacing) + SlotSpacing + 8f);
                _content.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
                _content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, SlotSize);
            }

            Canvas.ForceUpdateCanvases();
        }

        public void SetSlotHighlighted(string warriorId, bool highlighted)
        {
            for (var i = 0; i < _slots.Count; i++)
            {
                var slot = _slots[i];
                if (slot != null && string.Equals(slot.WarriorId, warriorId, StringComparison.Ordinal))
                {
                    slot.SetHighlighted(highlighted);
                    return;
                }
            }
        }

        public bool ContainsScreenPoint(Vector2 screenPoint, Camera eventCamera)
        {
            return ScreenPointInRect(BarRoot, screenPoint);
        }

        private void BeginLift()
        {
            if (_liftNotified)
            {
                return;
            }

            if (_pressedSlot == null || string.IsNullOrEmpty(_pressedSlot.WarriorId))
            {
                _pressedSlot = FindNearestSlot(_pressScreen, float.MaxValue);
            }

            if (_pressedSlot == null || string.IsNullOrEmpty(_pressedSlot.WarriorId))
            {
                _mode = PointerMode.Scrolling;
                return;
            }

            _mode = PointerMode.Lifting;
            _liftNotified = true;
            _pressedSlot.SetHighlighted(true);
            SlotLiftStarted?.Invoke(_pressedSlot);
        }

        private void ApplyScrollDelta(float deltaX)
        {
            if (_content == null)
            {
                return;
            }

            var viewport = _scrollRect != null ? _scrollRect.viewport : null;
            if (viewport == null && _scrollRect != null)
            {
                viewport = _scrollRect.transform as RectTransform;
            }

            var pos = _content.anchoredPosition;
            pos.x += deltaX;
            var viewW = viewport != null ? Mathf.Abs(viewport.rect.width) : 0f;
            var contentW = Mathf.Abs(_content.rect.width);
            var minX = viewW > 0f ? Mathf.Min(0f, viewW - contentW) : 0f;
            pos.x = Mathf.Clamp(pos.x, minX, 0f);
            pos.y = 0f;
            _content.anchoredPosition = pos;
        }

        private bool ScreenPointInRect(RectTransform rt, Vector2 screenPoint)
        {
            if (rt == null)
            {
                return false;
            }

            var cam = GetEventCamera();
            return RectTransformUtility.ScreenPointToLocalPointInRectangle(rt, screenPoint, cam, out var local)
                   && rt.rect.Contains(local);
        }

        private Camera GetEventCamera()
        {
            if (_canvas == null)
            {
                _canvas = GetComponentInParent<Canvas>();
            }

            if (_canvas == null || _canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                return null;
            }

            return _canvas.worldCamera;
        }

        private FormationSoldierSlotView FindSlotAt(Vector2 screenPoint)
        {
            for (var i = 0; i < _slots.Count; i++)
            {
                var slot = _slots[i];
                if (slot == null || !slot.gameObject.activeInHierarchy)
                {
                    continue;
                }

                if (ScreenPointInRect(slot.RectTransform, screenPoint))
                {
                    return slot;
                }
            }

            return null;
        }

        private FormationSoldierSlotView FindNearestSlot(Vector2 screenPoint, float maxDistance)
        {
            FormationSoldierSlotView best = null;
            var bestDist = maxDistance;
            var cam = GetEventCamera();
            for (var i = 0; i < _slots.Count; i++)
            {
                var slot = _slots[i];
                if (slot == null || !slot.gameObject.activeInHierarchy || string.IsNullOrEmpty(slot.WarriorId))
                {
                    continue;
                }

                var rt = slot.RectTransform;
                if (rt == null)
                {
                    continue;
                }

                var world = rt.TransformPoint(rt.rect.center);
                var center = RectTransformUtility.WorldToScreenPoint(cam, world);
                var dist = Vector2.Distance(screenPoint, center);
                if (dist < bestDist)
                {
                    bestDist = dist;
                    best = slot;
                }
            }

            return best;
        }

        private void DisablePointerCatcherRaycast()
        {
            var catcher = transform.Find("PointerCatcher");
            if (catcher == null)
            {
                return;
            }

            var img = catcher.GetComponent<Image>();
            if (img != null)
            {
                img.raycastTarget = false;
            }

            catcher.gameObject.SetActive(false);
        }

        private void EnsureScrollHierarchy()
        {
            if (_scrollRect == null)
            {
                _scrollRect = GetComponentInChildren<ScrollRect>(true);
            }

            if (_scrollRect == null)
            {
                return;
            }

            var scrollRt = _scrollRect.transform as RectTransform;
            if (_content == null)
            {
                var contentTf = _scrollRect.transform.Find("Viewport/Content")
                                ?? _scrollRect.transform.Find("Content");
                if (contentTf != null)
                {
                    _content = contentTf as RectTransform;
                }
            }

            if (_content == null)
            {
                return;
            }

            if (_scrollRect.viewport != null
                && _scrollRect.viewport != scrollRt
                && _content.parent == _scrollRect.viewport)
            {
                _scrollRect.content = _content;
                _scrollRect.enabled = false;
                return;
            }

            Transform viewportTf = _scrollRect.transform.Find("Viewport");
            RectTransform viewportRt;
            if (viewportTf == null)
            {
                var viewportGo = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(RectMask2D));
                viewportGo.transform.SetParent(_scrollRect.transform, false);
                viewportRt = viewportGo.GetComponent<RectTransform>();
                Stretch(viewportRt);
                var vpImg = viewportGo.GetComponent<Image>();
                vpImg.color = new Color(1f, 1f, 1f, 0.01f);
                vpImg.raycastTarget = false;
            }
            else
            {
                viewportRt = viewportTf as RectTransform;
                if (viewportTf.GetComponent<RectMask2D>() == null && viewportTf.GetComponent<Mask>() == null)
                {
                    viewportTf.gameObject.AddComponent<RectMask2D>();
                }

                var img = viewportTf.GetComponent<Image>();
                if (img != null)
                {
                    img.raycastTarget = false;
                }
            }

            if (_content.parent != viewportRt)
            {
                _content.SetParent(viewportRt, false);
            }

            _content.anchorMin = new Vector2(0f, 0.5f);
            _content.anchorMax = new Vector2(0f, 0.5f);
            _content.pivot = new Vector2(0f, 0.5f);

            var fitter = _content.GetComponent<ContentSizeFitter>();
            if (fitter != null)
            {
                Destroy(fitter);
            }

            _scrollRect.content = _content;
            _scrollRect.viewport = viewportRt;
            _scrollRect.enabled = false;
        }

        private void EnsureSlotCount(int count)
        {
            if (_slotTemplate == null || _content == null)
            {
                return;
            }

            _slotTemplate.gameObject.SetActive(false);
            while (_slots.Count < count)
            {
                var go = Instantiate(_slotTemplate.gameObject, _content);
                go.name = $"SoldierSlot_{_slots.Count}";
                go.SetActive(true);

                var rt = go.transform as RectTransform;
                if (rt != null)
                {
                    rt.sizeDelta = new Vector2(SlotSize, SlotSize);
                }

                var layout = go.GetComponent<LayoutElement>();
                if (layout == null)
                {
                    layout = go.AddComponent<LayoutElement>();
                }

                layout.preferredWidth = SlotSize;
                layout.preferredHeight = SlotSize;
                layout.minWidth = SlotSize;
                layout.minHeight = SlotSize;

                var slot = go.GetComponent<FormationSoldierSlotView>();
                if (slot == null)
                {
                    slot = go.AddComponent<FormationSoldierSlotView>();
                }

                _slots.Add(slot);
            }
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
    }
}
