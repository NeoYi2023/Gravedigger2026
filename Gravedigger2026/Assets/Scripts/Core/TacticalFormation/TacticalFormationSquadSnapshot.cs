using System;
using Gravedigger2026.Core.Config;

namespace Gravedigger2026.Core.TacticalFormation
{
    /// <summary>
    /// Live Prepare view of one tactical group (SPEC_03 §3.18 / TFG-02).
    /// Membership and facing persist on <c>BattleFormationSaveData.Groups</c>.
    /// Center and level are derived, not saved.
    /// </summary>
    public sealed class TacticalFormationSquadSnapshot
    {
        public string GroupInstanceId;
        public string FormationId;
        public string[] MemberIds = Array.Empty<string>();
        public float CenterX;
        public float CenterZ;
        public float FacingYawDegrees;

        /// <summary>Floor of mean ClassLevel. Not persisted.</summary>
        public int ComputedLevel;

        /// <summary>Parallel to <see cref="MemberIds"/>. Missing class row = 0. Not persisted.</summary>
        public int[] MemberClassLevels = Array.Empty<int>();

        /// <summary>
        /// Greatest config row with FormationLevel ≤ <see cref="ComputedLevel"/>.
        /// Null when no such row (group still exists; stats empty).
        /// </summary>
        public TacticalFormationConfigRow MatchedLevelRow;

        public bool Contains(string warriorId)
        {
            if (string.IsNullOrEmpty(warriorId) || MemberIds == null)
            {
                return false;
            }

            for (var i = 0; i < MemberIds.Length; i++)
            {
                if (string.Equals(MemberIds[i], warriorId, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
