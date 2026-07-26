using System;
using Gravedigger2026.Core.Config;
using Gravedigger2026.Core.Tech;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Gravedigger2026.UI
{
    /// <summary>
    /// One TechTree node on the Settings canvas (SPEC_03 §3.13 / UI-012).
    /// </summary>
    public sealed class TechTreeNodeView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        [SerializeField] private string _techId;
        [SerializeField] private Image _frameImage;
        [SerializeField] private Image _iconImage;
        [SerializeField] private Text _debugLabel;

        private TechTreeCanvasView _canvas;
        private TechTreeConfigRow _row;

        public string TechId => _techId;
        public RectTransform RectTransform => transform as RectTransform;

        public void Configure(TechTreeCanvasView canvas, TechTreeConfigRow row)
        {
            _canvas = canvas;
            _row = row;
            _techId = row?.TechId;
            if (_debugLabel != null && row != null)
            {
                _debugLabel.text = string.IsNullOrEmpty(row.DisplayName) ? row.TechId : row.DisplayName;
            }
        }

        public void RefreshVisual(TechTreeService service)
        {
            if (_frameImage == null || service == null || string.IsNullOrEmpty(_techId))
            {
                return;
            }

            Color color;
            if (service.IsLearned(_techId))
            {
                color = FrameColorLearned(_row != null ? _row.TechUiFrameType : TechUiFrameType.Normal);
            }
            else if (service.IsLearnable(_techId))
            {
                color = new Color(0.95f, 0.85f, 0.35f, 1f);
            }
            else
            {
                color = new Color(0.35f, 0.35f, 0.38f, 1f);
            }

            _frameImage.color = color;
            if (_iconImage != null)
            {
                _iconImage.color = service.IsLearned(_techId)
                    ? Color.white
                    : new Color(0.7f, 0.7f, 0.7f, 1f);
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _canvas?.ShowTooltip(_row);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _canvas?.HideTooltip();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left)
            {
                _canvas?.TryLearnNode(_techId);
            }
        }

        private static Color FrameColorLearned(TechUiFrameType frameType)
        {
            switch (frameType)
            {
                case TechUiFrameType.Root:
                    return new Color(0.35f, 0.75f, 0.95f, 1f);
                case TechUiFrameType.Key:
                    return new Color(0.85f, 0.45f, 0.95f, 1f);
                case TechUiFrameType.Capstone:
                    return new Color(0.95f, 0.55f, 0.25f, 1f);
                default:
                    return new Color(0.40f, 0.85f, 0.45f, 1f);
            }
        }
    }
}
