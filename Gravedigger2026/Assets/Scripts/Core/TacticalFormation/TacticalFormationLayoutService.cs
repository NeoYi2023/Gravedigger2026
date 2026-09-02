using System;
using System.Collections.Generic;
using Gravedigger2026.Core.AutoManufacture;
using Gravedigger2026.Core.Config;
using Gravedigger2026.Core.UpgradeManufacture;
using UnityEngine;

namespace Gravedigger2026.Core.TacticalFormation
{
    /// <summary>
    /// Prepare tactical-formation layout: group by FormationId, snap ≥Min, revert &lt;Min (SPEC_03 §3.18 / TF-03).
    /// Session membership is in-memory only (not saved); reconstructed on each Evaluate.
    /// </summary>
    public sealed class TacticalFormationLayoutService
    {
        private const float PositionEpsilon = 0.0001f;

        private readonly Dictionary<string, TacticalFormationSquadSnapshot> _squads =
            new Dictionary<string, TacticalFormationSquadSnapshot>(StringComparer.Ordinal);

        private readonly List<BattleFormationService.PositionWrite> _writes =
            new List<BattleFormationService.PositionWrite>(16);

        private readonly List<string> _idScratch = new List<string>(16);
        private readonly List<FormationZoneSpiralSearch.Footprint> _occupiedScratch =
            new List<FormationZoneSpiralSearch.Footprint>(32);

