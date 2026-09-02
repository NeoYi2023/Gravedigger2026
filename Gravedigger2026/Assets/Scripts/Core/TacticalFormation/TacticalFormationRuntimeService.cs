using System;
using System.Collections.Generic;
using Gravedigger2026.Core.Combat;
using Gravedigger2026.Core.Config;
using UnityEngine;

namespace Gravedigger2026.Core.TacticalFormation
{
    /// <summary>
    /// PushMap integrates along a FlowField sample; Defend holds the locked deploy center
    /// (SPEC_03 §3.18 / SPEC_04 §9.7 Approach A).
    /// </summary>
    public enum TacticalFormationCenterMode
    {
        Hold = 0,
        FollowFlowField = 1
    }

    public enum TacticalFormationMemberLostReason
    {
        CombatDead = 0,
        Rebel = 1
    }

    public readonly struct TacticalFormationMemberLostResult
    {
        public readonly bool SquadDissolved;
        public readonly string FormationId;
        public readonly string[] OverlayRemovedWarriorIds;

        public TacticalFormationMemberLostResult(
            bool squadDissolved,
            string formationId,
            string[] overlayRemovedWarriorIds)
        {
            SquadDissolved = squadDissolved;
            FormationId = formationId;
            OverlayRemovedWarriorIds = overlayRemovedWarriorIds ?? Array.Empty<string>();
        }
    }

    /// <summary>
    /// Combat lock for one tactical formation (copied from Prepare; not persisted).
    /// </summary>
    public readonly struct TacticalFormationCombatLock
    {
        public readonly string FormationId;
        public readonly string[] MemberIds;
        public readonly Vector2[] SlotLocalXZ;
        public readonly TacticalFormationMoveParams MoveParams;
        public readonly Vector2 CenterXZ;
        public readonly float FacingYawDegrees;
        public readonly int MinMemberCount;
        public readonly CombatStatMulBuff StatMul;
        public readonly string[] ExclusiveSkillIds;
        public readonly string[] ExclusiveSkillEffectIds;

        public TacticalFormationCombatLock(
            string formationId,
            string[] memberIds,
            Vector2[] slotLocalXZ,
            TacticalFormationMoveParams moveParams,
            Vector2 centerXZ,
            float facingYawDegrees)
            : this(
                formationId,
                memberIds,
                slotLocalXZ,
                moveParams,
                centerXZ,
                facingYawDegrees,
                1,
                CombatStatMulBuff.Identity,
                Array.Empty<string>(),
                Array.Empty<string>())
        {
        }

        public TacticalFormationCombatLock(
            string formationId,
            string[] memberIds,
            Vector2[] slotLocalXZ,
            TacticalFormationMoveParams moveParams,
            Vector2 centerXZ,
            float facingYawDegrees,
            int minMemberCount,
            CombatStatMulBuff statMul,
            string[] exclusiveSkillIds,
            string[] exclusiveSkillEffectIds)
        {
            FormationId = formationId;
            MemberIds = memberIds ?? Array.Empty<string>();
            SlotLocalXZ = slotLocalXZ ?? Array.Empty<Vector2>();
            MoveParams = moveParams;
            CenterXZ = centerXZ;
            FacingYawDegrees = facingYawDegrees;
            MinMemberCount = minMemberCount < 1 ? 1 : minMemberCount;
            StatMul = statMul;
            ExclusiveSkillIds = exclusiveSkillIds ?? Array.Empty<string>();
            ExclusiveSkillEffectIds = exclusiveSkillEffectIds ?? Array.Empty<string>();
        }
    }

    /// <summary>
    /// Combat virtual-center + slot world points + leash + overlay session (SPEC_03 §3.18 / TF-04a / TF-05).
    /// Center is pure data — not a <c>MassMoveScheduler</c> agent (no SoftCollision ghost).
    /// </summary>
    public sealed class TacticalFormationRuntimeService : ITacticalFormationOverlayLookup
    {
        private readonly Dictionary<string, CombatSquad> _squads =
            new Dictionary<string, CombatSquad>(StringComparer.Ordinal);

        private readonly Dictionary<string, MemberRef> _memberIndex =
            new Dictionary<string, MemberRef>(StringComparer.Ordinal);

        private readonly List<TacticalFormationSquadSnapshot> _layoutScratch =
            new List<TacticalFormationSquadSnapshot>(8);

        private readonly List<string> _lostScratch = new List<string>(8);

