using UnityEngine;
using UnityEngine.AI;

namespace Gravedigger2026.Gameplay.Defend
{
    /// <summary>
    /// D-101 dual-bake agent type lookup (SPEC_04 §9.22).
    /// Default Humanoid = 0; SpecialMove copies Humanoid in NavMeshAreas.
    /// </summary>
    public static class SpecialMoveNavMesh
    {
        public const string SpecialMoveAgentTypeName = "SpecialMove";
        public const int DefaultAgentTypeId = 0;

        public static bool TryResolveSpecialMoveAgentTypeId(out int agentTypeId)
        {
            agentTypeId = DefaultAgentTypeId;
            var count = NavMesh.GetSettingsCount();
            for (var i = 0; i < count; i++)
            {
                var settings = NavMesh.GetSettingsByIndex(i);
                var name = NavMesh.GetSettingsNameFromID(settings.agentTypeID);
                if (string.Equals(name, SpecialMoveAgentTypeName, System.StringComparison.Ordinal))
                {
                    agentTypeId = settings.agentTypeID;
                    return true;
                }
            }

            return false;
        }

        public static int ResolveAgentTypeId(bool hasSpecialMove)
        {
            if (!hasSpecialMove)
            {
                return DefaultAgentTypeId;
            }

            return TryResolveSpecialMoveAgentTypeId(out var id) ? id : DefaultAgentTypeId;
        }

        public static bool SampleWalkable(
            Vector3 source,
            float maxDistance,
            int agentTypeId,
            out NavMeshHit hit,
            int areaMask = -1)
        {
            var filter = new NavMeshQueryFilter
            {
                agentTypeID = agentTypeId,
                areaMask = areaMask < 0 ? NavMesh.AllAreas : areaMask
            };
            return NavMesh.SamplePosition(source, out hit, maxDistance, filter);
        }

        public static bool SampleWalkable(
            NavMeshAgent agent,
            Vector3 source,
            float maxDistance,
            out NavMeshHit hit)
        {
            var typeId = agent != null ? agent.agentTypeID : DefaultAgentTypeId;
            return SampleWalkable(source, maxDistance, typeId, out hit);
        }
    }
}
