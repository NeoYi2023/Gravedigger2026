using System;
using System.Collections.Generic;

namespace Gravedigger2026.Core.UpgradeManufacture
{
    /// <summary>
    /// Save-scoped BattleFormation shared by UM formation panel and Defend Prepare (SPEC_03 §3.11 / D-032).
    /// Continuous XZ coordinates; ControlPower usage is derived from deployed warriors.
    /// </summary>
    public sealed class BattleFormationService
    {
        public const float DefaultDeployStepX = 2f;
        public const float DefaultNudgeStep = 1f;

        private readonly List<BattleFormationEntry> _entries = new List<BattleFormationEntry>();
        private readonly WarriorPoolService _pool;

        public BattleFormationService(WarriorPoolService pool)
        {
            _pool = pool ?? throw new ArgumentNullException(nameof(pool));
        }

        public IReadOnlyList<BattleFormationEntry> Entries => _entries;

        public event Action Changed;

        public void Clear()
        {
            _entries.Clear();
            Changed?.Invoke();
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
            }

            _entries.RemoveAt(index);
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
            warrior = null;
            var list = _pool.Warriors;
            for (var i = 0; i < list.Count; i++)
            {
                if (string.Equals(list[i].Id, warriorId, StringComparison.Ordinal))
                {
                    warrior = list[i];
                    return true;
                }
            }

            return false;
        }
    }
}
