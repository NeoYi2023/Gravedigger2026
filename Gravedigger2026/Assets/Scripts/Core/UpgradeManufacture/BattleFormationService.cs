using System;
using System.Collections.Generic;
using UnityEngine;

namespace Gravedigger2026.Core.UpgradeManufacture
{
    /// <summary>
    /// Save-scoped BattleFormation shared by UM / Defend / PushMap Prepare (SPEC_03 §3.11 / SPEC_04 §6).
    /// PlayerPrefs JSON per slot + CampaignMode; mutate → immediate write when bound.
    /// </summary>
    public sealed class BattleFormationService
    {
        public const float DefaultDeployStepX = 2f;
        public const float DefaultNudgeStep = 1f;

        private readonly List<BattleFormationEntry> _entries = new List<BattleFormationEntry>();
        private readonly List<TacticalFormationGroupSaveEntry> _groups =
            new List<TacticalFormationGroupSaveEntry>();
        private readonly WarriorPoolService _pool;
        private int _slotIndex = -1;
        private CampaignMode _campaignMode = CampaignMode.Mode1;
        private bool _suppressPersist;

        public BattleFormationService(WarriorPoolService pool)
        {
            _pool = pool ?? throw new ArgumentNullException(nameof(pool));
        }

        public int BoundSlotIndex => _slotIndex;
        public CampaignMode BoundCampaignMode => _campaignMode;
        public IReadOnlyList<BattleFormationEntry> Entries => _entries;

        /// <summary>
        /// Manual tactical groups persisted with this formation (SPEC_03 §3.18 / TFG-02).
        /// Empty when the save has no <c>Groups</c> field. Not inferred from positions.
        /// </summary>
        public IReadOnlyList<TacticalFormationGroupSaveEntry> Groups => _groups;

        public event Action Changed;

        /// <summary>
        /// Load formation for slot+mode. Call after <see cref="WarriorPoolService.BindSlot"/> so orphan rows can be dropped.
        /// </summary>
        public void BindSlot(int slotIndex, CampaignMode campaignMode)
        {
            if (slotIndex < 0 || slotIndex > 2)
            {
                throw new ArgumentOutOfRangeException(nameof(slotIndex), slotIndex, "Slot index must be 0..2.");
            }

            var droppedOrphans = false;
            _suppressPersist = true;
            try
            {
                _slotIndex = slotIndex;
                _campaignMode = campaignMode;
                _entries.Clear();
                _groups.Clear();

                var key = FormationKey(slotIndex, campaignMode);
                var raw = PlayerPrefs.GetString(key, string.Empty);
                var migratedFromLegacy = false;

                if (string.IsNullOrEmpty(raw) && campaignMode == CampaignMode.Mode1)
                {
                    var legacyKey = SaveSlotPrefsKeys.LegacyDataKey(
                        slotIndex, SaveSlotPrefsKeys.BattleFormationSuffix);
                    raw = PlayerPrefs.GetString(legacyKey, string.Empty);
                    if (!string.IsNullOrEmpty(raw))
                    {
                        migratedFromLegacy = true;
                    }
                }

                if (!string.IsNullOrEmpty(raw))
                {
                    var data = JsonUtility.FromJson<BattleFormationSaveData>(raw);
                    if (ImportSaveData(data))
                    {
                        droppedOrphans = true;
                    }
                }

                if (migratedFromLegacy)
                {
                    _suppressPersist = false;
                    PersistIfBound();
                    _suppressPersist = true;
                    PlayerPrefs.DeleteKey(
                        SaveSlotPrefsKeys.LegacyDataKey(
                            slotIndex, SaveSlotPrefsKeys.BattleFormationSuffix));
                    PlayerPrefs.Save();
                }
            }
            finally
            {
                _suppressPersist = false;
            }

            if (droppedOrphans)
            {
                PersistIfBound();
            }

            Changed?.Invoke();
        }

