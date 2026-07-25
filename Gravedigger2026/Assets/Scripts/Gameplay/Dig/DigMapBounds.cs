using UnityEngine;

namespace Gravedigger2026.Gameplay.Dig
{
    /// <summary>Continuous placeable half-extents on DigMap Prefab (XZ).</summary>
    public sealed class DigMapBounds : MonoBehaviour
    {
        [SerializeField] private Vector2 _halfExtents = new Vector2(5f, 5f);

        public Vector2 HalfExtents => new Vector2(
            Mathf.Max(0.5f, _halfExtents.x),
            Mathf.Max(0.5f, _halfExtents.y));

        public Vector3 Center => transform.position;

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.2f, 0.8f, 0.4f, 0.25f);
            var size = new Vector3(HalfExtents.x * 2f, 0.05f, HalfExtents.y * 2f);
            Gizmos.DrawCube(Center, size);
        }
    }
}
