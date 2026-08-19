using UnityEngine;

namespace Gravedigger2026.Core.Combat
{
    /// <summary>Alive monster XZ snapshot for AOE skill-effect handlers (rules layer; no Transform).</summary>
    public readonly struct MonsterWorldXZ
    {
        public readonly string RuntimeId;
        public readonly Vector2 PositionXZ;
        public readonly Vector2 FacingXZ;
        public readonly float BodyRadius;

        public MonsterWorldXZ(string runtimeId, Vector2 positionXZ)
            : this(runtimeId, positionXZ, Vector2.zero, 0f)
        {
        }

        public MonsterWorldXZ(string runtimeId, Vector2 positionXZ, Vector2 facingXZ, float bodyRadius)
        {
            RuntimeId = runtimeId;
            PositionXZ = positionXZ;
            FacingXZ = facingXZ;
            BodyRadius = bodyRadius;
        }
    }
}