        private TacticalFormationCenterMode _centerMode = TacticalFormationCenterMode.Hold;
        private float _representativeMoveSpeed;

        public TacticalFormationCenterMode CenterMode => _centerMode;
        public int SquadCount => _squads.Count;
        public int MemberCount => _memberIndex.Count;

        public void Clear()
        {
            _squads.Clear();
            _memberIndex.Clear();
            _layoutScratch.Clear();
            _lostScratch.Clear();
            _centerMode = TacticalFormationCenterMode.Hold;
            _representativeMoveSpeed = 0f;
        }

        public void OnStartBattle(
            IReadOnlyList<TacticalFormationCombatLock> locks,
            TacticalFormationCenterMode centerMode,
            float representativeMoveSpeed)
        {
            Clear();
            _centerMode = centerMode;
            _representativeMoveSpeed = Mathf.Max(0f, representativeMoveSpeed);
            if (locks == null)
            {
                return;
            }

            for (var i = 0; i < locks.Count; i++)
            {
                TryAddLock(locks[i]);
            }
        }

        public void OnStartBattle(
            TacticalFormationLayoutService layout,
            ConfigCsvRepository configs,
            ITacticalFormationPatternLookup patterns,
            TacticalFormationCenterMode centerMode,
            float representativeMoveSpeed)
        {
            var locks = BuildLocks(layout, configs, patterns, _layoutScratch);
            OnStartBattle(locks, centerMode, representativeMoveSpeed);
        }

        public void Tick(float dt, Vector2 flowFieldDirXZ)
        {
            if (dt <= 0f || _squads.Count == 0)
            {
                return;
            }

            foreach (var kv in _squads)
            {
                kv.Value.Tick(dt, _centerMode, _representativeMoveSpeed, flowFieldDirXZ);
            }
        }

        /// <summary>
        /// SearchExtract gather Objective is the formation center (SPEC_03 §3.19 / SE-05).
        /// </summary>
        public void SnapAllCentersTo(Vector2 objectiveCenterXZ)
        {
            foreach (var kv in _squads)
            {
                var squad = kv.Value;
                if (squad == null)
                {
                    continue;
                }

                squad.CenterXZ = objectiveCenterXZ;
            }
        }

        public bool IsMember(string warriorId)
        {
            return !string.IsNullOrEmpty(warriorId) && _memberIndex.ContainsKey(warriorId);
        }

        public bool IsOverlayActive(string warriorId)
        {
            return TryGetSquadForMember(warriorId, out var squad, out _)
                   && squad.OverlayActive;
        }

        public bool TryGetStatMul(string warriorId, out CombatStatMulBuff statMul)
        {
            statMul = CombatStatMulBuff.Identity;
            if (!TryGetSquadForMember(warriorId, out var squad, out _) || !squad.OverlayActive)
            {
                return false;
            }

            statMul = squad.StatMul;
            return true;
        }

        public IReadOnlyList<string> GetExclusiveSkillIds(string warriorId)
        {
            if (!TryGetSquadForMember(warriorId, out var squad, out _) || !squad.OverlayActive)
            {
                return Array.Empty<string>();
            }

            return squad.ExclusiveSkillIds;
        }

        public IReadOnlyList<string> GetExclusiveSkillEffectIds(string warriorId)
        {
            if (!TryGetSquadForMember(warriorId, out var squad, out _) || !squad.OverlayActive)
            {
                return Array.Empty<string>();
            }

            return squad.ExclusiveSkillEffectIds;
        }

        /// <summary>
        /// Rebel / CombatDead: drop the member; dissolve the squad when living count &lt; Min.
        /// Overlay-removed ids are remaining living members on dissolve, or the rebel on leave.
        /// </summary>
        public bool TryNotifyMemberLost(
            string warriorId,
            TacticalFormationMemberLostReason reason,
            out TacticalFormationMemberLostResult result)
        {
            result = default;
            if (string.IsNullOrEmpty(warriorId)
                || !_memberIndex.TryGetValue(warriorId, out var memberRef)
                || !_squads.TryGetValue(memberRef.FormationId, out var squad)
                || squad == null)
            {
                return false;
            }

            _memberIndex.Remove(warriorId);
            squad.RemoveActive(warriorId);

            _lostScratch.Clear();
            var dissolved = false;
            if (squad.ActiveMemberCount < squad.MinMemberCount)
            {
                squad.CollectActive(_lostScratch);
                for (var i = 0; i < _lostScratch.Count; i++)
                {
                    _memberIndex.Remove(_lostScratch[i]);
                }

                squad.OverlayActive = false;
                _squads.Remove(squad.FormationId);
                dissolved = true;
                Debug.Log(
                    $"[TacticalFormation] Dissolve {squad.FormationId} remaining={_lostScratch.Count} " +
                    $"< Min={squad.MinMemberCount} trigger={warriorId} reason={reason}");
            }
            else if (reason == TacticalFormationMemberLostReason.Rebel && squad.OverlayActive)
            {
                _lostScratch.Add(warriorId);
                Debug.Log(
                    $"[TacticalFormation] Rebel leave {warriorId} formation={squad.FormationId} " +
                    $"living={squad.ActiveMemberCount}");
            }

            var removed = _lostScratch.Count == 0
                ? Array.Empty<string>()
                : _lostScratch.ToArray();
            result = new TacticalFormationMemberLostResult(dissolved, squad.FormationId, removed);
            return true;
        }

