using UnityEngine;

namespace Gravedigger2026.Gameplay.PushMap
{
    /// <summary>
    /// XZ capture circle on PushMap map Prefab (SPEC_03 §3.14 / SPEC_04 §9.22). Default radius 2.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CaptureZone : MonoBehaviour
    {
        [SerializeField] private float _radius = 2f;

        public float Radius => Mathf.Max(0.01f, _radius);

        public Vector3 Center => transform.position;

        public void SetRadius(float radius)
        {
            _radius = Mathf.Max(0.01f, radius);
        }

        public bool ContainsXZ(Vector3 worldPosition)
        {
            var dx = worldPosition.x - Center.x;
            var dz = worldPosition.z - Center.z;
            var r = Radius;
            return dx * dx + dz * dz <= r * r;
        }

        private void OnDrawGizmosSelected()
        {
            PushMapMarkerGizmos.DrawCircleXZ(Center, Radius, new Color(0.2f, 0.85f, 0.35f, 0.95f));
        }
    }
}
