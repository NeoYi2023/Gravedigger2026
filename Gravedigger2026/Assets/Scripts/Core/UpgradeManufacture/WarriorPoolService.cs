using System;
using System.Collections.Generic;
using Gravedigger2026.Core.Config;
using UnityEngine;

namespace Gravedigger2026.Core.UpgradeManufacture
{
    /// <summary>
    /// Save-scoped deployable soldier pool (SPEC_03 §3.11 / SPEC_04 §6).
    /// PlayerPrefs JSON per slot; mutate → immediate write when bound.
    /// </summary>
    public sealed class WarriorPoolService
    {
        private const string KeyPrefix = "Gravedigger2026.SaveSlot.";
        private const string PoolSuffix = ".WarriorPool";

        private readonly List<WarriorInstance> _warriors = new List<WarriorInstance>();
        private int _nextSerial = 1;
        private int _slotIndex = -1;
        private bool _suppressPersist;

        public int BoundSlotIndex => _slotIndex;
        public IReadOnlyList<WarriorInstance> Warriors => _warriors;

        public event Action Changed;

        public void BindSlot(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex > 2)
            {
                throw new ArgumentOutOfRangeException(nameof(slotIndex), slotIndex, "Slot index must be 0..2.");
            }

            _suppressPersist = true;
            try
            {
                _slotIndex = slotIndex;
                _warriors.Clear();
                _nextSerial = 1;

                var raw = PlayerPrefs.GetString(PoolKey(slotIndex), string.Empty);
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

            PlayerPrefs.DeleteKey(PoolKey(slotIndex));
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

            PlayerPrefs.SetString(PoolKey(_slotIndex), JsonUtility.ToJson(data));
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

        private static string PoolKey(int slotIndex)
        {
            return KeyPrefix + slotIndex + PoolSuffix;
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
                SourceSpiritCost = w.SourceSpiritCost
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
                SourceSpiritCost = dto.SourceSpiritCost
            };

            CopyIds(dto.LockedEquipIds, w.LockedEquipIds);
            CopyIds(dto.GemIds, w.GemIds);
            CopyIds(dto.SourceItemIds, w.SourceItemIds);
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
    }
}
