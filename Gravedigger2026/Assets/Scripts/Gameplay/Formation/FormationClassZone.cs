using Gravedigger2026.Gameplay.Maps;
using UnityEngine;

namespace Gravedigger2026.Gameplay.Formation
{
    /// <summary>
    /// Authoring marker on BattleMap Prefab: ClassId + IsoDiamond half-extents
    /// (SPEC_03 §3.15 / SPEC_04 §13 FormationClassZone; FZ-01). Same XZ diamond as WalkSurface.
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshCollider))]
    public sealed class FormationClassZone : MonoBehaviour
    {
        [SerializeField] private string _classId;
        [SerializeField] private Vector2 _halfExtents = new Vector2(0.45f, 0.35f);

        public string ClassId => string.IsNullOrWhiteSpace(_classId) ? string.Empty : _classId.Trim();

        public Vector2 HalfExtents => MapFootprintMath.SanitizeHalfExtents(
            _halfExtents,
            MapFootprintMath.MarkerMinHalfExtent);

        public Vector3 Center => transform.position;

        public void EditorSet(string classId, Vector2 halfExtents)
        {
            _classId = classId;
            _halfExtents = MapFootprintMath.SanitizeHalfExtents(
                halfExtents,
                MapFootprintMath.MarkerMinHalfExtent);
            RebuildMesh();
        }

        public bool ContainsXZ(Vector3 worldPosition)
        {
            return MapFootprintMath.ContainsXZ(
                Center,
                HalfExtents,
                worldPosition,
                MapFootprintMath.MarkerMinHalfExtent);
        }

        public void DrawDiamondGizmo()
        {
            MapFootprintMath.DrawDiamondGizmo(
                Center,
                HalfExtents,
                new Color(0.25f, 0.75f, 0.95f, 0.85f),
                0.05f,
                MapFootprintMath.MarkerMinHalfExtent);
        }

        private void Awake()
        {
            RebuildMesh();
        }

        private void OnEnable()
        {
            RebuildMesh();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            RebuildMesh();
        }
#endif

        public void RebuildMesh()
        {
            EnsureMeshComponents();
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;

            var mesh = MapFootprintMath.BuildDiamondMesh(
                HalfExtents,
                0.05f,
                MapFootprintMath.MarkerMinHalfExtent);

            var filter = GetComponent<MeshFilter>();
            if (filter != null)
            {
                filter.sharedMesh = mesh;
            }

            var col = GetComponent<MeshCollider>();
            if (col != null)
            {
                col.sharedMesh = null;
                col.sharedMesh = mesh;
                col.enabled = !Application.isPlaying;
            }

            var renderer = GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.enabled = false;
            }
        }

        public void EnsureMeshComponents()
        {
            if (GetComponent<MeshFilter>() == null)
            {
                gameObject.AddComponent<MeshFilter>();
            }

            if (GetComponent<MeshRenderer>() == null)
            {
                gameObject.AddComponent<MeshRenderer>();
            }

            if (GetComponent<MeshCollider>() == null)
            {
                gameObject.AddComponent<MeshCollider>();
            }
        }

        private void OnDrawGizmosSelected()
        {
            DrawDiamondGizmo();
        }
    }
}
