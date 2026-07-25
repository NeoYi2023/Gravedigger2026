using UnityEngine;

namespace Gravedigger2026.Gameplay.Dig
{
    /// <summary>Circle obstacle radius on Dig Prefabs (SPEC_03 §3.10 / SPEC_04 §9.2).</summary>
    public sealed class DigObstacleRadius : MonoBehaviour
    {
        [SerializeField] private float _radius = 0.6f;

        public float Radius => Mathf.Max(0.05f, _radius);

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.4f, 0.2f, 0.35f);
            Gizmos.DrawWireSphere(transform.position, Radius);
        }
    }
}
