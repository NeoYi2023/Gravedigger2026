using Gravedigger2026.Core.Coc;
using Gravedigger2026.Gameplay.Defend;
using UnityEngine;
using UnityEngine.AI;

namespace Gravedigger2026.Gameplay.Coc
{
    /// <summary>
    /// COC map-authored destructible wall (SPEC_03 §3.21 / D-100 slice 02 Approach A).
    /// Not a monster-table row. Carves a footprint box; death swaps to a wreck sprite.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CocDestructibleObstacle : MonoBehaviour
    {
        public const float DefaultFootprintWorldSize = 1f;
        public const float DefaultMaxHp = 5f;
        public const float DefaultTargetValue = 1f;
        public const int BrokenSortingOrder = 1;
        private const float MinFootprintWorldSize = 0.05f;
        private const int HpBarSortingOrder = 220;
        private const float HpBarWidth = 0.55f;
        private const float HpBarHeight = 0.07f;
        private const float HpBarLocalY = 0.55f;

        private static Sprite _pixel;

        [SerializeField] private float _maxHp = DefaultMaxHp;
        [SerializeField] private float _targetValue = DefaultTargetValue;
        [SerializeField]
        [Tooltip("World-unit footprint. X/Z block the ground; Y is carve height and the gizmo only.")]
        private Vector3 _footprintSize = Vector3.one;
        [SerializeField]
        [Tooltip("Sprite shown when HP reaches 0. Sample: Wall E18_S.")]
        private Sprite _brokenSprite;
        [SerializeField] private int _brokenSortingOrder = BrokenSortingOrder;
        /// <summary>
        /// Sprite root. When this component lives on child <c>Visual</c>, assign the
        /// CocDestructible parent. Empty uses the parent that owns the SpriteRenderer.
        /// </summary>
        [SerializeField] private Transform _sinkRoot;

        private NavMeshObstacle _carve;
        private MeshRenderer _renderer;
        private bool _destroyed;
        private string _runtimeId;
        private Transform _hpBarRoot;
        private Transform _hpFill;
        private SpriteRenderer _hpFillRenderer;

        private Transform Presentation
        {
            get
            {
                if (_sinkRoot != null)
                {
                    return _sinkRoot;
                }

                var parent = transform.parent;
                if (parent != null && parent.GetComponent<SpriteRenderer>() != null)
                {
                    return parent;
                }

                return transform;
            }
        }

        public string RuntimeId =>
            string.IsNullOrEmpty(_runtimeId) ? name : _runtimeId;

        public float MaxHp => Mathf.Max(1f, _maxHp);

        public float TargetValue => Mathf.Max(0f, _targetValue);

        public Vector3 FootprintSize => ClampFootprint(_footprintSize);

        /// <summary>Largest XZ half-extent so a long face still counts as in melee range.</summary>
        public float BodyRadius => Mathf.Max(FootprintSize.x, FootprintSize.z) * 0.5f;

        public bool IsAlive => !_destroyed;

        public void BindRuntimeId(string runtimeId)
        {
            _runtimeId = string.IsNullOrWhiteSpace(runtimeId) ? name : runtimeId.Trim();
        }

        public void NotifyDamaged(float remaining, float maxHp)
        {
            if (_destroyed)
            {
                return;
            }

            if (remaining <= 0f || maxHp <= 0f)
            {
                SetHpBarVisible(false);
                return;
            }

            EnsureHpBar();
            var ratio = Mathf.Clamp01(remaining / maxHp);
            if (_hpFill != null)
            {
                var scale = _hpFill.localScale;
                scale.x = Mathf.Max(0.01f, HpBarWidth * ratio);
                _hpFill.localScale = scale;
            }

            if (_hpFillRenderer != null)
            {
                _hpFillRenderer.color = Color.Lerp(new Color(0.75f, 0.15f, 0.12f, 1f), new Color(0.25f, 0.75f, 0.22f, 1f), ratio);
            }

            SetHpBarVisible(true);
        }

        private void Awake()
        {
            EnsureCarve();
            _renderer = GetComponent<MeshRenderer>();
            if (_renderer != null && _renderer.material != null)
            {
                _renderer.material.color = new Color(0.62f, 0.38f, 0.18f, 1f);
            }
        }

        private void OnValidate()
        {
            _footprintSize = ClampFootprint(_footprintSize);
            if (_brokenSortingOrder < 0)
            {
                _brokenSortingOrder = 0;
            }

            if (_carve != null)
            {
                ApplyCarveSize();
            }
        }

        private static Vector3 ClampFootprint(Vector3 size)
        {
            return new Vector3(
                Mathf.Max(MinFootprintWorldSize, size.x),
                Mathf.Max(MinFootprintWorldSize, size.y),
                Mathf.Max(MinFootprintWorldSize, size.z));
        }

        private void EnsureCarve()
        {
            if (_carve == null)
            {
                _carve = GetComponent<NavMeshObstacle>();
                if (_carve == null)
                {
                    _carve = gameObject.AddComponent<NavMeshObstacle>();
                }
            }

            _carve.shape = NavMeshObstacleShape.Box;
            _carve.center = Vector3.zero;
            ApplyCarveSize();
            _carve.carving = true;
            _carve.carveOnlyStationary = true;
            _carve.enabled = true;
        }

        private void ApplyCarveSize()
        {
            // Project Humanoid agent radius is 0.5, which inflates the carved hole
            // past the footprint so melee never reaches AttackRange. COC bakes with
            // CocCarveAgentRadius; shrink the source box by that radius per side.
            var inflate = DefendNavMeshBaker.CocCarveAgentRadius * 2f;
            var size = FootprintSize;
            _carve.size = new Vector3(
                Mathf.Max(0.02f, size.x - inflate),
                Mathf.Max(0.02f, size.y - inflate),
                Mathf.Max(0.02f, size.z - inflate));
        }

        public bool ContainsXZ(Vector3 worldPosition)
        {
            var local = Quaternion.Inverse(transform.rotation) * (worldPosition - transform.position);
            var size = FootprintSize;
            return Mathf.Abs(local.x) <= size.x * 0.5f && Mathf.Abs(local.z) <= size.z * 0.5f;
        }

        /// <summary>
        /// Ground XZ segment vs this wall's footprint rectangle (same local box as <see cref="ContainsXZ"/>).
        /// </summary>
        public bool SegmentCrossesXZ(Vector3 fromWorld, Vector3 toWorld, out float tEnter)
        {
            tEnter = 0f;
            if (_destroyed)
            {
                return false;
            }

            var y = transform.position.y;
            var from = new Vector3(fromWorld.x, y, fromWorld.z);
            var to = new Vector3(toWorld.x, y, toWorld.z);
            var inv = Quaternion.Inverse(transform.rotation);
            var localA = inv * (from - transform.position);
            var localB = inv * (to - transform.position);
            var size = FootprintSize;
            return CocMonsterObstaclePath.TrySegmentHitRect(
                new Vector2(localA.x, localA.z),
                new Vector2(localB.x, localB.z),
                size.x * 0.5f,
                size.z * 0.5f,
                out tEnter);
        }

        /// <summary>Air wall off, hide the bar, swap the authored wreck sprite.</summary>
        public void NotifyDestroyed()
        {
            if (_destroyed)
            {
                return;
            }

            _destroyed = true;
            if (_carve != null)
            {
                _carve.carving = false;
                _carve.enabled = false;
            }

            SetHpBarVisible(false);
            var spriteRenderer = Presentation.GetComponent<SpriteRenderer>();
            if (spriteRenderer == null || _brokenSprite == null)
            {
                return;
            }

            spriteRenderer.sprite = _brokenSprite;
            spriteRenderer.sortingOrder = _brokenSortingOrder;
        }

        private void EnsureHpBar()
        {
            if (_hpBarRoot != null)
            {
                return;
            }

            var root = Presentation;
            var bar = new GameObject("HpBar");
            bar.transform.SetParent(root, false);
            bar.transform.localPosition = new Vector3(0f, HpBarLocalY, 0f);
            bar.transform.localRotation = Quaternion.identity;
            bar.transform.localScale = Vector3.one;
            _hpBarRoot = bar.transform;

            CreateBarSprite(bar.transform, "Bg", HpBarWidth, HpBarHeight, new Color(0.08f, 0.08f, 0.08f, 0.9f), new Vector2(0.5f, 0.5f), 0);
            var fill = CreateBarSprite(
                bar.transform,
                "Fill",
                1f,
                HpBarHeight * 0.62f,
                new Color(0.25f, 0.75f, 0.22f, 1f),
                new Vector2(0f, 0.5f),
                1);
            fill.transform.localPosition = new Vector3(-HpBarWidth * 0.5f, 0f, -0.01f);
            fill.transform.localScale = new Vector3(HpBarWidth, HpBarHeight * 0.62f, 1f);
            _hpFill = fill.transform;
            _hpFillRenderer = fill;
            bar.SetActive(false);
        }

        private static SpriteRenderer CreateBarSprite(
            Transform parent,
            string name,
            float width,
            float height,
            Color color,
            Vector2 pivot,
            int orderOffset)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = new Vector3(width, height, 1f);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = Pixel(pivot);
            renderer.color = color;
            renderer.sortingOrder = HpBarSortingOrder + orderOffset;
            return renderer;
        }

        private static Sprite Pixel(Vector2 pivot)
        {
            if (_pixel == null)
            {
                var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
                tex.SetPixel(0, 0, Color.white);
                tex.Apply();
                tex.hideFlags = HideFlags.HideAndDontSave;
                _pixel = Sprite.Create(tex, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
                _pixel.hideFlags = HideFlags.HideAndDontSave;
            }

            if (pivot == new Vector2(0.5f, 0.5f))
            {
                return _pixel;
            }

            var left = Sprite.Create(_pixel.texture, new Rect(0f, 0f, 1f, 1f), pivot, 1f);
            left.hideFlags = HideFlags.HideAndDontSave;
            return left;
        }

        private void SetHpBarVisible(bool visible)
        {
            if (_hpBarRoot != null)
            {
                _hpBarRoot.gameObject.SetActive(visible);
            }
        }

        private void OnDrawGizmos()
        {
            var prev = Gizmos.matrix;
            Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);
            Gizmos.color = new Color(0.72f, 0.42f, 0.18f, 0.95f);
            Gizmos.DrawWireCube(Vector3.zero, FootprintSize);
            Gizmos.matrix = prev;
        }
    }
}
