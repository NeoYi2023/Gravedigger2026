namespace Gravedigger2026.Core.Config
{
    /// <summary>
    /// Monster archetype tag on <see cref="MonsterConfigRow"/> (SPEC_04 §9.19).
    /// Distinct from <c>PushMapSpawnConfig.IsBoss</c> (spawn-row clear target).
    /// </summary>
    public enum MonsterType
    {
        Normal = 1,
        Elite = 2,
        Boss = 3
    }
}