        public bool TryGetSlotWorldXZ(string warriorId, out Vector2 worldXZ)
        {
            worldXZ = default;
            if (!TryGetSquadForMember(warriorId, out var squad, out var slotIndex))
            {
                return false;
            }

            worldXZ = squad.SlotWorldXZ(slotIndex);
            return true;
        }

        public bool TryGetCenterXZ(string warriorId, out Vector2 centerXZ)
        {
            centerXZ = default;
            if (!TryGetSquadForMember(warriorId, out var squad, out _))
            {
                return false;
            }

            centerXZ = squad.CenterXZ;
            return true;
        }

        public bool TryGetAnyCenterXZ(out Vector2 centerXZ)
        {
            centerXZ = default;
            foreach (var kv in _squads)
            {
                if (kv.Value == null)
                {
                    continue;
                }

                centerXZ = kv.Value.CenterXZ;
                return true;
            }

            return false;
        }

        public bool TryGetMoveParams(string warriorId, out TacticalFormationMoveParams moveParams)
        {
            moveParams = TacticalFormationMoveParams.CreateDefault();
            if (!TryGetSquadForMember(warriorId, out var squad, out _))
            {
                return false;
            }

            moveParams = squad.MoveParams;
            return true;
        }

        public bool TryGetFacingYawDegrees(string warriorId, out float yawDegrees)
        {
            yawDegrees = 0f;
            if (!TryGetSquadForMember(warriorId, out var squad, out _))
            {
                return false;
            }

            yawDegrees = squad.FacingYawDegrees;
            return true;
        }

        /// <summary>
        /// Project <paramref name="worldXZ"/> onto the leash circle around
        /// <paramref name="centerXZ"/>. Radius ≤ 0 falls back to DefaultLeashRadius.
        /// </summary>
        public static Vector2 ClampToLeash(Vector2 centerXZ, Vector2 worldXZ, float leashRadius)
        {
            var radius = leashRadius > 0f
                ? leashRadius
                : TacticalFormationMoveParams.DefaultLeashRadius;
            var delta = worldXZ - centerXZ;
            var distSq = delta.sqrMagnitude;
            if (distSq <= radius * radius || distSq < 1e-12f)
            {
                return worldXZ;
            }

            return centerXZ + delta * (radius / Mathf.Sqrt(distSq));
        }

        public bool TryClampMemberAttackSlot(string warriorId, Vector2 attackSlotWorldXZ, out Vector2 clampedXZ)
        {
            clampedXZ = attackSlotWorldXZ;
            if (!TryGetSquadForMember(warriorId, out var squad, out _))
            {
                return false;
            }

            clampedXZ = ClampToLeash(squad.CenterXZ, attackSlotWorldXZ, squad.MoveParams.LeashRadius);
            return true;
        }

        public bool TryIsWorldInsideLeash(string warriorId, Vector2 worldXZ)
        {
            if (!TryGetSquadForMember(warriorId, out var squad, out _))
            {
                return false;
            }

            var radius = squad.MoveParams.LeashRadius;
            if (radius <= 0f)
            {
                radius = TacticalFormationMoveParams.DefaultLeashRadius;
            }

            return (worldXZ - squad.CenterXZ).sqrMagnitude <= radius * radius;
        }

        public static Vector2 RotateYaw(Vector2 localXZ, float yawDegrees)
        {
            var world = Quaternion.Euler(0f, yawDegrees, 0f) * new Vector3(localXZ.x, 0f, localXZ.y);
            return new Vector2(world.x, world.z);
        }

