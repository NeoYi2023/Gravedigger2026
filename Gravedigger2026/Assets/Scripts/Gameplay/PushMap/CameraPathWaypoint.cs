using UnityEngine;

namespace Gravedigger2026.Gameplay.PushMap
{
    /// <summary>
    /// Authoring waypoint on <see cref="PushMapCameraPath"/> (SPEC_03 §3.14 / SPEC_04 §9.22).
    /// Order ≥1; 0 falls back to sibling index + 1.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CameraPathWaypoint : MonoBehaviour
    {
        [SerializeField] private int _order;

        public int Order => _order > 0 ? _order : transform.GetSiblingIndex() + 1;

        public void SetOrder(int order)
        {
            _order = Mathf.Max(1, order);
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.35f, 0.85f, 1f, 0.95f);
            Gizmos.DrawWireSphere(transform.position + Vector3.up * 0.2f, 0.22f);
        }
    }
}
