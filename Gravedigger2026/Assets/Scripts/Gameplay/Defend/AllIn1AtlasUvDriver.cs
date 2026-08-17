using UnityEngine;

namespace Gravedigger2026.Gameplay.Defend
{
    /// <summary>
    /// Feeds AllIn1 ATLAS_ON UV rects and binds the live sprite texture via
    /// MaterialPropertyBlock (never mutates sharedMaterial). Binding _MainTex is
    /// required so _MainTex_TexelSize matches the spritesheet — an empty material
    /// MainTex yields 1×1 texel size and blows outline sampling into a full-cell
    /// fill (SPEC_04 §15.2). No ExecuteInEditMode.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class AllIn1AtlasUvDriver : MonoBehaviour
    {
        private static readonly int MainTexId = Shader.PropertyToID("_MainTex");
        private static readonly int MinXUvId = Shader.PropertyToID("_MinXUV");
        private static readonly int MaxXUvId = Shader.PropertyToID("_MaxXUV");
        private static readonly int MinYUvId = Shader.PropertyToID("_MinYUV");
        private static readonly int MaxYUvId = Shader.PropertyToID("_MaxYUV");

        private SpriteRenderer _spriteRenderer;
        private MaterialPropertyBlock _block;
        private Sprite _lastSprite;
        private Texture2D _lastTexture;

        private void Awake()
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
            _block = new MaterialPropertyBlock();
        }

        private void OnEnable()
        {
            if (_spriteRenderer == null)
            {
                _spriteRenderer = GetComponent<SpriteRenderer>();
            }

            _block ??= new MaterialPropertyBlock();
            _lastSprite = null;
            _lastTexture = null;
            Apply();
        }

        private void OnWillRenderObject()
        {
            Apply();
        }

        private void Apply()
        {
            if (_spriteRenderer == null)
            {
                return;
            }

            var sprite = _spriteRenderer.sprite;
            if (sprite == null)
            {
                return;
            }

            var texture = sprite.texture;
            if (texture == null)
            {
                return;
            }

            if (sprite == _lastSprite && texture == _lastTexture)
            {
                return;
            }

            _lastSprite = sprite;
            _lastTexture = texture;

            var r = sprite.textureRect;
            var tw = texture.width;
            var th = texture.height;
            if (tw <= 0 || th <= 0)
            {
                return;
            }

            _spriteRenderer.GetPropertyBlock(_block);
            _block.SetTexture(MainTexId, texture);
            _block.SetFloat(MinXUvId, r.xMin / tw);
            _block.SetFloat(MaxXUvId, r.xMax / tw);
            _block.SetFloat(MinYUvId, r.yMin / th);
            _block.SetFloat(MaxYUvId, r.yMax / th);
            _spriteRenderer.SetPropertyBlock(_block);
        }
    }
}
