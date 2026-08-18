using System;
using System.Collections.Generic;
using Gravedigger2026.Core;
using Gravedigger2026.Core.Config;
using UnityEngine;

namespace Gravedigger2026.Core.UpgradeManufacture
{
    /// <summary>
    /// Save-scoped deployable soldier pool (SPEC_03 §3.11 / SPEC_04 §6).
    /// PlayerPrefs JSON per slot + CampaignMode; mutate → immediate write when bound.
    /// </summary>
    public sealed class WarriorPoolService
    {
        private readonly List<WarriorInstance> _warriors = new List<WarriorInstance>();
        private int _nextSerial = 1;
        private int _slotIndex = -1;
        private CampaignMode _campaignMode = CampaignMode.Mode1;
        private bool _suppressPersist;

        public int BoundSlotIndex => _slotIndex;
        public CampaignMode BoundCampaignMode => _campaignMode;
        public IReadOnlyList<WarriorInstance> Warriors => _warriors;

        public event Action Changed;

        public void BindSlot(int slotIndex, CampaignMode campaignMode)
        {
            if (slotIndex < 0 || slotIndex > 2)
            {
                throw new ArgumentOutOfRangeException(nameof(slotIndex), slotIndex, "Slot index must be 0..2.");
            }

            _suppressPersist = true;
            try
            {
                _slotIndex = slotIndex;
                _campaignMode = campaignMode;
                _warriors.Clear();
                _nextSerial = 1;

                var key = PoolKey(slotIndex, campaignMode);
                var raw = PlayerPrefs.GetString(key, string.Empty);
                var migratedFromLegacy = false;

                if (string.IsNullOrEmpty(raw) && campaignMode == CampaignMode.Mode1)
                {
                    var legacyKey = SaveSlotPrefsKeys.LegacyDataKey(
                        slotIndex, SaveSlotPrefsKeys.WarriorPoolSuffix);
                    raw = PlayerPrefs.GetString(legacyKey, string.Empty);
                    if (!string.IsNullOrEmpty(raw))
                    {
                        migratedFromLegacy = true;
                    }
                }

                if (!string.IsNullOrEmpty(raw))
                {
                    var data = JsonUtility.FromJson<WarriorPoolSaveData>(raw);
                    if (data != null)
                    {
                        if (data.NextSerial > 0)
                        {
                            _nextSerial = data.NextSerial;
                        }

                        if (data.Warriors != null)
                        {
                            for (var i = 0; i < data.Warriors.Length; i++)
                            {
                                var dto = data.Warriors[i];
                                if (dto == null || string.IsNullOrEmpty(dto.Id))
                                {
                                    continue;
                                }

                                _warriors.Add(FromDto(dto));
                            }
                        }

                        EnsureNextSerialAboveExisting();
                    }
                }

                if (migratedFromLegacy)
                {
                    _suppressPersist = false;
                    PersistIfBound();
                    _suppressPersist = true;
                    PlayerPrefs.DeleteKey(
                        SaveSlotPrefsKeys.LegacyDataKey(slotIndex, SaveSlotPrefsKeys.WarriorPoolSuffix));
                    PlayerPrefs.Save();
                }
            }
            finally
            {
                _suppressPersist = false;
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
                _warriors.Clear();
                _nextSerial = 1;
            }
            finally
            {
                _suppressPersist = false;
            }

            Changed?.Invoke();
        }

        /// <summary>Clears memory without unbinding; used only when not yet bound.</summary>
        public void Clear()
        {
            _warriors.Clear();
            _nextSerial = 1;
            PersistIfBound();
            Changed?.Invoke();
        }

        public static void DeleteSlotData(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex > 2)
            {
                return;
            }

            PlayerPrefs.DeleteKey(PoolKey(slotIndex, CampaignMode.Mode1));
            PlayerPrefs.DeleteKey(PoolKey(slotIndex, CampaignMode.Mode2));
            PlayerPrefs.DeleteKey(
                SaveSlotPrefsKeys.LegacyDataKey(slotIndex, SaveSlotPrefsKeys.WarriorPoolSuffix));
            PlayerPrefs.Save();
        }

