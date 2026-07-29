using UnityEngine;
using UnityEngine.UI;

namespace Gravedigger2026.Gameplay.Formation
{
    /// <summary>
    /// Visual-only 80×80 soldier cell. Highlight = deployed / lifting brighten effect.
    /// </summary>
    public sealed class FormationSoldierSlotView : MonoBehaviour
    {
        private static readonly Color NormalColor = new Color(0.22f, 0.24f, 0.3f, 1f);
        private static readonly Color HighlightColor = new Color(0.55f, 0.72f, 0.95f, 1f);

        [SerializeField] private Image _thumbnail;
        [SerializeField] private Text _label;
        [SerializeField] private Image _background;

        private string _warriorId;
        private bool _highlighted;

        public string WarriorId => _warriorId;
        public bool IsHighlighted => _highlighted;
        public RectTransform RectTransform => transform as RectTransform;

        public void Bind(string warriorId, Sprite thumbnail, bool highlighted)
        {
            _warriorId = warriorId ?? string.Empty;
            if (_thumbnail != null)
            {
                _thumbnail.sprite = thumbnail;
                _thumbnail.enabled = true;
                _thumbnail.preserveAspect = true;
                _thumbnail.raycastTarget = false;
                _thumbnail.color = thumbnail != null ? Color.white : new Color(1f, 1f, 1f, 0.15f);
            }

            if (_label != null)
            {
                _label.text = string.IsNullOrEmpty(_warriorId) ? string.Empty : _warriorId;
                _label.raycastTarget = false;
            }

            SetHighlighted(highlighted);
        }

        public void SetHighlighted(bool highlighted)
        {
            _highlighted = highlighted;
            if (_background == null)
            {
                _background = GetComponent<Image>();
            }

            if (_background != null)
            {
                _background.color = highlighted ? HighlightColor : NormalColor;
                _background.raycastTarget = false;
            }
        }
    }
}
