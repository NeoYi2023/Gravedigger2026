using System;
using System.Collections.Generic;
using System.Globalization;
using Gravedigger2026.Core.Audio;
using Gravedigger2026.Core.Combat;
using Gravedigger2026.Core.Defend;
using Gravedigger2026.Core.Dig;
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
        private readonly List<WaveSpawnConfigRow> _waveSpawnRows = new List<WaveSpawnConfigRow>();
        private readonly Dictionary<string, MonsterConfigRow> _monsterById =
            new Dictionary<string, MonsterConfigRow>(StringComparer.Ordinal);
        private readonly Dictionary<string, GraveQualityConfigRow> _graveById =
            new Dictionary<string, GraveQualityConfigRow>(StringComparer.Ordinal);
        private readonly Dictionary<string, MaterialConfigRow> _materialById =
            new Dictionary<string, MaterialConfigRow>(StringComparer.Ordinal);
        private readonly Dictionary<string, CurrencyConfigRow> _currencyById =
            new Dictionary<string, CurrencyConfigRow>(StringComparer.Ordinal);
        private readonly Dictionary<string, ItemCatalogConfigRow> _itemCatalogById =
            new Dictionary<string, ItemCatalogConfigRow>(StringComparer.Ordinal);
        private readonly Dictionary<int, ProtagonistLevelConfigRow> _protagonistLevelById =
            new Dictionary<int, ProtagonistLevelConfigRow>();
        private readonly Dictionary<string, BodyPartConfigRow> _bodyPartById =
            new Dictionary<string, BodyPartConfigRow>(StringComparer.Ordinal);
        private readonly Dictionary<string, SoulConfigRow> _soulById =
            new Dictionary<string, SoulConfigRow>(StringComparer.Ordinal);
        private readonly Dictionary<string, ClassConfigRow> _classById =
            new Dictionary<string, ClassConfigRow>(StringComparer.Ordinal);
        private readonly Dictionary<string, SkillConfigRow> _skillByKey =
            new Dictionary<string, SkillConfigRow>(StringComparer.Ordinal);
        private readonly Dictionary<string, SkillLevelRange> _skillLevelRangeById =
            new Dictionary<string, SkillLevelRange>(StringComparer.Ordinal);
        private readonly Dictionary<string, SkillEffectConfigRow> _skillEffectById =
            new Dictionary<string, SkillEffectConfigRow>(StringComparer.Ordinal);
        private readonly Dictionary<string, MonsterSkillEffectConfigRow> _monsterSkillEffectById =
            new Dictionary<string, MonsterSkillEffectConfigRow>(StringComparer.Ordinal);
        private readonly Dictionary<string, MagicBookConfigRow> _magicBookById =
            new Dictionary<string, MagicBookConfigRow>(StringComparer.Ordinal);
        private readonly Dictionary<string, ProtagonistEquipmentConfigRow> _protagonistEquipmentByKey =
            new Dictionary<string, ProtagonistEquipmentConfigRow>(StringComparer.Ordinal);
        private readonly Dictionary<string, RaceConfigRow> _raceById =
            new Dictionary<string, RaceConfigRow>(StringComparer.Ordinal);
        private readonly Dictionary<string, GemConfigRow> _gemById =
            new Dictionary<string, GemConfigRow>(StringComparer.Ordinal);
        private readonly Dictionary<string, ExtraEquipmentConfigRow> _equipById =
            new Dictionary<string, ExtraEquipmentConfigRow>(StringComparer.Ordinal);
        private readonly Dictionary<string, string> _gemSuffixByComboKey =
            new Dictionary<string, string>(StringComparer.Ordinal);
        private readonly List<BodyAppearanceConfigRow> _appearances = new List<BodyAppearanceConfigRow>();
        private readonly Dictionary<string, BodyAppearanceConfigRow> _appearanceById =
            new Dictionary<string, BodyAppearanceConfigRow>(StringComparer.Ordinal);
        private readonly Dictionary<int, LossOfControlConfigRow> _lossOfControlByTier =
            new Dictionary<int, LossOfControlConfigRow>();
        private readonly Dictionary<string, float> _combatConstantByKey =
            new Dictionary<string, float>(StringComparer.Ordinal);
        private readonly List<TechTreeConfigRow> _techTreeRows = new List<TechTreeConfigRow>();
        private readonly Dictionary<string, TechTreeConfigRow> _techTreeById =
            new Dictionary<string, TechTreeConfigRow>(StringComparer.Ordinal);
        private readonly Dictionary<string, TechEffectConfigRow> _techEffectById =
            new Dictionary<string, TechEffectConfigRow>(StringComparer.Ordinal);
        private readonly Dictionary<string, PushMapGameplayConfigRow> _pushMapById =
            new Dictionary<string, PushMapGameplayConfigRow>(StringComparer.Ordinal);
        private readonly List<PushMapSpawnConfigRow> _pushMapSpawnRows = new List<PushMapSpawnConfigRow>();
        private readonly Dictionary<string, FormationBondConfigRow> _formationBondByKey =
            new Dictionary<string, FormationBondConfigRow>(StringComparer.Ordinal);
        private readonly List<FormationBondConfigRow> _formationBondRows = new List<FormationBondConfigRow>();
        private readonly Dictionary<string, List<FormationBondConfigRow>> _formationBondRowsByBondId =
            new Dictionary<string, List<FormationBondConfigRow>>(StringComparer.Ordinal);
        private readonly Dictionary<string, BondActivationCondition> _bondConditionByBondKey =
            new Dictionary<string, BondActivationCondition>(StringComparer.Ordinal);

        private readonly Dictionary<string, ShopPoolConfigRow> _shopPoolById =
            new Dictionary<string, ShopPoolConfigRow>(StringComparer.Ordinal);

        private readonly List<ShopPoolConfigRow> _shopPoolRows = new List<ShopPoolConfigRow>();

        private readonly Dictionary<int, ShopRefreshPriceConfigRow> _shopRefreshPriceByCount =
            new Dictionary<int, ShopRefreshPriceConfigRow>();

        private readonly List<BgmConfigRow> _bgmRows = new List<BgmConfigRow>();
        private readonly Dictionary<string, BgmConfigRow> _bgmById =
            new Dictionary<string, BgmConfigRow>(StringComparer.Ordinal);

        public bool IsLoaded { get; private set; }
        public string LastError { get; private set; }
        public CampaignMode? LoadedCampaignMode { get; private set; }

        private CampaignMode _loadMode = CampaignMode.Mode1;

        /// <summary>Reload using last loaded mode, or Mode1 if never loaded.</summary>
        public bool TryLoadAll()
        {
            return TryLoadAll(LoadedCampaignMode ?? CampaignMode.Mode1);
        }

        /// <summary>
        /// Load (or reload) all tables for the given CampaignMode CSV root.
        /// If already loaded for the same mode, returns true without re-reading.
        /// </summary>
        public bool TryLoadAll(CampaignMode mode)
        {
            if (IsLoaded && LoadedCampaignMode == mode)
            {
                return true;
            }

            IsLoaded = false;
            LoadedCampaignMode = null;
            LastError = null;
            _loadMode = mode;
            _levelOperations.Clear();
            _digById.Clear();
            _defendById.Clear();
            _waveSpawnRows.Clear();
            _monsterById.Clear();
            _graveById.Clear();
            _materialById.Clear();
            _currencyById.Clear();
            _itemCatalogById.Clear();
            _protagonistLevelById.Clear();
            _bodyPartById.Clear();
            _soulById.Clear();
            _classById.Clear();
            _skillByKey.Clear();
            _skillLevelRangeById.Clear();
            _skillEffectById.Clear();
            _monsterSkillEffectById.Clear();
            _magicBookById.Clear();
            _protagonistEquipmentByKey.Clear();
            _raceById.Clear();
            _gemById.Clear();
            _equipById.Clear();
            _gemSuffixByComboKey.Clear();
            _appearances.Clear();
            _appearanceById.Clear();
            _lossOfControlByTier.Clear();
            _combatConstantByKey.Clear();
            _techTreeRows.Clear();
            _techTreeById.Clear();
            _techEffectById.Clear();
            _pushMapById.Clear();
            _pushMapSpawnRows.Clear();
            _formationBondByKey.Clear();
            _formationBondRows.Clear();
            _formationBondRowsByBondId.Clear();
            _bondConditionByBondKey.Clear();

            _shopPoolById.Clear();
            _shopPoolRows.Clear();
            _shopRefreshPriceByCount.Clear();
            _bgmRows.Clear();
            _bgmById.Clear();

            try
            {
                LoadLevelOperations();
                LoadDigGameplay();
                LoadDefendGameplay();
                LoadWaveSpawn();
                LoadMonsters();
                LoadPushMapGameplay();
                LoadPushMapSpawn();
                LoadGraveQuality();
                LoadMaterial();
                LoadCurrency();
                LoadProtagonistLevels();
                LoadBodyParts();
                LoadSouls();
                LoadClasses();
                LoadSkills();
                LoadSkillEffects();
                LoadMonsterSkillEffects();
                LoadFormationBonds();
                LoadMagicBooks();
                LoadProtagonistEquipment();

                if (mode == CampaignMode.Mode2)
                {
                    LoadShopPoolConfig();
                    LoadShopRefreshPriceConfig();
                }

                LoadItemCatalog();
                LoadRaces();
                LoadGems();
                LoadExtraEquipment();
                LoadGemSuffixNames();
                LoadBodyAppearances();
                LoadLossOfControl();
                LoadCombatConstants();
                LoadTechTree();
                LoadTechEffects();
                LoadBgm();
                IsLoaded = true;
                LoadedCampaignMode = mode;
                Debug.Log(
                    $"[ConfigCsvRepository] CampaignMode={mode} root={CsvPathResolver.RelativeCsvFolderFor(mode)} Loaded LevelOps={_levelOperations.Count}, Dig={_digById.Count}, Defend={_defendById.Count}, WaveSpawn={_waveSpawnRows.Count}, Monster={_monsterById.Count}, PushMap={_pushMapById.Count}, PushMapSpawn={_pushMapSpawnRows.Count}, Grave={_graveById.Count}, Mat={_materialById.Count}, Cur={_currencyById.Count}, ItemCatalog={_itemCatalogById.Count}, ProtagonistLevel={_protagonistLevelById.Count}, BodyPart={_bodyPartById.Count}, Soul={_soulById.Count}, Class={_classById.Count}, Skill={_skillByKey.Count}, MagicBook={_magicBookById.Count}, ProtagonistEquip={_protagonistEquipmentByKey.Count}, Race={_raceById.Count}, Gem={_gemById.Count}, Equip={_equipById.Count}, GemSuffix={_gemSuffixByComboKey.Count}, Appearance={_appearances.Count}, LossOfControl={_lossOfControlByTier.Count}, CombatConstant={_combatConstantByKey.Count}, TechTree={_techTreeRows.Count}, TechEffect={_techEffectById.Count}, Bgm={_bgmRows.Count}.");
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

        /// <summary>
        /// Distinct LevelIds from loaded LevelOperationConfig (first-seen order; UI-008).
        /// </summary>
        public IReadOnlyList<string> GetDistinctLevelIds()
        {
            var result = new List<string>();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < _levelOperations.Count; i++)
            {
                var id = _levelOperations[i].LevelId;
                if (string.IsNullOrEmpty(id) || !seen.Add(id))
                {
                    continue;
                }

                result.Add(id);
            }

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

        /// <summary>All DefendGameplayConfig rows (ModeSelect Mode1 list / D-044).</summary>
        public IReadOnlyList<DefendGameplayConfigRow> GetAllDefendRows()
        {
            var list = new List<DefendGameplayConfigRow>(_defendById.Count);
            foreach (var kv in _defendById)
            {
                list.Add(kv.Value);
            }

            list.Sort((a, b) => string.CompareOrdinal(a.GameplayConfigId, b.GameplayConfigId));
            return list;
        }

        public List<WaveSpawnConfigRow> GetWaveSpawnRows(string waveConfigId)
        {
            var result = new List<WaveSpawnConfigRow>();
            if (string.IsNullOrEmpty(waveConfigId))
            {
                return result;
            }

            for (var i = 0; i < _waveSpawnRows.Count; i++)
            {
                var row = _waveSpawnRows[i];
                if (string.Equals(row.WaveConfigId, waveConfigId, StringComparison.Ordinal))
                {
                    result.Add(row);
                }
            }

            return result;
        }

        public bool TryGetMonster(string monsterId, out MonsterConfigRow row)
        {
            return _monsterById.TryGetValue(monsterId ?? string.Empty, out row);
        }

        public IEnumerable<MonsterConfigRow> Monsters => _monsterById.Values;

        public bool TryGetPushMap(string gameplayConfigId, out PushMapGameplayConfigRow row)
        {
            return _pushMapById.TryGetValue(gameplayConfigId ?? string.Empty, out row);
        }

        /// <summary>All PushMapGameplayConfig rows (ModeSelect Mode2 list / PM-02).</summary>
        public IReadOnlyList<PushMapGameplayConfigRow> GetAllPushMapRows()
        {
            var list = new List<PushMapGameplayConfigRow>(_pushMapById.Count);
            foreach (var kv in _pushMapById)
            {
                list.Add(kv.Value);
            }

            list.Sort((a, b) => string.CompareOrdinal(a.GameplayConfigId, b.GameplayConfigId));
            return list;
        }

        public List<PushMapSpawnConfigRow> GetPushMapSpawnRows(string gameplayConfigId)
        {
            var result = new List<PushMapSpawnConfigRow>();
            if (string.IsNullOrEmpty(gameplayConfigId))
            {
                return result;
            }

            for (var i = 0; i < _pushMapSpawnRows.Count; i++)
            {
                var row = _pushMapSpawnRows[i];
                if (string.Equals(row.GameplayConfigId, gameplayConfigId, StringComparison.Ordinal))
                {
                    result.Add(row);
                }
            }

            result.Sort((a, b) =>
            {
                var byPoint = string.CompareOrdinal(a.SpawnPointId, b.SpawnPointId);
                return byPoint != 0 ? byPoint : a.SpawnOrder.CompareTo(b.SpawnOrder);
            });
            return result;
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

        public IEnumerable<ClassConfigRow> Classes => _classById.Values;

        /// <summary>Composite PK lookup (SPEC_04 §9.21): SkillId + SkillLevel.</summary>
        public bool TryGetSkill(string skillId, int skillLevel, out SkillConfigRow row)
        {
            row = null;
            if (string.IsNullOrEmpty(skillId) || skillLevel < 1)
            {
                return false;
            }

            return _skillByKey.TryGetValue(MakeSkillKey(skillId, skillLevel), out row);
        }

        /// <summary>
        /// Min/max SkillLevel present for this SkillId (SS-04 clamp).
        /// False when the Id has no rows.
        /// </summary>
        public bool TryGetSkillLevelRange(string skillId, out int minLevel, out int maxLevel)
        {
            minLevel = 0;
            maxLevel = 0;
            if (string.IsNullOrEmpty(skillId)
                || !_skillLevelRangeById.TryGetValue(skillId, out var range))
            {
                return false;
            }

            minLevel = range.Min;
            maxLevel = range.Max;
            return true;
        }

        public IEnumerable<SkillConfigRow> Skills => _skillByKey.Values;

        /// <summary>PK lookup (SPEC_04 §9.21b): SkillEffectId.</summary>
        public bool TryGetSkillEffect(string skillEffectId, out SkillEffectConfigRow row)
        {
            return _skillEffectById.TryGetValue(skillEffectId ?? string.Empty, out row);
        }

        public IEnumerable<SkillEffectConfigRow> SkillEffects => _skillEffectById.Values;

        /// <summary>PK lookup (SPEC_04 §9.21c): MonsterSkillId.</summary>
        public bool TryGetMonsterSkillEffect(string monsterSkillId, out MonsterSkillEffectConfigRow row)
        {
            return _monsterSkillEffectById.TryGetValue(monsterSkillId ?? string.Empty, out row);
        }

        public IEnumerable<MonsterSkillEffectConfigRow> MonsterSkillEffects => _monsterSkillEffectById.Values;

        /// <summary>Composite PK lookup (SPEC_04 §9.26): BondId + BondLevel.</summary>
        public bool TryGetFormationBond(string bondId, int bondLevel, out FormationBondConfigRow row)
        {
            row = null;
            if (string.IsNullOrEmpty(bondId) || bondLevel < 1)
            {
                return false;
            }

            return _formationBondByKey.TryGetValue(MakeFormationBondKey(bondId, bondLevel), out row);
        }

        public IReadOnlyList<FormationBondConfigRow> GetFormationBondRowsForBondId(string bondId)
        {
            if (string.IsNullOrEmpty(bondId)
                || !_formationBondRowsByBondId.TryGetValue(bondId, out var rows))
            {
                return Array.Empty<FormationBondConfigRow>();
            }

            return rows;
        }

        public IReadOnlyList<FormationBondConfigRow> GetAllFormationBondRows()
        {
            return _formationBondRows;
        }

        public bool TryGetBondActivationCondition(
            FormationBondConfigRow row,
            out BondActivationCondition condition)
        {
            condition = null;
            if (row == null || string.IsNullOrEmpty(row.BondId) || row.BondLevel < 1)
            {
                return false;
            }

            var key = MakeFormationBondKey(row.BondId, row.BondLevel);
            return _bondConditionByBondKey.TryGetValue(key, out condition);
        }

        public bool TryGetMagicBook(string magicBookId, out MagicBookConfigRow row)
        {
            return _magicBookById.TryGetValue(magicBookId ?? string.Empty, out row);
        }

        public IEnumerable<MagicBookConfigRow> MagicBooks => _magicBookById.Values;

        public bool TryGetShopPool(string shopPoolId, out ShopPoolConfigRow row)
        {
            return _shopPoolById.TryGetValue(shopPoolId ?? string.Empty, out row);
        }

        public IReadOnlyList<ShopPoolConfigRow> ShopPoolRows => _shopPoolRows;

        public IReadOnlyList<BgmConfigRow> GetBgmRows(BgmContext context)
        {
            var result = new List<BgmConfigRow>();
            var key = context.ToString();
            for (var i = 0; i < _bgmRows.Count; i++)
            {
                var row = _bgmRows[i];
                if (row != null && string.Equals(row.Context, key, StringComparison.Ordinal))
                {
                    result.Add(row);
                }
            }

            return result;
        }

        public bool TryGetShopRefreshPrice(int refreshCount, out ShopRefreshPriceConfigRow row)
        {
            return _shopRefreshPriceByCount.TryGetValue(refreshCount, out row);
        }

        public bool TryGetItemCatalog(string itemId, out ItemCatalogConfigRow row)
        {
            return _itemCatalogById.TryGetValue(itemId ?? string.Empty, out row);
        }

        /// <summary>Composite PK lookup (SPEC_04 §9.25): EquipId + EquipLevel.</summary>
        public bool TryGetProtagonistEquipment(string equipId, int equipLevel, out ProtagonistEquipmentConfigRow row)
        {
            row = null;
            if (string.IsNullOrEmpty(equipId) || equipLevel < 1)
            {
                return false;
            }

            return _protagonistEquipmentByKey.TryGetValue(MakeProtagonistEquipmentKey(equipId, equipLevel), out row);
        }

        public IEnumerable<ProtagonistEquipmentConfigRow> ProtagonistEquipmentRows =>
            _protagonistEquipmentByKey.Values;

        public bool TryGetRace(string raceId, out RaceConfigRow row)
        {
            return _raceById.TryGetValue(raceId ?? string.Empty, out row);
        }

        public IEnumerable<RaceConfigRow> Races => _raceById.Values;

        public bool TryGetGem(string gemId, out GemConfigRow row)
        {
            return _gemById.TryGetValue(gemId ?? string.Empty, out row);
        }

        public bool TryGetLossOfControlTier(int tierId, out LossOfControlConfigRow row)
        {
            return _lossOfControlByTier.TryGetValue(tierId, out row);
        }

        /// <summary>Combat_CombatConstantConfig lookup (SPEC_04 §9.20b).</summary>
        public bool TryGetCombatConstant(string constantKey, out float value)
        {
            return _combatConstantByKey.TryGetValue(constantKey ?? string.Empty, out value);
        }

        /// <summary>
        /// Five CombatConvertCoeffs defaults from the constants table.
        /// Missing keys log Warning and use sample safety fallbacks (not business authority).
        /// </summary>
        public CombatConvertCoeffs GetCombatConvertCoeffDefaults()
        {
            return new CombatConvertCoeffs(
                GetCombatConstantOrFallback(
                    CombatConstantKeys.NormalAttackPrimaryMult,
                    CombatConvertCoeffs.SafetyNormalAttackPrimaryMult),
                GetCombatConstantOrFallback(
                    CombatConstantKeys.AttackSpeedBase,
                    CombatConvertCoeffs.SafetyAttackSpeedBase),
                GetCombatConstantOrFallback(
                    CombatConstantKeys.AttackSpeedAgiDiv,
                    CombatConvertCoeffs.SafetyAttackSpeedAgiDiv),
                GetCombatConstantOrFallback(
                    CombatConstantKeys.SkillCdIntDiv,
                    CombatConvertCoeffs.SafetySkillCdIntDiv),
                GetCombatConstantOrFallback(
                    CombatConstantKeys.SkillCdFloor,
                    CombatConvertCoeffs.SafetySkillCdFloor));
        }

        /// <summary>MaxHP Strength mult from constants table (sample 3).</summary>
        public float GetMaxHpStrengthMult()
        {
            return GetCombatConstantOrFallback(
                CombatConstantKeys.MaxHpStrengthMult,
                CombatConvertCoeffs.SafetyMaxHpStrengthMult);
        }

        /// <summary>
        /// CombatConstantConfig lookup with Warning + safety fallback when key missing
        /// (fallback is not business authority — SPEC_04 §9.20b).
        /// </summary>
        public float GetCombatConstantOrFallback(string key, float safetyFallback)
        {
            if (TryGetCombatConstant(key, out var value))
            {
                return value;
            }

            Debug.LogWarning(
                $"[ConfigCsvRepository] Missing CombatConstant '{key}' — using safety fallback {safetyFallback}.");
            return safetyFallback;
        }

        /// <summary>Top-down camera / PushMap follow-zoom tunables from the constants table.</summary>
        public CameraPresentationConstants GetCameraPresentationConstants()
        {
            return CameraPresentationConstants.FromRepository(this);
        }

        /// <summary>DigActionDuration = max(floor, base − DigDurationReductionSum).</summary>
        public void ApplyDigTimingConstants(DigProtagonistCapabilities caps)
        {
            if (caps == null)
            {
                return;
            }

            caps.BaseDigDuration = GetCombatConstantOrFallback(
                CombatConstantKeys.BaseDigDuration,
                CombatConstantKeys.Safety.BaseDigDuration);
            caps.DigActionDurationFloor = GetCombatConstantOrFallback(
                CombatConstantKeys.DigActionDurationFloor,
                CombatConstantKeys.Safety.DigActionDurationFloor);
        }

        public float GetDigTriggerDwellSeconds()
        {
            return GetCombatConstantOrFallback(
                CombatConstantKeys.DigTriggerDwellSeconds,
                CombatConstantKeys.Safety.DigTriggerDwellSeconds);
        }

        public IReadOnlyList<TechTreeConfigRow> GetAllTechTreeRows()
        {
            return _techTreeRows;
        }

        public bool TryGetTechTree(string techId, out TechTreeConfigRow row)
        {
            return _techTreeById.TryGetValue(techId ?? string.Empty, out row);
        }

        public bool TryGetTechEffect(string techId, out TechEffectConfigRow row)
        {
            return _techEffectById.TryGetValue(techId ?? string.Empty, out row);
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

        public bool TryGetAppearance(string appearanceId, out BodyAppearanceConfigRow row)
        {
            return _appearanceById.TryGetValue(appearanceId ?? string.Empty, out row);
        }

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

        private void LoadWaveSpawn()
        {
            const string table = "Defend_WaveSpawnConfig.csv";
            var path = RequirePath(table);
            var rows = SimpleCsv.ReadRows(path);
            for (var i = 0; i < rows.Count; i++)
            {
                var raw = rows[i];
                var rowIndex = i + 2;
                var orderText = SimpleCsv.Require(raw, "SpawnOrder", table, rowIndex);
                var remainingText = SimpleCsv.Require(raw, "SpawnRemainingSeconds", table, rowIndex);
                var countText = SimpleCsv.Require(raw, "SpawnCount", table, rowIndex);
                if (!int.TryParse(orderText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var order))
                {
                    throw new InvalidOperationException($"{table} row {rowIndex}: illegal SpawnOrder '{orderText}'.");
                }

                if (!int.TryParse(remainingText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var remaining))
                {
                    throw new InvalidOperationException(
                        $"{table} row {rowIndex}: illegal SpawnRemainingSeconds '{remainingText}'.");
                }

                if (!int.TryParse(countText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var count)
                    || count < 1)
                {
                    throw new InvalidOperationException($"{table} row {rowIndex}: illegal SpawnCount '{countText}'.");
                }

                var clockText = OptionalText(raw, "SpawnClockHour");
                var clockHour = 0;
                if (clockText.Length > 0
                    && !int.TryParse(clockText, NumberStyles.Integer, CultureInfo.InvariantCulture, out clockHour))
                {
                    throw new InvalidOperationException(
                        $"{table} row {rowIndex}: illegal SpawnClockHour '{clockText}'.");
                }

                _waveSpawnRows.Add(new WaveSpawnConfigRow
                {
                    WaveConfigId = SimpleCsv.Require(raw, "WaveConfigId", table, rowIndex),
                    SpawnOrder = order,
                    SpawnRemainingSeconds = remaining,
                    MonsterId = SimpleCsv.Require(raw, "MonsterId", table, rowIndex),
                    SpawnCount = count,
                    AppearLocation = SimpleCsv.Require(raw, "AppearLocation", table, rowIndex),
                    SpawnMode = SimpleCsv.Require(raw, "SpawnMode", table, rowIndex),
                    SpawnClockHour = clockHour
                });
            }
        }

        private void LoadMonsters()
        {
            const string table = "Defend_MonsterConfig.csv";
            var path = RequirePath(table);
            var rows = SimpleCsv.ReadRows(path);
            for (var i = 0; i < rows.Count; i++)
            {
                var raw = rows[i];
                var rowIndex = i + 2;
                var id = SimpleCsv.Require(raw, "MonsterId", table, rowIndex);
                var targetText = SimpleCsv.Require(raw, "TargetSelect", table, rowIndex);
                if (!Enum.TryParse(targetText, false, out TargetSelect targetSelect))
                {
                    throw new InvalidOperationException(
                        $"{table} row {rowIndex}: illegal TargetSelect '{targetText}'.");
                }

                var modeText = SimpleCsv.Require(raw, "AttackMode", table, rowIndex);
                if (!Enum.TryParse(modeText, false, out AttackMode attackMode))
                {
                    throw new InvalidOperationException($"{table} row {rowIndex}: illegal AttackMode '{modeText}'.");
                }

                var monsterTypeText = OptionalText(raw, "MonsterType");
                MonsterType monsterType;
                if (monsterTypeText.Length == 0)
                {
                    monsterType = MonsterType.Normal;
                }
                else if (!int.TryParse(monsterTypeText, NumberStyles.Integer, CultureInfo.InvariantCulture,
                             out var monsterTypeInt)
                         || monsterTypeInt < (int)MonsterType.Normal
                         || monsterTypeInt > (int)MonsterType.Boss)
                {
                    throw new InvalidOperationException(
                        $"{table} row {rowIndex}: illegal MonsterType '{monsterTypeText}'.");
                }
                else
                {
                    monsterType = (MonsterType)monsterTypeInt;
                }

                var attackRange = RequireFloat(raw, "AttackRange", table, rowIndex);
                var aggroText = OptionalText(raw, "AggroMode");
                AggroMode aggroMode;
                if (aggroText.Length == 0)
                {
                    aggroMode = AggroMode.ActiveChase;
                }
                else if (!Enum.TryParse(aggroText, false, out aggroMode))
                {
                    throw new InvalidOperationException(
                        $"{table} row {rowIndex}: illegal AggroMode '{aggroText}'.");
                }

                var alertText = OptionalText(raw, "AlertRadius");
                float alertRadius;
                if (alertText.Length == 0)
                {
                    alertRadius = attackRange;
                }
                else if (!float.TryParse(alertText, NumberStyles.Float, CultureInfo.InvariantCulture, out alertRadius)
                         || alertRadius < 0f)
                {
                    throw new InvalidOperationException(
                        $"{table} row {rowIndex}: illegal AlertRadius '{alertText}'.");
                }

                const float defaultBodyRadius = 0.35f;
                var bodyText = OptionalText(raw, "BodyRadius");
                float bodyRadius;
                if (bodyText.Length == 0)
                {
                    bodyRadius = defaultBodyRadius;
                }
                else if (!float.TryParse(bodyText, NumberStyles.Float, CultureInfo.InvariantCulture, out bodyRadius)
                         || bodyRadius < 0f)
                {
                    throw new InvalidOperationException(
                        $"{table} row {rowIndex}: illegal BodyRadius '{bodyText}'.");
                }

                var pushText = OptionalText(raw, "PushCoefficient");
                float pushCoefficient;
                if (pushText.Length == 0)
                {
                    pushCoefficient = MonsterConfigRow.DefaultPushCoefficient;
                }
                else if (!float.TryParse(pushText, NumberStyles.Float, CultureInfo.InvariantCulture, out pushCoefficient)
                         || pushCoefficient < 0f)
                {
                    throw new InvalidOperationException(
                        $"{table} row {rowIndex}: illegal PushCoefficient '{pushText}'.");
                }

                var repulsionText = OptionalText(raw, "RepulsionScale");
                float repulsionScale;
                if (repulsionText.Length == 0)
                {
                    repulsionScale = MonsterConfigRow.DefaultRepulsionScale;
                }
                else if (!float.TryParse(repulsionText, NumberStyles.Float, CultureInfo.InvariantCulture, out repulsionScale)
                         || repulsionScale < 0f)
                {
                    throw new InvalidOperationException(
                        $"{table} row {rowIndex}: illegal RepulsionScale '{repulsionText}'.");
                }

                var facingYawFlip = ParseFacingYawFlip(raw, table, rowIndex);

                var activeMoveMult = ParseOptionalNonNegFloat(
                    raw, "ActiveMoveMult", MonsterConfigRow.DefaultActiveMoveMult, table, rowIndex);
                var passiveMoveMult = ParseOptionalNonNegFloat(
                    raw, "PassiveMoveMult", MonsterConfigRow.DefaultPassiveMoveMult, table, rowIndex);

                var runSpeed = ParseOptionalNonNegFloat(raw, "RunSpeed", 0f, table, rowIndex);
                var walkToRunSeconds = ParseOptionalNonNegFloat(
                    raw, "WalkToRunSeconds", MonsterConfigRow.DefaultWalkToRunSeconds, table, rowIndex);

                var modelIdRaw = SimpleCsv.Require(raw, "ModelId", table, rowIndex);
                var modelIdPool = MonsterModelIdFieldParser.Parse(modelIdRaw);
                if (modelIdPool.Count == 0)
                {
                    throw new InvalidOperationException(
                        $"{table} row {rowIndex}: illegal ModelId '{modelIdRaw}' (empty or invalid weighted pool).");
                }

                _monsterById[id] = new MonsterConfigRow
                {
                    MonsterId = id,
                    ModelId = modelIdRaw,
                    ModelIdPool = modelIdPool,
                    DisplayName = SimpleCsv.Require(raw, "DisplayName", table, rowIndex),
                    TargetSelect = targetSelect,
                    AttackMode = attackMode,
                    MonsterType = monsterType,
                    AggroMode = aggroMode,
                    AlertRadius = alertRadius,
                    BodyRadius = bodyRadius,
                    PushCoefficient = pushCoefficient,
                    RepulsionScale = repulsionScale,
                    FacingYawFlip = facingYawFlip,
                    MaxHP = RequireFloat(raw, "MaxHP", table, rowIndex),
                    MoveSpeed = RequireFloat(raw, "MoveSpeed", table, rowIndex),
                    RunSpeed = runSpeed,
                    WalkToRunSeconds = walkToRunSeconds,
                    ActiveMoveMult = activeMoveMult,
                    PassiveMoveMult = passiveMoveMult,
                    AttackPower = RequireFloat(raw, "AttackPower", table, rowIndex),
                    AttackSpeed = RequireFloat(raw, "AttackSpeed", table, rowIndex),
                    AttackRange = attackRange,
                    MeleeWindupSeconds = OptionalFloat(raw, "MeleeWindupSeconds"),
                    RangedProjectileSpeed = OptionalFloat(raw, "RangedProjectileSpeed"),
                    RangedTimeoutSeconds = OptionalFloat(raw, "RangedTimeoutSeconds"),
                    Skills = OptionalText(raw, "Skills"),
                    NormalAttackAnims = OptionalText(raw, "NormalAttackAnims"),
                    WalkAnims = OptionalText(raw, "WalkAnims"),
                    RunAnims = OptionalText(raw, "RunAnims"),
                    LootDrop = OptionalText(raw, "LootDrop")
                };
            }

            WarnMonsterFacingYawFlipModelIdMismatch();
        }

        private void LoadPushMapGameplay()
        {
            const string table = "PushMap_PushMapGameplayConfig.csv";
            var path = RequirePath(table);
            var rows = SimpleCsv.ReadRows(path);
            for (var i = 0; i < rows.Count; i++)
            {
                var raw = rows[i];
                var rowIndex = i + 2;
                var id = SimpleCsv.Require(raw, "GameplayConfigId", table, rowIndex);
                var mapId = SimpleCsv.Require(raw, "MapId", table, rowIndex);
                if (!IsValidPushMapMapId(mapId))
                {
                    throw new InvalidOperationException(
                        $"{table} row {rowIndex}: illegal MapId '{mapId}' (expect Ground_01..05 or PushMap_*).");
                }

                var expText = SimpleCsv.Require(raw, "StageExpReward", table, rowIndex);
                if (!int.TryParse(expText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var stageExp))
                {
                    throw new InvalidOperationException(
                        $"{table} row {rowIndex}: illegal StageExpReward '{expText}'.");
                }

                var captureText = OptionalText(raw, "CaptureSeconds");
                float captureSeconds;
                if (captureText.Length == 0)
                {
                    captureSeconds = 5f;
                }
                else if (!float.TryParse(
                             captureText,
                             NumberStyles.Float,
                             CultureInfo.InvariantCulture,
                             out captureSeconds)
                         || captureSeconds < 0f)
                {
                    throw new InvalidOperationException(
                        $"{table} row {rowIndex}: illegal CaptureSeconds '{captureText}'.");
                }

                _pushMapById[id] = new PushMapGameplayConfigRow
                {
                    GameplayConfigId = id,
                    MapId = mapId,
                    DisplayName = SimpleCsv.Require(raw, "DisplayName", table, rowIndex),
                    StageExpReward = stageExp,
                    CaptureLoot = OptionalText(raw, "CaptureLoot"),
                    DungeonUnlockIds = OptionalText(raw, "DungeonUnlockIds"),
                    CaptureSeconds = captureSeconds
                };
            }
        }

        private void LoadPushMapSpawn()
        {
            const string table = "PushMap_PushMapSpawnConfig.csv";
            var path = RequirePath(table);
            var rows = SimpleCsv.ReadRows(path);
            for (var i = 0; i < rows.Count; i++)
            {
                var raw = rows[i];
                var rowIndex = i + 2;
                var gameplayId = SimpleCsv.Require(raw, "GameplayConfigId", table, rowIndex);
                var countText = SimpleCsv.Require(raw, "SpawnCount", table, rowIndex);
                if (!int.TryParse(countText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var count)
                    || count < 1)
                {
                    throw new InvalidOperationException(
                        $"{table} row {rowIndex}: illegal SpawnCount '{countText}'.");
                }

                var linkedText = OptionalText(raw, "LinkedObjectiveOrder");
                var linked = 0;
                if (linkedText.Length > 0
                    && !int.TryParse(linkedText, NumberStyles.Integer, CultureInfo.InvariantCulture, out linked))
                {
                    throw new InvalidOperationException(
                        $"{table} row {rowIndex}: illegal LinkedObjectiveOrder '{linkedText}'.");
                }

                var orderText = SimpleCsv.Require(raw, "SpawnOrder", table, rowIndex);
                if (!int.TryParse(orderText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var order))
                {
                    throw new InvalidOperationException(
                        $"{table} row {rowIndex}: illegal SpawnOrder '{orderText}'.");
                }

                var bossText = OptionalText(raw, "IsBoss");
                var isBoss = string.Equals(bossText, "1", StringComparison.Ordinal)
                             || string.Equals(bossText, "true", StringComparison.OrdinalIgnoreCase);

                var initialFacing = ParsePushMapInitialFacing(raw, table, rowIndex);

                _pushMapSpawnRows.Add(new PushMapSpawnConfigRow
                {
                    GameplayConfigId = gameplayId,
                    SpawnPointId = SimpleCsv.Require(raw, "SpawnPointId", table, rowIndex),
                    MonsterId = SimpleCsv.Require(raw, "MonsterId", table, rowIndex),
                    SpawnCount = count,
                    LinkedObjectiveOrder = linked,
                    TrapZoneId = OptionalText(raw, "TrapZoneId"),
                    IsBoss = isBoss,
                    SpawnOrder = order,
                    InitialFacing = initialFacing
                });
            }
        }

        /// <summary>SPEC_04 §9.23: missing/empty → 5; illegal → load fail.</summary>
        private static int ParsePushMapInitialFacing(Dictionary<string, string> raw, string table, int rowIndex)
        {
            var text = OptionalText(raw, "InitialFacing");
            if (text.Length == 0)
            {
                return 5;
            }

            if (!int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var facing)
                || facing < 0
                || facing > 8)
            {
                throw new InvalidOperationException(
                    $"{table} row {rowIndex}: illegal InitialFacing '{text}' (expected 0..8).");
            }

            return facing;
        }

        private static bool IsValidPushMapMapId(string mapId)
        {
            if (string.IsNullOrEmpty(mapId))
            {
                return false;
            }

            if (mapId.StartsWith("PushMap_", StringComparison.Ordinal))
            {
                return true;
            }

            return mapId.Length == 9
                   && mapId.StartsWith("Ground_0", StringComparison.Ordinal)
                   && mapId[8] >= '1'
                   && mapId[8] <= '5';
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

                var dropModeText = SimpleCsv.Require(raw, "DropMode", table, rowIndex);
                if (!int.TryParse(dropModeText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var dropMode))
                {
                    throw new InvalidOperationException($"{table} row {rowIndex}: illegal DropMode '{dropModeText}'.");
                }

                _graveById[id] = new GraveQualityConfigRow
                {
                    QualityId = id,
                    MaxHP = maxHp,
                    DropMode = dropMode,
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

                var isPrimaryHand = ParseOptional01(raw, "IsPrimaryHand", table, rowIndex);
                if (slot != BodySlot.Arm && isPrimaryHand != 0)
                {
                    Debug.LogWarning(
                        $"[Config] {table} row {rowIndex}: IsPrimaryHand={isPrimaryHand} only valid for Arm; forced 0.");
                    isPrimaryHand = 0;
                }

                var hasBodyPrimary = TryParseOptionalPrimaryStat(
                    raw, "BodyPrimaryStat", table, rowIndex, out var bodyPrimary);

                _bodyPartById[id] = new BodyPartConfigRow
                {
                    BodyPartId = id,
                    DisplayName = OptionalText(raw, "DisplayName"),
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
                    ArtAssetId = OptionalText(raw, "ArtAssetId"),
                    IsPrimaryHand = isPrimaryHand,
                    ClassRestrict = OptionalText(raw, "ClassRestrict"),
                    HasBodyPrimaryStat = hasBodyPrimary,
                    BodyPrimaryStat = bodyPrimary
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

                var attackModeText = OptionalText(raw, "AttackMode");
                var attackMode = AttackMode.Melee;
                if (attackModeText.Length > 0
                    && !Enum.TryParse(attackModeText, false, out attackMode))
                {
                    throw new InvalidOperationException(
                        $"{table} row {rowIndex}: illegal AttackMode '{attackModeText}'.");
                }

                _classById[id] = new ClassConfigRow
                {
                    ClassId = id,
                    ClassName = SimpleCsv.Require(raw, "ClassName", table, rowIndex),
                    BaseClass = ParseBaseClass(OptionalText(raw, "BaseClass"), table, rowIndex),
                    PromoteClass = OptionalText(raw, "PromoteClass"),
                    ClassLevel = ParseOptionalNonNegInt(raw, "ClassLevel", table, rowIndex),
                    PrimaryStat = primary,
                    CombatConvertCoeffs = OptionalText(raw, "CombatConvertCoeffs"),
                    AttackRange = OptionalFloat(raw, "AttackRange"),
                    MeleeWindupSeconds = OptionalFloat(raw, "MeleeWindupSeconds"),
                    RangedProjectileSpeed = OptionalFloat(raw, "RangedProjectileSpeed"),
                    RangedTimeoutSeconds = OptionalFloat(raw, "RangedTimeoutSeconds"),
                    BaseMoveSpeed = OptionalFloat(raw, "BaseMoveSpeed"),
                    ChaseMoveSpeedMult = ParseOptionalNonNegFloat(
                        raw,
                        "ChaseMoveSpeedMult",
                        ClassConfigRow.DefaultChaseMoveSpeedMult,
                        table,
                        rowIndex),
                    DeathKnockbackMult = ParseOptionalNonNegFloat(
                        raw,
                        "DeathKnockbackMult",
                        ClassConfigRow.DefaultDeathKnockbackMult,
                        table,
                        rowIndex),
                    AttackMode = attackMode,
                    PlacementOrder = ParseOptionalPlacementOrder(raw, "PlacementOrder", table, rowIndex),
                    DefaultAppearanceId = OptionalText(raw, "DefaultAppearanceId"),
                    DefaultSkillIds = ParseDefaultSkillIds(OptionalText(raw, "DefaultSkillIds"))
                };
            }
        }

        private void LoadSkills()
        {
            const string table = "Combat_SkillConfig.csv";
            var rows = SimpleCsv.ReadRows(RequirePath(table));
            var levelsBySkill = new Dictionary<string, List<int>>(StringComparer.Ordinal);
            for (var i = 0; i < rows.Count; i++)
            {
                var raw = rows[i];
                var rowIndex = i + 2;
                var skillId = SimpleCsv.Require(raw, "SkillId", table, rowIndex);
                var levelText = SimpleCsv.Require(raw, "SkillLevel", table, rowIndex);
                if (!int.TryParse(levelText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var level)
                    || level < 1)
                {
                    throw new InvalidOperationException(
                        $"{table} row {rowIndex}: illegal SkillLevel '{levelText}' (expect ≥ 1).");
                }

                var key = MakeSkillKey(skillId, level);
                if (_skillByKey.ContainsKey(key))
                {
                    throw new InvalidOperationException(
                        $"{table} row {rowIndex}: duplicate composite PK ({skillId}, {level}).");
                }

                _skillByKey[key] = new SkillConfigRow
                {
                    SkillId = skillId,
                    SkillLevel = level,
                    CooldownMode = OptionalText(raw, "CooldownMode"),
                    CastTarget = OptionalText(raw, "CastTarget"),
                    ExtraActivationCondition = OptionalText(raw, "ExtraActivationCondition"),
                    DisplayName = OptionalText(raw, "DisplayName"),
                    Description = OptionalText(raw, "Description"),
                    IconAssetId = OptionalText(raw, "IconAssetId"),
                    SkillEffectId = OptionalText(raw, "SkillEffectId"),
                    EffectImplemented = ParseOptionalBool01(raw, "EffectImplemented"),
                    BaseCooldownSeconds = OptionalFloat(raw, "BaseCooldownSeconds"),
                    LossOfControlChanceBonus = OptionalFloat(raw, "LossOfControlChanceBonus")
                };

                if (!levelsBySkill.TryGetValue(skillId, out var levels))
                {
                    levels = new List<int>();
                    levelsBySkill[skillId] = levels;
                }

                levels.Add(level);
            }

            foreach (var pair in levelsBySkill)
            {
                pair.Value.Sort();
                _skillLevelRangeById[pair.Key] = new SkillLevelRange
                {
                    Min = pair.Value[0],
                    Max = pair.Value[pair.Value.Count - 1]
                };

                for (var expected = 1; expected <= pair.Value.Count; expected++)
                {
                    if (pair.Value[expected - 1] == expected)
                    {
                        continue;
                    }

                    Debug.LogWarning(
                        $"[Config] {table}: SkillId '{pair.Key}' levels are not contiguous from 1 " +
                        $"(got [{string.Join(",", pair.Value)}]).");
                    break;
                }
            }
        }

        private void LoadSkillEffects()
        {
            const string table = "Combat_SkillEffectConfig.csv";
            var rows = SimpleCsv.ReadRows(RequirePath(table));
            for (var i = 0; i < rows.Count; i++)
            {
                var raw = rows[i];
                var rowIndex = i + 2;
                var skillEffectId = SimpleCsv.Require(raw, "SkillEffectId", table, rowIndex);
                if (_skillEffectById.ContainsKey(skillEffectId))
                {
                    throw new InvalidOperationException(
                        $"{table} row {rowIndex}: duplicate SkillEffectId '{skillEffectId}'.");
                }

                _skillEffectById[skillEffectId] = new SkillEffectConfigRow
                {
                    SkillEffectId = skillEffectId,
                    Notes = OptionalText(raw, "Notes"),
                    EffectKind = OptionalText(raw, "EffectKind"),
                    EffectParams = OptionalText(raw, "EffectParams"),
                    TriggerHook = OptionalText(raw, "TriggerHook")
                };
            }
        }

        private void LoadMonsterSkillEffects()
        {
            const string table = "Combat_MonsterSkillEffectConfig.csv";
            var rows = SimpleCsv.ReadRows(RequirePath(table));
            for (var i = 0; i < rows.Count; i++)
            {
                var raw = rows[i];
                var rowIndex = i + 2;
                var monsterSkillId = SimpleCsv.Require(raw, "MonsterSkillId", table, rowIndex);
                if (_monsterSkillEffectById.ContainsKey(monsterSkillId))
                {
                    throw new InvalidOperationException(
                        $"{table} row {rowIndex}: duplicate MonsterSkillId '{monsterSkillId}'.");
                }

                _monsterSkillEffectById[monsterSkillId] = new MonsterSkillEffectConfigRow
                {
                    MonsterSkillId = monsterSkillId,
                    DisplayName = OptionalText(raw, "DisplayName"),
                    EffectKind = OptionalText(raw, "EffectKind"),
                    EffectParams = OptionalText(raw, "EffectParams")
                };
            }
        }

        private void LoadFormationBonds()
        {
            const string table = "Combat_FormationBondConfig.csv";
            var rows = SimpleCsv.ReadRows(RequirePath(table));
            for (var i = 0; i < rows.Count; i++)
            {
                var raw = rows[i];
                var rowIndex = i + 2;
                var bondId = SimpleCsv.Require(raw, "BondId", table, rowIndex);
                var levelText = SimpleCsv.Require(raw, "BondLevel", table, rowIndex);
                if (!int.TryParse(levelText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var level)
                    || level < 1)
                {
                    throw new InvalidOperationException(
                        $"{table} row {rowIndex}: illegal BondLevel '{levelText}' (expect ≥ 1).");
                }

                var key = MakeFormationBondKey(bondId, level);
                if (_formationBondByKey.ContainsKey(key))
                {
                    throw new InvalidOperationException(
                        $"{table} row {rowIndex}: duplicate composite PK ({bondId}, {level}).");
                }

                var activationCondition = SimpleCsv.Require(raw, "ActivationCondition", table, rowIndex);
                if (!BondActivationCondition.TryParse(activationCondition, out var parsed, out var parseError))
                {
                    throw new InvalidOperationException(
                        $"{table} row {rowIndex}: {parseError}");
                }

                var bondBuff = OptionalText(raw, "BondBuff");
                if (!string.IsNullOrEmpty(bondBuff) && !_skillEffectById.ContainsKey(bondBuff))
                {
                    throw new InvalidOperationException(
                        $"{table} row {rowIndex}: BondBuff '{bondBuff}' not found in Combat_SkillEffectConfig.");
                }

                var row = new FormationBondConfigRow
                {
                    BondId = bondId,
                    BondLevel = level,
                    DisplayName = OptionalText(raw, "DisplayName"),
                    IconAssetId = OptionalText(raw, "IconAssetId"),
                    Description = OptionalText(raw, "Description"),
                    ActivationCondition = activationCondition,
                    BondBuff = bondBuff
                };

                _formationBondByKey[key] = row;
                _formationBondRows.Add(row);
                _bondConditionByBondKey[key] = parsed;

                if (!_formationBondRowsByBondId.TryGetValue(bondId, out var byId))
                {
                    byId = new List<FormationBondConfigRow>();
                    _formationBondRowsByBondId[bondId] = byId;
                }

                byId.Add(row);
            }

            foreach (var pair in _formationBondRowsByBondId)
            {
                pair.Value.Sort((a, b) => a.BondLevel.CompareTo(b.BondLevel));
            }
        }

        private static string MakeFormationBondKey(string bondId, int bondLevel)
        {
            return bondId + "\u001f" + bondLevel.ToString(CultureInfo.InvariantCulture);
        }

        private static string MakeSkillKey(string skillId, int skillLevel)
        {
            return skillId + "\u001f" + skillLevel.ToString(CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Empty = Unspecified; CSV Chinese 战士/射手/法师/刺客 (legacy 盗贼 still accepted);
        /// illegal → Warning + Unspecified.
        /// </summary>
        private static BaseClassKind ParseBaseClass(string text, string table, int rowIndex)
        {
            if (string.IsNullOrEmpty(text))
            {
                return BaseClassKind.Unspecified;
            }

            switch (text.Trim())
            {
                case "战士":
                    return BaseClassKind.Warrior;
                case "射手":
                    return BaseClassKind.Archer;
                case "法师":
                    return BaseClassKind.Mage;
                case "刺客":
                case "盗贼":
                    return BaseClassKind.Thief;
                default:
                    Debug.LogWarning(
                        $"[Config] {table} row {rowIndex}: illegal BaseClass '{text}' " +
                        "(expect 战士|射手|法师|刺客 or empty); using Unspecified.");
                    return BaseClassKind.Unspecified;
            }
        }

        /// <summary>
        /// Empty = none; else SkillId or SkillId|… . Duplicates keep first.
        /// Unknown SkillId is kept (grant-time Warning, SPEC_04 §9.9b).
        /// </summary>
        private static string[] ParseDefaultSkillIds(string encoded)
        {
            if (string.IsNullOrEmpty(encoded))
            {
                return Array.Empty<string>();
            }

            var parts = encoded.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
            var result = new List<string>();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < parts.Length; i++)
            {
                var id = parts[i].Trim();
                if (id.Length == 0 || !seen.Add(id))
                {
                    continue;
                }

                result.Add(id);
            }

            return result.Count == 0 ? Array.Empty<string>() : result.ToArray();
        }

        private struct SkillLevelRange
        {
            public int Min;
            public int Max;
        }

        private void LoadMagicBooks()
        {
            const string table = "Manufacture_MagicBookConfig.csv";
            var rows = SimpleCsv.ReadRows(RequirePath(table));
            for (var i = 0; i < rows.Count; i++)
            {
                var raw = rows[i];
                var rowIndex = i + 2;
                var id = SimpleCsv.Require(raw, "MagicBookId", table, rowIndex);
                _magicBookById[id] = new MagicBookConfigRow
                {
                    MagicBookId = id,
                    IsUnique = ParseOptional01(raw, "IsUnique", table, rowIndex),
                    IsProbabilistic = ParseOptional01(raw, "IsProbabilistic", table, rowIndex),
                    EffectPhase = OptionalText(raw, "EffectPhase"),
                    EffectPayload = OptionalText(raw, "EffectPayload"),
                    EffectParams = OptionalText(raw, "EffectParams"),
                    IconAssetId = OptionalText(raw, "IconAssetId"),
                    DisplayName = OptionalText(raw, "DisplayName"),
                    Description = OptionalText(raw, "Description"),
                    VisualStyleId = OptionalText(raw, "VisualStyleId"),
                    VisualPriority = ParseOptionalNonNegInt(raw, "VisualPriority", table, rowIndex),
                    VisualIntensityAdd = ParseVisualIntensityAdd(raw)
                };
            }
        }

        private void LoadProtagonistEquipment()
        {
            const string table = "Protagonist_ProtagonistEquipmentConfig.csv";
            var rows = SimpleCsv.ReadRows(RequirePath(table));
            var levelsByEquip = new Dictionary<string, List<int>>(StringComparer.Ordinal);
            for (var i = 0; i < rows.Count; i++)
            {
                var raw = rows[i];
                var rowIndex = i + 2;
                var equipId = SimpleCsv.Require(raw, "EquipId", table, rowIndex);
                var levelText = SimpleCsv.Require(raw, "EquipLevel", table, rowIndex);
                if (!int.TryParse(levelText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var level)
                    || level < 1)
                {
                    throw new InvalidOperationException(
                        $"{table} row {rowIndex}: illegal EquipLevel '{levelText}' (expect ≥ 1).");
                }

                var convertText = SimpleCsv.Require(raw, "ConvertExpValue", table, rowIndex);
                if (!int.TryParse(convertText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var convertExp))
                {
                    throw new InvalidOperationException(
                        $"{table} row {rowIndex}: illegal ConvertExpValue '{convertText}'.");
                }

                var key = MakeProtagonistEquipmentKey(equipId, level);
                if (_protagonistEquipmentByKey.ContainsKey(key))
                {
                    throw new InvalidOperationException(
                        $"{table} row {rowIndex}: duplicate composite PK ({equipId}, {level}).");
                }

                _protagonistEquipmentByKey[key] = new ProtagonistEquipmentConfigRow
                {
                    EquipId = equipId,
                    EquipLevel = level,
                    DisplayName = OptionalText(raw, "DisplayName"),
                    IconAssetId = OptionalText(raw, "IconAssetId"),
                    ExpToNextLevel = ParseOptionalNonNegInt(raw, "ExpToNextLevel", table, rowIndex),
                    ConvertExpValue = convertExp,
                    EffectDomain = OptionalText(raw, "EffectDomain"),
                    EquipEffect = OptionalText(raw, "EquipEffect"),
                    Description = OptionalText(raw, "Description")
                };

                if (!levelsByEquip.TryGetValue(equipId, out var levels))
                {
                    levels = new List<int>();
                    levelsByEquip[equipId] = levels;
                }

                levels.Add(level);
            }

            foreach (var pair in levelsByEquip)
            {
                pair.Value.Sort();
                for (var expected = 1; expected <= pair.Value.Count; expected++)
                {
                    if (pair.Value[expected - 1] == expected)
                    {
                        continue;
                    }

                    Debug.LogWarning(
                        $"[Config] {table}: EquipId '{pair.Key}' levels are not contiguous from 1 " +
                        $"(got [{string.Join(",", pair.Value)}]).");
                    break;
                }
            }
        }

        private void LoadItemCatalog()
        {
            const string table = "Item_ItemCatalogConfig.csv";
            var rows = SimpleCsv.ReadRows(RequirePath(table));
            for (var i = 0; i < rows.Count; i++)
            {
                var raw = rows[i];
                var rowIndex = i + 2;
                var itemId = SimpleCsv.Require(raw, "ItemId", table, rowIndex);
                if (_itemCatalogById.ContainsKey(itemId))
                {
                    throw new InvalidOperationException($"{table} row {rowIndex}: duplicate ItemId '{itemId}'.");
                }

                var itemType = SimpleCsv.Require(raw, "ItemType", table, rowIndex);
                var sourceTable = SimpleCsv.Require(raw, "SourceTable", table, rowIndex);
                ValidateItemCatalogSource(itemId, itemType, sourceTable, table, rowIndex);

                _itemCatalogById[itemId] = new ItemCatalogConfigRow
                {
                    ItemId = itemId,
                    DisplayName = OptionalText(raw, "DisplayName"),
                    IconAssetId = OptionalText(raw, "IconAssetId"),
                    ItemType = itemType,
                    SourceTable = sourceTable,
                    Description = OptionalText(raw, "Description"),
                    SellPrice = ParseOptionalNonNegInt(raw, "SellPrice", table, rowIndex)
                };
            }
        }

        private static string MakeProtagonistEquipmentKey(string equipId, int equipLevel)
        {
            return equipId + "\u001f" + equipLevel.ToString(CultureInfo.InvariantCulture);
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

                var bodyText = OptionalText(raw, "BodyRadius");
                float bodyRadius;
                if (bodyText.Length == 0)
                {
                    bodyRadius = BodyAppearanceConfigRow.DefaultBodyRadius;
                }
                else if (!float.TryParse(bodyText, NumberStyles.Float, CultureInfo.InvariantCulture, out bodyRadius)
                         || bodyRadius < 0f)
                {
                    throw new InvalidOperationException(
                        $"{table} row {rowIndex}: illegal BodyRadius '{bodyText}'.");
                }

                var pushText = OptionalText(raw, "PushCoefficient");
                float pushCoefficient;
                if (pushText.Length == 0)
                {
                    pushCoefficient = BodyAppearanceConfigRow.DefaultPushCoefficient;
                }
                else if (!float.TryParse(pushText, NumberStyles.Float, CultureInfo.InvariantCulture, out pushCoefficient)
                         || pushCoefficient < 0f)
                {
                    throw new InvalidOperationException(
                        $"{table} row {rowIndex}: illegal PushCoefficient '{pushText}'.");
                }

                var repulsionText = OptionalText(raw, "RepulsionScale");
                float repulsionScale;
                if (repulsionText.Length == 0)
                {
                    repulsionScale = BodyAppearanceConfigRow.DefaultRepulsionScale;
                }
                else if (!float.TryParse(repulsionText, NumberStyles.Float, CultureInfo.InvariantCulture, out repulsionScale)
                         || repulsionScale < 0f)
                {
                    throw new InvalidOperationException(
                        $"{table} row {rowIndex}: illegal RepulsionScale '{repulsionText}'.");
                }

                var facingYawFlip = ParseFacingYawFlip(raw, table, rowIndex);

                var id = SimpleCsv.Require(raw, "AppearanceId", table, rowIndex);
                var row = new BodyAppearanceConfigRow
                {
                    AppearanceId = id,
                    AppearanceLevel = level,
                    RaceId = SimpleCsv.Require(raw, "RaceId", table, rowIndex),
                    ClassAffinity = OptionalText(raw, "ClassAffinity"),
                    Description = OptionalText(raw, "Description"),
                    IsFallback = string.Equals(OptionalText(raw, "IsFallback"), "1", StringComparison.Ordinal),
                    BodyRadius = bodyRadius,
                    PushCoefficient = pushCoefficient,
                    RepulsionScale = repulsionScale,
                    FacingYawFlip = facingYawFlip
                };
                _appearances.Add(row);
                _appearanceById[id] = row;
            }
        }

        private void LoadLossOfControl()
        {
            const string table = "Combat_LossOfControlConfig.csv";
            var path = RequirePath(table);
            var rows = SimpleCsv.ReadRows(path);
            for (var i = 0; i < rows.Count; i++)
            {
                var raw = rows[i];
                var rowIndex = i + 2;
                var tierText = SimpleCsv.Require(raw, "TierId", table, rowIndex);
                if (!int.TryParse(tierText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var tierId)
                    || tierId < 1
                    || tierId > 4)
                {
                    throw new InvalidOperationException(
                        $"{table} row {rowIndex}: illegal TierId '{tierText}' (expect 1..4).");
                }

                var chance = RequireFloat(raw, "LossOfControlChance", table, rowIndex);
                if (chance < 0f || chance > 1f)
                {
                    throw new InvalidOperationException(
                        $"{table} row {rowIndex}: LossOfControlChance must be in [0,1].");
                }

                _lossOfControlByTier[tierId] = new LossOfControlConfigRow
                {
                    TierId = tierId,
                    DisplayName = OptionalText(raw, "DisplayName"),
                    Description = OptionalText(raw, "Description"),
                    LossOfControlChance = chance
                };
            }
        }

        private void LoadCombatConstants()
        {
            const string table = "Combat_CombatConstantConfig.csv";
            var path = RequirePath(table);
            var rows = SimpleCsv.ReadRows(path);
            for (var i = 0; i < rows.Count; i++)
            {
                var raw = rows[i];
                var rowIndex = i + 2;
                var key = SimpleCsv.Require(raw, "ConstantKey", table, rowIndex);
                if (_combatConstantByKey.ContainsKey(key))
                {
                    throw new InvalidOperationException(
                        $"{table} row {rowIndex}: duplicate ConstantKey '{key}'.");
                }

                var value = RequireFloat(raw, "Value", table, rowIndex);
                _combatConstantByKey[key] = value;
            }

            CombatRuntimeTuning.ApplyFromRepository(this);
        }

        private void LoadTechTree()
        {
            const string table = "Tech_TechTreeConfig.csv";
            var rows = SimpleCsv.ReadRows(RequirePath(table));
            for (var i = 0; i < rows.Count; i++)
            {
                var raw = rows[i];
                var rowIndex = i + 2;
                var id = SimpleCsv.Require(raw, "TechId", table, rowIndex);
                var costText = SimpleCsv.Require(raw, "LearnCost", table, rowIndex);
                if (!int.TryParse(costText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var learnCost)
                    || learnCost < 0)
                {
                    throw new InvalidOperationException(
                        $"{table} row {rowIndex}: illegal LearnCost '{costText}'.");
                }

                var frameText = SimpleCsv.Require(raw, "TechUiFrameType", table, rowIndex);
                if (!Enum.TryParse(frameText, false, out TechUiFrameType frameType))
                {
                    throw new InvalidOperationException(
                        $"{table} row {rowIndex}: illegal TechUiFrameType '{frameText}'.");
                }

                var initially = string.Equals(
                    OptionalText(raw, "InitiallyUnlocked"),
                    "true",
                    StringComparison.OrdinalIgnoreCase)
                    || string.Equals(OptionalText(raw, "InitiallyUnlocked"), "1", StringComparison.Ordinal);

                var nextEncoded = OptionalText(raw, "UnlockNextTechIds");
                var nextIds = string.IsNullOrEmpty(nextEncoded)
                    ? Array.Empty<string>()
                    : nextEncoded.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);

                for (var n = 0; n < nextIds.Length; n++)
                {
                    nextIds[n] = nextIds[n].Trim();
                }

                var row = new TechTreeConfigRow
                {
                    TechId = id,
                    IconId = OptionalText(raw, "IconId"),
                    DisplayName = OptionalText(raw, "DisplayName"),
                    EffectDescription = OptionalText(raw, "EffectDescription"),
                    UnlockNextTechIds = nextIds,
                    InitiallyUnlocked = initially,
                    LearnCost = learnCost,
                    TechUiFrameType = frameType
                };
                _techTreeRows.Add(row);
                _techTreeById[id] = row;
            }
        }

        private void LoadTechEffects()
        {
            const string table = "Tech_TechEffectConfig.csv";
            var rows = SimpleCsv.ReadRows(RequirePath(table));
            for (var i = 0; i < rows.Count; i++)
            {
                var raw = rows[i];
                var rowIndex = i + 2;
                var id = SimpleCsv.Require(raw, "TechId", table, rowIndex);
                if (_techEffectById.TryGetValue(id, out var existing))
                {
                    var extraMods = OptionalText(raw, "AttributeModifiers");
                    if (!string.IsNullOrEmpty(extraMods))
                    {
                        existing.AttributeModifiers = string.IsNullOrEmpty(existing.AttributeModifiers)
                            ? extraMods
                            : existing.AttributeModifiers + "|" + extraMods;
                    }

                    var feature = OptionalText(raw, "UnlockedFeatureSystemName");
                    if (!string.IsNullOrEmpty(feature) && string.IsNullOrEmpty(existing.UnlockedFeatureSystemName))
                    {
                        existing.UnlockedFeatureSystemName = feature;
                    }

                    continue;
                }

                _techEffectById[id] = new TechEffectConfigRow
                {
                    TechId = id,
                    AttributeModifiers = OptionalText(raw, "AttributeModifiers"),
                    UnlockedFeatureSystemName = OptionalText(raw, "UnlockedFeatureSystemName")
                };
            }
        }

        private void LoadShopPoolConfig()
        {
            // SPEC_04 §9.27：Mode2 shop pools with PoolItemsRaw parsing + basic validation.
            var equipIdSet = BuildProtagonistEquipIdSet();
            var magicBookIdSet = new HashSet<string>(_magicBookById.Keys, StringComparer.Ordinal);

            const string table = "Shop_ShopPoolConfig.csv";
            var rows = SimpleCsv.ReadRows(RequirePath(table));

            for (var i = 0; i < rows.Count; i++)
            {
                var raw = rows[i];
                var rowIndex = i + 2;

                var shopPoolId = SimpleCsv.Require(raw, "ShopPoolId", table, rowIndex);
                if (_shopPoolById.ContainsKey(shopPoolId))
                {
                    throw new InvalidOperationException(
                        $"{table} row {rowIndex}: duplicate ShopPoolId '{shopPoolId}'.");
                }

                var requiredText = SimpleCsv.Require(raw, "RequiredMaxLevelNumber", table, rowIndex);
                if (!int.TryParse(requiredText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var required)
                    || required < 0)
                {
                    throw new InvalidOperationException(
                        $"{table} row {rowIndex}: illegal RequiredMaxLevelNumber '{requiredText}'.");
                }

                var extraUnlockCondition = OptionalText(raw, "ExtraUnlockCondition");
                var poolItemsRaw = SimpleCsv.Require(raw, "PoolItemsRaw", table, rowIndex);

                var poolItems = ParseShopPoolItemsRaw(
                    poolItemsRaw,
                    table,
                    rowIndex,
                    equipIdSet,
                    magicBookIdSet);

                var row = new ShopPoolConfigRow
                {
                    ShopPoolId = shopPoolId,
                    RequiredMaxLevelNumber = required,
                    ExtraUnlockCondition = extraUnlockCondition,
                    PoolItemsRaw = poolItemsRaw,
                    PoolItems = poolItems
                };

                _shopPoolById[shopPoolId] = row;
                _shopPoolRows.Add(row);
            }
        }

        private void LoadShopRefreshPriceConfig()
        {
            // SPEC_04 §9.28：Mode2 shop manual refresh price progression.
            const string table = "Shop_ShopRefreshPriceConfig.csv";
            var rows = SimpleCsv.ReadRows(RequirePath(table));

            for (var i = 0; i < rows.Count; i++)
            {
                var raw = rows[i];
                var rowIndex = i + 2;

                var refreshCountText = SimpleCsv.Require(raw, "RefreshCount", table, rowIndex);
                if (!int.TryParse(refreshCountText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var refreshCount)
                    || refreshCount < 0)
                {
                    throw new InvalidOperationException(
                        $"{table} row {rowIndex}: illegal RefreshCount '{refreshCountText}'.");
                }

                if (_shopRefreshPriceByCount.ContainsKey(refreshCount))
                {
                    throw new InvalidOperationException(
                        $"{table} row {rowIndex}: duplicate RefreshCount '{refreshCount}'.");
                }

                var refreshPriceText = SimpleCsv.Require(raw, "RefreshPrice", table, rowIndex);
                if (!int.TryParse(refreshPriceText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var refreshPrice)
                    || refreshPrice < 0)
                {
                    throw new InvalidOperationException(
                        $"{table} row {rowIndex}: illegal RefreshPrice '{refreshPriceText}'.");
                }

                _shopRefreshPriceByCount[refreshCount] = new ShopRefreshPriceConfigRow
                {
                    RefreshCount = refreshCount,
                    RefreshPrice = refreshPrice
                };
            }
        }

        private void LoadBgm()
        {
            const string table = "Audio_BgmConfig.csv";
            var rows = SimpleCsv.ReadRows(RequirePath(table));
            for (var i = 0; i < rows.Count; i++)
            {
                var raw = rows[i];
                var rowIndex = i + 2;
                var id = SimpleCsv.Require(raw, "BgmId", table, rowIndex);
                if (string.IsNullOrWhiteSpace(id))
                {
                    throw new InvalidOperationException($"{table} row {rowIndex}: empty BgmId.");
                }

                if (_bgmById.ContainsKey(id))
                {
                    throw new InvalidOperationException($"{table} row {rowIndex}: duplicate BgmId '{id}'.");
                }

                var context = SimpleCsv.Require(raw, "Context", table, rowIndex).Trim();
                if (!IsValidBgmContext(context))
                {
                    throw new InvalidOperationException(
                        $"{table} row {rowIndex}: illegal Context '{context}'.");
                }

                var clipId = SimpleCsv.Require(raw, "ClipId", table, rowIndex);
                if (string.IsNullOrWhiteSpace(clipId))
                {
                    throw new InvalidOperationException($"{table} row {rowIndex}: empty ClipId.");
                }

                var loop = ParseBgmLoop(raw, table, rowIndex);
                var weight = ParseBgmWeight(raw, table, rowIndex);
                var volume = ParseBgmVolume(raw, table, rowIndex);

                var row = new BgmConfigRow
                {
                    BgmId = id,
                    Context = context,
                    ClipId = clipId,
                    Loop = loop,
                    Weight = weight,
                    Volume = volume
                };
                _bgmById[id] = row;
                _bgmRows.Add(row);
            }
        }

        private static bool IsValidBgmContext(string context)
        {
            return string.Equals(context, "Title", StringComparison.Ordinal)
                || string.Equals(context, "Dig", StringComparison.Ordinal)
                || string.Equals(context, "Combat", StringComparison.Ordinal);
        }

        private static bool ParseBgmLoop(Dictionary<string, string> raw, string table, int rowIndex)
        {
            if (!raw.TryGetValue("Loop", out var text) || string.IsNullOrWhiteSpace(text))
            {
                return true;
            }

            if (!int.TryParse(text.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var loop)
                || (loop != 0 && loop != 1))
            {
                throw new InvalidOperationException($"{table} row {rowIndex}: illegal Loop '{text}'.");
            }

            return loop == 1;
        }

        private static int ParseBgmWeight(Dictionary<string, string> raw, string table, int rowIndex)
        {
            if (!raw.TryGetValue("Weight", out var text) || string.IsNullOrWhiteSpace(text))
            {
                return 1;
            }

            if (!int.TryParse(text.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var weight)
                || weight < 0)
            {
                throw new InvalidOperationException($"{table} row {rowIndex}: illegal Weight '{text}'.");
            }

            return weight;
        }

        private static float ParseBgmVolume(Dictionary<string, string> raw, string table, int rowIndex)
        {
            if (!raw.TryGetValue("Volume", out var text) || string.IsNullOrWhiteSpace(text))
            {
                return 1f;
            }

            if (!float.TryParse(text.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var volume)
                || volume < 0f || volume > 1f)
            {
                throw new InvalidOperationException($"{table} row {rowIndex}: illegal Volume '{text}'.");
            }

            return volume;
        }

        private static List<ShopPoolItemCandidate> ParseShopPoolItemsRaw(
            string poolItemsRaw,
            string table,
            int rowIndex,
            HashSet<string> equipIdSet,
            HashSet<string> magicBookIdSet)
        {
            var result = new List<ShopPoolItemCandidate>();
            if (string.IsNullOrWhiteSpace(poolItemsRaw))
            {
                return result;
            }

            var segments = poolItemsRaw.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
            for (var i = 0; i < segments.Length; i++)
            {
                var segment = segments[i];
                var parts = segment.Split(new[] { ';' }, StringSplitOptions.None);
                if (parts.Length != 3)
                {
                    throw new InvalidOperationException(
                        $"{table} row {rowIndex}: PoolItemsRaw segment '{segment}' must be 'itemId;category;weight'.");
                }

                var itemId = parts[0]?.Trim() ?? string.Empty;
                var categoryText = parts[1]?.Trim() ?? string.Empty;
                var weightText = parts[2]?.Trim() ?? string.Empty;

                if (string.IsNullOrEmpty(itemId))
                {
                    throw new InvalidOperationException(
                        $"{table} row {rowIndex}: PoolItemsRaw segment '{segment}' has empty itemId.");
                }

                ShopPoolItemCategory category;
                if (string.Equals(categoryText, "A", StringComparison.Ordinal))
                {
                    category = ShopPoolItemCategory.A;
                }
                else if (string.Equals(categoryText, "B", StringComparison.Ordinal))
                {
                    category = ShopPoolItemCategory.B;
                }
                else
                {
                    throw new InvalidOperationException(
                        $"{table} row {rowIndex}: PoolItemsRaw segment '{segment}' has illegal category '{categoryText}' (expected A|B).");
                }

                if (!float.TryParse(weightText, NumberStyles.Float, CultureInfo.InvariantCulture, out var weight) || weight < 0f)
                {
                    throw new InvalidOperationException(
                        $"{table} row {rowIndex}: PoolItemsRaw segment '{segment}' has illegal weight '{weightText}' (expected non-negative).");
                }

                // Basic type validation (SS-01): A→ProtagonistEquipmentConfig EquipId, B→MagicBookConfig MagicBookId.
                if (category == ShopPoolItemCategory.A)
                {
                    if (!equipIdSet.Contains(itemId))
                    {
                        throw new InvalidOperationException(
                            $"{table} row {rowIndex}: PoolItemsRaw segment '{segment}' category=A but itemId '{itemId}' not in ProtagonistEquipmentConfig EquipId set.");
                    }
                }
                else
                {
                    if (!magicBookIdSet.Contains(itemId))
                    {
                        throw new InvalidOperationException(
                            $"{table} row {rowIndex}: PoolItemsRaw segment '{segment}' category=B but itemId '{itemId}' not in Manufacture_MagicBookConfig MagicBookId set.");
                    }
                }

                // weight=0 is treated as absent.
                if (weight == 0f)
                {
                    continue;
                }

                result.Add(new ShopPoolItemCandidate
                {
                    ItemId = itemId,
                    Category = category,
                    Weight = weight
                });
            }

            return result;
        }

        private HashSet<string> BuildProtagonistEquipIdSet()
        {
            var set = new HashSet<string>(StringComparer.Ordinal);
            foreach (var key in _protagonistEquipmentByKey.Keys)
            {
                if (string.IsNullOrEmpty(key))
                {
                    continue;
                }

                var idx = key.IndexOf('\u001f');
                var equipId = idx >= 0 ? key.Substring(0, idx) : key;
                if (!string.IsNullOrEmpty(equipId))
                {
                    set.Add(equipId);
                }
            }

            return set;
        }

        private static int ParseFacingYawFlip(Dictionary<string, string> raw, string table, int rowIndex)
        {
            var text = OptionalText(raw, "FacingYawFlip");
            if (text.Length == 0)
            {
                return 0;
            }

            if (!int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var flip)
                || (flip != 0 && flip != 1))
            {
                throw new InvalidOperationException(
                    $"{table} row {rowIndex}: illegal FacingYawFlip '{text}' (expected 0|1).");
            }

            return flip;
        }

        private void WarnMonsterFacingYawFlipModelIdMismatch()
        {
            var flipByModel = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var row in _monsterById.Values)
            {
                if (row == null || row.ModelIdPool == null || row.ModelIdPool.Count == 0)
                {
                    continue;
                }

                for (var p = 0; p < row.ModelIdPool.Count; p++)
                {
                    var subModelId = row.ModelIdPool[p].Id;
                    if (string.IsNullOrEmpty(subModelId))
                    {
                        continue;
                    }

                    if (!flipByModel.TryGetValue(subModelId, out var existing))
                    {
                        flipByModel[subModelId] = row.FacingYawFlip;
                        continue;
                    }

                    if (existing != row.FacingYawFlip)
                    {
                        Debug.LogWarning(
                            $"[ConfigCsvRepository] Defend_MonsterConfig: ModelId '{subModelId}' has inconsistent " +
                            $"FacingYawFlip values ({existing} vs {row.FacingYawFlip} on MonsterId '{row.MonsterId}').");
                    }
                }
            }
        }

        private static string OptionalText(Dictionary<string, string> raw, string column)
        {
            return raw.TryGetValue(column, out var value) ? (value ?? string.Empty).Trim() : string.Empty;
        }

        private void ValidateItemCatalogSource(string itemId, string itemType, string sourceTable, string table, int rowIndex)
        {
            if (string.Equals(itemType, "Currency", StringComparison.Ordinal))
            {
                if (!string.Equals(sourceTable, "Dig_CurrencyConfig", StringComparison.Ordinal) ||
                    !_currencyById.ContainsKey(itemId))
                {
                    throw new InvalidOperationException(
                        $"{table} row {rowIndex}: ItemId '{itemId}' Currency must map to Dig_CurrencyConfig.");
                }

                return;
            }

            if (string.Equals(itemType, "Material", StringComparison.Ordinal))
            {
                if (!string.Equals(sourceTable, "Dig_MaterialConfig", StringComparison.Ordinal) ||
                    !_materialById.ContainsKey(itemId))
                {
                    throw new InvalidOperationException(
                        $"{table} row {rowIndex}: ItemId '{itemId}' Material must map to Dig_MaterialConfig.");
                }

                return;
            }

            if (string.Equals(itemType, "BodyPart", StringComparison.Ordinal))
            {
                if (!string.Equals(sourceTable, "Manufacture_BodyPartConfig", StringComparison.Ordinal) ||
                    !_bodyPartById.ContainsKey(itemId))
                {
                    throw new InvalidOperationException(
                        $"{table} row {rowIndex}: ItemId '{itemId}' BodyPart must map to Manufacture_BodyPartConfig.");
                }

                return;
            }

            if (string.Equals(itemType, "MagicBook", StringComparison.Ordinal))
            {
                if (!string.Equals(sourceTable, "Manufacture_MagicBookConfig", StringComparison.Ordinal) ||
                    !_magicBookById.ContainsKey(itemId))
                {
                    throw new InvalidOperationException(
                        $"{table} row {rowIndex}: ItemId '{itemId}' MagicBook must map to Manufacture_MagicBookConfig.");
                }

                return;
            }

            if (string.Equals(itemType, "ProtagonistEquipment", StringComparison.Ordinal))
            {
                if (!string.Equals(sourceTable, "Protagonist_ProtagonistEquipmentConfig", StringComparison.Ordinal) ||
                    !_protagonistEquipmentByKey.ContainsKey(MakeProtagonistEquipmentKey(itemId, 1)))
                {
                    throw new InvalidOperationException(
                        $"{table} row {rowIndex}: ItemId '{itemId}' ProtagonistEquipment must map to Protagonist_ProtagonistEquipmentConfig Level 1.");
                }

                return;
            }

            throw new InvalidOperationException(
                $"{table} row {rowIndex}: unsupported ItemType '{itemType}' for ItemId '{itemId}'.");
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

        /// <summary>Missing/empty → false. "1" or "true" → true (SPEC_04 §9.21 EffectImplemented).</summary>
        private static bool ParseOptionalBool01(Dictionary<string, string> raw, string column)
        {
            var text = OptionalText(raw, column);
            return string.Equals(text, "1", StringComparison.Ordinal)
                   || string.Equals(text, "true", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Missing/empty VisualIntensityAdd → 1 (SPEC_04 §9.24).</summary>
        private static float ParseVisualIntensityAdd(Dictionary<string, string> raw)
        {
            var text = OptionalText(raw, "VisualIntensityAdd");
            if (text.Length == 0)
            {
                return 1f;
            }

            return float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value)
                ? value
                : 1f;
        }

        /// <summary>Missing/empty → 0; else must be 0 or 1.</summary>
        private static int ParseOptional01(
            Dictionary<string, string> raw,
            string column,
            string table,
            int rowIndex)
        {
            var text = OptionalText(raw, column);
            if (text.Length == 0)
            {
                return 0;
            }

            if (!int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)
                || (value != 0 && value != 1))
            {
                throw new InvalidOperationException($"{table} row {rowIndex}: illegal {column} '{text}'.");
            }

            return value;
        }

        /// <summary>Missing/empty → 0; else non-negative int (SPEC §9.25 ExpToNextLevel empty = max).</summary>
        private static int ParseOptionalNonNegInt(
            Dictionary<string, string> raw,
            string column,
            string table,
            int rowIndex)
        {
            var text = OptionalText(raw, column);
            if (text.Length == 0)
            {
                return 0;
            }

            if (!int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)
                || value < 0)
            {
                throw new InvalidOperationException($"{table} row {rowIndex}: illegal {column} '{text}'.");
            }

            return value;
        }

        /// <summary>
        /// Missing/empty → false (BodyPrimaryStat default unused). Non-empty must be Strength|Agility|Intelligence.
        /// </summary>
        private static bool TryParseOptionalPrimaryStat(
            Dictionary<string, string> raw,
            string column,
            string table,
            int rowIndex,
            out StatKind primary)
        {
            primary = StatKind.Strength;
            var text = OptionalText(raw, column);
            if (text.Length == 0)
            {
                return false;
            }

            if (!Enum.TryParse(text, false, out primary)
                || (primary != StatKind.Strength && primary != StatKind.Agility
                                                 && primary != StatKind.Intelligence))
            {
                throw new InvalidOperationException($"{table} row {rowIndex}: illegal {column} '{text}'.");
            }

            return true;
        }

        /// <summary>Missing/empty → DefaultPlacementOrderMissing; else int ≥ 1.</summary>
        private static int ParseOptionalPlacementOrder(
            Dictionary<string, string> raw,
            string column,
            string table,
            int rowIndex)
        {
            var text = OptionalText(raw, column);
            if (text.Length == 0)
            {
                return ClassConfigRow.DefaultPlacementOrderMissing;
            }

            if (!int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)
                || value < 1)
            {
                throw new InvalidOperationException($"{table} row {rowIndex}: illegal {column} '{text}'.");
            }

            return value;
        }

        /// <summary>
        /// Missing/empty → <paramref name="defaultValue"/>; parse fail or &lt; 0 → throw (SPEC load fail).
        /// </summary>
        private static float ParseOptionalNonNegFloat(
            Dictionary<string, string> raw,
            string column,
            float defaultValue,
            string table,
            int rowIndex)
        {
            var text = OptionalText(raw, column);
            if (text.Length == 0)
            {
                return defaultValue;
            }

            if (!float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value)
                || value < 0f)
            {
                throw new InvalidOperationException(
                    $"{table} row {rowIndex}: illegal {column} '{text}'.");
            }

            return value;
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

        private string RequirePath(string csvFileName)
        {
            var path = CsvPathResolver.ResolveExistingFile(csvFileName, _loadMode);
            if (path == null)
            {
                var roots = string.Join(" | ", CsvPathResolver.EnumerateCandidateRoots(_loadMode));
                throw new InvalidOperationException(
                    $"Missing CSV '{csvFileName}' (CampaignMode={_loadMode}). Looked in: {roots}");
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

            if (string.Equals(text, "PushMap", StringComparison.Ordinal))
            {
                state = GameplayState.PushMap;
                return true;
            }

            if (string.Equals(text, "AutoManufacture", StringComparison.Ordinal))
            {
                state = GameplayState.AutoManufacture;
                return true;
            }

            if (string.Equals(text, "Shop", StringComparison.Ordinal))
            {
                state = GameplayState.Shop;
                return true;
            }

            state = default;
            return false;
        }
    }
}
