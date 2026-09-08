using UnityEngine;
using UnityEngine.UI;

namespace Gravedigger2026.Gameplay.AutoManufacture
{
    /// <summary>
    /// MagicCircle_1 flash during UI-016 StepB (Overlay Image).
    /// </summary>
    [RequireComponent(typeof(RectTransform), typeof(CanvasRenderer), typeof(Image))]
    public sealed class AmMagicCircleView : MonoBehaviour
    {
        private static readonly Color BaseColor = Color.white;
        private static readonly Color FlashColor = new Color(1f, 0.2f, 0.2f, 1f);

        private Image _image;
        private float _flashHz;
        private bool _active;
        private float _phase;

        public static AmMagicCircleView Create(
            RectTransform parent,
            Sprite sprite,
            Vector2 anchoredPosition,
            float flashHz)
        {
            var go = new GameObject(
                "MagicCircle",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(AmMagicCircleView));
            go.transform.SetParent(parent, false);
            var view = go.GetComponent<AmMagicCircleView>();
            view.Configure(sprite, anchoredPosition, flashHz);
            go.transform.SetAsFirstSibling();
            return view;
        }

        public void Configure(Sprite sprite, Vector2 anchoredPosition, float flashHz)
        {
            CacheImage();
            _flashHz = Mathf.Max(0f, flashHz);
            var rt = (RectTransform)transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.localScale = Vector3.one;
            rt.localRotation = Quaternion.identity;
            _image.raycastTarget = false;
            _image.preserveAspect = true;
            _image.sprite = sprite;
            _image.color = BaseColor;
            _image.enabled = true;
            if (sprite != null)
            {
                rt.sizeDelta = new Vector2(sprite.rect.width, sprite.rect.height);
            }
            else
            {
                rt.sizeDelta = new Vector2(520f, 140f);
                _image.color = new Color(0.85f, 0.15f, 0.15f, 0.85f);
            }

            // Sit under the pile: top edge at the given floor Y.
            rt.anchoredPosition = new Vector2(
                anchoredPosition.x,
                anchoredPosition.y - rt.sizeDelta.y * 0.5f);
        }

        public void SetActiveFlash(bool active)
        {
            _active = active;
            if (!_active)
            {
                _phase = 0f;
                if (_image != null)
                {
                    _image.color = _image.sprite != null ? BaseColor : new Color(0.85f, 0.15f, 0.15f, 0.85f);
                }
            }
        }

        private void Update()
        {
            if (!_active || _flashHz <= 0f)
            {
                return;
            }

            CacheImage();
            _phase += Time.deltaTime * _flashHz * Mathf.PI * 2f;
            var t = (Mathf.Sin(_phase) + 1f) * 0.5f;
            var from = _image.sprite != null ? BaseColor : new Color(0.85f, 0.15f, 0.15f, 0.85f);
            _image.color = Color.Lerp(from, FlashColor, t);
        }

        private void CacheImage()
        {
            if (_image == null)
            {
                _image = GetComponent<Image>();
            }
        }
    }
}
