using UnityEngine;

namespace Gravedigger2026.Gameplay.Maps
{
    /// <summary>
    /// Invisible WalkSurface IsoDiamond mesh on Ground_* Prefab (SPEC_04 §9.7).
    /// Half-extents match Tilemap footprint: PaintRadius*(cellSize.x, cellSize.y).
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshCollider))]
    public sealed class WalkSurfaceIsoDiamond : MonoBehaviour
    {
        [SerializeField] private Vector2 _halfExtents = new Vector2(5f, 2.5f);

        public Vector2 HalfExtents => MapFootprintMath.SanitizeHalfExtents(_halfExtents);

        public void SetHalfExtents(Vector2 halfExtents)
        {
            _halfExtents = MapFootprintMath.SanitizeHalfExtents(halfExtents);
            RebuildMesh();
        }

        private void OnEnable()
        {
            RebuildMesh();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // MeshFilter.sharedMesh assignment uses SendMessage; Unity forbids that
            // during OnValidate / Awake / CheckConsistency.
            UnityEditor.EditorApplication.delayCall -= RebuildMeshOnEditorDelay;
            UnityEditor.EditorApplication.delayCall += RebuildMeshOnEditorDelay;
        }

        private void RebuildMeshOnEditorDelay()
        {
            UnityEditor.EditorApplication.delayCall -= RebuildMeshOnEditorDelay;
            if (this == null)
            {
                return;
            }

            RebuildMesh();
        }
#endif

        public void RebuildMesh()
        {
            var half = HalfExtents;
            var mesh = MapFootprintMath.BuildDiamondMesh(half);

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
            }

            var renderer = GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.enabled = false;
            }

            MapFootprintMath.ApplyWalkSurfaceTransform(transform);
        }

        private void OnDrawGizmosSelected()
        {
            MapFootprintMath.DrawDiamondGizmo(
                transform.position,
                HalfExtents,
                new Color(0.3f, 0.6f, 1f, 0.9f));
        }
    }
}
