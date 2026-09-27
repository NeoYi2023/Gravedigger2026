using System;
using System.Collections.Generic;
using Gravedigger2026.Core.AutoManufacture;
using Gravedigger2026.Core.Config;
using Gravedigger2026.Core.UpgradeManufacture;
using UnityEngine;

namespace Gravedigger2026.Core.TacticalFormation
{
    /// <summary>
    /// Prepare tactical-formation groups (SPEC_03 §3.18 / TFG-02).
    /// Membership lives on <see cref="BattleFormationService"/>; this session cache is rebuilt by <see cref="Restore"/>.
    /// </summary>
    public sealed class TacticalFormationLayoutService
    {
        /// <summary>SPEC_03 §3.18 / D-092: Q/E rotate step (degrees). Rule constant, not a table row.</summary>
        public const float FormationRotateStepDegrees = 15f;

        private const float PositionEpsilon = 0.0001f;

        private readonly List<TacticalFormationSquadSnapshot> _squads =
            new List<TacticalFormationSquadSnapshot>(4);

        private readonly List<BattleFormationService.PositionWrite> _writes =
            new List<BattleFormationService.PositionWrite>(16);

        private readonly List<string> _idScratch = new List<string>(16);
        private readonly List<FormationZoneSpiralSearch.Footprint> _occupiedScratch =
            new List<FormationZoneSpiralSearch.Footprint>(32);

        private readonly HashSet<string> _groupedScratch = new HashSet<string>(StringComparer.Ordinal);
        private readonly List<TacticalFormationMemberQuery.EligibleMember> _eligibleScratch =
            new List<TacticalFormationMemberQuery.EligibleMember>(16);

        private readonly List<string> _barScratch = new List<string>(16);
        private readonly List<string> _memberScratch = new List<string>(16);
        private readonly List<string> _newDeployScratch = new List<string>(16);

        private readonly List<TacticalFormationSquadSnapshot> _disbandScratch =
            new List<TacticalFormationSquadSnapshot>(4);

        public bool TryGetSquadByMember(string warriorId, out TacticalFormationSquadSnapshot squad)
        {
            squad = null;
            if (string.IsNullOrEmpty(warriorId))
            {
                return false;
            }

            for (var i = 0; i < _squads.Count; i++)
            {
                var s = _squads[i];
                if (s != null && s.Contains(warriorId))
                {
                    squad = s;
                    return true;
                }
            }

            return false;
        }

        public bool IsSquadMember(string warriorId)
        {
            return TryGetSquadByMember(warriorId, out _);
        }

        /// <summary>Copies active Prepare squad snapshots (same instances; Combat should lock data).</summary>
        public void CollectActiveSquads(List<TacticalFormationSquadSnapshot> into)
        {
            if (into == null)
            {
                return;
            }

            into.Clear();
            for (var i = 0; i < _squads.Count; i++)
            {
                if (_squads[i] != null)
                {
                    into.Add(_squads[i]);
                }
            }
        }

        /// <summary>
        /// Rebuild session snapshots from the formation save. Does not snap and does not create groups.
        /// Center is the member centroid. Level is recomputed.
        /// </summary>
        public void Restore(
            BattleFormationService formation,
            WarriorPoolService pool,
            ConfigCsvRepository configs)
        {
            _squads.Clear();
            if (formation == null)
            {
                return;
            }

            var saved = formation.Groups;
            if (saved == null)
            {
                return;
            }

            var seenMembers = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < saved.Count; i++)
            {
                var record = saved[i];
                if (record == null
                    || string.IsNullOrEmpty(record.GroupInstanceId)
                    || string.IsNullOrEmpty(record.FormationId)
                    || record.MemberIds == null
                    || record.MemberIds.Length == 0)
                {
                    continue;
                }

                _idScratch.Clear();
                for (var m = 0; m < record.MemberIds.Length; m++)
                {
                    var id = record.MemberIds[m];
                    if (string.IsNullOrEmpty(id) || !seenMembers.Add(id))
                    {
                        continue;
                    }

                    if (!formation.TryGetEntry(id, out _))
                    {
                        seenMembers.Remove(id);
                        continue;
                    }

                    _idScratch.Add(id);
                }

                if (_idScratch.Count == 0)
                {
                    continue;
                }

                var members = _idScratch.ToArray();
                var squad = new TacticalFormationSquadSnapshot
                {
                    GroupInstanceId = record.GroupInstanceId,
                    FormationId = record.FormationId,
                    MemberIds = members,
                    FacingYawDegrees = record.FacingYawDegrees
                };
                RecomputeCenter(squad, formation);
                ApplyLevel(squad, pool, configs);
                _squads.Add(squad);
            }
        }

