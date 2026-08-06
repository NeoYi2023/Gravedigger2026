using UnityEngine;

namespace Gravedigger2026.Gameplay.PushMap
{
    /// <summary>
    /// Trap zone marker on PushMap map Prefab (SPEC_03 §3.14 / SPEC_04 §9.22).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TrapZone : MonoBehaviour
    {
        [SerializeField] private string _trapZoneId = "TZ_01";
        [SerializeField] private float _radius = 1.5f;

        public string TrapZoneId => string.IsNullOrWhiteSpace(_trapZoneId)
            ? name
            : _trapZoneId.Trim();

        public float Radius => Mathf.Max(0.01f, _radius);

        public Vector3 Center => transform.position;

        public void SetTrapZoneId(string trapZoneId)
        {
            _trapZoneId = trapZoneId ?? string.Empty;
        }

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
            PushMapMarkerGizmos.DrawCircleXZ(Center, Radius, new Color(0.9f, 0.4f, 0.85f, 0.95f));
        }
    }
}
