using UnityEngine;

namespace Gravedigger2026.Gameplay.Formation
{
    /// <summary>
    /// Authoring marker on BattleMap Prefab: ClassId + XZ OBB half-extents + Transform Y
    /// (SPEC_03 §3.15 / SPEC_04 §13 FormationClassZone; AM-06).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class FormationClassZone : MonoBehaviour
    {
        [SerializeField] private string _classId;
        [SerializeField] private Vector2 _halfExtents = new Vector2(0.45f, 0.35f);

        public string ClassId => string.IsNullOrWhiteSpace(_classId) ? string.Empty : _classId.Trim();

        public Vector2 HalfExtents => new Vector2(
            Mathf.Max(0.05f, _halfExtents.x),
            Mathf.Max(0.05f, _halfExtents.y));

        public Vector3 Center => transform.position;

        public float RotationYDegrees => transform.eulerAngles.y;

        public void EditorSet(string classId, Vector2 halfExtents)
        {
            _classId = classId;
            _halfExtents = halfExtents;
        }

        public bool ContainsXZ(Vector3 worldPosition)
        {
            var half = HalfExtents;
            var c = Center;
            var dx = worldPosition.x - c.x;
            var dz = worldPosition.z - c.z;
            var rad = RotationYDegrees * Mathf.Deg2Rad;
            var cos = Mathf.Cos(rad);
            var sin = Mathf.Sin(rad);
            var localX = dx * cos - dz * sin;
            var localZ = dx * sin + dz * cos;
            return Mathf.Abs(localX) <= half.x && Mathf.Abs(localZ) <= half.y;
        }

        private void OnDrawGizmosSelected()
        {
            var half = HalfExtents;
            var c = Center;
            var y = c.y + 0.05f;
            var rad = RotationYDegrees * Mathf.Deg2Rad;
            var cos = Mathf.Cos(rad);
            var sin = Mathf.Sin(rad);

            Vector3 Corner(float lx, float lz)
            {
                var wx = c.x + lx * cos + lz * sin;
                var wz = c.z - lx * sin + lz * cos;
                return new Vector3(wx, y, wz);
            }

            Gizmos.color = new Color(0.25f, 0.75f, 0.95f, 0.85f);
            var p0 = Corner(-half.x, -half.y);
            var p1 = Corner(half.x, -half.y);
            var p2 = Corner(half.x, half.y);
            var p3 = Corner(-half.x, half.y);
            Gizmos.DrawLine(p0, p1);
            Gizmos.DrawLine(p1, p2);
            Gizmos.DrawLine(p2, p3);
            Gizmos.DrawLine(p3, p0);
        }
    }
}
