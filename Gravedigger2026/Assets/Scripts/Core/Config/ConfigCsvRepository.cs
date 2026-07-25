using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace Gravedigger2026.Core.Config
{
    /// <summary>
    /// Runtime CSV-only config store (SPEC_04 §14). Level + Dig/Defend keys + Dig tables for D-020.
    /// </summary>
    public sealed class ConfigCsvRepository
    {
        private readonly List<LevelOperationConfigRow> _levelOperations = new List<LevelOperationConfigRow>();
        private readonly Dictionary<string, DigGameplayConfigRow> _digById =
            new Dictionary<string, DigGameplayConfigRow>(StringComparer.Ordinal);
        private readonly Dictionary<string, DefendGameplayConfigRow> _defendById =
            new Dictionary<string, DefendGameplayConfigRow>(StringComparer.Ordinal);
        private readonly Dictionary<string, GraveQualityConfigRow> _graveById =
            new Dictionary<string, GraveQualityConfigRow>(StringComparer.Ordinal);
        private readonly Dictionary<string, MaterialConfigRow> _materialById =
            new Dictionary<string, MaterialConfigRow>(StringComparer.Ordinal);
        private readonly Dictionary<string, CurrencyConfigRow> _currencyById =
            new Dictionary<string, CurrencyConfigRow>(StringComparer.Ordinal);
        private readonly Dictionary<int, ProtagonistLevelConfigRow> _protagonistLevelById =
            new Dictionary<int, ProtagonistLevelConfigRow>();
        private readonly Dictionary<string, BodyPartConfigRow> _bodyPartById =
            new Dictionary<string, BodyPartConfigRow>(StringComparer.Ordinal);
        private readonly Dictionary<string, SoulConfigRow> _soulById =
            new Dictionary<string, SoulConfigRow>(StringComparer.Ordinal);
        private readonly Dictionary<string, ClassConfigRow> _classById =
            new Dictionary<string, ClassConfigRow>(StringComparer.Ordinal);
        private readonly Dictionary<string, RaceConfigRow> _raceById =
            new Dictionary<string, RaceConfigRow>(StringComparer.Ordinal);
        private readonly Dictionary<string, GemConfigRow> _gemById =
            new Dictionary<string, GemConfigRow>(StringComparer.Ordinal);
        private readonly Dictionary<string, ExtraEquipmentConfigRow> _equipById =
            new Dictionary<string, ExtraEquipmentConfigRow>(StringComparer.Ordinal);
        private readonly Dictionary<string, string> _gemSuffixByComboKey =
            new Dictionary<string, string>(StringComparer.Ordinal);
        private readonly List<BodyAppearanceConfigRow> _appearances = new List<BodyAppearanceConfigRow>();

        public bool IsLoaded { get; private set; }
        public string LastError { get; private set; }

        public bool TryLoadAll()
        {
            IsLoaded = false;
            LastError = null;
            _levelOperations.Clear();
            _digById.Clear();
            _defendById.Clear();
            _graveById.Clear();
            _materialById.Clear();
            _currencyById.Clear();
            _protagonistLevelById.Clear();
            _bodyPartById.Clear();
            _soulById.Clear();
            _classById.Clear();
            _raceById.Clear();
            _gemById.Clear();
            _equipById.Clear();
            _gemSuffixByComboKey.Clear();
            _appearances.Clear();

            try
            {
                LoadLevelOperations();
                LoadDigGameplay();
                LoadDefendGameplay();
                LoadGraveQuality();
                LoadMaterial();
                LoadCurrency();
                LoadProtagonistLevels();
                LoadBodyParts();
                LoadSouls();
                LoadClasses();
                LoadRaces();
                LoadGems();
                LoadExtraEquipment();
                LoadGemSuffixNames();
                LoadBodyAppearances();
                IsLoaded = true;
                Debug.Log(
                    $"[ConfigCsvRepository] Loaded LevelOps={_levelOperations.Count}, Dig={_digById.Count}, Defend={_defendById.Count}, Grave={_graveById.Count}, Mat={_materialById.Count}, Cur={_currencyById.Count}, ProtagonistLevel={_protagonistLevelById.Count}, BodyPart={_bodyPartById.Count}, Soul={_soulById.Count}, Class={_classById.Count}, Race={_raceById.Count}, Gem={_gemById.Count}, Equip={_equipById.Count}, GemSuffix={_gemSuffixByComboKey.Count}, Appearance={_appearances.Count}.");
                return true;
            }
            catch (Exception ex)
            {
                LastError = ex.Message;
                Debug.LogError($"[ConfigCsvRepository] Load failed: {ex.Message}");
                return false;
            }
        }

        public List<LevelOperationConfigRow> GetStagesForLevel(string levelId)
        {
            var result = new List<LevelOperationConfigRow>();
            if (string.IsNullOrEmpty(levelId))
            {
                return result;
            }

            for (var i = 0; i < _levelOperations.Count; i++)
            {
                var row = _levelOperations[i];
                if (string.Equals(row.LevelId, levelId, StringComparison.Ordinal))
                {
                    result.Add(row);
                }
            }

            result.Sort((a, b) => a.StageNumber.CompareTo(b.StageNumber));
            return result;
        }

        public bool TryGetDig(string gameplayConfigId, out DigGameplayConfigRow row)
        {
            return _digById.TryGetValue(gameplayConfigId ?? string.Empty, out row);
        }

        public bool TryGetDefend(string gameplayConfigId, out DefendGameplayConfigRow row)
        {
            return _defendById.TryGetValue(gameplayConfigId ?? string.Empty, out row);
        }

        public bool TryGetGraveQuality(string qualityId, out GraveQualityConfigRow row)
        {
            return _graveById.TryGetValue(qualityId ?? string.Empty, out row);
        }

        public bool TryGetMaterial(string materialId, out MaterialConfigRow row)
        {
            return _materialById.TryGetValue(materialId ?? string.Empty, out row);
        }

        public bool TryGetCurrency(string currencyId, out CurrencyConfigRow row)
        {
            return _currencyById.TryGetValue(currencyId ?? string.Empty, out row);
        }

        public IReadOnlyCollection<string> GetAllGraveQualityIds()
        {
            return _graveById.Keys;
        }

        public bool TryGetProtagonistLevel(int level, out ProtagonistLevelConfigRow row)
        {
            return _protagonistLevelById.TryGetValue(level, out row);
        }

        public List<ProtagonistLevelConfigRow> GetAllProtagonistLevels()
        {
            var result = new List<ProtagonistLevelConfigRow>(_protagonistLevelById.Values);
            result.Sort((a, b) => a.Level.CompareTo(b.Level));
            return result;
        }

        public bool TryGetBodyPart(string bodyPartId, out BodyPartConfigRow row)
        {
            return _bodyPartById.TryGetValue(bodyPartId ?? string.Empty, out row);
        }

        public bool TryGetSoul(string soulId, out SoulConfigRow row)
        {
            return _soulById.TryGetValue(soulId ?? string.Empty, out row);
        }

        public bool TryGetClass(string classId, out ClassConfigRow row)
        {
            return _classById.TryGetValue(classId ?? string.Empty, out row);
        }

        public bool TryGetRace(string raceId, out RaceConfigRow row)
        {
            return _raceById.TryGetValue(raceId ?? string.Empty, out row);
        }

        public bool TryGetGem(string gemId, out GemConfigRow row)
        {
            return _gemById.TryGetValue(gemId ?? string.Empty, out row);
        }

        public bool TryGetExtraEquipment(string equipId, out ExtraEquipmentConfigRow row)
        {
            return _equipById.TryGetValue(equipId ?? string.Empty, out row);
        }

        public bool TryGetGemSuffix(string comboKey, out string suffix)
        {
            return _gemSuffixByComboKey.TryGetValue(comboKey ?? string.Empty, out suffix);
        }

        public IReadOnlyList<BodyAppearanceConfigRow> BodyAppearances => _appearances;

        public IEnumerable<BodyPartConfigRow> BodyParts => _bodyPartById.Values;

        public IEnumerable<SoulConfigRow> Souls => _soulById.Values;

        public IEnumerable<GemConfigRow> Gems => _gemById.Values;

        public IEnumerable<ExtraEquipmentConfigRow> ExtraEquipments => _equipById.Values;

        private void LoadLevelOperations()
        {
            const string table = "Level_LevelOperationConfig.csv";
            var path = RequirePath(table);
            var rows = SimpleCsv.ReadRows(path);
            for (var i = 0; i < rows.Count; i++)
            {
                var raw = rows[i];
                var rowIndex = i + 2;
                var typeText = SimpleCsv.Require(raw, "GameplayType", table, rowIndex);
                if (!TryParseGameplayType(typeText, out var gameplayType))
                {
                    throw new InvalidOperationException(
                        $"{table} row {rowIndex}: illegal GameplayType '{typeText}'.");
                }

                var stageText = SimpleCsv.Require(raw, "StageNumber", table, rowIndex);
                if (!int.TryParse(stageText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var stageNumber))
                {
                    throw new InvalidOperationException(
                        $"{table} row {rowIndex}: illegal StageNumber '{stageText}'.");
                }

                _levelOperations.Add(new LevelOperationConfigRow
                {
                    LevelId = SimpleCsv.Require(raw, "LevelId", table, rowIndex),
                    StageNumber = stageNumber,
                    GameplayType = gameplayType,
                    GameplayConfigId = SimpleCsv.Require(raw, "GameplayConfigId", table, rowIndex)
                });
            }
        }

        private void LoadDigGameplay()
        {
            const string table = "Dig_DigGameplayConfig.csv";
            var path = RequirePath(table);
            var rows = SimpleCsv.ReadRows(path);
            for (var i = 0; i < rows.Count; i++)
            {
                var raw = rows[i];
                var rowIndex = i + 2;
                var id = SimpleCsv.Require(raw, "GameplayConfigId", table, rowIndex);
                var durationText = SimpleCsv.Require(raw, "LevelDurationSeconds", table, rowIndex);
                var graveText = SimpleCsv.Require(raw, "InitialGraveCount", table, rowIndex);
                if (!float.TryParse(durationText, NumberStyles.Float, CultureInfo.InvariantCulture, out var duration))
                {
                    throw new InvalidOperationException(
                        $"{table} row {rowIndex}: illegal LevelDurationSeconds '{durationText}'.");
                }

                if (!int.TryParse(graveText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var graves))
                {
                    throw new InvalidOperationException(
                        $"{table} row {rowIndex}: illegal InitialGraveCount '{graveText}'.");
                }

                var dig = new DigGameplayConfigRow
                {
                    GameplayConfigId = id,
                    DigMapId = SimpleCsv.Require(raw, "DigMapId", table, rowIndex),
                    LevelDurationSeconds = duration,
                    InitialGraveCount = graves,
                    SpawnRate = SimpleCsv.Require(raw, "SpawnRate", table, rowIndex),
                    GraveSpawnWeights = SimpleCsv.Require(raw, "GraveSpawnWeights", table, rowIndex)
                };
                _digById[id] = dig;
            }
        }

        private void LoadDefendGameplay()
        {
            const string table = "Defend_DefendGameplayConfig.csv";
            var path = RequirePath(table);
            var rows = SimpleCsv.ReadRows(path);
            for (var i = 0; i < rows.Count; i++)
            {
                var raw = rows[i];
                var rowIndex = i + 2;
                var id = SimpleCsv.Require(raw, "GameplayConfigId", table, rowIndex);
                var combatText = SimpleCsv.Require(raw, "CombatDurationSeconds", table, rowIndex);
                var retargetText = SimpleCsv.Require(raw, "TargetRetargetIntervalSeconds", table, rowIndex);
                if (!int.TryParse(combatText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var combat))
                {
                    throw new InvalidOperationException(
                        $"{table} row {rowIndex}: illegal CombatDurationSeconds '{combatText}'.");
                }

                if (!float.TryParse(retargetText, NumberStyles.Float, CultureInfo.InvariantCulture, out var retarget))
                {
                    throw new InvalidOperationException(
                        $"{table} row {rowIndex}: illegal TargetRetargetIntervalSeconds '{retargetText}'.");
                }

                var defend = new DefendGameplayConfigRow
                {
                    GameplayConfigId = id,
                    BattleMapId = SimpleCsv.Require(raw, "BattleMapId", table, rowIndex),
                    WaveConfigId = SimpleCsv.Require(raw, "WaveConfigId", table, rowIndex),
                    CombatDurationSeconds = combat,
                    TargetRetargetIntervalSeconds = retarget
                };
                _defendById[id] = defend;
            }
        }

        private void LoadGraveQuality()
        {
            const string table = "Dig_GraveQualityConfig.csv";
            var path = RequirePath(table);
            var rows = SimpleCsv.ReadRows(path);
            for (var i = 0; i < rows.Count; i++)
            {
                var raw = rows[i];
                var rowIndex = i + 2;
                var id = SimpleCsv.Require(raw, "QualityId", table, rowIndex);
                var hpText = SimpleCsv.Require(raw, "MaxHP", table, rowIndex);
                if (!float.TryParse(hpText, NumberStyles.Float, CultureInfo.InvariantCulture, out var maxHp))
                {
                    throw new InvalidOperationException($"{table} row {rowIndex}: illegal MaxHP '{hpText}'.");
                }

                _graveById[id] = new GraveQualityConfigRow
                {
                    QualityId = id,
                    MaxHP = maxHp,
                    LootDrop = SimpleCsv.Require(raw, "LootDrop", table, rowIndex),
                    IconStyleHighId = SimpleCsv.Require(raw, "IconStyleHighId", table, rowIndex),
                    IconStyleMidId = SimpleCsv.Require(raw, "IconStyleMidId", table, rowIndex),
                    IconStyleLowId = SimpleCsv.Require(raw, "IconStyleLowId", table, rowIndex)
                };
            }
        }

        private void LoadMaterial()
        {
            const string table = "Dig_MaterialConfig.csv";
            var path = RequirePath(table);
            var rows = SimpleCsv.ReadRows(path);
            for (var i = 0; i < rows.Count; i++)
            {
                var raw = rows[i];
                var rowIndex = i + 2;
                var id = SimpleCsv.Require(raw, "MaterialId", table, rowIndex);
                var convertText = SimpleCsv.Require(raw, "AutoConvert", table, rowIndex);
                if (!float.TryParse(convertText, NumberStyles.Float, CultureInfo.InvariantCulture, out var autoConvert))
                {
                    throw new InvalidOperationException(
                        $"{table} row {rowIndex}: illegal AutoConvert '{convertText}'.");
                }

                _materialById[id] = new MaterialConfigRow
                {
                    MaterialId = id,
                    AutoConvert = autoConvert,
                    AppearanceIconId = SimpleCsv.Require(raw, "AppearanceIconId", table, rowIndex),
                    AssetPath = SimpleCsv.Require(raw, "AssetPath", table, rowIndex),
                    WarehouseQualityOutlineId = SimpleCsv.Require(raw, "WarehouseQualityOutlineId", table, rowIndex)
                };
            }
        }

        private void LoadCurrency()
        {
            const string table = "Dig_CurrencyConfig.csv";
            var path = RequirePath(table);
            var rows = SimpleCsv.ReadRows(path);
            for (var i = 0; i < rows.Count; i++)
            {
                var raw = rows[i];
                var rowIndex = i + 2;
                var id = SimpleCsv.Require(raw, "CurrencyId", table, rowIndex);
                _currencyById[id] = new CurrencyConfigRow
                {
                    CurrencyId = id,
                    AppearanceIconId = SimpleCsv.Require(raw, "AppearanceIconId", table, rowIndex),
                    AssetPath = SimpleCsv.Require(raw, "AssetPath", table, rowIndex),
                    WarehouseQualityOutlineId = SimpleCsv.Require(raw, "WarehouseQualityOutlineId", table, rowIndex)
                };
            }
        }

        private void LoadProtagonistLevels()
        {
            const string table = "Manufacture_ProtagonistLevelConfig.csv";
            var path = RequirePath(table);
            var rows = SimpleCsv.ReadRows(path);
            for (var i = 0; i < rows.Count; i++)
            {
                var raw = rows[i];
                var rowIndex = i + 2;
                var levelText = SimpleCsv.Require(raw, "Level", table, rowIndex);
                var expText = SimpleCsv.Require(raw, "RequiredTotalExperience", table, rowIndex);
                var techText = SimpleCsv.Require(raw, "TechPointsReward", table, rowIndex);
                var capText = SimpleCsv.Require(raw, "ControlPowerCap", table, rowIndex);
                var hpText = SimpleCsv.Require(raw, "ProtagonistMaxHP", table, rowIndex);

                if (!int.TryParse(levelText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var level)
                    || level < 1)
                {
                    throw new InvalidOperationException($"{table} row {rowIndex}: illegal Level '{levelText}'.");
                }

                if (!long.TryParse(expText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var requiredExp))
                {
                    throw new InvalidOperationException(
                        $"{table} row {rowIndex}: illegal RequiredTotalExperience '{expText}'.");
                }

                if (!int.TryParse(techText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var techPoints))
                {
                    throw new InvalidOperationException(
                        $"{table} row {rowIndex}: illegal TechPointsReward '{techText}'.");
                }

                if (!int.TryParse(capText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var controlCap))
                {
                    throw new InvalidOperationException(
                        $"{table} row {rowIndex}: illegal ControlPowerCap '{capText}'.");
                }

                if (!int.TryParse(hpText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var maxHp))
                {
                    throw new InvalidOperationException(
                        $"{table} row {rowIndex}: illegal ProtagonistMaxHP '{hpText}'.");
                }

                raw.TryGetValue("UnlockedFeatureIds", out var unlocked);
                _protagonistLevelById[level] = new ProtagonistLevelConfigRow
                {
                    Level = level,
                    RequiredTotalExperience = requiredExp,
                    UnlockedFeatureIds = unlocked ?? string.Empty,
                    TechPointsReward = techPoints,
                    ControlPowerCap = controlCap,
                    ProtagonistMaxHP = maxHp
                };
            }
        }

        private void LoadBodyParts()
        {
            const string table = "Manufacture_BodyPartConfig.csv";
            var rows = SimpleCsv.ReadRows(RequirePath(table));
            for (var i = 0; i < rows.Count; i++)
            {
                var raw = rows[i];
                var rowIndex = i + 2;
                var id = SimpleCsv.Require(raw, "BodyPartId", table, rowIndex);
                var slotText = SimpleCsv.Require(raw, "BodySlot", table, rowIndex);
                if (!Enum.TryParse(slotText, false, out BodySlot slot))
                {
                    throw new InvalidOperationException($"{table} row {rowIndex}: illegal BodySlot '{slotText}'.");
                }

                _bodyPartById[id] = new BodyPartConfigRow
                {
                    BodyPartId = id,
                    BodyLevel = RequireFloat(raw, "BodyLevel", table, rowIndex),
                    BodySlot = slot,
                    RaceId = SimpleCsv.Require(raw, "RaceId", table, rowIndex),
                    ControlPowerCost = OptionalFloat(raw, "ControlPowerCost"),
                    SpiritCost = OptionalFloat(raw, "SpiritCost"),
                    StatBonus = StatFieldParser.Parse(
                        SimpleCsv.Require(raw, "StatBonus", table, rowIndex),
                        m => Debug.LogWarning($"[Config] {table} row {rowIndex}: {m}")),
                    AutoConvert = OptionalFloat(raw, "AutoConvert"),
                    Description = OptionalText(raw, "Description"),
                    ArtAssetId = OptionalText(raw, "ArtAssetId")
                };
            }
        }

        private void LoadSouls()
        {
            const string table = "Manufacture_SoulConfig.csv";
            var rows = SimpleCsv.ReadRows(RequirePath(table));
            for (var i = 0; i < rows.Count; i++)
            {
                var raw = rows[i];
                var rowIndex = i + 2;
                var id = SimpleCsv.Require(raw, "SoulId", table, rowIndex);
                var modeText = SimpleCsv.Require(raw, "AttackMode", table, rowIndex);
                if (!Enum.TryParse(modeText, false, out AttackMode mode))
                {
                    throw new InvalidOperationException($"{table} row {rowIndex}: illegal AttackMode '{modeText}'.");
                }

                _soulById[id] = new SoulConfigRow
                {
                    SoulId = id,
                    ClassId = SimpleCsv.Require(raw, "ClassId", table, rowIndex),
                    AttackMode = mode,
                    Skills = OptionalText(raw, "Skills"),
                    AttackPriority = OptionalText(raw, "AttackPriority"),
                    MoveStyle = OptionalText(raw, "MoveStyle"),
                    SpiritCost = OptionalFloat(raw, "SpiritCost"),
                    ControlPowerCost = OptionalFloat(raw, "ControlPowerCost")
                };
            }
        }

        private void LoadClasses()
        {
            const string table = "Manufacture_ClassConfig.csv";
            var rows = SimpleCsv.ReadRows(RequirePath(table));
            for (var i = 0; i < rows.Count; i++)
            {
                var raw = rows[i];
                var rowIndex = i + 2;
                var id = SimpleCsv.Require(raw, "ClassId", table, rowIndex);
                var primaryText = SimpleCsv.Require(raw, "PrimaryStat", table, rowIndex);
                if (!Enum.TryParse(primaryText, false, out StatKind primary)
                    || (primary != StatKind.Strength && primary != StatKind.Agility
                                                     && primary != StatKind.Intelligence))
                {
                    throw new InvalidOperationException(
                        $"{table} row {rowIndex}: illegal PrimaryStat '{primaryText}'.");
                }

                _classById[id] = new ClassConfigRow
                {
                    ClassId = id,
                    ClassName = SimpleCsv.Require(raw, "ClassName", table, rowIndex),
                    PrimaryStat = primary,
                    CombatConvertCoeffs = OptionalText(raw, "CombatConvertCoeffs"),
                    AttackRange = OptionalFloat(raw, "AttackRange"),
                    MeleeWindupSeconds = OptionalFloat(raw, "MeleeWindupSeconds"),
                    RangedProjectileSpeed = OptionalFloat(raw, "RangedProjectileSpeed"),
                    RangedTimeoutSeconds = OptionalFloat(raw, "RangedTimeoutSeconds")
                };
            }
        }

        private void LoadRaces()
        {
            const string table = "Manufacture_RaceConfig.csv";
            var rows = SimpleCsv.ReadRows(RequirePath(table));
            for (var i = 0; i < rows.Count; i++)
            {
                var raw = rows[i];
                var rowIndex = i + 2;
                var id = SimpleCsv.Require(raw, "RaceId", table, rowIndex);
                var adjust = new StatBlock
                {
                    MaxHP = OptionalFloat(raw, "RaceAdjustCoeff.MaxHP"),
                    MoveSpeed = OptionalFloat(raw, "RaceAdjustCoeff.MoveSpeed"),
                    Strength = OptionalFloat(raw, "RaceAdjustCoeff.Strength"),
                    Agility = OptionalFloat(raw, "RaceAdjustCoeff.Agility"),
                    Intelligence = OptionalFloat(raw, "RaceAdjustCoeff.Intelligence")
                };

                _raceById[id] = new RaceConfigRow
                {
                    RaceId = id,
                    DisplayNameKey = OptionalText(raw, "DisplayNameKey"),
                    RaceAdjustCoeff = adjust,
                    LossOfControlChanceBonus = OptionalFloat(raw, "LossOfControlChanceBonus")
                };
            }
        }

        private void LoadGems()
        {
            const string table = "Manufacture_GemConfig.csv";
            var rows = SimpleCsv.ReadRows(RequirePath(table));
            for (var i = 0; i < rows.Count; i++)
            {
                var raw = rows[i];
                var rowIndex = i + 2;
                var id = SimpleCsv.Require(raw, "GemId", table, rowIndex);
                var typeText = SimpleCsv.Require(raw, "GemType", table, rowIndex);
                if (!Enum.TryParse(typeText, false, out GemType gemType))
                {
                    throw new InvalidOperationException($"{table} row {rowIndex}: illegal GemType '{typeText}'.");
                }

                var mult = new StatBlock
                {
                    MaxHP = OptionalFloat(raw, "GemMult.MaxHP"),
                    MoveSpeed = OptionalFloat(raw, "GemMult.MoveSpeed"),
                    Strength = OptionalFloat(raw, "GemMult.Strength"),
                    Agility = OptionalFloat(raw, "GemMult.Agility"),
                    Intelligence = OptionalFloat(raw, "GemMult.Intelligence")
                };

                _gemById[id] = new GemConfigRow
                {
                    GemId = id,
                    GemType = gemType,
                    GemMult = mult,
                    Skills = OptionalText(raw, "Skills"),
                    SpiritCost = OptionalFloat(raw, "SpiritCost"),
                    ControlPowerCost = OptionalFloat(raw, "ControlPowerCost"),
                    LossOfControlChanceBonus = OptionalFloat(raw, "LossOfControlChanceBonus")
                };
            }
        }

        private void LoadExtraEquipment()
        {
            const string table = "Manufacture_ExtraEquipmentConfig.csv";
            var rows = SimpleCsv.ReadRows(RequirePath(table));
            for (var i = 0; i < rows.Count; i++)
            {
                var raw = rows[i];
                var rowIndex = i + 2;
                var id = SimpleCsv.Require(raw, "EquipId", table, rowIndex);
                var slotText = SimpleCsv.Require(raw, "EquipSlot", table, rowIndex);
                if (!Enum.TryParse(slotText, false, out EquipSlot slot))
                {
                    throw new InvalidOperationException($"{table} row {rowIndex}: illegal EquipSlot '{slotText}'.");
                }

                _equipById[id] = new ExtraEquipmentConfigRow
                {
                    EquipId = id,
                    EquipSlot = slot,
                    NamePrefix = OptionalText(raw, "NamePrefix"),
                    SpiritCost = OptionalFloat(raw, "SpiritCost"),
                    ControlPowerCost = OptionalFloat(raw, "ControlPowerCost"),
                    EquipStats = StatFieldParser.Parse(
                        OptionalText(raw, "EquipStats"),
                        m => Debug.LogWarning($"[Config] {table} row {rowIndex}: {m}")),
                    Skills = OptionalText(raw, "Skills")
                };
            }
        }

        private void LoadGemSuffixNames()
        {
            const string table = "Manufacture_GemSuffixNameConfig.csv";
            var rows = SimpleCsv.ReadRows(RequirePath(table));
            for (var i = 0; i < rows.Count; i++)
            {
                var raw = rows[i];
                var rowIndex = i + 2;
                var key = SimpleCsv.Require(raw, "ComboKey", table, rowIndex);
                _gemSuffixByComboKey[key] = OptionalText(raw, "Suffix");
            }
        }

        private void LoadBodyAppearances()
        {
            const string table = "Manufacture_BodyAppearanceConfig.csv";
            var rows = SimpleCsv.ReadRows(RequirePath(table));
            for (var i = 0; i < rows.Count; i++)
            {
                var raw = rows[i];
                var rowIndex = i + 2;
                var levelText = SimpleCsv.Require(raw, "AppearanceLevel", table, rowIndex);
                if (!int.TryParse(levelText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var level))
                {
                    throw new InvalidOperationException(
                        $"{table} row {rowIndex}: illegal AppearanceLevel '{levelText}'.");
                }

                _appearances.Add(new BodyAppearanceConfigRow
                {
                    AppearanceId = SimpleCsv.Require(raw, "AppearanceId", table, rowIndex),
                    AppearanceLevel = level,
                    RaceId = SimpleCsv.Require(raw, "RaceId", table, rowIndex),
                    ClassAffinity = OptionalText(raw, "ClassAffinity"),
                    Description = OptionalText(raw, "Description"),
                    IsFallback = string.Equals(OptionalText(raw, "IsFallback"), "1", StringComparison.Ordinal)
                });
            }
        }

        private static string OptionalText(Dictionary<string, string> raw, string column)
        {
            return raw.TryGetValue(column, out var value) ? (value ?? string.Empty).Trim() : string.Empty;
        }

        private static float OptionalFloat(Dictionary<string, string> raw, string column)
        {
            var text = OptionalText(raw, column);
            if (text.Length == 0)
            {
                return 0f;
            }

            return float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) ? value : 0f;
        }

        private static float RequireFloat(Dictionary<string, string> raw, string column, string table, int rowIndex)
        {
            var text = SimpleCsv.Require(raw, column, table, rowIndex).Trim();
            if (!float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
            {
                throw new InvalidOperationException($"{table} row {rowIndex}: illegal {column} '{text}'.");
            }

            return value;
        }

        private static string RequirePath(string csvFileName)
        {
            var path = CsvPathResolver.ResolveExistingFile(csvFileName);
            if (path == null)
            {
                var roots = string.Join(" | ", CsvPathResolver.EnumerateCandidateRoots());
                throw new InvalidOperationException($"Missing CSV '{csvFileName}'. Looked in: {roots}");
            }

            return path;
        }

        private static bool TryParseGameplayType(string text, out GameplayState state)
        {
            if (string.Equals(text, "Dig", StringComparison.Ordinal))
            {
                state = GameplayState.Dig;
                return true;
            }

            if (string.Equals(text, "UpgradeManufacture", StringComparison.Ordinal))
            {
                state = GameplayState.UpgradeManufacture;
                return true;
            }

            if (string.Equals(text, "Defend", StringComparison.Ordinal))
            {
                state = GameplayState.Defend;
                return true;
            }

            state = default;
            return false;
        }
    }
}
