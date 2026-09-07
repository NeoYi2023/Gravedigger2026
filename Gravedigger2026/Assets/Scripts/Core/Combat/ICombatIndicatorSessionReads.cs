using System.Collections.Generic;

namespace Gravedigger2026.Core.Combat
{
    /// <summary>
    /// Shared thin-read surface for CombatIndicator (UI-033 / D-089 Approach A).
    /// PushMap / SearchExtract sessions implement the same copy APIs; sorting stays in SnapshotBuilder.
    /// </summary>
    public interface ICombatIndicatorSessionReads
    {
        /// <summary>Copy registered warriors into a reused list (no per-call list allocation).</summary>
        IReadOnlyList<CombatIndicatorWarriorRead> CopyWarriorIndicatorReads();

        /// <summary>Copy registered monsters into a reused list (no per-call list allocation).</summary>
        IReadOnlyList<CombatIndicatorMonsterRead> CopyMonsterIndicatorReads();
    }
}