        public string ReserveNextId()
        {
            return "W_" + _nextSerial.ToString("D3");
        }

        public void Add(WarriorInstance instance)
        {
            if (instance == null)
            {
                return;
            }

            _warriors.Add(instance);
            _nextSerial++;
            PersistIfBound();
            Changed?.Invoke();
        }

        public bool TryRemove(string warriorId)
        {
            if (string.IsNullOrEmpty(warriorId))
            {
                return false;
            }

            for (var i = 0; i < _warriors.Count; i++)
            {
                if (string.Equals(_warriors[i].Id, warriorId, StringComparison.Ordinal))
                {
                    _warriors.RemoveAt(i);
                    PersistIfBound();
                    Changed?.Invoke();
                    return true;
                }
            }

            return false;
        }

        public bool TryGet(string warriorId, out WarriorInstance instance)
        {
            instance = null;
            if (string.IsNullOrEmpty(warriorId))
            {
                return false;
            }

            for (var i = 0; i < _warriors.Count; i++)
            {
                if (string.Equals(_warriors[i].Id, warriorId, StringComparison.Ordinal))
                {
                    instance = _warriors[i];
                    return true;
                }
            }

            return false;
        }

        /// <summary>Call after mutating a bound warrior's RemainingHP (or other snapshot fields).</summary>
        public void NotifyMutated()
        {
            PersistIfBound();
            Changed?.Invoke();
        }

        private void PersistIfBound()
        {
            if (_suppressPersist || _slotIndex < 0)
            {
                return;
            }

            var data = new WarriorPoolSaveData
            {
                NextSerial = _nextSerial,
                Warriors = new WarriorSaveDto[_warriors.Count]
            };

            for (var i = 0; i < _warriors.Count; i++)
            {
                data.Warriors[i] = ToDto(_warriors[i]);
            }

            PlayerPrefs.SetString(PoolKey(_slotIndex, _campaignMode), JsonUtility.ToJson(data));
            PlayerPrefs.Save();
        }

        private void EnsureNextSerialAboveExisting()
        {
            var maxFromIds = 0;
            for (var i = 0; i < _warriors.Count; i++)
            {
                var id = _warriors[i].Id;
                if (string.IsNullOrEmpty(id) || !id.StartsWith("W_", StringComparison.Ordinal))
                {
                    continue;
                }

                if (int.TryParse(id.Substring(2), out var n) && n > maxFromIds)
                {
                    maxFromIds = n;
                }
            }

            if (_nextSerial <= maxFromIds)
            {
                _nextSerial = maxFromIds + 1;
            }
        }

        private static string PoolKey(int slotIndex, CampaignMode mode)
        {
            return SaveSlotPrefsKeys.DataKey(slotIndex, mode, SaveSlotPrefsKeys.WarriorPoolSuffix);
        }

        private static WarriorSaveDto ToDto(WarriorInstance w)
        {
            return new WarriorSaveDto
            {
                Id = w.Id,
                WarriorName = w.WarriorName,
                RemainingHP = w.RemainingHP,
                RaceId = w.RaceId,
                RaceAdjustCoeff = w.RaceAdjustCoeff,
                BaseStats = w.BaseStats,
                AppearanceId = w.AppearanceId,
                SoulId = w.SoulId,
                ClassId = w.ClassId,
                AttackMode = (int)w.AttackMode,
                LockedEquipIds = ToArray(w.LockedEquipIds),
                GemIds = ToArray(w.GemIds),
                GemMult = w.GemMult,
                ControlPowerCost = w.ControlPowerCost,
                EquipStats = w.EquipStats,
                BodyLife = w.BodyLife,
                SourceItemIds = ToArray(w.SourceItemIds),
                SourceSpiritCost = w.SourceSpiritCost,
                SoldierSkills = ToSkillArray(w.SoldierSkills),
                VisualStyleId = w.VisualStyleId,
                VisualPriority = w.VisualPriority,
                VisualIntensity = w.VisualIntensity,
                VisualModelScale = WarriorVisualModelScale.Resolve(w)
            };
        }