        public static List<TacticalFormationCombatLock> BuildLocks(
            TacticalFormationLayoutService layout,
            ConfigCsvRepository configs,
            ITacticalFormationPatternLookup patterns,
            List<TacticalFormationSquadSnapshot> scratch = null)
        {
            var result = new List<TacticalFormationCombatLock>(4);
            if (layout == null)
            {
                return result;
            }

            if (scratch == null)
            {
                scratch = new List<TacticalFormationSquadSnapshot>(8);
            }

            layout.CollectActiveSquads(scratch);
            for (var i = 0; i < scratch.Count; i++)
            {
                var squad = scratch[i];
                if (squad == null || string.IsNullOrEmpty(squad.FormationId))
                {
                    continue;
                }

                if (configs == null
                    || !configs.IsLoaded
                    || !configs.TryGetTacticalFormation(squad.FormationId, out var row)
                    || row == null)
                {
                    Debug.LogWarning(
                        $"[TacticalFormationRuntime] FormationId '{squad.FormationId}' missing config — skip lock.");
                    continue;
                }

                if (patterns == null
                    || !patterns.TryGetSlotLocalXZ(row.PrefabId, out var slots)
                    || slots == null
                    || slots.Length == 0)
                {
                    Debug.LogWarning(
                        $"[TacticalFormationRuntime] PrefabId '{row.PrefabId}' for {squad.FormationId} missing slots — skip lock.");
                    continue;
                }

                var moveParams = TacticalFormationMoveParams.CreateDefault();
                if (patterns.TryGetMoveParams(row.PrefabId, out var fromPattern))
                {
                    moveParams = fromPattern;
                }

                var members = squad.MemberIds ?? Array.Empty<string>();
                var take = Mathf.Min(members.Length, slots.Length);
                if (take <= 0)
                {
                    continue;
                }

                var ids = new string[take];
                var locals = new Vector2[take];
                for (var s = 0; s < take; s++)
                {
                    ids[s] = members[s];
                    var local = slots[s];
                    locals[s] = new Vector2(local.x, local.z);
                }

                result.Add(new TacticalFormationCombatLock(
                    squad.FormationId,
                    ids,
                    locals,
                    moveParams,
                    new Vector2(squad.CenterX, squad.CenterZ),
                    squad.FacingYawDegrees,
                    row.MinMemberCount,
                    TacticalFormationStatOverlay.Parse(row.StatModifiers, squad.FormationId),
                    row.ExclusiveSkillIds,
                    row.ExclusiveSkillEffectIds));
            }

            return result;
        }

        private void TryAddLock(TacticalFormationCombatLock lockData)
        {
            if (string.IsNullOrEmpty(lockData.FormationId)
                || lockData.MemberIds == null
                || lockData.MemberIds.Length == 0
                || lockData.SlotLocalXZ == null
                || lockData.SlotLocalXZ.Length == 0)
            {
                return;
            }

            var take = Mathf.Min(lockData.MemberIds.Length, lockData.SlotLocalXZ.Length);
            if (take <= 0)
            {
                return;
            }

            if (_squads.ContainsKey(lockData.FormationId))
            {
                Debug.LogWarning(
                    $"[TacticalFormationRuntime] Duplicate FormationId '{lockData.FormationId}' — Demo max 1 instance, skip.");
                return;
            }

            var ids = new string[take];
            var locals = new Vector2[take];
            Array.Copy(lockData.MemberIds, ids, take);
            Array.Copy(lockData.SlotLocalXZ, locals, take);

            var squad = new CombatSquad(
                lockData.FormationId,
                ids,
                locals,
                lockData.MoveParams,
                lockData.CenterXZ,
                lockData.FacingYawDegrees,
                lockData.MinMemberCount,
                lockData.StatMul,
                lockData.ExclusiveSkillIds,
                lockData.ExclusiveSkillEffectIds);
            _squads[lockData.FormationId] = squad;

            for (var i = 0; i < take; i++)
            {
                var id = ids[i];
                if (string.IsNullOrEmpty(id))
                {
                    continue;
                }

                if (_memberIndex.ContainsKey(id))
                {
                    Debug.LogWarning(
                        $"[TacticalFormationRuntime] Warrior '{id}' already in a squad — skip duplicate.");
                    continue;
                }

                _memberIndex[id] = new MemberRef(lockData.FormationId, i);
                squad.AddActive(id);
            }

            if (squad.OverlayActive)
            {
                Debug.Log(
                    $"[TacticalFormation] Overlay ON {lockData.FormationId} members={squad.ActiveMemberCount} " +
                    $"Stat={lockData.StatMul} Skills={lockData.ExclusiveSkillIds.Length} " +
                    $"Effects={lockData.ExclusiveSkillEffectIds.Length}");
            }
        }

