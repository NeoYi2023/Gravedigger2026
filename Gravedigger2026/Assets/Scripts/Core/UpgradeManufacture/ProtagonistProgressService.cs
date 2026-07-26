using System;
using System.Collections.Generic;
using Gravedigger2026.Core.Config;
using UnityEngine;

namespace Gravedigger2026.Core.UpgradeManufacture
{
    /// <summary>
    /// In-memory protagonist Level / LifetimeExperience / TechPoints / caps (SPEC_03 §3.11 / D-030 / D-043).
    /// Formal Exp credit on Defend victory via AddExperience(DemoStageExperienceReward).
    /// </summary>
    public sealed class ProtagonistProgressService
    {
        private readonly Dictionary<int, ProtagonistLevelConfigRow> _levelsById =
            new Dictionary<int, ProtagonistLevelConfigRow>();

        private int _maxConfiguredLevel;

        public int Level { get; private set; } = 1;
        public long LifetimeExperience { get; private set; }
        public int TechPoints { get; private set; }
        public int ControlPowerCap { get; private set; }
        public int ProtagonistMaxHP { get; private set; }

        public event Action Changed;

        public void ResetToLevelOne(ConfigCsvRepository configs)
        {
            RebuildLevelIndex(configs);
            Level = 1;
            LifetimeExperience = 0;
            TechPoints = 0;
            ApplyRowCaps(1, grantReward: false);
            Changed?.Invoke();
        }

        public void EnsureLoaded(ConfigCsvRepository configs)
        {
            if (_levelsById.Count == 0)
            {
                RebuildLevelIndex(configs);
                if (Level < 1)
                {
                    Level = 1;
                }

                ApplyRowCaps(Level, grantReward: false);
            }
        }

        /// <summary>
        /// Adds lifetime Exp and chain-levels while thresholds are met (does not deduct Exp).
        /// </summary>
        public int AddExperience(long amount)
        {
            if (amount <= 0 || _levelsById.Count == 0)
            {
                return 0;
            }

            LifetimeExperience += amount;
            var levelsGained = 0;
            while (TryGetRow(Level + 1, out var next)
                   && LifetimeExperience >= next.RequiredTotalExperience)
            {
                Level = next.Level;
                TechPoints += Math.Max(0, next.TechPointsReward);
                ControlPowerCap = next.ControlPowerCap;
                ProtagonistMaxHP = next.ProtagonistMaxHP;
                levelsGained++;
            }

            Changed?.Invoke();
            return levelsGained;
        }

        public bool TryGetRow(int level, out ProtagonistLevelConfigRow row)
        {
            return _levelsById.TryGetValue(level, out row);
        }

        public long GetNextRequiredTotalExperience()
        {
            if (TryGetRow(Level + 1, out var next))
            {
                return next.RequiredTotalExperience;
            }

            return LifetimeExperience;
        }

        public bool IsMaxLevel => Level >= _maxConfiguredLevel;

        public bool TrySpendTechPoints(int amount)
        {
            if (amount < 0)
            {
                return false;
            }

            if (amount == 0)
            {
                return true;
            }

            if (TechPoints < amount)
            {
                return false;
            }

            TechPoints -= amount;
            Changed?.Invoke();
            return true;
        }

        public void DebugGrantTechPoints(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            TechPoints += amount;
            Changed?.Invoke();
        }

        private void RebuildLevelIndex(ConfigCsvRepository configs)
        {
            _levelsById.Clear();
            _maxConfiguredLevel = 0;
            if (configs == null)
            {
                Debug.LogError("[ProtagonistProgress] ConfigCsvRepository null.");
                return;
            }

            var rows = configs.GetAllProtagonistLevels();
            for (var i = 0; i < rows.Count; i++)
            {
                var row = rows[i];
                if (row == null || row.Level < 1)
                {
                    continue;
                }

                _levelsById[row.Level] = row;
                if (row.Level > _maxConfiguredLevel)
                {
                    _maxConfiguredLevel = row.Level;
                }
            }

            if (_levelsById.Count == 0)
            {
                Debug.LogError("[ProtagonistProgress] ProtagonistLevelConfig empty.");
            }
        }

        private void ApplyRowCaps(int level, bool grantReward)
        {
            if (!TryGetRow(level, out var row))
            {
                Debug.LogWarning($"[ProtagonistProgress] Missing level row {level}.");
                ControlPowerCap = 0;
                ProtagonistMaxHP = 0;
                return;
            }

            ControlPowerCap = row.ControlPowerCap;
            ProtagonistMaxHP = row.ProtagonistMaxHP;
            if (grantReward)
            {
                TechPoints += Math.Max(0, row.TechPointsReward);
            }
        }
    }
}