        /// <summary>
        /// Create one group for <paramref name="formationId"/>.
        /// Deployed matches fill first; undeployed SoldierBar matches fill the rest.
        /// Fails without moving anyone when the taken count stays below Min.
        /// </summary>
        public bool TryCreateGroup(
            string formationId,
            BattleFormationService formation,
            WarriorPoolService pool,
            ConfigCsvRepository configs,
            ITacticalFormationPatternLookup patterns,
            TacticalFormationLayoutContext context)
        {
            if (formation == null || pool == null || configs == null || !configs.IsLoaded
                || string.IsNullOrEmpty(formationId))
            {
                return false;
            }

            if (!configs.TryGetTacticalFormation(formationId, out var row) || row == null)
            {
                Debug.LogWarning(
                    $"[TacticalFormationLayout] FormationId '{formationId}' missing TacticalFormationConfig — create failed.");
                return false;
            }

            if (patterns == null
                || !patterns.TryGetSlotLocalXZ(row.PrefabId, out var slots)
                || slots == null
                || slots.Length == 0)
            {
                Debug.LogWarning(
                    $"[TacticalFormationLayout] PrefabId '{row.PrefabId}' for {formationId} missing Pattern slots — create failed.");
                return false;
            }

            FillGroupedIds(formation);
            TacticalFormationMemberQuery.CollectEligible(
                formationId,
                formation,
                pool,
                configs,
                _groupedScratch,
                _eligibleScratch);

            var min = Mathf.Max(1, row.MinMemberCount);
            var cap = row.MaxMemberCount < min ? min : row.MaxMemberCount;
            var slotLimit = Mathf.Min(cap, slots.Length);
            if (slotLimit < min)
            {
                Debug.LogWarning(
                    $"[TacticalFormationLayout] {formationId} slots={slots.Length} < MinMemberCount={min} — create failed.");
                return false;
            }

            var deployedTake = Mathf.Min(_eligibleScratch.Count, slotLimit);
            var barNeed = slotLimit - deployedTake;
            _barScratch.Clear();
            if (barNeed > 0)
            {
                TacticalFormationMemberQuery.CollectUndeployed(
                    formationId,
                    formation,
                    pool,
                    configs,
                    _groupedScratch,
                    _barScratch);
                if (_barScratch.Count > barNeed)
                {
                    _barScratch.RemoveRange(barNeed, _barScratch.Count - barNeed);
                }
            }

            if (deployedTake + _barScratch.Count < min)
            {
                return false;
            }

            float cx;
            float cz;
            if (deployedTake > 0)
            {
                cx = 0f;
                cz = 0f;
                for (var m = 0; m < deployedTake; m++)
                {
                    cx += _eligibleScratch[m].Entry.PositionX;
                    cz += _eligibleScratch[m].Entry.PositionZ;
                }

                cx /= deployedTake;
                cz /= deployedTake;
            }
            else if (context.HasFallbackCenter)
            {
                cx = context.FallbackCenterRelX;
                cz = context.FallbackCenterRelZ;
            }
            else
            {
                return false;
            }

            _memberScratch.Clear();
            for (var i = 0; i < deployedTake; i++)
            {
                _memberScratch.Add(_eligibleScratch[i].WarriorId);
            }

            for (var i = 0; i < _barScratch.Count; i++)
            {
                _memberScratch.Add(_barScratch[i]);
            }

            var yaw = ResolveCreateYaw(cx, cz, context);
            FillSlotWrites(_memberScratch, slots, cx, cz, yaw);

            _newDeployScratch.Clear();
            var barFailed = false;
            for (var i = deployedTake; i < _memberScratch.Count; i++)
            {
                var write = _writes[i];
                if (!formation.TryDeployAt(write.WarriorId, write.X, write.Z, out _))
                {
                    barFailed = true;
                    break;
                }

                _newDeployScratch.Add(write.WarriorId);
            }

            if (barFailed)
            {
                for (var i = 0; i < _newDeployScratch.Count; i++)
                {
                    formation.TryUndeploy(_newDeployScratch[i], out _);
                }

                _newDeployScratch.Clear();
                if (deployedTake < min)
                {
                    return false;
                }

                if (_memberScratch.Count > deployedTake)
                {
                    _memberScratch.RemoveRange(deployedTake, _memberScratch.Count - deployedTake);
                }

                FillSlotWrites(_memberScratch, slots, cx, cz, yaw);
            }

            formation.ApplyPositionBatch(_writes);
            var snappedIds = _memberScratch.ToArray();
            var squad = new TacticalFormationSquadSnapshot
            {
                GroupInstanceId = Guid.NewGuid().ToString("N"),
                FormationId = formationId,
                MemberIds = snappedIds,
                CenterX = cx,
                CenterZ = cz,
                FacingYawDegrees = yaw
            };
            ApplyLevel(squad, pool, configs);
            _squads.Add(squad);
            PersistGroups(formation);
            Debug.Log(
                $"[TacticalFormationLayout] Create {squad.GroupInstanceId} {formationId} members={snappedIds.Length} " +
                $"level={squad.ComputedLevel} center=({cx:0.###},{cz:0.###}) yaw={yaw:0.#}");
            return true;
        }

