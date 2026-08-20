using UnityEngine;

namespace Gravedigger2026.Gameplay.Dig
{
    /// <summary>
    /// Ground-aligned red translucent blast disc (D-077). Tilt matches AllyFootCircle / isometric floor.
    /// </summary>
    public sealed class DigExplosionRadiusView : MonoBehaviour
    {
        public const int SortingOrder = 40;
        public const float FillAlpha = 0.4f;
        public const float FootOffsetY = 0.02f;
        public const float FootLocalEulerX = -30f;

        private static readonly Color StrokeColor = new Color(1f, 0.15f, 0.12f, 0.9f);
        private static readonly Color FillColor = new Color(1f, 0.12f, 0.1f, FillAlpha);
        private const int DiscTextureSize = 128;
        private const float StrokeThicknessRatio = 0.1f;

        private static Sprite s_circleSprite;

        private float _remaining;
        private bool _playing;
        private SpriteRenderer _renderer;

        public void Play(Vector3 worldPosition, float radius, float duration)
        {
            transform.position = worldPosition;
            EnsureVisual();
            var diameter = Mathf.Max(0.1f, radius * 1f);
            _renderer.transform.localScale = new Vector3(diameter, diameter, 1f);
            _remaining = Mathf.Max(0.01f, duration);
            _playing = true;
            gameObject.SetActive(true);
        }

        private void Update()
        {
            if (!_playing)
            {
                return;
            }

            _remaining -= Time.deltaTime;
            if (_remaining > 0f)
            {
                return;
            }

            _playing = false;
            Destroy(gameObject);
        }

        private void EnsureVisual()
        {
            if (_renderer != null)
            {
                return;
            }

            var go = new GameObject("Disc");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(0f, FootOffsetY, 0f);
            go.transform.localRotation = Quaternion.Euler(FootLocalEulerX, 0f, 0f);
            _renderer = go.AddComponent<SpriteRenderer>();
            _renderer.sprite = GetOrCreateCircleSprite();
            _renderer.color = Color.white;
            _renderer.sortingOrder = SortingOrder;
        }

        private static Sprite GetOrCreateCircleSprite()
        {
            if (s_circleSprite != null)
            {
                return s_circleSprite;
            }

            var tex = new Texture2D(DiscTextureSize, DiscTextureSize, TextureFormat.RGBA32, false)
            {
                name = "DigExplosionRadius",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };

            var center = (DiscTextureSize - 1) * 0.5f;
            var outerR = center;
            var innerR = outerR * (1f - StrokeThicknessRatio);
            var outerRSqr = outerR * outerR;
            var innerRSqr = innerR * innerR;
            var stroke = (Color32)StrokeColor;
            var fill = (Color32)FillColor;
            var clear = new Color32(0, 0, 0, 0);
            var pixels = new Color32[DiscTextureSize * DiscTextureSize];
            for (var y = 0; y < DiscTextureSize; y++)
            {
                for (var x = 0; x < DiscTextureSize; x++)
                {
                    var dx = x - center;
                    var dy = y - center;
                    var dSqr = dx * dx + dy * dy;
                    if (dSqr > outerRSqr)
                    {
                        pixels[y * DiscTextureSize + x] = clear;
                    }
                    else if (dSqr >= innerRSqr)
                    {
                        pixels[y * DiscTextureSize + x] = stroke;
                    }
                    else
                    {
                        pixels[y * DiscTextureSize + x] = fill;
                    }
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply(false, true);
            s_circleSprite = Sprite.Create(
                tex,
                new Rect(0f, 0f, DiscTextureSize, DiscTextureSize),
                new Vector2(0.5f, 0.5f),
                DiscTextureSize);
            s_circleSprite.name = "DigExplosionRadiusSprite";
            s_circleSprite.hideFlags = HideFlags.HideAndDontSave;
            return s_circleSprite;
        }
    }
}
