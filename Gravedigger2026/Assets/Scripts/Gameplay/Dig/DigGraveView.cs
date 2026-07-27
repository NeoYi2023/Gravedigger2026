using UnityEngine;

namespace Gravedigger2026.Gameplay.Dig
{
    /// <summary>
    /// Dig grave presentation. Prefab authors a SpriteRenderer; runtime applies top-down layout.
    /// </summary>
    public sealed class DigGraveView : MonoBehaviour
    {
        [SerializeField] private Renderer _bodyRenderer;

        private SpriteRenderer _spriteRenderer;
        private MaterialPropertyBlock _block;
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        // Face +Y toward Dig top-down camera; Z 180 keeps art upright.
        private static readonly Quaternion TopDownSpriteLocalRotation = Quaternion.Euler(-90f, 0f, 180f);
        private const float SpriteLiftY = 0.35f;
        private const int SpriteSortingOrder = 200;

        public int InstanceId { get; private set; }

        private void Awake()
        {
            CacheSpriteRenderer();
            ApplyTopDownSpriteLayout();
        }

        public void Bind(int instanceId, string qualityId)
        {
            InstanceId = instanceId;
            gameObject.name = $"Grave_{qualityId}_{instanceId}";
            CacheSpriteRenderer();
            ApplyTopDownSpriteLayout();
        }

        public void SetIconTier(int tier)
        {
            Color color;
            switch (tier)
            {
                case 1:
                    color = new Color(0.55f, 0.75f, 0.55f, 1f);
                    break;
                case 2:
                    color = new Color(0.85f, 0.75f, 0.35f, 1f);
                    break;
                default:
                    color = new Color(0.85f, 0.35f, 0.30f, 1f);
                    break;
            }

            ApplyColor(color);
        }

        public void SetBusy(bool busy)
        {
            transform.localScale = Vector3.one * (busy ? 1.15f : 1f);
        }

        private void CacheSpriteRenderer()
        {
            if (_spriteRenderer == null)
            {
                _spriteRenderer = _bodyRenderer as SpriteRenderer;
            }

            if (_spriteRenderer == null)
            {
                _spriteRenderer = GetComponentInChildren<SpriteRenderer>(true);
            }

            if (_bodyRenderer == null)
            {
                _bodyRenderer = _spriteRenderer != null
                    ? _spriteRenderer
                    : GetComponentInChildren<Renderer>(true);
            }
        }

        private void ApplyTopDownSpriteLayout()
        {
            if (_spriteRenderer == null)
            {
                return;
            }

            // Must stay enabled — Dig presentation is the Prefab SpriteRenderer.
            _spriteRenderer.enabled = true;

            var t = _spriteRenderer.transform;
            t.localRotation = TopDownSpriteLocalRotation;
            var p = t.localPosition;
            t.localPosition = new Vector3(p.x, SpriteLiftY, p.z);
            _spriteRenderer.sortingOrder = SpriteSortingOrder;
            _spriteRenderer.flipX = false;
            _spriteRenderer.flipY = false;
        }

        private void ApplyColor(Color color)
        {
            if (_spriteRenderer != null)
            {
                _spriteRenderer.color = color;
                return;
            }

            if (_bodyRenderer == null)
            {
                return;
            }

            if (_block == null)
            {
                _block = new MaterialPropertyBlock();
            }

            _bodyRenderer.GetPropertyBlock(_block);
            _block.SetColor(ColorId, color);
            _block.SetColor(BaseColorId, color);
            _bodyRenderer.SetPropertyBlock(_block);
        }
    }
}
