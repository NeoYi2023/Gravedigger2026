using System.Collections.Generic;
using UnityEngine;

namespace Gravedigger2026.Gameplay.PushMap
{
    /// <summary>
    /// HitFlash (PM-12/13, SPEC_03 §3.14 / SPEC_04 §9.22): on a successful hit, tint the
    /// target subtree Renderers bright red (monster) / white (soldier) for 2×0.1s pulses
    /// back-to-back with no off gap — a continuous ≈0.2s tint — then restore. Re-hit
    /// mid-flash restarts from t=0. SpriteRenderers tint via .color; other Renderers via
    /// MaterialPropertyBlock (shared materials never mutated, aligned with DigGraveView).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class HitFlashView : MonoBehaviour
    {
        public static readonly Color MonsterFlashColor = new Color(1f, 0.3f, 0.25f);
        public static readonly Color SoldierFlashColor = Color.white;

        /// <summary>2 pulses × 0.1s back-to-back, no off gap → one continuous 0.2s window.</summary>
        private const float FlashSeconds = 0.2f;

        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        private Renderer[] _renderers;
        private Dictionary<SpriteRenderer, Color> _spriteOriginals;
        private MaterialPropertyBlock _block;
        private float _remaining;
        private bool _flashing;

        /// <summary>Play (or refresh) the flash tint.</summary>
        public void Play(Color color)
        {
            EnsureRenderers();
            if (_renderers == null || _renderers.Length == 0)
            {
                return;
            }

            _block ??= new MaterialPropertyBlock();
            for (var i = 0; i < _renderers.Length; i++)
            {
                var r = _renderers[i];
                if (r == null)
                {
                    continue;
                }

                if (r is SpriteRenderer sprite)
                {
                    sprite.color = color;
                    continue;
                }

                r.GetPropertyBlock(_block);
                _block.SetColor(ColorId, color);
                _block.SetColor(BaseColorId, color);
                r.SetPropertyBlock(_block);
            }

            _remaining = FlashSeconds;
            _flashing = true;
        }

        private void Update()
        {
            if (!_flashing)
            {
                return;
            }

            _remaining -= Time.deltaTime;
            if (_remaining <= 0f)
            {
                Restore();
            }
        }

        private void OnDisable()
        {
            if (_flashing)
            {
                Restore();
            }
        }

        private void Restore()
        {
            _flashing = false;
            _remaining = 0f;
            if (_renderers == null)
            {
                return;
            }

            _block ??= new MaterialPropertyBlock();
            for (var i = 0; i < _renderers.Length; i++)
            {
                var r = _renderers[i];
                if (r == null)
                {
                    continue;
                }

                if (r is SpriteRenderer sprite)
                {
                    if (_spriteOriginals != null && _spriteOriginals.TryGetValue(sprite, out var original))
                    {
                        sprite.color = original;
                    }

                    continue;
                }

                _block.Clear();
                r.SetPropertyBlock(_block);
            }
        }

        private void EnsureRenderers()
        {
            if (_renderers != null)
            {
                return;
            }

            _renderers = GetComponentsInChildren<Renderer>(true);
            _spriteOriginals = new Dictionary<SpriteRenderer, Color>();
            for (var i = 0; i < _renderers.Length; i++)
            {
                if (_renderers[i] is SpriteRenderer sprite)
                {
                    _spriteOriginals[sprite] = sprite.color;
                }
            }
        }
    }
}