        public void ClearBound()
        {
            _slotIndex = -1;
            _campaignMode = CampaignMode.Mode1;
            _suppressPersist = true;
            try
            {
                _entries.Clear();
                _groups.Clear();
            }
            finally
            {
                _suppressPersist = false;
            }

            Changed?.Invoke();
        }

        public void Clear()
        {
            _entries.Clear();
            _groups.Clear();
            PersistIfBound();
            Changed?.Invoke();
        }

        /// <summary>
        /// Replace entries and groups from a DTO. Null <see cref="BattleFormationSaveData.Groups"/>
        /// (old JSON) loads as no groups. Does not infer groups from positions.
        /// Returns true when a row or member was dropped.
        /// </summary>
        public bool ImportSaveData(BattleFormationSaveData data)
        {
            _entries.Clear();
            _groups.Clear();
            if (data == null)
            {
                return false;
            }

            var dropped = false;
            if (data.Entries != null)
            {
                for (var i = 0; i < data.Entries.Length; i++)
                {
                    var e = data.Entries[i];
                    if (e == null || string.IsNullOrEmpty(e.WarriorId))
                    {
                        continue;
                    }

                    if (!_pool.TryGet(e.WarriorId, out _))
                    {
                        dropped = true;
                        continue;
                    }

                    _entries.Add(new BattleFormationEntry
                    {
                        WarriorId = e.WarriorId,
                        PositionX = e.PositionX,
                        PositionZ = e.PositionZ,
                        RemainingHP = e.RemainingHP
                    });
                }
            }

            if (data.Groups != null)
            {
                var seenMembers = new HashSet<string>(StringComparer.Ordinal);
                var seenGroups = new HashSet<string>(StringComparer.Ordinal);
                for (var i = 0; i < data.Groups.Length; i++)
                {
                    var g = data.Groups[i];
                    if (g == null
                        || string.IsNullOrEmpty(g.GroupInstanceId)
                        || string.IsNullOrEmpty(g.FormationId))
                    {
                        dropped = true;
                        continue;
                    }

                    if (!seenGroups.Add(g.GroupInstanceId))
                    {
                        dropped = true;
                        continue;
                    }

                    var source = g.MemberIds;
                    var kept = new List<string>(source != null ? source.Length : 0);
                    if (source != null)
                    {
                        for (var m = 0; m < source.Length; m++)
                        {
                            var id = source[m];
                            if (string.IsNullOrEmpty(id) || !seenMembers.Add(id))
                            {
                                dropped = true;
                                continue;
                            }

                            if (!_pool.TryGet(id, out _))
                            {
                                dropped = true;
                                seenMembers.Remove(id);
                                continue;
                            }

                            kept.Add(id);
                        }
                    }

                    if (kept.Count == 0)
                    {
                        dropped = true;
                        continue;
                    }

                    if (source == null || kept.Count != source.Length)
                    {
                        dropped = true;
                    }

                    _groups.Add(new TacticalFormationGroupSaveEntry
                    {
                        GroupInstanceId = g.GroupInstanceId,
                        FormationId = g.FormationId,
                        MemberIds = kept.ToArray(),
                        FacingYawDegrees = g.FacingYawDegrees
                    });
                }
            }

            return dropped;
        }

        /// <summary>
        /// Replace the persisted group list. Does not move soldiers and does not raise <see cref="Changed"/>.
        /// </summary>
        public void ReplaceTacticalGroups(IReadOnlyList<TacticalFormationGroupSaveEntry> groups)
        {
            _groups.Clear();
            if (groups != null)
            {
                for (var i = 0; i < groups.Count; i++)
                {
                    var g = groups[i];
                    if (g == null || string.IsNullOrEmpty(g.GroupInstanceId))
                    {
                        continue;
                    }

                    _groups.Add(CloneGroup(g));
                }
            }

            PersistIfBound();
        }

        public static void DeleteSlotData(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex > 2)
            {
                return;
            }

