using Gravedigger2026.Gameplay.Dig;
using UnityEngine;

namespace Gravedigger2026.Gameplay.Maps
{
    /// <summary>
    /// World-space map edge fog on map Prefabs (SPEC_04 §13 MapEdgeFog / ME-01).
    /// Covers blank outside IsoDiamond; static — no Update. Distinct from CameraFogOverlay.
    /// Transform is authoring-owned unless Auto Fit To Bounds is enabled.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MapEdgeFogView : MonoBehaviour
    {
        public const string ChildName = "MapEdgeFog";
        public const int DefaultSortingOrder = 10;

        [SerializeField] private Sprite _fogSprite;
        [SerializeField] private Color _color = Color.white;
        [SerializeField] private float _sizeMul = 2.4f;
        [SerializeField] private float _heightY = 0.02f;
        [SerializeField] private int _sortingOrder = DefaultSortingOrder;
        [SerializeField] private DigMapBounds _bounds;

        [Tooltip("When on, Play/OnEnable resets position/rotation/scale from DigMapBounds. Leave off after manual placement.")]
        [SerializeField] private bool _autoFitToBounds;

        private SpriteRenderer _renderer;
        private static Material s_spritesDefault;

        public Sprite FogSprite => _fogSprite;
        public Color Color => _color;
        public float SizeMul => _sizeMul;
        public int SortingOrder => _sortingOrder;
        public bool AutoFitToBounds => _autoFitToBounds;

        private void OnEnable()
        {
            ApplyVisuals();
            if (_autoFitToBounds)
            {
                FitToBounds();
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            ApplyVisuals();
            if (_autoFitToBounds)
            {
                FitToBounds();
            }
        }
#endif

        public void Configure(
            Sprite fogSprite,
            Color color,
            float sizeMul,
            float heightY,
            int sortingOrder,
            DigMapBounds bounds,
            bool fitToBounds = true,
            bool keepAutoFit = false)
        {
            _fogSprite = fogSprite;
            _color = color;
            _sizeMul = Mathf.Max(0.1f, sizeMul);
            _heightY = heightY;
            _sortingOrder = sortingOrder;
            _bounds = bounds;
            _autoFitToBounds = keepAutoFit;
            ApplyVisuals();
            if (fitToBounds)
            {
                FitToBounds();
            }
        }

        public void SetAutoFitToBounds(bool enabled)
        {
            _autoFitToBounds = enabled;
        }

        /// <summary>Sprite / color / sorting / material only — does not move transform.</summary>
        public void ApplyVisuals()
        {
            EnsureRenderer();
            if (_renderer == null)
            {
                return;
            }

            _renderer.sprite = _fogSprite;
            _renderer.color = _color;
            _renderer.sortingOrder = _sortingOrder;
            EnsureDefaultSpriteMaterial(_renderer);
        }

        /// <summary>Place / rotate / scale from DigMapBounds (and Size Mul / Height Y).</summary>
        public void FitToBounds()
        {
            EnsureRenderer();

            var half = ResolveHalfExtents();
            var center = _bounds != null
                ? _bounds.Center
                : transform.parent != null
                    ? transform.parent.position
                    : transform.position;

            transform.position = new Vector3(center.x, center.y + _heightY, center.z);
            transform.rotation = Quaternion.Euler(90f, 0f, 0f);

            if (_fogSprite == null)
            {
                transform.localScale = Vector3.one;
                return;
            }

            var spriteSize = _fogSprite.bounds.size;
            var sx = spriteSize.x > 0.001f ? spriteSize.x : 1f;
            var sy = spriteSize.y > 0.001f ? spriteSize.y : 1f;
            var targetW = half.x * 2f * Mathf.Max(0.1f, _sizeMul);
            var targetH = half.y * 2f * Mathf.Max(0.1f, _sizeMul);
            // RotX 90°: local X → world X, local Y → world −Z
            transform.localScale = new Vector3(targetW / sx, targetH / sy, 1f);
        }

        private Vector2 ResolveHalfExtents()
        {
            if (_bounds == null && transform.parent != null)
            {
                _bounds = transform.parent.GetComponent<DigMapBounds>();
            }

            if (_bounds != null)
            {
                return _bounds.HalfExtents;
            }

            return MapFootprintMath.HalfExtentsFromIsoCell(5, MapFootprintMath.DemoIsoCellSize);
        }

        private void EnsureRenderer()
        {
            if (_renderer != null)
            {
                return;
            }

            _renderer = GetComponent<SpriteRenderer>();
            if (_renderer == null)
            {
                _renderer = gameObject.AddComponent<SpriteRenderer>();
            }
        }

        /// <summary>
        /// Null material serializes as missing and renders magenta; use built-in Sprites/Default.
        /// </summary>
        private static void EnsureDefaultSpriteMaterial(SpriteRenderer renderer)
        {
            if (renderer == null)
            {
                return;
            }

            if (renderer.sharedMaterial != null && renderer.sharedMaterial.shader != null)
            {
                return;
            }

            if (s_spritesDefault == null)
            {
                var shader = Shader.Find("Sprites/Default");
                if (shader == null)
                {
                    return;
                }

                s_spritesDefault = new Material(shader)
                {
                    name = "Sprites-Default (MapEdgeFog)",
                    hideFlags = HideFlags.HideAndDontSave
                };
            }

            renderer.sharedMaterial = s_spritesDefault;
        }
    }
}
