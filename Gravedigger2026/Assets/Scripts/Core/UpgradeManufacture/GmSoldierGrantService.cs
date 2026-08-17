using System;
using System.Collections.Generic;
using Gravedigger2026.Core.AutoManufacture;
using Gravedigger2026.Core.Config;
using UnityEngine;

namespace Gravedigger2026.Core.UpgradeManufacture
{
    public enum GmSoldierGrantError
    {
        None = 0,
        SoldierNotFound = 1,
        InvalidArgs = 2
    }

    /// <summary>
    /// Demo GM add-soldier (SPEC_03 UI-020 / D-064): free grant by class+race appearance match.
    /// </summary>
    public sealed class GmSoldierGrantService
    {
        public const float DemoBaseMaxHp = 100f;
        public const float DemoBaseMoveSpeed = 3f;
        public const float DemoBasePrimary = 20f;

        private readonly ConfigCsvRepository _configs;
        private readonly WarriorPoolService _warriorPool;
        private readonly AutoFormationDeployService _autoDeploy;

        public GmSoldierGrantService(
            ConfigCsvRepository configs,
            WarriorPoolService warriorPool,
            AutoFormationDeployService autoDeploy)
        {
            _configs = configs ?? throw new ArgumentNullException(nameof(configs));
            _warriorPool = warriorPool ?? throw new ArgumentNullException(nameof(warriorPool));
            _autoDeploy = autoDeploy ?? throw new ArgumentNullException(nameof(autoDeploy));
        }

        public bool TryAdd(
            string classId,
            string raceId,
            int count,
            bool autoDeploy,
            IReadOnlyList<FormationClassZoneSnapshot> zones,
            out int added,
            out int deployed,
            out GmSoldierGrantError error)
        {
            added = 0;
            deployed = 0;
            error = GmSoldierGrantError.None;

            if (string.IsNullOrEmpty(classId) || string.IsNullOrEmpty(raceId))
            {
                error = GmSoldierGrantError.InvalidArgs;
                return false;
            }

            if (!_configs.TryGetClass(classId, out var classRow) || classRow == null)
            {
                error = GmSoldierGrantError.SoldierNotFound;
                return false;
            }

            if (!_configs.TryGetRace(raceId, out var raceRow) || raceRow == null)
            {
                error = GmSoldierGrantError.SoldierNotFound;
                return false;
            }

            if (!TryPickAppearance(raceId, classRow, out var appearanceId))
            {
                error = GmSoldierGrantError.SoldierNotFound;
                return false;
            }

            var clamped = Mathf.Clamp(count, 1, 999);
            var batchIds = new List<string>(clamped);
            for (var i = 0; i < clamped; i++)
            {
                var instance = BuildInstance(classRow, raceRow, appearanceId);
                _warriorPool.Add(instance);
                batchIds.Add(instance.Id);
                added++;
            }

            if (autoDeploy && batchIds.Count > 0)
            {
                deployed = _autoDeploy.DeployBatch(batchIds, zones);
            }

            Debug.Log(
                $"[GmSoldierGrant] class={classId} race={raceId} appearance={appearanceId} " +
                $"added={added} autoDeploy={autoDeploy} deployed={deployed}");
            return true;
        }

        /// <summary>
        /// SPEC_03 §3.5 / D-064: RaceId + ClassAffinity(ClassName) match set.
        /// Multi-match: DefaultAppearanceId in set, else AppearanceLevel==ClassLevel, else first table order.
        /// No DefaultAppearanceId fallback when the set is empty.
        /// </summary>
        private bool TryPickAppearance(string raceId, ClassConfigRow classRow, out string appearanceId)
        {
            appearanceId = null;
            var className = classRow != null ? classRow.ClassName ?? string.Empty : string.Empty;
            var matches = new List<BodyAppearanceConfigRow>();
            var rows = _configs.BodyAppearances;
            for (var i = 0; i < rows.Count; i++)
            {
                var row = rows[i];
                if (row == null)
                {
                    continue;
                }

                if (!string.Equals(row.RaceId, raceId, StringComparison.Ordinal))
                {
                    continue;
                }

                if (!HasClassAffinity(row.ClassAffinity, className))
                {
                    continue;
                }

                matches.Add(row);
            }

            if (matches.Count == 0)
            {
                return false;
            }

            var defaultId = classRow.DefaultAppearanceId;
            if (!string.IsNullOrEmpty(defaultId))
            {
                for (var i = 0; i < matches.Count; i++)
                {
                    if (string.Equals(matches[i].AppearanceId, defaultId, StringComparison.Ordinal))
                    {
                        appearanceId = defaultId;
                        return true;
                    }
                }
            }

            var classLevel = classRow.ClassLevel;
            for (var i = 0; i < matches.Count; i++)
            {
                if (matches[i].AppearanceLevel == classLevel
                    && !string.IsNullOrEmpty(matches[i].AppearanceId))
                {
                    appearanceId = matches[i].AppearanceId;
                    return true;
                }
            }

            appearanceId = matches[0].AppearanceId;
            return !string.IsNullOrEmpty(appearanceId);
        }

        private WarriorInstance BuildInstance(
            ClassConfigRow classRow,
            RaceConfigRow raceRow,
            string appearanceId)
        {
            var baseStats = new StatBlock
            {
                MaxHP = DemoBaseMaxHp,
                MoveSpeed = DemoBaseMoveSpeed,
                Strength = DemoBasePrimary,
                Agility = DemoBasePrimary,
                Intelligence = DemoBasePrimary
            };
            var equip = default(StatBlock);
            var gemMult = default(StatBlock);
            var raceAdjust = raceRow.RaceAdjustCoeff;
            var staticStats = WarriorStatMath.ComputeStaticStats(baseStats, equip, gemMult, raceAdjust);
            var bodyLife = WarriorStatMath.ComputeBodyLife(baseStats, equip);
            var maxHp = WarriorStatMath.ComputeMaxHP(
                bodyLife, staticStats.Strength, _configs.GetMaxHpStrengthMult());

            var raceDisplay = string.IsNullOrEmpty(raceRow.DisplayNameKey)
                ? raceRow.RaceId
                : raceRow.DisplayNameKey;
            var className = classRow.ClassName ?? string.Empty;

            var instance = new WarriorInstance
            {
                Id = _warriorPool.ReserveNextId(),
                WarriorName = raceDisplay + className,
                RemainingHP = maxHp,
                RaceId = raceRow.RaceId,
                RaceAdjustCoeff = raceAdjust,
                BaseStats = baseStats,
                AppearanceId = appearanceId,
                SoulId = string.Empty,
                ClassId = classRow.ClassId,
                AttackMode = classRow.AttackMode,
                GemMult = gemMult,
                ControlPowerCost = 0f,
                EquipStats = equip,
                BodyLife = bodyLife,
                SourceSpiritCost = 0f
            };
            SoldierSkillGrant.GrantDefaultSkillsAtLevel1(instance, _configs);
            return instance;
        }

        private static bool HasClassAffinity(string classAffinity, string className)
        {
            if (string.IsNullOrEmpty(classAffinity) || string.IsNullOrEmpty(className))
            {
                return false;
            }

            var parts = classAffinity.Split('|');
            for (var i = 0; i < parts.Length; i++)
            {
                if (string.Equals(parts[i].Trim(), className, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
