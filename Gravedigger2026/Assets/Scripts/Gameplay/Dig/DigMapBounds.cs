using Gravedigger2026.Gameplay.Maps;
using UnityEngine;

namespace Gravedigger2026.Gameplay.Dig
{
    /// <summary>
    /// Continuous IsoDiamond placeable half-extents on DigMap Prefab (XZ).
    /// Half-extents = diamond vertex-to-center distance (SPEC_03 §3.10).
    /// </summary>
    public sealed class DigMapBounds : MonoBehaviour
    {
        [SerializeField] private Vector2 _halfExtents = new Vector2(5f, 2.5f);

        public Vector2 HalfExtents => MapFootprintMath.SanitizeHalfExtents(_halfExtents);

        public Vector3 Center => transform.position;

        public void SetHalfExtents(Vector2 halfExtents)
        {
            _halfExtents = MapFootprintMath.SanitizeHalfExtents(halfExtents);
        }

        public bool ContainsXZ(Vector3 worldPosition)
        {
            return MapFootprintMath.ContainsXZ(Center, HalfExtents, worldPosition);
        }

        private void OnDrawGizmosSelected()
        {
            MapFootprintMath.DrawDiamondGizmo(
                Center,
                HalfExtents,
                new Color(0.2f, 0.8f, 0.4f, 0.9f));
        }
    }
}
