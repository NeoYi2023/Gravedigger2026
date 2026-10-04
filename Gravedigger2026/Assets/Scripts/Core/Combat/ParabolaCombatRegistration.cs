using Gravedigger2026.Core.Config;
using Gravedigger2026.Core.Defend;
using Gravedigger2026.Core.UpgradeManufacture;
using UnityEngine;

namespace Gravedigger2026.Core.Combat
{
    /// <summary>
    /// Copies ClassConfig Parabola columns onto the combat snapshot (SPEC_03 §3.12).
    /// A class row marked Parabola wins over a legacy instance still stored as Ranged.
    /// </summary>
    public static class ParabolaCombatRegistration
    {
        public static AttackMode ResolveAttackMode(WarriorInstance warrior, ClassConfigRow classRow)
        {
            if (classRow != null && classRow.AttackMode == AttackMode.Parabola)
            {
                return AttackMode.Parabola;
            }

            return warrior != null ? warrior.AttackMode : AttackMode.Melee;
        }

        public static void CopyParams(DefendCombatWarriorState state, ClassConfigRow classRow)
        {
            if (state == null)
            {
                return;
            }

            state.ParabolaMeleeRange = classRow != null
                ? classRow.ParabolaMeleeRange
                : ClassConfigRow.DefaultParabolaMeleeRange;
            state.ParabolaArcMinDistance = classRow != null
                ? classRow.ParabolaArcMinDistance
                : ClassConfigRow.DefaultParabolaArcMinDistance;
            state.ParabolaHitRate = Mathf.Clamp01(classRow != null
                ? classRow.ParabolaHitRate
                : ClassConfigRow.DefaultParabolaHitRate);
            state.ParabolaMissOvershoot = classRow != null
                ? classRow.ParabolaMissOvershoot
                : ClassConfigRow.DefaultParabolaMissOvershoot;
            state.ParabolaMissLingerSeconds = classRow != null
                ? classRow.ParabolaMissLingerSeconds
                : ClassConfigRow.DefaultParabolaMissLingerSeconds;
            state.ParabolaMeleeAttackAnims = classRow != null
                ? classRow.ParabolaMeleeAttackAnims ?? string.Empty
                : string.Empty;
        }
    }
}
