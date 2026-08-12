using UnityEngine;
using UnityEngine.UI;

namespace Gravedigger2026.Gameplay.AutoManufacture
{
    /// <summary>
    /// One of six MagicBook presentation slots (UI-016 Step1/2).
    /// </summary>
    public sealed class AutoMfgMagicBookSlotView : MonoBehaviour
    {
        public const float SlotWidth = 120f;
        public const float SlotHeight = 160f;

        [SerializeField] private Image _background;
        [SerializeField] private Image _icon;
        [SerializeField] private Text _nameLabel;

        private RectTransform _rect;
        private Vector3 _baseScale = Vector3.one;

        public RectTransform RectTransform => _rect != null ? _rect : (_rect = transform as RectTransform);

        public void RuntimeWire(Image background, Image icon, Text nameLabel)
        {
            _background = background;
            _icon = icon;
            _nameLabel = nameLabel;
            _rect = transform as RectTransform;
            _baseScale = transform.localScale;
        }

        public void BindEmpty()
        {
            if (_nameLabel != null)
            {
                _nameLabel.text = string.Empty;
                _nameLabel.gameObject.SetActive(false);
            }

            if (_icon != null)
            {
                _icon.sprite = null;
                _icon.enabled = false;
            }
        }

        public void BindBook(string displayName, Sprite icon)
        {
            if (_nameLabel != null)
            {
                _nameLabel.text = displayName ?? string.Empty;
                _nameLabel.gameObject.SetActive(!string.IsNullOrEmpty(displayName));
            }

            if (_icon != null)
            {
                _icon.sprite = icon;
                _icon.enabled = icon != null;
                _icon.preserveAspect = true;
            }
        }

        public void ResetScale()
        {
            transform.localScale = _baseScale;
        }

        public void SetPulseScale(float scale)
        {
            transform.localScale = _baseScale * scale;
        }
    }
}
