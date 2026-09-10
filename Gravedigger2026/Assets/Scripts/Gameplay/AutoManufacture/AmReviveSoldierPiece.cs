using System.Collections;
using Gravedigger2026.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Gravedigger2026.Gameplay.AutoManufacture
{
    /// <summary>
    /// UI-016 StepB revive: UnknownSoldier mystery → idle sprite → fall in canvas pixels.
    /// Foot oval shadow is a child Image (SPEC_03 §3.15 / AutoMfgSoldierShadow*).
    /// Bottom pivot: feet sit on LandY; art fit max-edge like body pieces.
    /// </summary>
    [RequireComponent(typeof(RectTransform), typeof(CanvasRenderer), typeof(Image))]
    public sealed class AmReviveSoldierPiece : MonoBehaviour
    {
        public const float DefaultSizePx = 64f;
        public const float DefaultVisualScale = 8f;
        private const float MorphHoldSeconds = 0.15f;
        private const float LandSpeedPx = 40f;
        private const int DiscTextureSize = 128;

        private static Sprite s_discSprite;

        private RectTransform _rt;
        private Image _image;
        private RectTransform _shadowRt;
        private Image _shadowImage;
        private Vector2 _velocity;
        private float _landY;
        private float _gravityPx;
        private float _sizePx = DefaultSizePx;
        private float _visualScale = DefaultVisualScale;
        private float _shadowWidthPx = 20f;
        private float _shadowHeightPx = 8f;
        private float _shadowAlpha = 0.45f;
        private float _shadowOffsetYPx = -32f;
        private bool _landed;
        private bool _falling;
        private Coroutine _morphRoutine;

        public bool IsLanded => _landed;

        public static AmReviveSoldierPiece EnsurePrefab()
        {
            var prefab = Resources.Load<GameObject>("Prefabs/AutoManufacture/AmReviveSoldierPiece");
            if (prefab != null)
            {
                var fromPrefab = prefab.GetComponent<AmReviveSoldierPiece>();
                if (fromPrefab != null && prefab.GetComponent<Image>() != null)
                {
                    return fromPrefab;
                }
            }

            var go = new GameObject(
                "AmReviveSoldierPiece",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(AmReviveSoldierPiece));
            var piece = go.GetComponent<AmReviveSoldierPiece>();
            piece.ConfigureRuntime();
            return piece;
        }

        public void ConfigureRuntime()
        {
            CacheComponents();
            _image.raycastTarget = false;
            _image.preserveAspect = true;
            _image.color = Color.white;
            _rt.anchorMin = _rt.anchorMax = new Vector2(0.5f, 0.5f);
            _rt.pivot = new Vector2(0.5f, 0f);
            EnsureShadow();
        }

        public void SpawnMystery(
            Vector2 anchoredPosition,
            float landY,
            float sizePx,
            float visualScale,
            float shadowWidthPx,
            float shadowHeightPx,
            float shadowAlpha,
            float shadowOffsetYPx)
        {
            CacheComponents();
            EnsureShadow();
            _landY = landY;
            _sizePx = sizePx > 0.01f ? sizePx : DefaultSizePx;
            _visualScale = visualScale > 0.01f ? visualScale : DefaultVisualScale;
            _shadowWidthPx = Mathf.Max(1f, shadowWidthPx);
            _shadowHeightPx = Mathf.Max(1f, shadowHeightPx);
            _shadowAlpha = Mathf.Clamp01(shadowAlpha);
            _shadowOffsetYPx = shadowOffsetYPx;
            _landed = false;
            _falling = false;
            _velocity = Vector2.zero;
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            _rt.pivot = new Vector2(0.5f, 0f);
            _rt.anchoredPosition = anchoredPosition;
            _rt.localRotation = Quaternion.identity;
            _rt.localScale = Vector3.one * _visualScale;
            var mystery = DigBodyArtLoader.LoadUnknownSoldier();
            _image.sprite = mystery;
            _image.color = mystery != null ? Color.white : new Color(0.9f, 0.9f, 0.95f, 1f);
            _image.enabled = true;
            ApplyFixedSize();
            RefreshShadow();
        }

        public void BeginMorph(Sprite idleSprite, float gravityPx)
        {
            if (_morphRoutine != null)
            {
                StopCoroutine(_morphRoutine);
            }

            _gravityPx = Mathf.Max(200f, gravityPx);
            _morphRoutine = StartCoroutine(CoMorph(idleSprite));
        }

        public void TickFall(float dt)
        {
            if (_landed || !_falling || _rt == null)
            {
                return;
            }

            _velocity.y -= _gravityPx * dt;
            _rt.anchoredPosition += _velocity * dt;

            // Bottom pivot: anchoredPosition.y is the feet.
            var pos = _rt.anchoredPosition;
            if (pos.y <= _landY && _velocity.y <= LandSpeedPx)
            {
                pos.y = _landY;
                _rt.anchoredPosition = pos;
                _velocity = Vector2.zero;
                _landed = true;
                _falling = false;
                RefreshShadow();
            }
        }

        public void Cleanup()
        {
            if (_morphRoutine != null)
            {
                StopCoroutine(_morphRoutine);
                _morphRoutine = null;
            }

            gameObject.SetActive(false);
        }

        private IEnumerator CoMorph(Sprite idleSprite)
        {
            yield return new WaitForSeconds(MorphHoldSeconds);
            if (idleSprite != null)
            {
                _image.sprite = idleSprite;
                _image.color = Color.white;
                ApplyFixedSize();
                RefreshShadow();
            }

            _falling = true;
            _morphRoutine = null;
        }

        private void ApplyFixedSize()
        {
            _rt.sizeDelta = FitSize(_image != null ? _image.sprite : null, _sizePx);
            _rt.localScale = Vector3.one * _visualScale;
            _rt.pivot = new Vector2(0.5f, 0f);
        }

        private static Vector2 FitSize(Sprite sprite, float maxEdgePx)
        {
            return AmBodyPartPiece.FitSize(sprite, maxEdgePx);
        }

        private void EnsureShadow()
        {
            if (_shadowRt != null && _shadowImage != null)
            {
                return;
            }

            var existing = transform.Find("Shadow");
            if (existing != null)
            {
                _shadowRt = existing as RectTransform;
                _shadowImage = existing.GetComponent<Image>();
            }

            if (_shadowRt == null)
            {
                var go = new GameObject(
                    "Shadow",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image));
                _shadowRt = go.GetComponent<RectTransform>();
                _shadowRt.SetParent(transform, false);
                _shadowImage = go.GetComponent<Image>();
            }

            _shadowRt.SetAsFirstSibling();
            _shadowRt.anchorMin = _shadowRt.anchorMax = new Vector2(0.5f, 0.5f);
            _shadowRt.pivot = new Vector2(0.5f, 0.5f);
            _shadowRt.localRotation = Quaternion.identity;
            _shadowRt.localScale = Vector3.one;
            _shadowImage.raycastTarget = false;
            _shadowImage.preserveAspect = false;
            _shadowImage.sprite = GetOrCreateDiscSprite();
            _shadowImage.type = Image.Type.Simple;
            _shadowImage.color = new Color(0f, 0f, 0f, _shadowAlpha);
            _shadowImage.enabled = true;
        }

        private void RefreshShadow()
        {
            EnsureShadow();
            if (_shadowRt == null || _rt == null)
            {
                return;
            }

            _shadowRt.sizeDelta = new Vector2(_shadowWidthPx, _shadowHeightPx);
            _shadowRt.anchoredPosition = new Vector2(0f, _shadowOffsetYPx);
            _shadowImage.color = new Color(0f, 0f, 0f, _shadowAlpha);
            _shadowImage.enabled = true;
        }

        private static Sprite GetOrCreateDiscSprite()
        {
            if (s_discSprite != null)
            {
                return s_discSprite;
            }

            var tex = new Texture2D(DiscTextureSize, DiscTextureSize, TextureFormat.RGBA32, false)
            {
                name = "AmReviveSoldierFootShadow",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };

            var center = (DiscTextureSize - 1) * 0.5f;
            var outerR = center;
            var outerRSqr = outerR * outerR;
            var fill = new Color32(255, 255, 255, 255);
            var clear = new Color32(0, 0, 0, 0);
            var pixels = new Color32[DiscTextureSize * DiscTextureSize];
            for (var y = 0; y < DiscTextureSize; y++)
            {
                for (var x = 0; x < DiscTextureSize; x++)
                {
                    var dx = x - center;
                    var dy = y - center;
                    pixels[y * DiscTextureSize + x] = dx * dx + dy * dy <= outerRSqr ? fill : clear;
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply(false, true);

            s_discSprite = Sprite.Create(
                tex,
                new Rect(0f, 0f, DiscTextureSize, DiscTextureSize),
                new Vector2(0.5f, 0.5f),
                DiscTextureSize);
            s_discSprite.name = "AmReviveSoldierFootShadowSprite";
            s_discSprite.hideFlags = HideFlags.HideAndDontSave;
            return s_discSprite;
        }

        private void CacheComponents()
        {
            if (_rt == null)
            {
                _rt = GetComponent<RectTransform>();
            }

            if (_image == null)
            {
                _image = GetComponent<Image>();
            }
        }
    }
}
