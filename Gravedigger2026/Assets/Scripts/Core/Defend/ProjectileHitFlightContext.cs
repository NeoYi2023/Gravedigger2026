using System.Collections.Generic;

namespace Gravedigger2026.Core.Defend
{
    /// <summary>
    /// Generic pierce bag for ranged hits (SPEC_04 §6 SE-07).
    /// View fills <see cref="AlreadyHitRuntimeIds"/> (including the current target);
    /// Handler writes <see cref="ExtraHitsRemaining"/> (0 = despawn).
    /// </summary>
    public sealed class ProjectileHitFlightContext
    {
        public IReadOnlyCollection<string> AlreadyHitRuntimeIds;
        public int ExtraHitsRemaining;
    }
}
