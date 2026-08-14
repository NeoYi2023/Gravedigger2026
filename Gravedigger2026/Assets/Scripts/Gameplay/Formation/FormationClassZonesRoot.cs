using Gravedigger2026.Gameplay.Dig;
using Gravedigger2026.Gameplay.Maps;
using UnityEngine;

namespace Gravedigger2026.Gameplay.Formation
{
    /// <summary>
    /// Authoring parent of FormationClassZone markers.
    /// Identity rotation so child IsoDiamonds match WalkSurface / GroundTilemap
    /// (SPEC_03 §3.15 / SPEC_04 §13).
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class FormationClassZonesRoot : MonoBehaviour
    {
        public Vector3 MapCenter
        {
            get
            {
                var bounds = GetComponentInParent<DigMapBounds>();
                return bounds != null ? bounds.Center : transform.position;
            }
        }

        public Vector2 MapHalfExtents
        {
            get
            {
                var bounds = GetComponentInParent<DigMapBounds>();
                return bounds != null ? bounds.HalfExtents : new Vector2(5f, 2.5f);
            }
        }

        private void OnEnable()
        {
            ApplyIdentityAuthoringFrame();
        }

        public void ApplyIdentityAuthoringFrame()
        {
            var childCount = transform.childCount;
            var worldPos = new Vector3[childCount];
            for (var i = 0; i < childCount; i++)
            {
                worldPos[i] = transform.GetChild(i).position;
            }

            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;

            for (var i = 0; i < childCount; i++)
            {
                var child = transform.GetChild(i);
                child.position = worldPos[i];
                child.localRotation = Quaternion.identity;
                child.localScale = Vector3.one;
            }
        }

        public void DrawIsoDiamondGizmo(Color color)
        {
            MapFootprintMath.DrawDiamondGizmo(MapCenter, MapHalfExtents, color);
        }

        public void DrawChildZoneGizmos()
        {
            var zones = GetComponentsInChildren<FormationClassZone>(true);
            for (var i = 0; i < zones.Length; i++)
            {
                if (zones[i] != null)
                {
                    zones[i].DrawDiamondGizmo();
                }
            }
        }
    }
}
