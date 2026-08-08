namespace Gravedigger2026.Core.Pathing
{
    /// <summary>
    /// Surround gap sector direction on the AttackSlot ring (SPEC_03 §3.12 Approach B+).
    /// All directions are relative to the approach axis
    /// <c>normalize(targetPos - attackerCentroid)</c>:
    /// <see cref="Bottom"/> = far side beyond the target (SPEC default "背侧"),
    /// <see cref="Top"/> = toward the attackers, <see cref="Left"/>/<see cref="Right"/>
    /// = ±90° around the axis, <see cref="Random"/> = debug only (deterministic hash
    /// of target id, not runtime RNG).
    /// </summary>
    public enum SurroundGapDirection
    {
        Bottom = 0,
        Top = 1,
        Left = 2,
        Right = 3,
        Random = 4,
    }

    /// <summary>
    /// Optional surround-gap claim policy for <c>AttackSlotService.TryClaim</c>
    /// (SPEC_04 §9.7 B+ contract). Null/absent → legacy full-ring Chase behavior.
    /// Melee multi-vs-one passes this by default; ranged stays Chase (no surround).
    /// </summary>
    public struct SurroundParams
    {
        /// <summary>Demo default gap width (SPEC_04 §9.7): 60°.</summary>
        public const float DefaultGapDegrees = 60f;

        public SurroundGapDirection GapDir;
        public float GapDegrees;

        public static SurroundParams Default => new SurroundParams
        {
            GapDir = SurroundGapDirection.Bottom,
            GapDegrees = DefaultGapDegrees,
        };
    }
}
