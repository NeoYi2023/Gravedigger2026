using UnityEngine;

namespace Gravedigger2026.Gameplay.PushMap
{
    /// <summary>
    /// Air wall authoring marker on PushMap map Prefab (SPEC_03 §3.14 / SPEC_04 §9.22 PM-08).
    /// Y-axis euler supports 0°/45°/90°…; StartBattle bake injects Not Walkable Box.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class AirWall : MonoBehaviour
    {
        [SerializeField] private Vector3 _halfExtents = new Vector3(2.5f, 0.75f, 0.15f);

        public Vector3 HalfExtents => new Vector3(
            Mathf.Max(0.01f, _halfExtents.x),
            Mathf.Max(0.01f, _halfExtents.y),
            Mathf.Max(0.01f, _halfExtents.z));

        /// <summary>Full box size for NavMeshBuildSource (HalfExtents × 2).</summary>
        public Vector3 FullSize => HalfExtents * 2f;

        public void SetHalfExtents(Vector3 halfExtents)
        {
            _halfExtents = new Vector3(
                Mathf.Max(0.01f, halfExtents.x),
                Mathf.Max(0.01f, halfExtents.y),
                Mathf.Max(0.01f, halfExtents.z));
        }

        private void OnDrawGizmosSelected()
        {
            var prev = Gizmos.matrix;
            Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);
            Gizmos.color = new Color(0.55f, 0.75f, 1f, 0.95f);
            Gizmos.DrawWireCube(Vector3.zero, HalfExtents * 2f);
            Gizmos.matrix = prev;
        }
    }
}
