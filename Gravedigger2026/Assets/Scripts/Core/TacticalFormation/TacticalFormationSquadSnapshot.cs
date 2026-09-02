using System;

namespace Gravedigger2026.Core.TacticalFormation
{
    /// <summary>
    /// Session membership for one activated tactical formation in Prepare (SPEC_03 §3.18).
    /// Not persisted; reconstructed by <see cref="TacticalFormationLayoutService.EvaluateAndApply"/>.
    /// </summary>
    public sealed class TacticalFormationSquadSnapshot
    {
        public string FormationId;
        public string[] MemberIds = Array.Empty<string>();
        public float CenterX;
        public float CenterZ;
        public float FacingYawDegrees;

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
