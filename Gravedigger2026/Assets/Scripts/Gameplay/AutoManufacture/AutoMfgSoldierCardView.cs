using UnityEngine;
using UnityEngine.UI;

namespace Gravedigger2026.Gameplay.AutoManufacture
{
    /// <summary>
    /// One AutoManufacture presentation soldier card (UI-016 Step1/2).
    /// </summary>
    public sealed class AutoMfgSoldierCardView : MonoBehaviour
    {
        public const float CardWidth = 150f;
        public const float CardHeight = 200f;

        [SerializeField] private Image _background;
        [SerializeField] private Text _questionMark;
        [SerializeField] private Text _className;
        [SerializeField] private Image _idleThumbnail;
        [SerializeField] private Text _amplifyLabel;

        private string _warriorId;
        private RectTransform _rect;

        public string WarriorId => _warriorId;
        public RectTransform RectTransform => _rect != null ? _rect : (_rect = transform as RectTransform);

        public void RuntimeWire(
            Image background,
            Text questionMark,
            Text className,
            Image idleThumbnail,
            Text amplifyLabel)
        {
            _background = background;
            _questionMark = questionMark;
            _className = className;
            _idleThumbnail = idleThumbnail;
            _amplifyLabel = amplifyLabel;
            _rect = transform as RectTransform;
        }

        public void BindMystery(string warriorId, string className)
        {
            _warriorId = warriorId ?? string.Empty;
            if (_className != null)
            {
                _className.text = className ?? string.Empty;
                _className.gameObject.SetActive(true);
            }

            if (_questionMark != null)
            {
                _questionMark.text = "?";
                _questionMark.gameObject.SetActive(true);
            }

            if (_idleThumbnail != null)
            {
                _idleThumbnail.enabled = false;
                _idleThumbnail.sprite = null;
            }

            SetAmplifyVisible(false);
        }

        public void RevealIdle(Sprite idleSprite)
        {
            if (_questionMark != null)
            {
                _questionMark.gameObject.SetActive(false);
            }

            if (_idleThumbnail != null)
            {
                _idleThumbnail.sprite = idleSprite;
                _idleThumbnail.enabled = true;
                _idleThumbnail.preserveAspect = true;
                _idleThumbnail.color = idleSprite != null
                    ? Color.white
                    : new Color(1f, 1f, 1f, 0.2f);
            }
        }

        public void SetAmplifyVisible(bool visible)
        {
            if (_amplifyLabel != null)
            {
                _amplifyLabel.gameObject.SetActive(visible);
                if (visible)
                {
                    _amplifyLabel.text = "加强";
                }
            }
        }
    }
}
