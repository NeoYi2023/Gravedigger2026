namespace Gravedigger2026.Core.Defend
{
    /// <summary>
    /// Ranged HitConfirm contract consumed by ProjectileView (SPEC_04 §9.22 PM-12).
    /// Implemented by DefendSessionService and PushMapSessionService so the same
    /// Projectile prefab/View serves both stages without sharing session lifetimes.
    /// </summary>
    public interface IProjectileCombatSession
    {
        /// <summary>Session active + Combat phase + shooter still combat-active.</summary>
        bool IsProjectileCombatActive(string warriorId);

        bool IsMonsterAlive(string monsterRuntimeId);

        /// <summary>Soft-collision hit reported by the View; rules settle NormalAttackPower.</summary>
        bool TryConfirmRangedHit(string warriorId, string monsterRuntimeId);
    }
}
