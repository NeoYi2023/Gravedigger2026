using System.Collections;
using Gravedigger2026.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Gravedigger2026.Gameplay.AutoManufacture
{
    /// <summary>
    /// UI-016 StepB revive: UnknownSoldier rises from MagicCircle with book pulses,
    /// then morphs Idle in place at LandY (SPEC_03 §3.15 Approach B).
    /// Foot oval shadow child Image exists; Demo hides when AutoMfgSoldierShadowAlpha≤0.
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
        private float _shadowAlpha = 0f;
        private float _shadowOffsetYPx = -32f;
        private bool _landed;
        private bool _falling;
        private Color _baseColor = Color.white;
        private Coroutine _morphRoutine;
        private Coroutine _riseRoutine;
        private Coroutine _flashRoutine;
        public bool IsLanded => _landed;

        public void SetAnchoredX(float x)
        {
            CacheComponents();
            if (_rt == null)
            {
                return;
            }
            var pos = _rt.anchoredPosition;
            pos.x = x;
            _rt.anchoredPosition = pos;
        }

        public float AnchoredX
        {
            get
            {
                CacheComponents();
                return _rt != null ? _rt.anchoredPosition.x : 0f;
            }
        }

        public float AnchoredY
        {
            get
            {
                CacheComponents();
                return _rt != null ? _rt.anchoredPosition.y : 0f;
            }
        }

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
            StopRise();
            StopFlash();
            if (_morphRoutine != null)
            {
                StopCoroutine(_morphRoutine);
                _morphRoutine = null;
            }
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
            _baseColor = mystery != null ? Color.white : new Color(0.9f, 0.9f, 0.95f, 1f);
            _image.color = _baseColor;
            _image.enabled = true;
            ApplyFixedSize();
            RefreshShadow();
        }

        /// <summary>Smoothly rise feet Y from→to over duration (one book-slot beat).</summary>
        public void BeginRiseStep(float fromY, float toY, float duration)
        {
            CacheComponents();
            StopRise();
            if (_rt == null)
            {
                return;
            }
            _riseRoutine = StartCoroutine(CoRiseStep(fromY, toY, duration));
        }

        /// <summary>Brief bright flash on mystery; next call interrupts and restarts.</summary>
        public void FlashMystery(float duration)
        {
            CacheComponents();
            StopFlash();
            if (_image == null)
            {
                return;
            }
            _flashRoutine = StartCoroutine(CoFlashMystery(Mathf.Max(0.01f, duration)));
        }

        /// <summary>Morph mystery → Idle in place; marks landed (no gravity fall).</summary>
        public void BeginMorphInPlace(Sprite idleSprite, float holdSeconds = MorphHoldSeconds)
        {
            if (_morphRoutine != null)
            {
                StopCoroutine(_morphRoutine);
            }
            _falling = false;
            _morphRoutine = StartCoroutine(CoMorphInPlace(idleSprite, Mathf.Max(0.01f, holdSeconds)));
        }

        /// <summary>Legacy path: morph then gravity fall. Prefer BeginMorphInPlace for Approach B.</summary>
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
            StopRise();
            StopFlash();
            if (_morphRoutine != null)
            {
                StopCoroutine(_morphRoutine);
                _morphRoutine = null;
            }
            gameObject.SetActive(false);
        }

        private IEnumerator CoRiseStep(float fromY, float toY, float duration)
        {
            var pos = _rt.anchoredPosition;
            pos.y = fromY;
            _rt.anchoredPosition = pos;
            var dur = Mathf.Max(0.01f, duration);
            var t = 0f;
            while (t < dur)
            {
                t += Time.deltaTime;
                var u = Mathf.Clamp01(t / dur);
                pos = _rt.anchoredPosition;
                pos.y = Mathf.Lerp(fromY, toY, u);
                _rt.anchoredPosition = pos;
                yield return null;
            }
            pos = _rt.anchoredPosition;
            pos.y = toY;
            _rt.anchoredPosition = pos;
            _riseRoutine = null;
        }

        private IEnumerator CoFlashMystery(float duration)
        {
            // Warm flash so UnknownSoldier (already near-white) remains readable.
            var flashColor = new Color(1f, 0.92f, 0.45f, 1f);
            var half = duration * 0.5f;
            var t = 0f;
            while (t < half)
            {
                t += Time.deltaTime;
                var u = Mathf.Clamp01(t / half);
                _image.color = Color.Lerp(_baseColor, flashColor, u);
                yield return null;
            }
            t = 0f;
            while (t < half)
            {
                t += Time.deltaTime;
                var u = Mathf.Clamp01(t / half);
                _image.color = Color.Lerp(flashColor, _baseColor, u);
                yield return null;
            }
            _image.color = _baseColor;
            _flashRoutine = null;
        }

        private IEnumerator CoMorphInPlace(Sprite idleSprite, float holdSeconds)
        {
            yield return new WaitForSeconds(holdSeconds);
            if (idleSprite != null)
            {
                _image.sprite = idleSprite;
                _baseColor = Color.white;
                _image.color = _baseColor;
                ApplyFixedSize();
                RefreshShadow();
            }
            var pos = _rt.anchoredPosition;
            pos.y = _landY;
            _rt.anchoredPosition = pos;
            _falling = false;
            _landed = true;
            RefreshShadow();
            _morphRoutine = null;
        }

        private IEnumerator CoMorph(Sprite idleSprite)
        {
            yield return new WaitForSeconds(MorphHoldSeconds);
            if (idleSprite != null)
            {
                _image.sprite = idleSprite;
                _baseColor = Color.white;
                _image.color = _baseColor;
                ApplyFixedSize();
                RefreshShadow();
            }
            _falling = true;
            _morphRoutine = null;
        }

        private void StopRise()
        {
            if (_riseRoutine != null)
            {
                StopCoroutine(_riseRoutine);
                _riseRoutine = null;
            }
        }

        private void StopFlash()
        {
            if (_flashRoutine != null)
            {
                StopCoroutine(_flashRoutine);
                _flashRoutine = null;
            }
            if (_image != null)
            {
                _image.color = _baseColor;
            }
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
            _shadowImage.enabled = _shadowAlpha > 0f;
        }

        private void RefreshShadow()
        {
            EnsureShadow();
            if (_shadowRt == null || _shadowImage == null || _rt == null)
            {
                return;
            }
            if (_shadowAlpha <= 0f)
            {
                _shadowImage.enabled = false;
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
