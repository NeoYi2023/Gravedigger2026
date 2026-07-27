using Gravedigger2026.Gameplay.Maps;
using UnityEngine;

namespace Gravedigger2026.Gameplay.Defend
{
    /// <summary>
    /// IsoDiamond EngageZone on BattleMap Prefab (SPEC_03 §3.12). Used by WarriorAgentView targeting.
    /// </summary>
    public sealed class EngageZone : MonoBehaviour
    {
        [SerializeField] private Vector2 _halfExtents = new Vector2(4.25f, 2.125f);

        public Vector2 HalfExtents => MapFootprintMath.SanitizeHalfExtents(_halfExtents);

        public Vector3 Center => transform.position;

        public void SetHalfExtents(Vector2 halfExtents)
        {
            _halfExtents = MapFootprintMath.SanitizeHalfExtents(halfExtents);
        }

        public bool ContainsXZ(Vector3 worldPosition)
        {
            return MapFootprintMath.ContainsXZ(Center, HalfExtents, worldPosition);
        }

        private void OnDrawGizmosSelected()
        {
            MapFootprintMath.DrawDiamondGizmo(
                Center,
                HalfExtents,
                new Color(0.9f, 0.55f, 0.15f, 0.9f));
        }
    }
}
