using UnityEngine;
using UnityEngine.UI;

namespace Gravedigger2026.Gameplay.Formation
{
    /// <summary>
    /// Visual-only 80×80 soldier cell. Highlight = deployed / lifting brighten effect.
    /// Label stack: ClassName above, Lv.{ClassLevel} below (SPEC_03 §3.11).
    /// </summary>
    public sealed class FormationSoldierSlotView : MonoBehaviour
    {
        private static readonly Color NormalColor = new Color(0.22f, 0.24f, 0.3f, 1f);
        private static readonly Color HighlightColor = new Color(0.55f, 0.72f, 0.95f, 1f);

        [SerializeField] private Image _thumbnail;
        [SerializeField] private Text _label;
        [SerializeField] private Text _classLevelLabel;
        [SerializeField] private Image _background;

        private string _warriorId;
        private bool _highlighted;

        public string WarriorId => _warriorId;
        public bool IsHighlighted => _highlighted;
        public RectTransform RectTransform => transform as RectTransform;

        public void Bind(string warriorId, string displayName, int classLevel, Sprite thumbnail, bool highlighted)
        {
            EnsureLabelStack();
            _warriorId = warriorId ?? string.Empty;
            if (_thumbnail != null)
            {
                _thumbnail.sprite = thumbnail;
                _thumbnail.enabled = true;
                _thumbnail.preserveAspect = true;
                _thumbnail.raycastTarget = false;
                _thumbnail.color = thumbnail != null ? Color.white : new Color(1f, 1f, 1f, 0.15f);
            }

            var empty = string.IsNullOrEmpty(_warriorId);
            if (_label != null)
            {
                _label.text = empty ? string.Empty : (displayName ?? string.Empty);
                _label.raycastTarget = false;
            }

            if (_classLevelLabel != null)
            {
                var level = classLevel < 0 ? 0 : classLevel;
                _classLevelLabel.text = empty ? string.Empty : $"Lv.{level}";
                _classLevelLabel.raycastTarget = false;
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

        private void EnsureLabelStack()
        {
            if (_label == null)
            {
                var labelTf = transform.Find("Label");
                if (labelTf != null)
                {
                    _label = labelTf.GetComponent<Text>();
                }
            }

            if (_classLevelLabel == null)
            {
                var levelTf = transform.Find("ClassLevel");
                if (levelTf != null)
                {
                    _classLevelLabel = levelTf.GetComponent<Text>();
                }
            }

            if (_classLevelLabel == null && _label != null)
            {
                _classLevelLabel = CreateClassLevelLabel(_label);
            }

            LayoutLabelStack();
        }

        private void LayoutLabelStack()
        {
            if (_label != null)
            {
                var labelRt = _label.rectTransform;
                labelRt.anchorMin = new Vector2(0f, 0f);
                labelRt.anchorMax = new Vector2(1f, 0f);
                labelRt.pivot = new Vector2(0.5f, 0f);
                labelRt.anchoredPosition = new Vector2(0f, 12f);
                labelRt.sizeDelta = new Vector2(0f, 12f);
                _label.fontSize = 10;
                _label.alignment = TextAnchor.MiddleCenter;
                _label.horizontalOverflow = HorizontalWrapMode.Overflow;
                _label.verticalOverflow = VerticalWrapMode.Overflow;
            }

            if (_classLevelLabel != null)
            {
                var levelRt = _classLevelLabel.rectTransform;
                levelRt.anchorMin = new Vector2(0f, 0f);
                levelRt.anchorMax = new Vector2(1f, 0f);
                levelRt.pivot = new Vector2(0.5f, 0f);
                levelRt.anchoredPosition = new Vector2(0f, 1f);
                levelRt.sizeDelta = new Vector2(0f, 11f);
                _classLevelLabel.fontSize = 9;
                _classLevelLabel.alignment = TextAnchor.MiddleCenter;
                _classLevelLabel.horizontalOverflow = HorizontalWrapMode.Overflow;
                _classLevelLabel.verticalOverflow = VerticalWrapMode.Overflow;
            }

            if (_thumbnail == null)
            {
                var thumbTf = transform.Find("Thumbnail");
                if (thumbTf != null)
                {
                    _thumbnail = thumbTf.GetComponent<Image>();
                }
            }

            if (_thumbnail != null)
            {
                var thumbRt = _thumbnail.rectTransform;
                thumbRt.offsetMin = new Vector2(4f, 24f);
                thumbRt.offsetMax = new Vector2(-4f, -4f);
            }
        }

        private static Text CreateClassLevelLabel(Text classNameLabel)
        {
            var go = new GameObject("ClassLevel", typeof(RectTransform), typeof(Text));
            go.transform.SetParent(classNameLabel.transform.parent, false);
            var text = go.GetComponent<Text>();
            text.font = classNameLabel.font;
            text.color = classNameLabel.color;
            text.raycastTarget = false;
            return text;
        }
    }
}
