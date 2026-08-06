namespace Gravedigger2026.Core.Config
{
    /// <summary>
    /// Monster aggro stance (SPEC_04 §9.19 / SPEC_03 §3.14). Distinct from <see cref="AttackMode"/>.
    /// </summary>
    public enum AggroMode
    {
        ActiveChase = 0,
        PassiveChase = 1,
        StationaryActive = 2,
        StationaryPassive = 3
    }
}
