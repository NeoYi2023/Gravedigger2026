using System;
using Gravedigger2026.Core.TacticalFormation;
using UnityEngine;

namespace Gravedigger2026.Gameplay.Formation
{
    /// <summary>
    /// Authoring component on tactical formation Pattern Prefabs
    /// (SPEC_03 §3.18 / SPEC_04 §9.30 / §13). Root = center; root forward (XZ) = facing.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TacticalFormationPattern : MonoBehaviour
    {
        public const float DefaultLeashRadius = TacticalFormationMoveParams.DefaultLeashRadius;
        public const float DefaultSlotArriveEpsilon = TacticalFormationMoveParams.DefaultSlotArriveEpsilon;
        public const float DefaultCenterMoveSpeedMul = TacticalFormationMoveParams.DefaultCenterMoveSpeedMul;
        public const float DefaultFacingTurnRate = TacticalFormationMoveParams.DefaultFacingTurnRate;
        public const bool DefaultKeepFormationWhileEngage =
            TacticalFormationMoveParams.DefaultKeepFormationWhileEngage;

        [SerializeField] private Transform[] _slots = Array.Empty<Transform>();
        [SerializeField] private float _leashRadius = DefaultLeashRadius;
        [SerializeField] private float _slotArriveEpsilon = DefaultSlotArriveEpsilon;
        [SerializeField] private float _centerMoveSpeedMul = DefaultCenterMoveSpeedMul;
        [SerializeField] private float _facingTurnRate = DefaultFacingTurnRate;
        [SerializeField] private bool _keepFormationWhileEngage = DefaultKeepFormationWhileEngage;

        public Transform[] Slots => _slots ?? Array.Empty<Transform>();

        public int SlotCount
        {
            get
            {
                var slots = Slots;
                var count = 0;
                for (var i = 0; i < slots.Length; i++)
                {
                    if (slots[i] != null)
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        /// <summary>≤0 falls back to <see cref="DefaultLeashRadius"/>.</summary>
        public float LeashRadius => _leashRadius > 0f ? _leashRadius : DefaultLeashRadius;

        /// <summary>≤0 falls back to <see cref="DefaultSlotArriveEpsilon"/>.</summary>
        public float SlotArriveEpsilon =>
            _slotArriveEpsilon > 0f ? _slotArriveEpsilon : DefaultSlotArriveEpsilon;

        /// <summary>≤0 falls back to <see cref="DefaultCenterMoveSpeedMul"/>.</summary>
        public float CenterMoveSpeedMul =>
            _centerMoveSpeedMul > 0f ? _centerMoveSpeedMul : DefaultCenterMoveSpeedMul;

        /// <summary>
        /// Degrees per second. &lt;0 falls back to <see cref="DefaultFacingTurnRate"/>;
        /// 0 = do not turn (lock facing).
        /// </summary>
        public float FacingTurnRate =>
            _facingTurnRate < 0f ? DefaultFacingTurnRate : _facingTurnRate;

        public bool KeepFormationWhileEngage => _keepFormationWhileEngage;

        public TacticalFormationMoveParams ReadMoveParams()
        {
            return new TacticalFormationMoveParams(
                LeashRadius,
                SlotArriveEpsilon,
                CenterMoveSpeedMul,
                FacingTurnRate,
                KeepFormationWhileEngage);
        }

        /// <summary>Local XZ of slot i (Y forced 0). Missing slot → Vector3.zero.</summary>
        public Vector3 GetSlotLocalXZ(int index)
        {
            var slots = Slots;
            if (index < 0 || index >= slots.Length || slots[index] == null)
            {
                return Vector3.zero;
            }

            var local = slots[index].localPosition;
            local.y = 0f;
            return local;
        }

        public void RefreshSlotsFromChildren()
        {
            var found = new Transform[transform.childCount];
            var count = 0;
            for (var i = 0; i < transform.childCount; i++)
            {
                var child = transform.GetChild(i);
                if (child != null && child.name.StartsWith("Slot_", StringComparison.Ordinal))
                {
                    found[count++] = child;
                }
            }

            if (count != found.Length)
            {
                Array.Resize(ref found, count);
            }

            Array.Sort(found, (a, b) => string.CompareOrdinal(a.name, b.name));
            _slots = found;
        }

#if UNITY_EDITOR
        public void EditorSetMoveParams(
            float leashRadius,
            float slotArriveEpsilon,
            float centerMoveSpeedMul,
            float facingTurnRate,
            bool keepFormationWhileEngage)
        {
            _leashRadius = leashRadius;
            _slotArriveEpsilon = slotArriveEpsilon;
            _centerMoveSpeedMul = centerMoveSpeedMul;
            _facingTurnRate = facingTurnRate;
            _keepFormationWhileEngage = keepFormationWhileEngage;
        }

        private void OnValidate()
        {
            RefreshSlotsFromChildren();
        }

        private void OnDrawGizmosSelected()
        {
            var center = transform.position;
            center.y = 0f;

            Gizmos.color = new Color(0.2f, 0.85f, 1f, 0.95f);
            Gizmos.DrawLine(center + Vector3.left * 0.25f, center + Vector3.right * 0.25f);
            Gizmos.DrawLine(center + Vector3.back * 0.25f, center + Vector3.forward * 0.25f);

            var facing = transform.forward;
            facing.y = 0f;
            if (facing.sqrMagnitude > 0.0001f)
            {
                facing.Normalize();
                Gizmos.color = new Color(0.35f, 0.95f, 0.45f, 0.95f);
                Gizmos.DrawLine(center, center + facing * 1.4f);
            }

            Gizmos.color = new Color(0.95f, 0.75f, 0.2f, 0.95f);
            var slots = Slots;
            for (var i = 0; i < slots.Length; i++)
            {
                if (slots[i] == null)
                {
                    continue;
                }

                var p = slots[i].position;
                p.y = 0f;
                Gizmos.DrawWireSphere(p, 0.18f);
            }

            Gizmos.color = new Color(0.95f, 0.35f, 0.75f, 0.85f);
            DrawWireCircleXZ(center, LeashRadius, 48);
        }

        private static void DrawWireCircleXZ(Vector3 center, float radius, int segments)
        {
            if (radius <= 0f || segments < 8)
            {
                return;
            }

            var prev = center + new Vector3(radius, 0f, 0f);
            for (var i = 1; i <= segments; i++)
            {
                var angle = (i / (float)segments) * Mathf.PI * 2f;
                var next = center + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
                Gizmos.DrawLine(prev, next);
                prev = next;
            }
        }
#endif
    }
}
