using UnityEngine;
using UnityEngine.UI;

namespace Gravedigger2026.UI
{
    /// <summary>
    /// Map-mode Selectable/Running cue: slow alpha blink + ±10% scale (UI-031).
    /// </summary>
    public sealed class LevelRouteOptionIconPulse : MonoBehaviour
    {
        public const float PeriodSeconds = 1.6f;
        public const float ScaleAmplitude = 0.1f;
        public const float AlphaMin = 0.55f;
        public const float AlphaMax = 1f;

        private Image _image;
        private RectTransform _rt;
        private Color _baseRgb = Color.white;
        private float _t;

        public void Configure(Image image)
        {
            _image = image;
            _rt = image != null ? image.rectTransform : null;
            if (_image != null)
            {
                var c = _image.color;
                _baseRgb = new Color(c.r, c.g, c.b, 1f);
            }

            _t = 0f;
            enabled = true;
        }

        public void ResetVisual()
        {
            if (_rt != null)
            {
                _rt.localScale = Vector3.one;
            }

            if (_image != null)
            {
                _image.color = new Color(_baseRgb.r, _baseRgb.g, _baseRgb.b, 1f);
            }
        }

        private void OnDisable()
        {
            ResetVisual();
        }

        private void Update()
        {
            if (_image == null || _rt == null)
            {
                return;
            }

            _t += Time.unscaledDeltaTime;
            var wave = (Mathf.Sin(_t * (Mathf.PI * 2f / PeriodSeconds)) + 1f) * 0.5f;
            var scale = 1f - ScaleAmplitude + wave * (ScaleAmplitude * 2f);
            _rt.localScale = new Vector3(scale, scale, 1f);
            var a = Mathf.Lerp(AlphaMin, AlphaMax, wave);
            _image.color = new Color(_baseRgb.r, _baseRgb.g, _baseRgb.b, a);
        }
    }
}