        private static float ResolveCreateYaw(float cx, float cz, TacticalFormationLayoutContext context)
        {
            if (!context.HasFacingTarget)
            {
                return 0f;
            }

            var dx = context.FacingTargetRelX - cx;
            var dz = context.FacingTargetRelZ - cz;
            if (dx * dx + dz * dz <= 0.0001f)
            {
                return 0f;
            }

            return Mathf.Atan2(dx, dz) * Mathf.Rad2Deg;
        }

        private void FillSlotWrites(
            List<string> ids,
            Vector3[] slots,
            float cx,
            float cz,
            float yaw)
        {
            _writes.Clear();
            var rot = Quaternion.Euler(0f, yaw, 0f);
            var count = ids.Count;
            for (var s = 0; s < count; s++)
            {
                var local = slots[s];
                local.y = 0f;
                var world = rot * local;
                _writes.Add(new BattleFormationService.PositionWrite(
                    ids[s],
                    cx + world.x,
                    cz + world.z));
            }
        }

        /// <summary>Return members to class-zone spirals and delete the group.</summary>
        public bool TryDisbandGroup(
            string groupInstanceId,
            BattleFormationService formation,
            WarriorPoolService pool,
            ConfigCsvRepository configs,
            TacticalFormationLayoutContext context)
        {
            if (formation == null || string.IsNullOrEmpty(groupInstanceId))
            {
                return false;
            }

            TacticalFormationSquadSnapshot squad = null;
            for (var i = 0; i < _squads.Count; i++)
            {
                if (string.Equals(_squads[i].GroupInstanceId, groupInstanceId, StringComparison.Ordinal))
                {
                    squad = _squads[i];
                    _squads.RemoveAt(i);
                    break;
                }
            }

            if (squad == null)
            {
                return false;
            }

            PersistGroups(formation);
            RevertSquad(formation, pool, configs, context.Zones, squad);
            return true;
        }

