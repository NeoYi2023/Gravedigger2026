using UnityEngine;
using UnityEngine.UI;

namespace Gravedigger2026.Gameplay.Dig
{
    /// <summary>UI-011 DigStageSummary reward cell: square icon + bottom-right qty + name below.</summary>
    public sealed class DigSummaryItemCell : MonoBehaviour
    {
        [SerializeField] private Image _icon;
        [SerializeField] private Text _quantityText;
        [SerializeField] private Text _nameText;

        public void Bind(Sprite icon, string displayName, string quantityText)
        {
            if (_icon != null)
            {
                _icon.sprite = icon;
                _icon.enabled = icon != null;
                _icon.preserveAspect = true;
            }

            if (_quantityText != null)
            {
                _quantityText.text = quantityText ?? string.Empty;
            }

            if (_nameText != null)
            {
                _nameText.text = displayName ?? string.Empty;
            }
        }
    }
}