            PlayerPrefs.DeleteKey(FormationKey(slotIndex, CampaignMode.Mode1));
            PlayerPrefs.DeleteKey(FormationKey(slotIndex, CampaignMode.Mode2));
            PlayerPrefs.DeleteKey(
                SaveSlotPrefsKeys.LegacyDataKey(slotIndex, SaveSlotPrefsKeys.BattleFormationSuffix));
            PlayerPrefs.Save();
        }

        public bool IsDeployed(string warriorId)
        {
            return FindIndex(warriorId) >= 0;
        }

        public bool TryGetEntry(string warriorId, out BattleFormationEntry entry)
        {
            var index = FindIndex(warriorId);
            if (index < 0)
            {
                entry = null;
                return false;
            }

            entry = _entries[index];
            return true;
        }

        public bool TryDeploy(string warriorId, out string error)
        {
            var pos = NextAutoPosition();
            return TryDeployAt(warriorId, pos.x, pos.z, out error);
        }

        /// <summary>
        /// Deploy at continuous BattleMap-relative XZ (FormationEditor drag-drop).
        /// </summary>
        public bool TryDeployAt(string warriorId, float positionX, float positionZ, out string error)
        {
            error = null;
            if (string.IsNullOrEmpty(warriorId))
            {
                error = "士兵 Id 为空";
                return false;
            }

            if (FindIndex(warriorId) >= 0)
            {
                error = "该士兵已上阵";
                return false;
            }

            if (!TryFindWarrior(warriorId, out var warrior))
            {
                error = "士兵不在可上阵池";
                return false;
            }

            _entries.Add(new BattleFormationEntry
            {
                WarriorId = warriorId,
                PositionX = positionX,
                PositionZ = positionZ,
                RemainingHP = warrior.RemainingHP
            });
            PersistIfBound();
            Changed?.Invoke();
            return true;
        }

        public bool TryUndeploy(string warriorId, out string error)
        {
            error = null;
            var index = FindIndex(warriorId);
            if (index < 0)
            {
                error = "该士兵未上阵";
                return false;
            }

            var entry = _entries[index];
            if (TryFindWarrior(warriorId, out var warrior))
            {
                warrior.RemainingHP = entry.RemainingHP;
                _pool.NotifyMutated();
            }

            _entries.RemoveAt(index);
            PersistIfBound();
            Changed?.Invoke();
            return true;
        }

        public bool TryNudge(string warriorId, float deltaX, float deltaZ, out string error)
        {
            error = null;
            var index = FindIndex(warriorId);
            if (index < 0)
            {
                error = "该士兵未上阵";
                return false;
            }

            var entry = _entries[index];
            entry.PositionX += deltaX;
            entry.PositionZ += deltaZ;
            PersistIfBound();
            Changed?.Invoke();
            return true;
        }

        public bool TrySetPosition(string warriorId, float x, float z, out string error)
        {
            error = null;
            var index = FindIndex(warriorId);
            if (index < 0)
            {
                error = "该士兵未上阵";
                return false;
            }

            var entry = _entries[index];
            entry.PositionX = x;
            entry.PositionZ = z;
            PersistIfBound();
            Changed?.Invoke();
            return true;
        }

        public readonly struct PositionWrite
        {
            public readonly string WarriorId;
            public readonly float X;
            public readonly float Z;

            public PositionWrite(string warriorId, float x, float z)
            {
                WarriorId = warriorId;
                X = x;
                Z = z;
            }
        }