        /// <summary>
        /// Drop members who are no longer deployed. Remaining below Min disbands that group only.
        /// Does not pull newly deployed soldiers in, and does not re-slot survivors.
        /// </summary>
        public void PruneGroups(
            BattleFormationService formation,
            WarriorPoolService pool,
            ConfigCsvRepository configs,
            TacticalFormationLayoutContext context)
        {
            if (formation == null || pool == null || configs == null)
            {
                return;
            }

            var changed = false;
            _disbandScratch.Clear();
            for (var i = _squads.Count - 1; i >= 0; i--)
            {
                var squad = _squads[i];
                if (squad?.MemberIds == null)
                {
                    _squads.RemoveAt(i);
                    changed = true;
                    continue;
                }

                _idScratch.Clear();
                for (var m = 0; m < squad.MemberIds.Length; m++)
                {
                    var id = squad.MemberIds[m];
                    if (!string.IsNullOrEmpty(id) && formation.TryGetEntry(id, out _))
                    {
                        _idScratch.Add(id);
                    }
                }

                var min = ResolveMin(configs, squad.FormationId);
                if (_idScratch.Count < min)
                {
                    _squads.RemoveAt(i);
                    changed = true;
                    if (_idScratch.Count > 0)
                    {
                        squad.MemberIds = _idScratch.ToArray();
                        _disbandScratch.Add(squad);
                    }

                    continue;
                }

                if (_idScratch.Count == squad.MemberIds.Length)
                {
                    continue;
                }

                squad.MemberIds = _idScratch.ToArray();
                RecomputeCenter(squad, formation);
                ApplyLevel(squad, pool, configs);
                changed = true;
            }

            if (changed)
            {
                PersistGroups(formation);
            }

            var zones = context.Zones;
            for (var i = 0; i < _disbandScratch.Count; i++)
            {
                RevertSquad(formation, pool, configs, zones, _disbandScratch[i]);
            }
        }

        /// <summary>
        /// Translate whole squad by map-relative delta. Keeps offsets and facing.
        /// </summary>
        public bool TryApplySquadCenterDelta(
            BattleFormationService formation,
            string memberWarriorId,
            float deltaX,
            float deltaZ)
        {
            if (formation == null
                || string.IsNullOrEmpty(memberWarriorId)
                || !TryGetSquadByMember(memberWarriorId, out var squad)
                || squad == null
                || squad.MemberIds == null
                || squad.MemberIds.Length == 0)
            {
                return false;
            }

            if (Mathf.Abs(deltaX) < PositionEpsilon && Mathf.Abs(deltaZ) < PositionEpsilon)
            {
                return true;
            }

            _writes.Clear();
            for (var i = 0; i < squad.MemberIds.Length; i++)
            {
                var id = squad.MemberIds[i];
                if (!formation.TryGetEntry(id, out var entry) || entry == null)
                {
                    continue;
                }

                _writes.Add(new BattleFormationService.PositionWrite(
                    id,
                    entry.PositionX + deltaX,
                    entry.PositionZ + deltaZ));
            }

            if (_writes.Count == 0)
            {
                return false;
            }

            formation.ApplyPositionBatch(_writes);
            squad.CenterX += deltaX;
            squad.CenterZ += deltaZ;
            return true;
        }

        /// <summary>
        /// Rotate whole squad about its center by delta yaw (degrees). Updates FacingYawDegrees and the save.
        /// SPEC_03 §3.18 / D-092. Key is the member's <c>GroupInstanceId</c>.
        /// </summary>
        public bool TryApplySquadYawDelta(
            BattleFormationService formation,
            string memberWarriorId,
            float deltaYawDegrees)
        {
            if (formation == null
                || string.IsNullOrEmpty(memberWarriorId)
                || Mathf.Abs(deltaYawDegrees) < PositionEpsilon
                || !TryGetSquadByMember(memberWarriorId, out var squad)
                || squad == null
                || squad.MemberIds == null
                || squad.MemberIds.Length == 0)
            {
                return false;
            }

            var cx = squad.CenterX;
            var cz = squad.CenterZ;
            var rot = Quaternion.Euler(0f, deltaYawDegrees, 0f);

            _writes.Clear();
            for (var i = 0; i < squad.MemberIds.Length; i++)
            {
                var id = squad.MemberIds[i];
                if (!formation.TryGetEntry(id, out var entry) || entry == null)
                {
                    continue;
                }

                var offset = rot * new Vector3(entry.PositionX - cx, 0f, entry.PositionZ - cz);
                _writes.Add(new BattleFormationService.PositionWrite(
                    id,
                    cx + offset.x,
                    cz + offset.z));
            }

            if (_writes.Count == 0)
            {
                return false;
            }

            squad.FacingYawDegrees += deltaYawDegrees;
            formation.ApplyPositionBatch(_writes);
            PersistGroups(formation);
            return true;
        }