        private static WarriorInstance FromDto(WarriorSaveDto dto)
        {
            var w = new WarriorInstance
            {
                Id = dto.Id,
                WarriorName = dto.WarriorName,
                RemainingHP = dto.RemainingHP,
                RaceId = dto.RaceId,
                RaceAdjustCoeff = dto.RaceAdjustCoeff,
                BaseStats = dto.BaseStats,
                AppearanceId = dto.AppearanceId,
                SoulId = dto.SoulId,
                ClassId = dto.ClassId,
                AttackMode = dto.AttackMode == (int)AttackMode.Ranged ? AttackMode.Ranged : AttackMode.Melee,
                GemMult = dto.GemMult,
                ControlPowerCost = dto.ControlPowerCost,
                EquipStats = dto.EquipStats,
                BodyLife = dto.BodyLife,
                SourceSpiritCost = dto.SourceSpiritCost,
                VisualStyleId = dto.VisualStyleId,
                VisualPriority = dto.VisualPriority,
                VisualIntensity = dto.VisualIntensity,
                VisualModelScale = dto.VisualModelScale > 0f ? dto.VisualModelScale : 1f
            };

            CopyIds(dto.LockedEquipIds, w.LockedEquipIds);
            CopyIds(dto.GemIds, w.GemIds);
            CopyIds(dto.SourceItemIds, w.SourceItemIds);
            CopySkills(dto.SoldierSkills, w.SoldierSkills);
            return w;
        }

        private static string[] ToArray(List<string> list)
        {
            if (list == null || list.Count == 0)
            {
                return Array.Empty<string>();
            }

            return list.ToArray();
        }

        private static void CopyIds(string[] source, List<string> dest)
        {
            dest.Clear();
            if (source == null)
            {
                return;
            }

            for (var i = 0; i < source.Length; i++)
            {
                if (!string.IsNullOrEmpty(source[i]))
                {
                    dest.Add(source[i]);
                }
            }
        }

        private static SoldierSkillEntry[] ToSkillArray(List<SoldierSkillEntry> list)
        {
            if (list == null || list.Count == 0)
            {
                return Array.Empty<SoldierSkillEntry>();
            }

            var count = 0;
            for (var i = 0; i < list.Count; i++)
            {
                if (IsPersistableSkill(list[i]))
                {
                    count++;
                }
            }

            if (count == 0)
            {
                return Array.Empty<SoldierSkillEntry>();
            }

            var result = new SoldierSkillEntry[count];
            var n = 0;
            for (var i = 0; i < list.Count; i++)
            {
                var entry = list[i];
                if (!IsPersistableSkill(entry))
                {
                    continue;
                }

                result[n++] = CloneSkill(entry);
            }

            return result;
        }

        private static void CopySkills(SoldierSkillEntry[] source, List<SoldierSkillEntry> dest)
        {
            dest.Clear();
            if (source == null || source.Length == 0)
            {
                return;
            }

            for (var i = 0; i < source.Length; i++)
            {
                var entry = source[i];
                if (!IsPersistableSkill(entry))
                {
                    continue;
                }

                dest.Add(CloneSkill(entry));
            }
        }

        private static bool IsPersistableSkill(SoldierSkillEntry entry)
        {
            return entry != null && !string.IsNullOrEmpty(entry.SkillId);
        }

        private static SoldierSkillEntry CloneSkill(SoldierSkillEntry entry)
        {
            return new SoldierSkillEntry
            {
                SkillId = entry.SkillId,
                SkillLevel = entry.SkillLevel
            };
        }
    }
}