        private bool TryGetSquadForMember(string warriorId, out CombatSquad squad, out int slotIndex)
        {
            squad = null;
            slotIndex = -1;
            if (string.IsNullOrEmpty(warriorId)
                || !_memberIndex.TryGetValue(warriorId, out var memberRef)
                || !_squads.TryGetValue(memberRef.FormationId, out squad)
                || squad == null)
            {
                return false;
            }

            slotIndex = memberRef.SlotIndex;
            return true;
        }

        private readonly struct MemberRef
        {
            public readonly string FormationId;
            public readonly int SlotIndex;

            public MemberRef(string formationId, int slotIndex)
            {
                FormationId = formationId;
                SlotIndex = slotIndex;
            }
        }

        private sealed class CombatSquad
        {
            public readonly string FormationId;
            public readonly string[] MemberIds;
            public readonly Vector2[] SlotLocalXZ;
            public readonly TacticalFormationMoveParams MoveParams;
            public readonly int MinMemberCount;
            public readonly CombatStatMulBuff StatMul;
            public readonly string[] ExclusiveSkillIds;
            public readonly string[] ExclusiveSkillEffectIds;
            public Vector2 CenterXZ;
            public float FacingYawDegrees;
            public bool OverlayActive = true;

            private readonly HashSet<string> _activeMembers =
                new HashSet<string>(StringComparer.Ordinal);

            public int ActiveMemberCount => _activeMembers.Count;

            public CombatSquad(
                string formationId,
                string[] memberIds,
                Vector2[] slotLocalXZ,
                TacticalFormationMoveParams moveParams,
                Vector2 centerXZ,
                float facingYawDegrees,
                int minMemberCount,
                CombatStatMulBuff statMul,
                string[] exclusiveSkillIds,
                string[] exclusiveSkillEffectIds)
            {
                FormationId = formationId;
                MemberIds = memberIds;
                SlotLocalXZ = slotLocalXZ;
                MoveParams = moveParams;
                CenterXZ = centerXZ;
                FacingYawDegrees = facingYawDegrees;
                MinMemberCount = minMemberCount < 1 ? 1 : minMemberCount;
                StatMul = statMul;
                ExclusiveSkillIds = exclusiveSkillIds ?? Array.Empty<string>();
                ExclusiveSkillEffectIds = exclusiveSkillEffectIds ?? Array.Empty<string>();
            }

            public void AddActive(string warriorId)
            {
                if (!string.IsNullOrEmpty(warriorId))
                {
                    _activeMembers.Add(warriorId);
                }
            }

            public void RemoveActive(string warriorId)
            {
                if (!string.IsNullOrEmpty(warriorId))
                {
                    _activeMembers.Remove(warriorId);
                }
            }

            public void CollectActive(List<string> dst)
            {
                dst.Clear();
                foreach (var id in _activeMembers)
                {
                    dst.Add(id);
                }
            }

            public Vector2 SlotWorldXZ(int slotIndex)
            {
                if (slotIndex < 0 || slotIndex >= SlotLocalXZ.Length)
                {
                    return CenterXZ;
                }

                return CenterXZ + RotateYaw(SlotLocalXZ[slotIndex], FacingYawDegrees);
            }

            public void Tick(
                float dt,
                TacticalFormationCenterMode centerMode,
                float representativeMoveSpeed,
                Vector2 flowFieldDirXZ)
            {
                if (centerMode != TacticalFormationCenterMode.FollowFlowField)
                {
                    return;
                }

                if (flowFieldDirXZ.sqrMagnitude < 1e-8f)
                {
                    return;
                }

                var dir = flowFieldDirXZ.normalized;
                var speed = representativeMoveSpeed * MoveParams.CenterMoveSpeedMul;
                if (speed > 0f)
                {
                    CenterXZ += dir * (speed * dt);
                }

                if (MoveParams.FacingTurnRate <= 0f)
                {
                    return;
                }

                var targetYaw = Mathf.Atan2(dir.x, dir.y) * Mathf.Rad2Deg;
                FacingYawDegrees = Mathf.MoveTowardsAngle(
                    FacingYawDegrees,
                    targetYaw,
                    MoveParams.FacingTurnRate * dt);
            }
        }
    }
}
