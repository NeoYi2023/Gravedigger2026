using UnityEngine;

namespace Gravedigger2026.Gameplay.Dig
{
    public sealed class DigGraveView : MonoBehaviour
    {
        [SerializeField] private Renderer _bodyRenderer;

        private MaterialPropertyBlock _block;
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        public int InstanceId { get; private set; }

        public void Bind(int instanceId, string qualityId)
        {
            InstanceId = instanceId;
            gameObject.name = $"Grave_{qualityId}_{instanceId}";

            if (_bodyRenderer == null)
            {
                _bodyRenderer = GetComponentInChildren<Renderer>();
            }
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
            var scale = busy ? 1.15f : 1f;
            transform.localScale = Vector3.one * scale;
        }

        private void ApplyColor(Color color)
        {
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
