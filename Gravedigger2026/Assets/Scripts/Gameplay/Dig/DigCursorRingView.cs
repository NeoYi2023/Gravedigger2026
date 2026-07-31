using UnityEngine;

namespace Gravedigger2026.Gameplay.Dig
{
    /// <summary>
    /// Screen-space dig cursor ring Prefab view: stroke outer + fill inner.
    /// Stroke pixel thickness stays constant when diameter changes (SPEC_03 §3.10).
    /// </summary>
    public sealed class DigCursorRingView : MonoBehaviour
    {
        [SerializeField] private RectTransform _root;
        [SerializeField] private RectTransform _stroke;
        [SerializeField] private RectTransform _fill;
        [SerializeField] private float _strokeWidthPx = 3f;

        public RectTransform Root => _root != null ? _root : transform as RectTransform;

        /// <summary>Stroke gap in the same units as <see cref="ApplyDiameter"/> (canvas units after scaleFactor conversion).</summary>
        public float StrokeWidthPx
        {
            get => _strokeWidthPx;
            set => _strokeWidthPx = Mathf.Max(0.01f, value);
        }

        /// <param name="diameterCanvasUnits">Outer diameter in Canvas / RectTransform units (not raw screen pixels).</param>
        public void ApplyDiameter(float diameterCanvasUnits)
        {
            var stroke = Mathf.Max(0.01f, _strokeWidthPx);
            var d = Mathf.Max(diameterCanvasUnits, stroke * 2f + 1f);
            var root = Root;
            if (root != null)
            {
                root.sizeDelta = new Vector2(d, d);
            }

            if (_stroke != null)
            {
                _stroke.sizeDelta = new Vector2(d, d);
            }

            if (_fill != null)
            {
                var inner = Mathf.Max(1f, d - stroke * 2f);
                _fill.sizeDelta = new Vector2(inner, inner);
            }
        }

#if UNITY_EDITOR
        public void EditorBind(RectTransform root, RectTransform stroke, RectTransform fill, float strokeWidthPx)
        {
            _root = root;
            _stroke = stroke;
            _fill = fill;
            _strokeWidthPx = strokeWidthPx;
        }
#endif
    }
}
