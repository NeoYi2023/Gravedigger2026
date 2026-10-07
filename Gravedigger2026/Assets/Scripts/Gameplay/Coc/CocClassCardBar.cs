using System;
using System.Collections.Generic;
using Gravedigger2026.Gameplay.Formation;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Gravedigger2026.Gameplay.Coc
{
    /// <summary>
    /// Bottom class cards for COC deploy (SPEC_03 §3.21 / UI-037 / UI-038).
    /// Short press selects. Holding left-click for CardHoldSeconds expands the group above the card
    /// whether or not it was already selected. The expand row is inspect-only.
    /// ClassCardRow is a left-aligned masked viewport; horizontal drag scrolls when cards overflow.
    /// </summary>
    public sealed class CocClassCardBar : MonoBehaviour
    {
        private const float CardWidth = 132f;
        private const float CardHeight = 168f;
        private const float Gap = 8f;
        private const float RowViewWidth = 1600f;
        private const int VisiblePeekCount = 4;

        private readonly List<CardSlot> _slots = new List<CardSlot>(8);
        private readonly Dictionary<string, Sprite> _sprites =
            new Dictionary<string, Sprite>(StringComparer.Ordinal);
        private RectTransform _row;
        private RectTransform _content;
        private Font _font;
        private float _holdSeconds = 1f;
        private bool _holding;
        private bool _expanded;
        private bool _expandReachedThisPress;
        private bool _rowScrolledThisPress;
        private string _holdingClassId;
        private float _holdElapsed;
        private GameObject _expandHost;
        private RectTransform _expandContent;
        private float _scroll;
        private float _maxScroll;
        private float _rowScroll;
        private float _rowMaxScroll;
        private float _canvasScale = 1f;

        public event Action<string> ClassSelected;

        public void SetHoldSeconds(float seconds)
        {
            _holdSeconds = seconds > 0f ? seconds : 1f;
        }

        public void Ensure(Transform parent)
        {
            if (_row != null)
            {
                return;
            }

            var rowGo = new GameObject("ClassCardRow", typeof(RectTransform), typeof(RectMask2D));
            rowGo.transform.SetParent(parent, false);
            _row = rowGo.GetComponent<RectTransform>();
            _row.anchorMin = new Vector2(0.5f, 0f);
            _row.anchorMax = new Vector2(0.5f, 0f);
            _row.pivot = new Vector2(0.5f, 0f);
            _row.anchoredPosition = new Vector2(0f, 108f);
            _row.sizeDelta = new Vector2(RowViewWidth, CardHeight);

            var contentGo = new GameObject("Content", typeof(RectTransform));
            contentGo.transform.SetParent(rowGo.transform, false);
            _content = contentGo.GetComponent<RectTransform>();
            _content.anchorMin = new Vector2(0f, 0.5f);
            _content.anchorMax = new Vector2(0f, 0.5f);
            _content.pivot = new Vector2(0f, 0.5f);
            _content.sizeDelta = new Vector2(0f, CardHeight);
            _content.anchoredPosition = Vector2.zero;

            _font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        public void Rebuild(IReadOnlyList<CocClassCardModel> cards, string selectedClassId)
        {
            if (_row == null || _content == null)
            {
                return;
            }

            ClearSlots();
            if (cards == null || cards.Count == 0)
            {
                _content.sizeDelta = new Vector2(0f, CardHeight);
                _rowScroll = 0f;
                _rowMaxScroll = 0f;
                ApplyRowScroll();
                return;
            }

            for (var i = 0; i < cards.Count; i++)
            {
                var model = cards[i];
                if (model == null || string.IsNullOrEmpty(model.ClassId) || model.Count <= 0)
                {
                    continue;
                }

                var selected = string.Equals(model.ClassId, selectedClassId, StringComparison.Ordinal);
                var slot = CreateSlot(model, selected);
                var rt = slot.Root;
                rt.anchoredPosition = new Vector2(_slots.Count * (CardWidth + Gap), 0f);
                _slots.Add(slot);
            }

            var count = _slots.Count;
            var contentWidth = count > 0
                ? count * CardWidth + (count - 1) * Gap
                : 0f;
            _content.sizeDelta = new Vector2(contentWidth, CardHeight);
            _rowMaxScroll = Mathf.Max(0f, contentWidth - RowViewWidth);
            _rowScroll = 0f;
            ApplyRowScroll();
        }

        public void Clear()
        {
            ClearSlots();
            _sprites.Clear();
        }

        public void BeginHold(string classId)
        {
            DismissExpandVisual();
            if (string.IsNullOrEmpty(classId))
            {
                _holding = false;
                _expanded = false;
                _expandReachedThisPress = false;
                _rowScrolledThisPress = false;
                _holdingClassId = null;
                _holdElapsed = 0f;
                return;
            }

            // Hold works whether or not the card is already selected (UI-038).
            _holding = true;
            _expanded = false;
            _expandReachedThisPress = false;
            _rowScrolledThisPress = false;
            _holdingClassId = classId;
            _holdElapsed = 0f;
            _scroll = 0f;
            _maxScroll = 0f;
            RefreshCanvasScale();
        }

        /// <summary>
        /// Ends the press, dismisses expand, and selects the card when this press was valid.
        /// Selection does not gate expand: hold works on selected and unselected cards alike.
        /// A horizontal drag that scrolled the bottom bar does not select.
        /// </summary>
        public void EndHold()
        {
            var classId = _holdingClassId;
            var shouldSelect = _holding
                && !_rowScrolledThisPress
                && !string.IsNullOrEmpty(classId);
            ResetHoldState();
            if (shouldSelect)
            {
                ClassSelected?.Invoke(classId);
            }
        }

        public void CancelIfPending(string classId)
        {
            // Ignore exit while the mouse button is still down: creating the expand row
            // can spuriously fire PointerExit and made hold feel like it needed a prior select.
            if (Input.GetMouseButton(0))
            {
                return;
            }

            if (!_holding || _expanded || _expandReachedThisPress)
            {
                return;
            }

            if (!string.Equals(_holdingClassId, classId, StringComparison.Ordinal))
            {
                return;
            }

            ResetHoldState();
        }

        private void ResetHoldState()
        {
            _holding = false;
            _expanded = false;
            _expandReachedThisPress = false;
            _rowScrolledThisPress = false;
            _holdingClassId = null;
            _holdElapsed = 0f;
            _scroll = 0f;
            _maxScroll = 0f;
            DismissExpandVisual();
        }

        public bool IsExpanded => _expanded;

        public void DragExpanded(float deltaX)
        {
            if (!_expanded || _expandContent == null || _maxScroll <= 0f)
            {
                return;
            }

            var scale = _canvasScale > 0f ? _canvasScale : 1f;
            _scroll = Mathf.Clamp(_scroll - deltaX / scale, 0f, _maxScroll);
            var pos = _expandContent.anchoredPosition;
            pos.x = -_scroll;
            _expandContent.anchoredPosition = pos;
        }

        /// <summary>
        /// Scrolls the bottom class-card row. Cancels a pending hold-to-expand on first use.
        /// </summary>
        public void DragRow(float deltaX)
        {
            if (_expanded || _content == null)
            {
                return;
            }

            if (!_rowScrolledThisPress)
            {
                _rowScrolledThisPress = true;
                _holding = false;
                _holdElapsed = 0f;
            }

            if (_rowMaxScroll <= 0f)
            {
                return;
            }

            var scale = _canvasScale > 0f ? _canvasScale : 1f;
            _rowScroll = Mathf.Clamp(_rowScroll - deltaX / scale, 0f, _rowMaxScroll);
            ApplyRowScroll();
        }

        private void ApplyRowScroll()
        {
            if (_content == null)
            {
                return;
            }

            var pos = _content.anchoredPosition;
            pos.x = -_rowScroll;
            _content.anchoredPosition = pos;
        }

        private void RefreshCanvasScale()
        {
            var canvas = _row != null ? _row.GetComponentInParent<Canvas>() : null;
            _canvasScale = canvas != null && canvas.scaleFactor > 0f ? canvas.scaleFactor : 1f;
        }

        private void Update()
        {
            if (!_holding || _expanded || _rowScrolledThisPress)
            {
                return;
            }

            _holdElapsed += Time.deltaTime;
            if (_holdElapsed < _holdSeconds)
            {
                return;
            }

            // Mark before spawning children so a spurious PointerExit cannot cancel this press.
            _expandReachedThisPress = true;
            _expanded = true;
            ShowExpand(_holdingClassId);
        }

        private void ShowExpand(string classId)
        {
            CardSlot slot = default;
            var found = false;
            for (var i = 0; i < _slots.Count; i++)
            {
                if (string.Equals(_slots[i].ClassId, classId, StringComparison.Ordinal))
                {
                    slot = _slots[i];
                    found = true;
                    break;
                }
            }

            if (!found || slot.Root == null || slot.Members == null || slot.Members.Count == 0)
            {
                _holding = false;
                _expanded = false;
                _expandReachedThisPress = false;
                _holdingClassId = null;
                DismissExpandVisual();
                return;
            }

            var count = slot.Members.Count;
            var contentWidth = count * CardWidth + (count - 1) * Gap;
            var visible = Mathf.Min(count, VisiblePeekCount);
            var viewWidth = visible * CardWidth + (visible - 1) * Gap;

            var hostGo = new GameObject("GroupExpand", typeof(RectTransform), typeof(RectMask2D));
            hostGo.transform.SetParent(slot.Root, false);
            var hostRt = hostGo.GetComponent<RectTransform>();
            hostRt.anchorMin = new Vector2(0.5f, 1f);
            hostRt.anchorMax = new Vector2(0.5f, 1f);
            hostRt.pivot = new Vector2(0.5f, 0f);
            hostRt.anchoredPosition = new Vector2(0f, 8f);
            hostRt.sizeDelta = new Vector2(viewWidth, CardHeight);

            var catchGo = new GameObject(
                "HitCatch",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            catchGo.transform.SetParent(hostGo.transform, false);
            var catchRt = catchGo.GetComponent<RectTransform>();
            catchRt.anchorMin = Vector2.zero;
            catchRt.anchorMax = Vector2.one;
            catchRt.offsetMin = Vector2.zero;
            catchRt.offsetMax = Vector2.zero;
            var catchImage = catchGo.GetComponent<Image>();
            catchImage.color = new Color(0f, 0f, 0f, 0.01f);
            catchImage.raycastTarget = true;

            var contentGo = new GameObject("Content", typeof(RectTransform));
            contentGo.transform.SetParent(hostGo.transform, false);
            var content = contentGo.GetComponent<RectTransform>();
            content.anchorMin = new Vector2(0f, 0.5f);
            content.anchorMax = new Vector2(0f, 0.5f);
            content.pivot = new Vector2(0f, 0.5f);
            content.sizeDelta = new Vector2(contentWidth, CardHeight);
            content.anchoredPosition = Vector2.zero;

            for (var i = 0; i < count; i++)
            {
                CreatePeek(content, slot.Members[i], i);
            }

            RefreshCanvasScale();
            _expandHost = hostGo;
            _expandContent = content;
            _scroll = 0f;
            _maxScroll = Mathf.Max(0f, contentWidth - viewWidth);
        }

        private void DismissExpandVisual()
        {
            if (_expandHost != null)
            {
                Destroy(_expandHost);
                _expandHost = null;
                _expandContent = null;
            }
        }

        private void CreatePeek(RectTransform content, CocClassCardMember member, int index)
        {
            var go = new GameObject(
                "Peek_" + index,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            go.transform.SetParent(content, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0.5f);
            rt.anchorMax = new Vector2(0f, 0.5f);
            rt.pivot = new Vector2(0f, 0.5f);
            rt.sizeDelta = new Vector2(CardWidth, CardHeight);
            rt.anchoredPosition = new Vector2(index * (CardWidth + Gap), 0f);

            var image = go.GetComponent<Image>();
            image.color = new Color(0.18f, 0.2f, 0.24f, 0.96f);
            image.raycastTarget = true;

            var sprite = member != null ? ResolveSprite(member.AppearancePrefab) : null;
            if (sprite != null)
            {
                var thumbGo = new GameObject("Thumb", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                thumbGo.transform.SetParent(go.transform, false);
                var thumbRt = thumbGo.GetComponent<RectTransform>();
                thumbRt.anchorMin = new Vector2(0.5f, 0.5f);
                thumbRt.anchorMax = new Vector2(0.5f, 0.5f);
                thumbRt.pivot = new Vector2(0.5f, 0.5f);
                thumbRt.sizeDelta = new Vector2(88f, 88f);
                thumbRt.anchoredPosition = new Vector2(0f, 8f);
                var thumb = thumbGo.GetComponent<Image>();
                thumb.sprite = sprite;
                thumb.preserveAspect = true;
                thumb.raycastTarget = false;
            }

            var label = member != null ? member.Label : string.Empty;
            AddLabel(go.transform, "Name", label, 18, new Vector2(0f, 62f), new Vector2(120f, 28f));
            AddLabel(go.transform, "Index", (index + 1).ToString(), 22, new Vector2(0f, -62f), new Vector2(120f, 28f));
        }

        private CardSlot CreateSlot(CocClassCardModel model, bool selected)
        {
            var go = new GameObject(
                "ClassCard_" + model.ClassId,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Button),
                typeof(CocClassCardHoldRelay));
            go.transform.SetParent(_content, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0.5f);
            rt.anchorMax = new Vector2(0f, 0.5f);
            rt.pivot = new Vector2(0f, 0.5f);
            rt.sizeDelta = new Vector2(CardWidth, CardHeight);

            var image = go.GetComponent<Image>();
            image.color = selected
                ? new Color(0.45f, 0.62f, 0.85f, 0.96f)
                : new Color(0.18f, 0.2f, 0.24f, 0.94f);

            var sprite = ResolveSprite(model.AppearancePrefab);
            if (sprite != null)
            {
                var thumbGo = new GameObject("Thumb", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                thumbGo.transform.SetParent(go.transform, false);
                var thumbRt = thumbGo.GetComponent<RectTransform>();
                thumbRt.anchorMin = new Vector2(0.5f, 0.5f);
                thumbRt.anchorMax = new Vector2(0.5f, 0.5f);
                thumbRt.pivot = new Vector2(0.5f, 0.5f);
                thumbRt.sizeDelta = new Vector2(88f, 88f);
                thumbRt.anchoredPosition = new Vector2(0f, 8f);
                var thumb = thumbGo.GetComponent<Image>();
                thumb.sprite = sprite;
                thumb.preserveAspect = true;
                thumb.raycastTarget = false;
            }

            AddLabel(go.transform, "Name", model.ClassName, 20, new Vector2(0f, 62f), new Vector2(120f, 28f));
            AddLabel(go.transform, "Count", model.Count.ToString(), 22, new Vector2(0f, -62f), new Vector2(120f, 28f));

            var captured = model.ClassId;
            var button = go.GetComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.None;
            // Selection is handled on pointer-up only when this press did not expand,
            // so hold-to-peek does not require a prior click-select.
            button.onClick.RemoveAllListeners();

            var relay = go.GetComponent<CocClassCardHoldRelay>();
            relay.Bar = this;
            relay.ClassId = captured;

            var members = new List<CocClassCardMember>(model.Members != null ? model.Members.Count : 0);
            if (model.Members != null)
            {
                for (var i = 0; i < model.Members.Count; i++)
                {
                    members.Add(model.Members[i]);
                }
            }

            return new CardSlot(rt, button, captured, members);
        }

        private void AddLabel(Transform parent, string name, string text, int size, Vector2 pos, Vector2 sizeDelta)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = sizeDelta;
            rt.anchoredPosition = pos;
            var label = go.GetComponent<Text>();
            label.font = _font;
            label.fontSize = size;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Color.white;
            label.text = text ?? string.Empty;
            label.raycastTarget = false;
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
        }

        private Sprite ResolveSprite(GameObject appearancePrefab)
        {
            if (appearancePrefab == null)
            {
                return null;
            }

            var key = appearancePrefab.name;
            if (_sprites.TryGetValue(key, out var cached))
            {
                return cached;
            }

            var sprite = FormationBattlefieldPreview.SampleIdleSprite(appearancePrefab);
            _sprites[key] = sprite;
            return sprite;
        }

        private void ClearSlots()
        {
            ResetHoldState();
            for (var i = 0; i < _slots.Count; i++)
            {
                var slot = _slots[i];
                if (slot.Button != null)
                {
                    slot.Button.onClick.RemoveAllListeners();
                }

                if (slot.Root != null)
                {
                    Destroy(slot.Root.gameObject);
                }
            }

            _slots.Clear();
        }

        private struct CardSlot
        {
            public RectTransform Root;
            public Button Button;
            public string ClassId;
            public List<CocClassCardMember> Members;

            public CardSlot(RectTransform root, Button button, string classId, List<CocClassCardMember> members)
            {
                Root = root;
                Button = button;
                ClassId = classId;
                Members = members;
            }
        }
    }

    /// <summary>
    /// Forwards press, release, and drag from one class card. Peek cells are not buttons.
    /// </summary>
    public sealed class CocClassCardHoldRelay : MonoBehaviour,
        IPointerDownHandler,
        IPointerUpHandler,
        IPointerExitHandler,
        IBeginDragHandler,
        IDragHandler
    {
        private const float RowScrollThresholdPx = 12f;

        public CocClassCardBar Bar;
        public string ClassId;

        private bool _rowDragActive;
        private Vector2 _pressScreen;

        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left || Bar == null)
            {
                return;
            }

            _rowDragActive = false;
            _pressScreen = eventData.position;
            Bar.BeginHold(ClassId);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left || Bar == null)
            {
                return;
            }

            Bar.EndHold();
            _rowDragActive = false;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (Bar == null)
            {
                return;
            }

            Bar.CancelIfPending(ClassId);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left || Bar == null)
            {
                return;
            }

            if (Bar.IsExpanded)
            {
                return;
            }

            var total = eventData.position - _pressScreen;
            if (Mathf.Abs(total.x) >= RowScrollThresholdPx
                && Mathf.Abs(total.x) >= Mathf.Abs(total.y))
            {
                _rowDragActive = true;
                Bar.DragRow(eventData.delta.x);
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left || Bar == null)
            {
                return;
            }

            if (Bar.IsExpanded)
            {
                Bar.DragExpanded(eventData.delta.x);
                return;
            }

            if (!_rowDragActive)
            {
                var total = eventData.position - _pressScreen;
                if (Mathf.Abs(total.x) < RowScrollThresholdPx
                    || Mathf.Abs(total.x) < Mathf.Abs(total.y))
                {
                    return;
                }

                _rowDragActive = true;
            }

            Bar.DragRow(eventData.delta.x);
        }
    }

    public sealed class CocClassCardMember
    {
        public string Label;
        public GameObject AppearancePrefab;
    }

    public sealed class CocClassCardModel
    {
        public string ClassId;
        public string ClassName;
        public int Count;
        public GameObject AppearancePrefab;
        public List<CocClassCardMember> Members = new List<CocClassCardMember>(4);
    }
}
