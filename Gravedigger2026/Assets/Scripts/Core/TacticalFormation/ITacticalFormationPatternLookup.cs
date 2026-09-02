using UnityEngine;

namespace Gravedigger2026.Core.TacticalFormation
{
    /// <summary>
    /// Resolves table <c>PrefabId</c> → Pattern slot local XZ + move params (SPEC_04 §9.30).
    /// Core-safe; no Prefab ownership.
    /// </summary>
    public interface ITacticalFormationPatternLookup
    {
        bool TryGetSlotLocalXZ(string prefabId, out Vector3[] slotLocalXZ);

        bool TryGetMoveParams(string prefabId, out TacticalFormationMoveParams moveParams);
    }
}
