namespace Gravedigger2026.Core.Defend
{
    /// <summary>
    /// Optional pierce settle for <see cref="IProjectileCombatSession"/> implementers.
    /// Handler decides remaining extra hits; View never branches on SkillId.
    /// Defend does not implement this (SE-07 PushMap only).
    /// </summary>
    public interface IProjectilePierceChannel
    {
        bool TryConfirmRangedHit(
            string warriorId,
            string monsterRuntimeId,
            ProjectileHitFlightContext flight);
    }
}
