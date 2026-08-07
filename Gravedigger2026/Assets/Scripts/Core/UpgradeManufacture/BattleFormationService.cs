using System;
using System.Collections.Generic;
using UnityEngine;

namespace Gravedigger2026.Core.UpgradeManufacture
{
    /// <summary>
    /// Save-scoped BattleFormation shared by UM / Defend / PushMap Prepare (SPEC_03 §3.11 / SPEC_04 §6).
    /// PlayerPrefs JSON per slot; mutate → immediate write when bound.
    /// </summary>
    public sealed class BattleFormationService
    {
        public const float DefaultDeployStepX = 2f;
        public const float DefaultNudgeStep = 1f;

        private const string KeyPrefix = "Gravedigger2026.SaveSlot.";
        private const string FormationSuffix = ".BattleFormation";

        private readonly List<BattleFormationEntry> _entries = new List<BattleFormationEntry>();
        private readonly WarriorPoolService _pool;
        private int _slotIndex = -1;
        private bool _suppressPersist;

        public BattleFormationService(WarriorPoolService pool)
        {
            _pool = pool ?? throw new ArgumentNullException(nameof(pool));
        }

        public int BoundSlotIndex => _slotIndex;
        public IReadOnlyList<BattleFormationEntry> Entries => _entries;

        public event Action Changed;

        /// <summary>
        /// Load formation for slot. Call after <see cref="WarriorPoolService.BindSlot"/> so orphan rows can be dropped.
        /// </summary>
        public void BindSlot(int slotIndex)
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
                _entries.Clear();

                var raw = PlayerPrefs.GetString(FormationKey(slotIndex), string.Empty);
                if (!string.IsNullOrEmpty(raw))
                {
                    var data = JsonUtility.FromJson<BattleFormationSaveData>(raw);
                    if (data?.Entries != null)
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
                                droppedOrphans = true;
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
            _suppressPersist = true;
            try
            {
                _entries.Clear();
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
            PersistIfBound();
            Changed?.Invoke();
        }

        public static void DeleteSlotData(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex > 2)
            {
                return;
            }

            PlayerPrefs.DeleteKey(FormationKey(slotIndex));
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
                Entries = new BattleFormationSaveEntry[_entries.Count]
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

            PlayerPrefs.SetString(FormationKey(_slotIndex), JsonUtility.ToJson(data));
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

        private static string FormationKey(int slotIndex)
        {
            return KeyPrefix + slotIndex + FormationSuffix;
        }
    }
}
