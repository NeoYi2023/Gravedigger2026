using UnityEngine;

namespace Gravedigger2026.Gameplay.Dig
{
    public sealed class DigDiggerView : MonoBehaviour
    {
        [SerializeField] private Transform _visual;
        [SerializeField] private Color _idleColor = new Color(0.35f, 0.55f, 0.85f, 1f);
        [SerializeField] private Color _digColor = new Color(0.95f, 0.70f, 0.25f, 1f);

        private Renderer _renderer;
        private MaterialPropertyBlock _block;
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private float _pulse;

        private void Awake()
        {
            if (_visual == null)
            {
                _visual = transform;
            }

            _renderer = GetComponentInChildren<Renderer>();
            SetDigging(false);
        }

        private void Update()
        {
            if (_pulse <= 0f || _visual == null)
            {
                return;
            }

            var s = 1f + Mathf.Sin(Time.time * 8f) * 0.08f;
            _visual.localScale = Vector3.one * s;
        }

        public void SetDigging(bool digging)
        {
            _pulse = digging ? 1f : 0f;
            if (_visual != null && !digging)
            {
                _visual.localScale = Vector3.one;
            }

            ApplyColor(digging ? _digColor : _idleColor);
        }

        private void ApplyColor(Color color)
        {
            if (_renderer == null)
            {
                return;
            }

            if (_block == null)
            {
                _block = new MaterialPropertyBlock();
            }

            _renderer.GetPropertyBlock(_block);
            _block.SetColor(ColorId, color);
            _block.SetColor(BaseColorId, color);
            _renderer.SetPropertyBlock(_block);
        }
    }
}
