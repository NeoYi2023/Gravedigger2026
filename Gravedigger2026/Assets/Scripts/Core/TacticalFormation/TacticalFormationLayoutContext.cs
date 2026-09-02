using System.Collections.Generic;
using Gravedigger2026.Core.AutoManufacture;

namespace Gravedigger2026.Core.TacticalFormation
{
    /// <summary>
    /// Map-relative inputs for Prepare snap / revert (SPEC_03 §3.18).
    /// Positions on <see cref="Core.UpgradeManufacture.BattleFormationService"/> are map-center relative XZ.
    /// </summary>
    public readonly struct TacticalFormationLayoutContext
    {
        public readonly IReadOnlyList<FormationClassZoneSnapshot> Zones;
        public readonly bool HasFacingTarget;
        public readonly float FacingTargetRelX;
        public readonly float FacingTargetRelZ;

        public TacticalFormationLayoutContext(
            IReadOnlyList<FormationClassZoneSnapshot> zones,
            bool hasFacingTarget,
            float facingTargetRelX,
            float facingTargetRelZ)
        {
            Zones = zones;
            HasFacingTarget = hasFacingTarget;
            FacingTargetRelX = facingTargetRelX;
            FacingTargetRelZ = facingTargetRelZ;
        }

        public static TacticalFormationLayoutContext DefaultPlusZ(
            IReadOnlyList<FormationClassZoneSnapshot> zones)
        {
            return new TacticalFormationLayoutContext(zones, false, 0f, 0f);
        }

        public static TacticalFormationLayoutContext Toward(
            IReadOnlyList<FormationClassZoneSnapshot> zones,
            float targetRelX,
            float targetRelZ)
        {
            return new TacticalFormationLayoutContext(zones, true, targetRelX, targetRelZ);
        }
    }
}
