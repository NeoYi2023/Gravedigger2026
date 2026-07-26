using UnityEngine;

namespace Gravedigger2026.Gameplay.Defend
{
    /// <summary>
    /// Axis-aligned EngageZone on BattleMap Prefab (SPEC_03 §3.12). Used by WarriorAgentView targeting.
    /// </summary>
    public sealed class EngageZone : MonoBehaviour
    {
        [SerializeField] private Vector2 _halfExtents = new Vector2(4f, 4f);

        public Vector2 HalfExtents => new Vector2(
            Mathf.Max(0.5f, _halfExtents.x),
            Mathf.Max(0.5f, _halfExtents.y));

        public Vector3 Center => transform.position;

        public void SetHalfExtents(Vector2 halfExtents)
        {
            _halfExtents = new Vector2(
                Mathf.Max(0.5f, halfExtents.x),
                Mathf.Max(0.5f, halfExtents.y));
        }

        public bool ContainsXZ(Vector3 worldPosition)
        {
            var delta = worldPosition - Center;
            return Mathf.Abs(delta.x) <= HalfExtents.x && Mathf.Abs(delta.z) <= HalfExtents.y;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.9f, 0.55f, 0.15f, 0.2f);
            Gizmos.DrawCube(Center, new Vector3(HalfExtents.x * 2f, 0.05f, HalfExtents.y * 2f));
        }
    }
}
