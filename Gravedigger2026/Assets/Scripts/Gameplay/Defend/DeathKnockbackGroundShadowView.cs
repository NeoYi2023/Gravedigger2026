using Gravedigger2026.Core.Config;
using UnityEngine;

namespace Gravedigger2026.Gameplay.Defend
{
    /// <summary>
    /// Ground-aligned black disc shadow during monster corpse parabolic knockback (SPEC_04 §15.5).
    /// Stays on ground XZ; scale grows with height above y0.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DeathKnockbackGroundShadowView : MonoBehaviour
    {
        public const int SortingOrder = 45;
        public const float GroundOffsetY = 0.02f;
        public const float DiscLocalEulerX = -45f;
        private const int DiscTextureSize = 128;

        private static Sprite s_discSprite;

        private Transform _discRoot;
        private SpriteRenderer _renderer;
        private bool _visible;

        public void Show()
        {
            EnsureVisuals();
            _visible = true;
        }

        public void Hide()
        {
            _visible = false;
            if (_discRoot != null)
            {
                _discRoot.gameObject.SetActive(false);
                _discRoot.localPosition = Vector3.zero;
            }
        }

        public void UpdateGroundShadow(
            float groundY,
            Vector3 corpsePos,
            float bodyRadius,
            float heightAboveGround)
        {
            if (!_visible)
            {
                return;
            }

            EnsureVisuals();

            var scaleMul = MonsterDeathPresentation.ComputeShadowScaleMul(heightAboveGround);
            if (scaleMul <= 0f)
            {
                // Keep _visible true during knockback; height=0 at arc start/end is expected.
                _discRoot.gameObject.SetActive(false);
                return;
            }

            _discRoot.gameObject.SetActive(true);

            // World-position the disc only — never move the monster root (this component's transform).
            _discRoot.position = new Vector3(
                corpsePos.x,
                groundY + GroundOffsetY,
                corpsePos.z);

            var baseMul = CombatRuntimeTuning.DeathKnockbackShadowBaseRadiusMul;
            var diameter = Mathf.Max(0.1f, bodyRadius * 2f * baseMul * scaleMul);
            _discRoot.localScale = new Vector3(diameter, diameter, 1f);

            var alpha = CombatRuntimeTuning.DeathKnockbackShadowAlphaMul;
            _renderer.color = new Color(0f, 0f, 0f, alpha);
        }

        private void OnDisable() => Hide();

        private void EnsureVisuals()
        {
            if (_discRoot != null)
            {
                _discRoot.localRotation = Quaternion.Euler(DiscLocalEulerX, 0f, 0f);
                if (_renderer != null)
                {
                    _renderer.sortingOrder = SortingOrder;
                }

                return;
            }

            var go = new GameObject("KnockbackGroundShadow");
            _discRoot = go.transform;
            _discRoot.SetParent(transform, false);
            _discRoot.localPosition = Vector3.zero;
            _discRoot.localRotation = Quaternion.Euler(DiscLocalEulerX, 0f, 0f);
            _discRoot.localScale = Vector3.one;

            _renderer = go.AddComponent<SpriteRenderer>();
            _renderer.sprite = GetOrCreateDiscSprite();
            _renderer.sortingOrder = SortingOrder;
        }

        private static Sprite GetOrCreateDiscSprite()
        {
            if (s_discSprite != null)
            {
                return s_discSprite;
            }

            var tex = new Texture2D(DiscTextureSize, DiscTextureSize, TextureFormat.RGBA32, false)
            {
                name = "DeathKnockbackGroundShadow",
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
            s_discSprite.name = "DeathKnockbackGroundShadowSprite";
            s_discSprite.hideFlags = HideFlags.HideAndDontSave;
            return s_discSprite;
        }
    }
}
