using System.Collections.Generic;
using Gravedigger2026.Core.AutoManufacture;
using Gravedigger2026.Core.TacticalFormation;
using Gravedigger2026.Gameplay.Defend;
using Gravedigger2026.Gameplay.PushMap;
using UnityEngine;

namespace Gravedigger2026.Gameplay.Formation
{
    /// <summary>
    /// Builds Prepare facing context: PushMap → first Objective; Defend → EngageZone center; else +Z
    /// (SPEC_03 §3.18).
    /// </summary>
    public static class TacticalFormationLayoutContextFactory
    {
        public static TacticalFormationLayoutContext Create(
            FormationEditorMode mode,
            GameObject map,
            Vector3 mapCenter,
            IReadOnlyList<FormationClassZoneSnapshot> zones)
        {
            if (map == null)
            {
                return TacticalFormationLayoutContext.DefaultPlusZ(zones);
            }

            if (FormationEditorModeUtil.UsesPushMapPrepareFraming(mode))
            {
                var points = map.GetComponentsInChildren<ObjectivePoint>(true);
                ObjectivePoint best = null;
                var bestOrder = int.MaxValue;
                for (var i = 0; i < points.Length; i++)
                {
                    var p = points[i];
                    if (p == null || !p.isActiveAndEnabled)
                    {
                        continue;
                    }

                    var order = p.ObjectiveOrder;
                    if (best == null || order < bestOrder)
                    {
                        best = p;
                        bestOrder = order;
                    }
                }

                if (best != null)
                {
                    var t = best.transform.position;
                    return TacticalFormationLayoutContext.Toward(
                        zones,
                        t.x - mapCenter.x,
                        t.z - mapCenter.z);
                }
            }
            else if (mode == FormationEditorMode.DefendPrepare)
            {
                var zone = map.GetComponentInChildren<EngageZone>(true);
                if (zone != null)
                {
                    var c = zone.Center;
                    return TacticalFormationLayoutContext.Toward(
                        zones,
                        c.x - mapCenter.x,
                        c.z - mapCenter.z);
                }
            }

            return TacticalFormationLayoutContext.DefaultPlusZ(zones);
        }
    }
}
