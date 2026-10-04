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
        public readonly string[] OverlayRefreshedWarriorIds;

        public TacticalFormationMemberLostResult(
            bool squadDissolved,
            string formationId,
            string[] overlayRemovedWarriorIds,
            string[] overlayRefreshedWarriorIds = null)
        {
            SquadDissolved = squadDissolved;
            FormationId = formationId;
            OverlayRemovedWarriorIds = overlayRemovedWarriorIds ?? Array.Empty<string>();
            OverlayRefreshedWarriorIds = overlayRefreshedWarriorIds ?? Array.Empty<string>();
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
        public readonly string GroupInstanceId;
        public readonly int[] MemberClassLevels;

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
            string[] exclusiveSkillEffectIds,
            string groupInstanceId = null,
            int[] memberClassLevels = null)
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
            GroupInstanceId = groupInstanceId ?? string.Empty;
            MemberClassLevels = memberClassLevels;
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
        private readonly List<string> _refreshScratch = new List<string>(8);
        private readonly List<SlotVacancy> _pendingSlotFills = new List<SlotVacancy>(8);

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
            _refreshScratch.Clear();
            _pendingSlotFills.Clear();
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
            FlushSlotFills();
            TickSquads(dt, flowFieldDirXZ, null);
        }

        /// <summary>
        /// Each group samples FlowField at its own center (PushMap). Defend / SearchExtract keep Hold.
        /// </summary>
        public void Tick(float dt, Func<Vector2, Vector2> sampleDirAtCenter)
        {
            FlushSlotFills();
            TickSquads(dt, default, sampleDirAtCenter);
        }

        /// <summary>
        /// First living member MoveSpeed per group. Squads with no speed keep the service-wide fallback.
        /// </summary>
        public void AssignRepresentativeMoveSpeeds(Func<string, float> moveSpeedOf)
        {
            if (moveSpeedOf == null)
            {
                return;
            }

            foreach (var kv in _squads)
            {
                kv.Value?.AssignRepresentativeMoveSpeed(moveSpeedOf);
            }
        }

        /// <summary>
        /// SearchExtract gather countdown: shift each locked center so the StartBattle
        /// army centroid lands on the objective. Repeated calls use the original lock
        /// center and do not accumulate (SPEC_03 §3.19).
        /// </summary>
        public void PlaceCentersPreservingLayout(
            Vector2 objectiveWorldXZ,
            Vector2 deployAnchorWorldXZ,
            Vector2 mapCenterXZ)
        {
            var delta = objectiveWorldXZ - deployAnchorWorldXZ;
            foreach (var kv in _squads)
            {
                var squad = kv.Value;
                if (squad == null)
                {
                    continue;
                }

                var originWorld = mapCenterXZ + squad.OriginCenterMapRel;
                squad.CenterXZ = originWorld + delta;
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
            out TacticalFormationMemberLostResult result,
            ConfigCsvRepository configs = null)
        {
            result = default;
            if (string.IsNullOrEmpty(warriorId)
                || !_memberIndex.TryGetValue(warriorId, out var memberRef)
                || !_squads.TryGetValue(memberRef.GroupKey, out var squad)
                || squad == null)
            {
                return false;
            }

            var hadOverlay = squad.OverlayActive;
            _memberIndex.Remove(warriorId);
            squad.RemoveActive(warriorId);

            _lostScratch.Clear();
            _refreshScratch.Clear();
            var dissolved = false;
            if (squad.ActiveMemberCount < squad.MinMemberCount)
            {
                squad.CollectActive(_lostScratch);
                for (var i = 0; i < _lostScratch.Count; i++)
                {
                    _memberIndex.Remove(_lostScratch[i]);
                }

                squad.OverlayActive = false;
                _squads.Remove(squad.GroupKey);
                dissolved = true;
                Debug.Log(
                    $"[TacticalFormation] Dissolve group={squad.GroupKey} formation={squad.FormationId} " +
                    $"remaining={_lostScratch.Count} < Min={squad.MinMemberCount} trigger={warriorId} reason={reason}");
            }
            else if (squad.TrySwapOverlay(configs, out var changed) && changed)
            {
                squad.CollectActive(_refreshScratch);
            }

            if (reason == TacticalFormationMemberLostReason.Rebel && hadOverlay)
            {
                _lostScratch.Add(warriorId);
                Debug.Log(
                    $"[TacticalFormation] Rebel leave {warriorId} group={squad.GroupKey} " +
                    $"formation={squad.FormationId} living={squad.ActiveMemberCount}");
            }

            if (!dissolved)
            {
                _pendingSlotFills.Add(new SlotVacancy(memberRef.GroupKey, memberRef.SlotIndex));
            }

            var removed = _lostScratch.Count == 0
                ? Array.Empty<string>()
                : _lostScratch.ToArray();
            var refreshed = _refreshScratch.Count == 0
                ? Array.Empty<string>()
                : _refreshScratch.ToArray();
            result = new TacticalFormationMemberLostResult(dissolved, squad.FormationId, removed, refreshed);
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

        /// <summary>D-095: group facing 0° is world +Z (north), same as slot rotation.</summary>
        public static Vector3 FacingYawToWorldForward(float yawDegrees)
        {
            var xz = RotateYaw(new Vector2(0f, 1f), yawDegrees);
            return new Vector3(xz.x, 0f, xz.y);
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
                var classLevels = new int[take];
                var sourceLevels = squad.MemberClassLevels;
                for (var s = 0; s < take; s++)
                {
                    ids[s] = members[s];
                    var local = slots[s];
                    locals[s] = new Vector2(local.x, local.z);
                    if (sourceLevels != null && s < sourceLevels.Length)
                    {
                        classLevels[s] = sourceLevels[s];
                    }
                }

                var matched = squad.MatchedLevelRow;
                var statMul = CombatStatMulBuff.Identity;
                var exclusiveSkills = Array.Empty<string>();
                var exclusiveEffects = Array.Empty<string>();
                if (matched == null)
                {
                    Debug.LogWarning(
                        $"[TacticalFormationRuntime] Group '{squad.GroupInstanceId}' FormationId '{squad.FormationId}' " +
                        $"computed level {squad.ComputedLevel} has no FormationLevel row — lock with empty stats.");
                }
                else
                {
                    statMul = TacticalFormationStatOverlay.Parse(matched.StatModifiers, squad.FormationId);
                    exclusiveSkills = matched.ExclusiveSkillIds ?? Array.Empty<string>();
                    exclusiveEffects = matched.ExclusiveSkillEffectIds ?? Array.Empty<string>();
                }

                result.Add(new TacticalFormationCombatLock(
                    squad.FormationId,
                    ids,
                    locals,
                    moveParams,
                    new Vector2(squad.CenterX, squad.CenterZ),
                    squad.FacingYawDegrees,
                    row.MinMemberCount,
                    statMul,
                    exclusiveSkills,
                    exclusiveEffects,
                    squad.GroupInstanceId,
                    classLevels));
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

            var groupKey = string.IsNullOrEmpty(lockData.GroupInstanceId)
                ? lockData.FormationId
                : lockData.GroupInstanceId;
            if (string.IsNullOrEmpty(groupKey))
            {
                return;
            }

            if (_squads.ContainsKey(groupKey))
            {
                Debug.LogWarning(
                    $"[TacticalFormationRuntime] Duplicate group '{groupKey}' — skip.");
                return;
            }

            var ids = new string[take];
            var locals = new Vector2[take];
            var classLevels = lockData.MemberClassLevels;
            Array.Copy(lockData.MemberIds, ids, take);
            Array.Copy(lockData.SlotLocalXZ, locals, take);

            var squad = new CombatSquad(
                groupKey,
                lockData.FormationId,
                ids,
                locals,
                lockData.MoveParams,
                lockData.CenterXZ,
                lockData.FacingYawDegrees,
                lockData.MinMemberCount,
                lockData.StatMul,
                lockData.ExclusiveSkillIds,
                lockData.ExclusiveSkillEffectIds,
                classLevels);
            _squads[groupKey] = squad;

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

                _memberIndex[id] = new MemberRef(groupKey, i);
                squad.AddActive(id);
            }

            if (squad.OverlayActive)
            {
                Debug.Log(
                    $"[TacticalFormation] Overlay ON group={groupKey} formation={lockData.FormationId} " +
                    $"members={squad.ActiveMemberCount} Stat={lockData.StatMul} " +
                    $"Skills={lockData.ExclusiveSkillIds.Length} Effects={lockData.ExclusiveSkillEffectIds.Length}");
            }
        }

        /// <summary>
        /// D-094: move the living member with the highest slot index into each vacated
        /// lower index, lowest vacancy first. No-op when the vacated index is already highest.
        /// </summary>
        public void FlushSlotFills()
        {
            if (_pendingSlotFills.Count == 0)
            {
                return;
            }

            _pendingSlotFills.Sort(CompareVacancy);
            for (var i = 0; i < _pendingSlotFills.Count; i++)
            {
                var vacancy = _pendingSlotFills[i];
                if (!_squads.ContainsKey(vacancy.GroupKey))
                {
                    continue;
                }

                if (!TryFindHighestActiveSlot(vacancy.GroupKey, out var highestId, out var highestSlot)
                    || highestSlot <= vacancy.SlotIndex)
                {
                    continue;
                }

                _memberIndex[highestId] = new MemberRef(vacancy.GroupKey, vacancy.SlotIndex);
            }

            _pendingSlotFills.Clear();
        }

        public bool TryGetSlotIndex(string warriorId, out int slotIndex)
        {
            slotIndex = -1;
            if (!TryGetSquadForMember(warriorId, out _, out slotIndex))
            {
                slotIndex = -1;
                return false;
            }

            return true;
        }

        private void TickSquads(float dt, Vector2 uniformDir, Func<Vector2, Vector2> sampleDirAtCenter)
        {
            if (dt <= 0f || _squads.Count == 0)
            {
                return;
            }

            foreach (var kv in _squads)
            {
                var squad = kv.Value;
                if (squad == null)
                {
                    continue;
                }

                var dir = sampleDirAtCenter != null ? sampleDirAtCenter(squad.CenterXZ) : uniformDir;
                var speed = squad.RepresentativeMoveSpeed > 0.01f
                    ? squad.RepresentativeMoveSpeed
                    : _representativeMoveSpeed;
                squad.Tick(dt, _centerMode, speed, dir);
            }
        }

        private bool TryGetSquadForMember(string warriorId, out CombatSquad squad, out int slotIndex)
        {
            squad = null;
            slotIndex = -1;
            if (string.IsNullOrEmpty(warriorId)
                || !_memberIndex.TryGetValue(warriorId, out var memberRef)
                || !_squads.TryGetValue(memberRef.GroupKey, out squad)
                || squad == null)
            {
                return false;
            }

            slotIndex = memberRef.SlotIndex;
            return true;
        }

        private readonly struct SlotVacancy
        {
            public readonly string GroupKey;
            public readonly int SlotIndex;

            public SlotVacancy(string groupKey, int slotIndex)
            {
                GroupKey = groupKey;
                SlotIndex = slotIndex;
            }
        }

        private static int CompareVacancy(SlotVacancy a, SlotVacancy b)
        {
            var group = string.CompareOrdinal(a.GroupKey, b.GroupKey);
            if (group != 0)
            {
                return group;
            }

            return a.SlotIndex.CompareTo(b.SlotIndex);
        }

        private bool TryFindHighestActiveSlot(string groupKey, out string warriorId, out int slotIndex)
        {
            warriorId = null;
            slotIndex = -1;
            foreach (var kv in _memberIndex)
            {
                if (!string.Equals(kv.Value.GroupKey, groupKey, StringComparison.Ordinal))
                {
                    continue;
                }

                if (kv.Value.SlotIndex > slotIndex)
                {
                    slotIndex = kv.Value.SlotIndex;
                    warriorId = kv.Key;
                }
            }

            return !string.IsNullOrEmpty(warriorId);
        }

        private readonly struct MemberRef
        {
            public readonly string GroupKey;
            public readonly int SlotIndex;

            public MemberRef(string groupKey, int slotIndex)
            {
                GroupKey = groupKey;
                SlotIndex = slotIndex;
            }
        }

        private sealed class CombatSquad
        {
            public readonly string GroupKey;
            public readonly string FormationId;
            public readonly string[] MemberIds;
            public readonly Vector2[] SlotLocalXZ;
            public readonly TacticalFormationMoveParams MoveParams;
            public readonly int MinMemberCount;
            public CombatStatMulBuff StatMul;
            public string[] ExclusiveSkillIds;
            public string[] ExclusiveSkillEffectIds;
            public Vector2 CenterXZ;

            /// <summary>Combat-lock center in map-relative XZ. Never updated by relocate.</summary>
            public readonly Vector2 OriginCenterMapRel;

            public float FacingYawDegrees;
            public float RepresentativeMoveSpeed;
            public bool OverlayActive = true;

            private readonly HashSet<string> _activeMembers =
                new HashSet<string>(StringComparer.Ordinal);

            private readonly Dictionary<string, int> _classLevelByMember =
                new Dictionary<string, int>(StringComparer.Ordinal);

            private readonly bool _hasClassLevels;

            public int ActiveMemberCount => _activeMembers.Count;

            public CombatSquad(
                string groupKey,
                string formationId,
                string[] memberIds,
                Vector2[] slotLocalXZ,
                TacticalFormationMoveParams moveParams,
                Vector2 centerXZ,
                float facingYawDegrees,
                int minMemberCount,
                CombatStatMulBuff statMul,
                string[] exclusiveSkillIds,
                string[] exclusiveSkillEffectIds,
                int[] memberClassLevels)
            {
                GroupKey = groupKey;
                FormationId = formationId;
                MemberIds = memberIds;
                SlotLocalXZ = slotLocalXZ;
                MoveParams = moveParams;
                CenterXZ = centerXZ;
                OriginCenterMapRel = centerXZ;
                FacingYawDegrees = facingYawDegrees;
                MinMemberCount = minMemberCount < 1 ? 1 : minMemberCount;
                StatMul = statMul;
                ExclusiveSkillIds = exclusiveSkillIds ?? Array.Empty<string>();
                ExclusiveSkillEffectIds = exclusiveSkillEffectIds ?? Array.Empty<string>();
                _hasClassLevels = memberClassLevels != null;
                if (!_hasClassLevels || memberIds == null)
                {
                    return;
                }

                var n = Math.Min(memberIds.Length, memberClassLevels.Length);
                for (var i = 0; i < n; i++)
                {
                    var id = memberIds[i];
                    if (!string.IsNullOrEmpty(id))
                    {
                        _classLevelByMember[id] = memberClassLevels[i];
                    }
                }
            }

            public void AssignRepresentativeMoveSpeed(Func<string, float> moveSpeedOf)
            {
                RepresentativeMoveSpeed = 0f;
                if (moveSpeedOf == null || MemberIds == null)
                {
                    return;
                }

                for (var i = 0; i < MemberIds.Length; i++)
                {
                    var id = MemberIds[i];
                    if (string.IsNullOrEmpty(id) || !_activeMembers.Contains(id))
                    {
                        continue;
                    }

                    var speed = moveSpeedOf(id);
                    if (speed > 0.01f)
                    {
                        RepresentativeMoveSpeed = speed;
                        return;
                    }
                }
            }

            /// <summary>
            /// Recompute Floor(mean ClassLevel) of living members and swap the level-row overlay.
            /// No class levels or configs → leave the locked row (legacy single-squad locks).
            /// </summary>
            public bool TrySwapOverlay(ConfigCsvRepository configs, out bool changed)
            {
                changed = false;
                if (!_hasClassLevels || configs == null || !configs.IsLoaded)
                {
                    return false;
                }

                var sum = 0;
                var count = 0;
                foreach (var id in _activeMembers)
                {
                    count++;
                    if (_classLevelByMember.TryGetValue(id, out var level))
                    {
                        sum += level;
                    }
                }

                var computed = count > 0 ? sum / count : 0;
                var nextStat = CombatStatMulBuff.Identity;
                var nextSkills = Array.Empty<string>();
                var nextEffects = Array.Empty<string>();
                if (!configs.TryGetTacticalFormationForComputedLevel(FormationId, computed, out var row)
                    || row == null)
                {
                    Debug.LogWarning(
                        $"[TacticalFormation] Group '{GroupKey}' FormationId '{FormationId}' " +
                        $"computed level {computed} has no FormationLevel row — stats empty.");
                }
                else
                {
                    nextStat = TacticalFormationStatOverlay.Parse(row.StatModifiers, FormationId);
                    nextSkills = row.ExclusiveSkillIds ?? Array.Empty<string>();
                    nextEffects = row.ExclusiveSkillEffectIds ?? Array.Empty<string>();
                }

                changed = !SameStat(StatMul, nextStat)
                          || !SameIds(ExclusiveSkillIds, nextSkills)
                          || !SameIds(ExclusiveSkillEffectIds, nextEffects);
                if (!changed)
                {
                    return true;
                }

                StatMul = nextStat;
                ExclusiveSkillIds = nextSkills;
                ExclusiveSkillEffectIds = nextEffects;
                Debug.Log(
                    $"[TacticalFormation] Overlay swap group={GroupKey} formation={FormationId} " +
                    $"level={computed} living={count} Stat={nextStat}");
                return true;
            }

            private static bool SameStat(CombatStatMulBuff a, CombatStatMulBuff b)
            {
                return Mathf.Approximately(a.MaxHpBodyLifeMul, b.MaxHpBodyLifeMul)
                       && Mathf.Approximately(a.StrengthMul, b.StrengthMul)
                       && Mathf.Approximately(a.AgilityMul, b.AgilityMul)
                       && Mathf.Approximately(a.IntelligenceMul, b.IntelligenceMul)
                       && Mathf.Approximately(a.MoveSpeedMul, b.MoveSpeedMul);
            }

            private static bool SameIds(string[] a, string[] b)
            {
                a ??= Array.Empty<string>();
                b ??= Array.Empty<string>();
                if (a.Length != b.Length)
                {
                    return false;
                }

                for (var i = 0; i < a.Length; i++)
                {
                    if (!string.Equals(a[i], b[i], StringComparison.Ordinal))
                    {
                        return false;
                    }
                }

                return true;
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
