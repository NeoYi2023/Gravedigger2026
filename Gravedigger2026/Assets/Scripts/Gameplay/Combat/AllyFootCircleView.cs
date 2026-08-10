using UnityEngine;

namespace Gravedigger2026.Gameplay.Combat
{
    /// <summary>
    /// Loyal soldier foot circle (SPEC_03 §3.12/§3.14, SPEC_04 §9.7): green stroke + black fill α160/255.
    /// Child of soldier root — follows movement without per-frame reposition.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class AllyFootCircleView : MonoBehaviour
    {
        public const int SortingOrder = 1;
        /// <summary>Fill alpha as Color32 A (0–255).</summary>
        public const byte FillAlphaByte = 160;
        public const float FillAlpha = FillAlphaByte / 255f;
        public const float FootOffsetY = -0.05f;
        public const float FootOffsetZ = -0.2f;
        public const float FootLocalEulerX = -30f;
        public const float StrokeThicknessMin = 0.02f;
        public const float StrokeThicknessRatio = 0.12f;

        private static readonly Color StrokeColor = new Color(0.2f, 1f, 0.25f, 1f);
        private static readonly Color FillColor = new Color(0f, 0f, 0f, FillAlpha);

        private const int DiscTextureSize = 128;

        private static Sprite s_circleSprite;

        private Transform _root;
        private SpriteRenderer _renderer;
        private float _bodyRadius = 0.1f;
        private bool _visible = true;

        public void Bind(float bodyRadius)
        {
            _bodyRadius = Mathf.Max(0.05f, bodyRadius);
            EnsureVisuals();
            ApplyScale();
            SetVisible(true);
        }

        public void SetVisible(bool visible)
        {
            _visible = visible;
            if (_root != null && _root.gameObject.activeSelf != visible)
            {
                _root.gameObject.SetActive(visible);
            }
        }

        private void EnsureVisuals()
        {
            if (_root != null)
            {
                return;
            }

            var go = new GameObject("AllyFootCircle");
            _root = go.transform;
            _root.SetParent(transform, false);
            _root.localPosition = new Vector3(0f, FootOffsetY, FootOffsetZ);
            _root.localRotation = Quaternion.Euler(FootLocalEulerX, 0f, 0f);
            _root.localScale = Vector3.one;

            _renderer = go.AddComponent<SpriteRenderer>();
            _renderer.sprite = GetOrCreateCircleSprite();
            _renderer.color = Color.white;
            _renderer.sortingOrder = SortingOrder;
        }

        private void ApplyScale()
        {
            if (_root == null)
            {
                return;
            }

            var diameter = _bodyRadius * 2f;
            _root.localScale = new Vector3(diameter, diameter, 1f);
        }

        private static Sprite GetOrCreateCircleSprite()
        {
            if (s_circleSprite != null)
            {
                return s_circleSprite;
            }

            var tex = new Texture2D(DiscTextureSize, DiscTextureSize, TextureFormat.RGBA32, false)
            {
                name = "AllyFootCircle",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };

            // Bake stroke thickness for a unit-radius disc; world scale = 2*BodyRadius.
            var center = (DiscTextureSize - 1) * 0.5f;
            var outerR = center;
            var strokeNorm = Mathf.Clamp(StrokeThicknessRatio, 0.05f, 0.35f);
            var innerR = outerR * (1f - strokeNorm);
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
            s_circleSprite.name = "AllyFootCircleSprite";
            s_circleSprite.hideFlags = HideFlags.HideAndDontSave;
            return s_circleSprite;
        }
    }
}