        public bool TryGetSquadByMember(string warriorId, out TacticalFormationSquadSnapshot squad)
        {
            squad = null;
            if (string.IsNullOrEmpty(warriorId))
            {
                return false;
            }

            foreach (var kv in _squads)
            {
                var s = kv.Value;
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
            foreach (var kv in _squads)
            {
                if (kv.Value != null)
                {
                    into.Add(kv.Value);
                }
            }
        }

        /// <summary>
        /// Translate whole squad by map-relative delta. Keeps offsets and facing. Does not re-Evaluate.
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

        public void EvaluateAndApply(
            BattleFormationService formation,
            WarriorPoolService pool,
            ConfigCsvRepository configs,
            ITacticalFormationPatternLookup patterns,
            TacticalFormationLayoutContext context)
        {
            if (formation == null || pool == null || configs == null || !configs.IsLoaded)
            {
                return;
            }

            var groups = GroupDeployed(formation, pool, configs);
            var previous = CopySquads();
            var next = new Dictionary<string, TacticalFormationSquadSnapshot>(StringComparer.Ordinal);

            CollectFormationIds(groups, previous, _idScratch);
            _idScratch.Sort(StringComparer.Ordinal);

            for (var i = 0; i < _idScratch.Count; i++)
            {
                var formationId = _idScratch[i];
                groups.TryGetValue(formationId, out var members);
                var count = members != null ? members.Count : 0;

                if (!configs.TryGetTacticalFormation(formationId, out var row) || row == null)
                {
                    Debug.LogWarning(
                        $"[TacticalFormationLayout] FormationId '{formationId}' missing TacticalFormationConfig — skip.");
                    continue;
                }

                var min = Mathf.Max(1, row.MinMemberCount);
                if (count < min)
                {
                    if (previous.TryGetValue(formationId, out var oldSquad) && oldSquad != null)
                    {
                        RevertSquad(formation, pool, configs, context.Zones, oldSquad);
                    }

                    continue;
                }

                if (patterns == null || !patterns.TryGetSlotLocalXZ(row.PrefabId, out var slots) || slots == null
                    || slots.Length == 0)
                {
                    Debug.LogWarning(
                        $"[TacticalFormationLayout] PrefabId '{row.PrefabId}' for {formationId} missing Pattern slots — skip snap.");
                    if (previous.TryGetValue(formationId, out var missingPrefabSquad) && missingPrefabSquad != null)
                    {
                        RevertSquad(formation, pool, configs, context.Zones, missingPrefabSquad);
                    }

                    continue;
                }

                members.Sort(CompareDeployOrder);
                var cap = row.MaxMemberCount < min ? min : row.MaxMemberCount;
                var take = Mathf.Min(count, cap, slots.Length);
                if (take < min)
                {
                    Debug.LogWarning(
                        $"[TacticalFormationLayout] {formationId} slots={slots.Length} < MinMemberCount={min} — skip snap.");
                    if (previous.TryGetValue(formationId, out var oldSquad) && oldSquad != null)
                    {
                        RevertSquad(formation, pool, configs, context.Zones, oldSquad);
                    }

                    continue;
                }

                var cx = 0f;
                var cz = 0f;
                for (var m = 0; m < take; m++)
                {
                    cx += members[m].Entry.PositionX;
                    cz += members[m].Entry.PositionZ;
                }

                cx /= take;
                cz /= take;

                var yaw = 0f;
                if (context.HasFacingTarget)
                {
                    var dx = context.FacingTargetRelX - cx;
                    var dz = context.FacingTargetRelZ - cz;
                    if (dx * dx + dz * dz > 0.0001f)
                    {
                        yaw = Mathf.Atan2(dx, dz) * Mathf.Rad2Deg;
                    }
                }

                var rot = Quaternion.Euler(0f, yaw, 0f);
                _writes.Clear();
                var snappedIds = new string[take];
                for (var s = 0; s < take; s++)
                {
                    var local = slots[s];
                    local.y = 0f;
                    var world = rot * local;
                    var id = members[s].WarriorId;
                    snappedIds[s] = id;
                    _writes.Add(new BattleFormationService.PositionWrite(
                        id,
                        cx + world.x,
                        cz + world.z));
                }

                formation.ApplyPositionBatch(_writes);
                next[formationId] = new TacticalFormationSquadSnapshot
                {
                    FormationId = formationId,
                    MemberIds = snappedIds,
                    CenterX = cx,
                    CenterZ = cz,
                    FacingYawDegrees = yaw
                };
                Debug.Log(
                    $"[TacticalFormationLayout] Snap {formationId} members={take} " +
                    $"center=({cx:0.###},{cz:0.###}) yaw={yaw:0.#}");
            }

            _squads.Clear();
            foreach (var kv in next)
            {
                _squads[kv.Key] = kv.Value;
            }
        }

        private Dictionary<string, TacticalFormationSquadSnapshot> CopySquads()
        {
            var copy = new Dictionary<string, TacticalFormationSquadSnapshot>(
                _squads.Count,
                StringComparer.Ordinal);
            foreach (var kv in _squads)
            {
                copy[kv.Key] = kv.Value;
            }

            return copy;
        }

        private static void CollectFormationIds(
            Dictionary<string, List<Candidate>> groups,
            Dictionary<string, TacticalFormationSquadSnapshot> previous,
            List<string> into)
        {
            into.Clear();
            foreach (var kv in groups)
            {
                if (!string.IsNullOrEmpty(kv.Key) && !into.Contains(kv.Key))
                {
                    into.Add(kv.Key);
                }
            }

            foreach (var kv in previous)
            {
                if (!string.IsNullOrEmpty(kv.Key) && !into.Contains(kv.Key))
                {
                    into.Add(kv.Key);
                }
            }
        }

        private static Dictionary<string, List<Candidate>> GroupDeployed(
            BattleFormationService formation,
            WarriorPoolService pool,
            ConfigCsvRepository configs)
        {
            var groups = new Dictionary<string, List<Candidate>>(StringComparer.Ordinal);
            var entries = formation.Entries;
            for (var i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                if (entry == null || string.IsNullOrEmpty(entry.WarriorId))
                {
                    continue;
                }

                if (!pool.TryGet(entry.WarriorId, out var warrior) || warrior == null)
                {
                    continue;
                }

                var formationId = ResolveFormationId(warrior, configs);
                if (string.IsNullOrEmpty(formationId))
                {
                    continue;
                }

                if (!groups.TryGetValue(formationId, out var list))
                {
                    list = new List<Candidate>(4);
                    groups[formationId] = list;
                }

                list.Add(new Candidate(entry.WarriorId, i, entry));
            }

            return groups;
        }

        internal static string ResolveFormationId(WarriorInstance warrior, ConfigCsvRepository configs)
        {
            if (warrior?.SoldierSkills == null || configs == null)
            {
                return null;
            }

            for (var i = 0; i < warrior.SoldierSkills.Count; i++)
            {
                var skill = warrior.SoldierSkills[i];
                if (skill == null || string.IsNullOrEmpty(skill.SkillId))
                {
                    continue;
                }

                if (!TryResolveSkillRow(configs, skill.SkillId, skill.SkillLevel, out var row) || row == null)
                {
                    continue;
                }

                if (!string.IsNullOrEmpty(row.FormationId))
                {
                    return row.FormationId;
                }
            }

            return null;
        }

        private static bool TryResolveSkillRow(
            ConfigCsvRepository configs,
            string skillId,
            int skillLevel,
            out SkillConfigRow row)
        {
            var level = skillLevel < 1 ? 1 : skillLevel;
            if (configs.TryGetSkill(skillId, level, out row) && row != null)
            {
                return true;
            }

            if (configs.TryGetSkillLevelRange(skillId, out var min, out _)
                && configs.TryGetSkill(skillId, min, out row)
                && row != null)
            {
                return true;
            }

            row = null;
            return false;
        }

        private static int CompareDeployOrder(Candidate a, Candidate b)
        {
            var byIndex = a.DeployIndex.CompareTo(b.DeployIndex);
            return byIndex != 0 ? byIndex : string.CompareOrdinal(a.WarriorId, b.WarriorId);
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

        private readonly struct Candidate
        {
            public readonly string WarriorId;
            public readonly int DeployIndex;
            public readonly BattleFormationEntry Entry;

            public Candidate(string warriorId, int deployIndex, BattleFormationEntry entry)
            {
                WarriorId = warriorId;
                DeployIndex = deployIndex;
                Entry = entry;
            }
        }
    }
}
