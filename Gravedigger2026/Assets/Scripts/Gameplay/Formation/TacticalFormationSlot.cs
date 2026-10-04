using Gravedigger2026.Core.Config;
using UnityEngine;

namespace Gravedigger2026.Gameplay.Formation
{
    /// <summary>
    /// Authoring on each Pattern <c>Slot_*</c> child (SPEC_03 §3.18 / SPEC_04 §9.30).
    /// Soft-prefers <see cref="ClassConfigRow.BaseClass"/> when assigning members at group create.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TacticalFormationSlot : MonoBehaviour
    {
        [SerializeField] private BaseClassKind _preferredClass = BaseClassKind.Unspecified;

        /// <summary>
        /// Preferred <c>ClassConfig.BaseClass</c>. <see cref="BaseClassKind.Unspecified"/> = no preference.
        /// </summary>
        public BaseClassKind PreferredClass => _preferredClass;

#if UNITY_EDITOR
        public void EditorSetPreferredClass(BaseClassKind preferredClass)
        {
            _preferredClass = preferredClass;
        }
#endif
    }
}