        private void FillGroupedIds(BattleFormationService formation)
        {
            _groupedScratch.Clear();
            for (var i = 0; i < _squads.Count; i++)
            {
                var members = _squads[i]?.MemberIds;
                if (members == null)
                {
                    continue;
                }

                for (var m = 0; m < members.Length; m++)
                {
                    if (!string.IsNullOrEmpty(members[m]))
                    {
                        _groupedScratch.Add(members[m]);
                    }
                }
            }

            var saved = formation != null ? formation.Groups : null;
            if (saved == null)
            {
                return;
            }

            for (var i = 0; i < saved.Count; i++)
            {
                var members = saved[i]?.MemberIds;
                if (members == null)
                {
                    continue;
                }

                for (var m = 0; m < members.Length; m++)
                {
                    if (!string.IsNullOrEmpty(members[m]))
                    {
                        _groupedScratch.Add(members[m]);
                    }
                }
            }
        }

        private void PersistGroups(BattleFormationService formation)
        {
            if (formation == null)
            {
                return;
            }

            var records = new List<TacticalFormationGroupSaveEntry>(_squads.Count);
            for (var i = 0; i < _squads.Count; i++)
            {
                var squad = _squads[i];
                if (squad == null || string.IsNullOrEmpty(squad.GroupInstanceId))
                {
                    continue;
                }

                var members = squad.MemberIds ?? Array.Empty<string>();
                var copy = new string[members.Length];
                Array.Copy(members, copy, members.Length);
                records.Add(new TacticalFormationGroupSaveEntry
                {
                    GroupInstanceId = squad.GroupInstanceId,
                    FormationId = squad.FormationId,
                    MemberIds = copy,
                    FacingYawDegrees = squad.FacingYawDegrees
                });
            }

            formation.ReplaceTacticalGroups(records);
        }

        private static int ResolveMin(ConfigCsvRepository configs, string formationId)
        {
            if (configs != null
                && configs.TryGetTacticalFormation(formationId, out var row)
                && row != null)
            {
                return Mathf.Max(1, row.MinMemberCount);
            }

            return 1;
        }

        private static void RecomputeCenter(TacticalFormationSquadSnapshot squad, BattleFormationService formation)
        {
            if (squad?.MemberIds == null || squad.MemberIds.Length == 0 || formation == null)
            {
                return;
            }

            var cx = 0f;
            var cz = 0f;
            var n = 0;
            for (var i = 0; i < squad.MemberIds.Length; i++)
            {
                if (!formation.TryGetEntry(squad.MemberIds[i], out var entry) || entry == null)
                {
                    continue;
                }

                cx += entry.PositionX;
                cz += entry.PositionZ;
                n++;
            }

            if (n <= 0)
            {
                return;
            }

            squad.CenterX = cx / n;
            squad.CenterZ = cz / n;
        }

        private static void ApplyLevel(
            TacticalFormationSquadSnapshot squad,
            WarriorPoolService pool,
            ConfigCsvRepository configs)
        {
            if (squad == null)
            {
                return;
            }

            var members = squad.MemberIds ?? Array.Empty<string>();
            var levels = new int[members.Length];
            var sum = 0;
            for (var i = 0; i < members.Length; i++)
            {
                if (pool == null || !pool.TryGet(members[i], out var warrior) || warrior == null)
                {
                    continue;
                }

                if (configs != null
                    && !string.IsNullOrEmpty(warrior.ClassId)
                    && configs.TryGetClass(warrior.ClassId, out var classRow)
                    && classRow != null)
                {
                    levels[i] = classRow.ClassLevel;
                    sum += classRow.ClassLevel;
                }
            }

            squad.MemberClassLevels = levels;

            var count = members.Length;
            squad.ComputedLevel = count > 0 ? sum / count : 0;
            squad.MatchedLevelRow = null;
            if (configs == null
                || !configs.TryGetTacticalFormationForComputedLevel(
                    squad.FormationId,
                    squad.ComputedLevel,
                    out var levelRow)
                || levelRow == null)
            {
                Debug.LogWarning(
                    $"[TacticalFormationLayout] Group '{squad.GroupInstanceId}' FormationId '{squad.FormationId}' " +
                    $"computed level {squad.ComputedLevel} has no FormationLevel row — stats empty.");
                return;
            }

            squad.MatchedLevelRow = levelRow;
        }