        /// <summary>
        /// Batch map-relative position writes; persist + notify once. Unchanged coords are skipped.
        /// </summary>
        public int ApplyPositionBatch(IReadOnlyList<PositionWrite> writes)
        {
            if (writes == null || writes.Count == 0)
            {
                return 0;
            }

            const float epsilon = 0.0001f;
            var changed = 0;
            for (var i = 0; i < writes.Count; i++)
            {
                var w = writes[i];
                var index = FindIndex(w.WarriorId);
                if (index < 0)
                {
                    continue;
                }

                var entry = _entries[index];
                if (Mathf.Abs(entry.PositionX - w.X) < epsilon
                    && Mathf.Abs(entry.PositionZ - w.Z) < epsilon)
                {
                    continue;
                }

                entry.PositionX = w.X;
                entry.PositionZ = w.Z;
                changed++;
            }

            if (changed <= 0)
            {
                return 0;
            }

            PersistIfBound();
            Changed?.Invoke();
            return changed;
        }

        public bool TrySetRemainingHp(string warriorId, float remainingHp, out string error)
        {
            error = null;
            var index = FindIndex(warriorId);
            if (index < 0)
            {
                error = "该士兵未上阵";
                return false;
            }

            _entries[index].RemainingHP = remainingHp;
            PersistIfBound();
            Changed?.Invoke();
            return true;
        }

        public float SumControlPowerCost()
        {
            var sum = 0f;
            for (var i = 0; i < _entries.Count; i++)
            {
                if (TryFindWarrior(_entries[i].WarriorId, out var warrior))
                {
                    sum += warrior.ControlPowerCost;
                }
            }

            return sum;
        }

        /// <summary>
        /// LossOfControlDegree = ΣCost / Cap − 1; ≤0 means not out of control.
        /// </summary>
        public float ComputeLossOfControlDegree(float controlPowerCap)
        {
            if (controlPowerCap <= 0f)
            {
                return _entries.Count > 0 ? float.PositiveInfinity : 0f;
            }

            return SumControlPowerCost() / controlPowerCap - 1f;
        }

        private void PersistIfBound()
        {
            if (_suppressPersist || _slotIndex < 0)
            {
                return;
            }

            var data = new BattleFormationSaveData
            {
                Entries = new BattleFormationSaveEntry[_entries.Count],
                Groups = new TacticalFormationGroupSaveEntry[_groups.Count]
            };

            for (var i = 0; i < _entries.Count; i++)
            {
                var e = _entries[i];
                data.Entries[i] = new BattleFormationSaveEntry
                {
                    WarriorId = e.WarriorId,
                    PositionX = e.PositionX,
                    PositionZ = e.PositionZ,
                    RemainingHP = e.RemainingHP
                };
            }

            for (var i = 0; i < _groups.Count; i++)
            {
                data.Groups[i] = CloneGroup(_groups[i]);
            }

            PlayerPrefs.SetString(FormationKey(_slotIndex, _campaignMode), JsonUtility.ToJson(data));
            PlayerPrefs.Save();
        }

        private (float x, float z) NextAutoPosition()
        {
            return (_entries.Count * DefaultDeployStepX, 0f);
        }

        private int FindIndex(string warriorId)
        {
            for (var i = 0; i < _entries.Count; i++)
            {
                if (string.Equals(_entries[i].WarriorId, warriorId, StringComparison.Ordinal))
                {
                    return i;
                }
            }

            return -1;
        }

        private bool TryFindWarrior(string warriorId, out WarriorInstance warrior)
        {
            return _pool.TryGet(warriorId, out warrior);
        }

        private static TacticalFormationGroupSaveEntry CloneGroup(TacticalFormationGroupSaveEntry source)
        {
            var members = source.MemberIds ?? Array.Empty<string>();
            var copy = new string[members.Length];
            Array.Copy(members, copy, members.Length);
            return new TacticalFormationGroupSaveEntry
            {
                GroupInstanceId = source.GroupInstanceId,
                FormationId = source.FormationId,
                MemberIds = copy,
                FacingYawDegrees = source.FacingYawDegrees
            };
        }

        private static string FormationKey(int slotIndex, CampaignMode mode)
        {
            return SaveSlotPrefsKeys.DataKey(slotIndex, mode, SaveSlotPrefsKeys.BattleFormationSuffix);
        }
    }
}
