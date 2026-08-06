using UnityEngine;

namespace Gravedigger2026.Gameplay.Dig
{
    /// <summary>
    /// Offline-baked dig hit hull on Grave Prefabs (SPEC_03 §3.10 / SPEC_04 §9.2).
    /// Local XZ convex polygon; separate from DigObstacleRadius.
    /// </summary>
    public sealed class DigHitShape : MonoBehaviour
    {
        [SerializeField] private Vector2[] _localXZ = System.Array.Empty<Vector2>();
        [SerializeField] private float _boundingRadius = 0.55f;

        public Vector2[] LocalXZ => _localXZ;
        public float BoundingRadius => Mathf.Max(0.05f, _boundingRadius);
        public bool HasValidPolygon => _localXZ != null && _localXZ.Length >= 3;

        public void SetBaked(Vector2[] localXZ, float boundingRadius)
        {
            _localXZ = localXZ ?? System.Array.Empty<Vector2>();
            _boundingRadius = Mathf.Max(0.05f, boundingRadius);
        }

        private void OnDrawGizmosSelected()
        {
            if (!HasValidPolygon)
            {
                return;
            }

            Gizmos.color = new Color(0.2f, 0.85f, 1f, 0.85f);
            var origin = transform.position;
            for (var i = 0; i < _localXZ.Length; i++)
            {
                var a = _localXZ[i];
                var b = _localXZ[(i + 1) % _localXZ.Length];
                var wa = origin + new Vector3(a.x, 0.05f, a.y);
                var wb = origin + new Vector3(b.x, 0.05f, b.y);
                Gizmos.DrawLine(wa, wb);
            }

            Gizmos.color = new Color(0.2f, 0.85f, 1f, 0.25f);
            Gizmos.DrawWireSphere(origin, BoundingRadius);
        }
    }
}