        private void RevertSquad(
            BattleFormationService formation,
            WarriorPoolService pool,
            ConfigCsvRepository configs,
            IReadOnlyList<FormationClassZoneSnapshot> zones,
            TacticalFormationSquadSnapshot squad)
        {
            if (squad?.MemberIds == null || squad.MemberIds.Length == 0)
            {
                return;
            }

            var revertSet = new HashSet<string>(squad.MemberIds, StringComparer.Ordinal);
            BuildOccupiedExcluding(formation, pool, configs, revertSet);

            _writes.Clear();
            for (var i = 0; i < squad.MemberIds.Length; i++)
            {
                var id = squad.MemberIds[i];
                if (!formation.TryGetEntry(id, out var entry) || entry == null)
                {
                    continue;
                }

                if (!pool.TryGet(id, out var warrior) || warrior == null)
                {
                    continue;
                }

                var zone = FindZone(zones, warrior.ClassId);
                var radius = ResolveBodyRadius(warrior, configs);
                if (!zone.HasValue)
                {
                    Debug.LogWarning(
                        $"[TacticalFormationLayout] Revert {id} ClassId={warrior.ClassId} — no FormationClassZone, keep position.");
                    continue;
                }

                if (!FormationZoneSpiralSearch.TryFindSlot(
                        zone.Value,
                        radius,
                        _occupiedScratch,
                        out var relX,
                        out var relZ))
                {
                    Debug.LogWarning(
                        $"[TacticalFormationLayout] Revert {id} — no free class-zone slot, keep position.");
                    continue;
                }

                _writes.Add(new BattleFormationService.PositionWrite(id, relX, relZ));
                _occupiedScratch.Add(new FormationZoneSpiralSearch.Footprint(relX, relZ, radius));
            }

            if (_writes.Count > 0)
            {
                formation.ApplyPositionBatch(_writes);
            }
        }

        private void BuildOccupiedExcluding(
            BattleFormationService formation,
            WarriorPoolService pool,
            ConfigCsvRepository configs,
            HashSet<string> exclude)
        {
            _occupiedScratch.Clear();
            var entries = formation.Entries;
            for (var i = 0; i < entries.Count; i++)
            {
                var e = entries[i];
                if (e == null || string.IsNullOrEmpty(e.WarriorId) || exclude.Contains(e.WarriorId))
                {
                    continue;
                }

                if (!pool.TryGet(e.WarriorId, out var warrior) || warrior == null)
                {
                    continue;
                }

                var radius = ResolveBodyRadius(warrior, configs);
                _occupiedScratch.Add(
                    new FormationZoneSpiralSearch.Footprint(e.PositionX, e.PositionZ, radius));
            }
        }

        private static FormationClassZoneSnapshot? FindZone(
            IReadOnlyList<FormationClassZoneSnapshot> zones,
            string classId)
        {
            if (zones == null || string.IsNullOrEmpty(classId))
            {
                return null;
            }

            for (var i = 0; i < zones.Count; i++)
            {
                if (string.Equals(zones[i].ClassId, classId, StringComparison.Ordinal))
                {
                    return zones[i];
                }
            }

            return null;
        }

        private static float ResolveBodyRadius(WarriorInstance warrior, ConfigCsvRepository configs)
        {
            var scale = WarriorVisualModelScale.Resolve(warrior);
            var appearanceId = warrior != null ? warrior.AppearanceId : null;
            if (!string.IsNullOrEmpty(appearanceId)
                && configs != null
                && configs.TryGetAppearance(appearanceId, out var row)
                && row != null)
            {
                return Mathf.Max(FormationZoneSpiralSearch.MinRadius, row.BodyRadius * scale);
            }

            return BodyAppearanceConfigRow.DefaultBodyRadius * scale;
        }
    }
}
