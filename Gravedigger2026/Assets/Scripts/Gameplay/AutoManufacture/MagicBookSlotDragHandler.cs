using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Gravedigger2026.Gameplay.AutoManufacture
{
    /// <summary>
    /// LMB drag reorder for MagicBookSlotsPanel BookRow (UI-023 / D-068). AM presentation does not mount this.
    /// </summary>
    public sealed class MagicBookSlotDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private static readonly List<RaycastResult> RaycastBuffer = new List<RaycastResult>(8);

        private BookRowView _row;
        private int _slotIndex;
        private RectTransform _ghost;
        private CanvasGroup _sourceGroup;
        private float _sourceAlpha = 1f;
        private bool _dragging;

        public int SlotIndex => _slotIndex;

        public void Wire(BookRowView row, int slotIndex)
        {
            _row = row;
            _slotIndex = slotIndex;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (!isActiveAndEnabled || eventData.button != PointerEventData.InputButton.Left || _row == null)
            {
                return;
            }

            _dragging = true;
            _sourceGroup = GetComponent<CanvasGroup>();
            if (_sourceGroup == null)
            {
                _sourceGroup = gameObject.AddComponent<CanvasGroup>();
            }

            _sourceAlpha = _sourceGroup.alpha;
            _sourceGroup.alpha = 0.45f;
            CreateGhost(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!_dragging || _ghost == null)
            {
                return;
            }

            MoveGhost(eventData);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!_dragging)
            {
                return;
            }

            _dragging = false;
            if (_sourceGroup != null)
            {
                _sourceGroup.alpha = _sourceAlpha;
            }

            DestroyGhost();

            if (eventData.button != PointerEventData.InputButton.Left || _row == null)
            {
                return;
            }

            var target = FindDropTarget(eventData);
            if (target == null || target == this)
            {
                return;
            }

            _row.TryReorder(_slotIndex, target.SlotIndex);
        }

        private void OnDisable()
        {
            if (_dragging && _sourceGroup != null)
            {
                _sourceGroup.alpha = _sourceAlpha;
            }

            _dragging = false;
            DestroyGhost();
        }

        private void CreateGhost(PointerEventData eventData)
        {
            DestroyGhost();
            var canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                return;
            }

            var root = canvas.rootCanvas != null ? canvas.rootCanvas : canvas;
            var ghostGo = new GameObject(
                "BookDragGhost",
                typeof(RectTransform),
                typeof(CanvasGroup),
                typeof(Image));
            ghostGo.transform.SetParent(root.transform, false);
            ghostGo.transform.SetAsLastSibling();

            var cg = ghostGo.GetComponent<CanvasGroup>();
            cg.blocksRaycasts = false;
            cg.alpha = 0.9f;

            var ghostRt = ghostGo.GetComponent<RectTransform>();
            ghostRt.sizeDelta = new Vector2(
                AutoMfgMagicBookSlotView.SlotWidth,
                AutoMfgMagicBookSlotView.SlotHeight);
            ghostRt.pivot = new Vector2(0.5f, 0.5f);

            var srcImage = GetComponent<Image>();
            var ghostImage = ghostGo.GetComponent<Image>();
            ghostImage.raycastTarget = false;
            ghostImage.color = srcImage != null ? srcImage.color : new Color(0.28f, 0.3f, 0.4f, 0.95f);

            var srcIcon = transform.Find("Icon")?.GetComponent<Image>();
            if (srcIcon != null && srcIcon.enabled && srcIcon.sprite != null)
            {
                var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
                iconGo.transform.SetParent(ghostGo.transform, false);
                var iconRt = iconGo.GetComponent<RectTransform>();
                iconRt.anchorMin = new Vector2(0.15f, 0.35f);
                iconRt.anchorMax = new Vector2(0.85f, 0.9f);
                iconRt.offsetMin = Vector2.zero;
                iconRt.offsetMax = Vector2.zero;
                var icon = iconGo.GetComponent<Image>();
                icon.sprite = srcIcon.sprite;
                icon.preserveAspect = true;
                icon.raycastTarget = false;
            }

            var srcName = transform.Find("Name")?.GetComponent<Text>();
            if (srcName != null && srcName.gameObject.activeSelf && !string.IsNullOrEmpty(srcName.text))
            {
                var nameGo = new GameObject("Name", typeof(RectTransform), typeof(Text));
                nameGo.transform.SetParent(ghostGo.transform, false);
                var nameRt = nameGo.GetComponent<RectTransform>();
                nameRt.anchorMin = new Vector2(0.05f, 0.02f);
                nameRt.anchorMax = new Vector2(0.95f, 0.32f);
                nameRt.offsetMin = Vector2.zero;
                nameRt.offsetMax = Vector2.zero;
                var name = nameGo.GetComponent<Text>();
                name.text = srcName.text;
                name.font = srcName.font;
                name.fontSize = srcName.fontSize;
                name.alignment = srcName.alignment;
                name.color = srcName.color;
                name.raycastTarget = false;
            }

            _ghost = ghostRt;
            MoveGhost(eventData);
        }

        private void MoveGhost(PointerEventData eventData)
        {
            if (_ghost == null)
            {
                return;
            }

            var canvas = _ghost.GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                return;
            }

            var root = canvas.rootCanvas != null ? canvas.rootCanvas : canvas;
            var canvasRt = root.transform as RectTransform;
            if (canvasRt == null)
            {
                return;
            }

            var cam = root.renderMode == RenderMode.ScreenSpaceOverlay ? null : root.worldCamera;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    canvasRt,
                    eventData.position,
                    cam,
                    out var local))
            {
                _ghost.localPosition = local;
            }
        }

        private void DestroyGhost()
        {
            if (_ghost == null)
            {
                return;
            }

            Destroy(_ghost.gameObject);
            _ghost = null;
        }

        private MagicBookSlotDragHandler FindDropTarget(PointerEventData eventData)
        {
            if (EventSystem.current == null)
            {
                return null;
            }

            RaycastBuffer.Clear();
            EventSystem.current.RaycastAll(eventData, RaycastBuffer);
            for (var i = 0; i < RaycastBuffer.Count; i++)
            {
                var go = RaycastBuffer[i].gameObject;
                if (go == null)
                {
                    continue;
                }

                var handler = go.GetComponentInParent<MagicBookSlotDragHandler>();
                if (handler != null && handler.enabled && handler._row == _row)
                {
                    return handler;
                }
            }

            return null;
        }
    }
}
