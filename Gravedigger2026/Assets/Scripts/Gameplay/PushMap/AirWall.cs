using UnityEngine;

namespace Gravedigger2026.Gameplay.PushMap
{
    /// <summary>
    /// Air wall authoring marker on PushMap map Prefab (SPEC_03 §3.14 / SPEC_04 §9.22 PM-08 / D-101).
    /// Y-axis euler supports 0°/45°/90°…; StartBattle bake injects Not Walkable Box.
    /// SupportsSpecialMove walls are walkable only for ClassConfig.SpecialMove=1 soldiers (slice 02).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class AirWall : MonoBehaviour
    {
        [SerializeField] private Vector3 _halfExtents = new Vector3(2.5f, 0.75f, 0.15f);

        [SerializeField]
        [Tooltip("支持特殊移动：勾选后，职业 SpecialMove=1 的士兵可将本墙视为可走（D-101）。缺省否。")]
        private bool _supportsSpecialMove;

        public Vector3 HalfExtents => new Vector3(
            Mathf.Max(0.01f, _halfExtents.x),
            Mathf.Max(0.01f, _halfExtents.y),
            Mathf.Max(0.01f, _halfExtents.z));

        /// <summary>Full box size for NavMeshBuildSource (HalfExtents × 2).</summary>
        public Vector3 FullSize => HalfExtents * 2f;

        /// <summary>
        /// When true, this wall is walkable for soldiers with ClassConfig.SpecialMove=1 (D-101).
        /// </summary>
        public bool SupportsSpecialMove => _supportsSpecialMove;

        public void SetHalfExtents(Vector3 halfExtents)
        {
            _halfExtents = new Vector3(
                Mathf.Max(0.01f, halfExtents.x),
                Mathf.Max(0.01f, halfExtents.y),
                Mathf.Max(0.01f, halfExtents.z));
        }

        public void SetSupportsSpecialMove(bool supportsSpecialMove)
        {
            _supportsSpecialMove = supportsSpecialMove;
        }

        public bool ContainsXZ(Vector3 worldPosition)
        {
            var local = Quaternion.Inverse(transform.rotation) * (worldPosition - transform.position);
            var he = HalfExtents;
            return Mathf.Abs(local.x) <= he.x && Mathf.Abs(local.z) <= he.z;
        }

        private void OnDrawGizmosSelected()
        {
            var prev = Gizmos.matrix;
            Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);
            Gizmos.color = _supportsSpecialMove
                ? new Color(0.35f, 0.95f, 0.45f, 0.95f)
                : new Color(0.55f, 0.75f, 1f, 0.95f);
            Gizmos.DrawWireCube(Vector3.zero, HalfExtents * 2f);
            Gizmos.matrix = prev;
        }
    }
}
