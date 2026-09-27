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

        /// <summary>
        /// Map-relative center used when a new group has no deployed members
        /// (view-center ground point). SPEC_03 §3.18 / UI-030.
        /// </summary>
        public readonly bool HasFallbackCenter;
        public readonly float FallbackCenterRelX;
        public readonly float FallbackCenterRelZ;

        public TacticalFormationLayoutContext(
            IReadOnlyList<FormationClassZoneSnapshot> zones,
            bool hasFacingTarget,
            float facingTargetRelX,
            float facingTargetRelZ,
            bool hasFallbackCenter = false,
            float fallbackCenterRelX = 0f,
            float fallbackCenterRelZ = 0f)
        {
            Zones = zones;
            HasFacingTarget = hasFacingTarget;
            FacingTargetRelX = facingTargetRelX;
            FacingTargetRelZ = facingTargetRelZ;
            HasFallbackCenter = hasFallbackCenter;
            FallbackCenterRelX = fallbackCenterRelX;
            FallbackCenterRelZ = fallbackCenterRelZ;
        }

        public TacticalFormationLayoutContext WithFallbackCenter(float relX, float relZ)
        {
            return new TacticalFormationLayoutContext(
                Zones,
                HasFacingTarget,
                FacingTargetRelX,
                FacingTargetRelZ,
                true,
                relX,
                relZ);
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
