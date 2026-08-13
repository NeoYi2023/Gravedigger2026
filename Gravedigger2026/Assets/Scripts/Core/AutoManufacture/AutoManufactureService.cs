using System;
using System.Collections.Generic;
using Gravedigger2026.Core.Config;
using Gravedigger2026.Core.Dig;
using Gravedigger2026.Core.UpgradeManufacture;
using UnityEngine;

namespace Gravedigger2026.Core.AutoManufacture
{
    /// <summary>
    /// Mode2 AutoManufacture pick/class/finalize/pool flush (SPEC_03 §3.15).
    /// Does not touch Mode1 ManufactureService.
    /// </summary>
    public sealed class AutoManufactureService
    {
        private readonly ConfigCsvRepository _configs;
        private readonly WarehouseService _warehouse;
        private readonly TempWarriorWarehouse _tempWarehouse;
        private readonly WarriorPoolService _warriorPool;
        private readonly ISoldierManufactureMagicBookHook _magicBookHook;
        private readonly System.Random _rng;
        private int _tempIdSeq;

        public AutoManufactureService(
            ConfigCsvRepository configs,
            WarehouseService warehouse,
            TempWarriorWarehouse tempWarehouse,
            WarriorPoolService warriorPool,
            ISoldierManufactureMagicBookHook magicBookHook = null)
        {
            _configs = configs ?? throw new ArgumentNullException(nameof(configs));
            _warehouse = warehouse ?? throw new ArgumentNullException(nameof(warehouse));
            _tempWarehouse = tempWarehouse ?? throw new ArgumentNullException(nameof(tempWarehouse));
            _warriorPool = warriorPool ?? throw new ArgumentNullException(nameof(warriorPool));
            _magicBookHook = magicBookHook ?? NoOpSoldierManufactureMagicBookHook.Instance;
            _rng = new System.Random(Environment.TickCount);
        }

        public TempWarriorWarehouse TempWarehouse => _tempWarehouse;

        /// <summary>
        /// Craft until warehouse cannot satisfy min recipe or a pick step stops crafting,
        /// then flush temp warehouse → WarriorPool (persist). Does not spend Spirit.
        /// Clear formation + zone deploy are orchestrated by StageModule (AM-06).
        /// </summary>
        /// <param name="flushedIds">Optional; receives WarriorPool Ids added this batch (for auto-deploy).</param>
        public int RunBatch(out string stopReason, IList<string> flushedIds = null)
        {
            _tempWarehouse.Clear();
            _tempIdSeq = 0;
            stopReason = null;
            flushedIds?.Clear();

            var crafted = 0;
            while (true)
            {
                if (!MeetsMinRecipeStock(out var stockReason))
                {
                    stopReason = stockReason ?? "最低配方不足，停造";
                    break;
                }

                if (!TryCraftOne(out var craftReason))
                {
                    stopReason = craftReason ?? "选料失败，停造";
                    break;
                }

                crafted++;
            }

            var flushed = FlushTempToPool(flushedIds);
            Debug.Log(
                $"[AutoManufacture] Batch done crafted={crafted} flushed={flushed} " +
                $"tempLeft={_tempWarehouse.Count} stop={stopReason}");
            return crafted;
        }

        /// <summary>
        /// Convert finalized drafts to WarriorInstance and Add+persist; clears temp warehouse.
        /// </summary>
        public int FlushTempToPool(IList<string> flushedIds = null)
        {
            var drafts = _tempWarehouse.Drafts;
            var count = 0;
            for (var i = 0; i < drafts.Count; i++)
            {
                var draft = drafts[i];
                if (draft == null)
                {
                    continue;
                }

                var instance = BuildWarriorInstance(draft);
                _warriorPool.Add(instance);
                flushedIds?.Add(instance.Id);
                count++;
                Debug.Log(
                    $"[AutoManufacture] Pool+ {instance.Id} Name={instance.WarriorName} " +
                    $"Class={instance.ClassId} Appearance={instance.AppearanceId} " +
                    $"AttackMode={instance.AttackMode} SoulId='{instance.SoulId ?? string.Empty}' " +
                    $"ControlCost={instance.ControlPowerCost} MaxHP={instance.RemainingHP}");
            }

            _tempWarehouse.Clear();
            return count;
        }

        /// <summary>
        /// Editor/handcheck helper: grants one min-recipe set from Mode2 sample BodyParts (does not spend Spirit).
        /// </summary>
        public void DebugGrantMinRecipeKit()
        {
            _warehouse.AddItem("BP_Head_Human", 1);
            _warehouse.AddItem("BP_Torso_Human", 1);
            _warehouse.AddItem("BP_Arm_Elf", 1);
            _warehouse.AddItem("BP_Arm_Human", 1);
            _warehouse.AddItem("BP_Leg_Elf", 1);
            _warehouse.AddItem("BP_Leg_Dwarf", 1);
            Debug.Log("[AutoManufacture] DebugGrantMinRecipeKit credited sample Head/Torso/Primary+Secondary/2Leg.");
        }

        public bool MeetsMinRecipeStock(out string reason)
        {
            reason = null;
            var head = 0;
            var torso = 0;
            var arm = 0;
            var primary = 0;
            var leg = 0;

            foreach (var pair in _warehouse.Materials)
            {
                if (pair.Value < 1 || !_configs.TryGetBodyPart(pair.Key, out var part) || part == null)
                {
                    continue;
                }

                switch (part.BodySlot)
                {
                    case BodySlot.Head:
                        head += pair.Value;
                        break;
                    case BodySlot.Torso:
                        torso += pair.Value;
                        break;
                    case BodySlot.Arm:
                        arm += pair.Value;
                        if (part.IsPrimaryHand == 1)
                        {
                            primary += pair.Value;
                        }

                        break;
                    case BodySlot.Leg:
                        leg += pair.Value;
                        break;
                }
            }

            if (head < 1 || torso < 1 || arm < 2 || primary < 1 || leg < 2)
            {
                reason =
                    $"最低配方不足 Head={head} Torso={torso} Arm={arm}(Primary={primary}) Leg={leg}";
                return false;
            }

            return true;
        }

        private bool TryCraftOne(out string reason)
        {
            reason = null;
            var reserved = CloneStock();

            if (!TryPickPrimaryHand(reserved, out var primary, out reason))
            {
                return false;
            }

            Reserve(reserved, primary.BodyPartId);

            if (!TryPickSecondaryHand(reserved, primary, out var secondary, out reason))
            {
                return false;
            }

            Reserve(reserved, secondary.BodyPartId);

            if (!TryPickRemaining(reserved, primary, BodySlot.Head, out var head, out reason)
                || !TryPickRemaining(reserved, primary, BodySlot.Torso, out var torso, out reason)
                || !TryPickRemaining(reserved, primary, BodySlot.Leg, out var leg1, out reason)
                || !TryPickRemaining(reserved, primary, BodySlot.Leg, out var leg2, out reason))
            {
                return false;
            }

            var parts = new[] { primary, secondary, head, torso, leg1, leg2 };
            if (!TryResolveClass(primary, secondary, out var classRow, out reason))
            {
                return false;
            }

            var baseStats = default(StatBlock);
            var raceCandidates = new List<string>(6);
            var consumed = new List<string>(6);
            for (var i = 0; i < parts.Length; i++)
            {
                var part = parts[i];
                baseStats.Add(part.StatBonus);
                raceCandidates.Add(part.RaceId);
                consumed.Add(part.BodyPartId);
            }

            for (var i = 0; i < consumed.Count; i++)
            {
                if (!_warehouse.TryConsume(consumed[i], 1))
                {
                    reason = $"扣料失败：{consumed[i]}（库存与选料不同步）";
                    Debug.LogError($"[AutoManufacture] {reason}");
                    return false;
                }
            }

            var raceId = _magicBookHook.HasRaceWeightPick()
                ? RaceResolve.PickWeighted(raceCandidates, _rng)
                : RaceResolve.ResolveDefaultRace(raceCandidates);
            _configs.TryGetRace(raceId, out var raceRow);
            var raceAdjust = raceRow != null ? raceRow.RaceAdjustCoeff : default;

            var draft = new AutoCraftDraft
            {
                TempId = $"Temp-{++_tempIdSeq}",
                ClassId = classRow.ClassId,
                ClassName = classRow.ClassName ?? string.Empty,
                AttackMode = classRow.AttackMode,
                RaceId = raceId,
                RaceAdjustCoeff = raceAdjust,
                BaseStats = baseStats,
                EquipStats = default,
                GemMult = default,
                ControlPowerCost = 0f,
                SoulId = null
            };
            draft.ConsumedBodyPartIds.AddRange(consumed);
            for (var i = 0; i < parts.Length; i++)
            {
                draft.BodyLevels.Add(parts[i].BodyLevel);
            }

            // Hook may mutate BaseStats later; FinalizeDraft applies appearance (incl. A-empty Undead).
            _magicBookHook.ApplySoldierManufactureEffects(draft);
            FinalizeDraft(draft, classRow, raceRow);
            _tempWarehouse.Add(draft);

            Debug.Log(
                $"[AutoManufacture] Crafted {draft.TempId} Class={draft.ClassId} AttackMode={draft.AttackMode} " +
                $"Race={draft.RaceId} Appearance={draft.AppearanceId} Name={draft.WarriorName} " +
                $"MaxHP={draft.MaxHP} Parts={string.Join(",", consumed)}");
            return true;
        }

        private bool TryPickPrimaryHand(
            Dictionary<string, int> reserved,
            out BodyPartConfigRow primary,
            out string reason)
        {
            primary = null;
            reason = null;
            BodyPartConfigRow best = null;
            foreach (var pair in reserved)
            {
                if (pair.Value < 1 || !_configs.TryGetBodyPart(pair.Key, out var part) || part == null)
                {
                    continue;
                }

                if (part.BodySlot != BodySlot.Arm || part.IsPrimaryHand != 1)
                {
                    continue;
                }

                if (best == null || part.BodyLevel > best.BodyLevel)
                {
                    best = part;
                }
            }

            if (best == null)
            {
                reason = "无主要手，停造";
                Debug.LogWarning($"[AutoManufacture] {reason}");
                return false;
            }

            if (string.IsNullOrWhiteSpace(best.ClassRestrict))
            {
                reason = $"主要手 ClassRestrict 为空（配置错误）BodyPartId={best.BodyPartId}，停造";
                Debug.LogError($"[AutoManufacture] {reason}");
                return false;
            }

            primary = best;
            return true;
        }

        private bool TryPickSecondaryHand(
            Dictionary<string, int> reserved,
            BodyPartConfigRow primary,
            out BodyPartConfigRow secondary,
            out string reason)
        {
            secondary = null;
            reason = null;
            var primaryClasses = ParseClassRestrict(primary.ClassRestrict);
            var approx = new List<BodyPartConfigRow>();

            foreach (var pair in reserved)
            {
                if (pair.Value < 1 || !_configs.TryGetBodyPart(pair.Key, out var part) || part == null)
                {
                    continue;
                }

                if (part.BodySlot != BodySlot.Arm || part.IsPrimaryHand != 0)
                {
                    continue;
                }

                if (!IsApproxBodyLevel(part.BodyLevel, primary.BodyLevel))
                {
                    continue;
                }

                approx.Add(part);
            }

            if (approx.Count == 0)
            {
                reason = "无可用次要手（近似品质），停造";
                Debug.LogWarning($"[AutoManufacture] {reason}");
                return false;
            }

            var overlap = new List<BodyPartConfigRow>();
            for (var i = 0; i < approx.Count; i++)
            {
                if (HasClassOverlap(primaryClasses, ParseClassRestrict(approx[i].ClassRestrict)))
                {
                    overlap.Add(approx[i]);
                }
            }

            var pool = overlap.Count > 0 ? overlap : approx;
            secondary = PickByApproxLevelPreference(pool, primary.BodyLevel);
            if (secondary == null)
            {
                reason = "次要手抽取失败，停造";
                Debug.LogWarning($"[AutoManufacture] {reason}");
                return false;
            }

            return true;
        }

        private bool TryPickRemaining(
            Dictionary<string, int> reserved,
            BodyPartConfigRow primary,
            BodySlot slot,
            out BodyPartConfigRow picked,
            out string reason)
        {
            picked = null;
            reason = null;
            var approx = new List<BodyPartConfigRow>();
            foreach (var pair in reserved)
            {
                if (pair.Value < 1 || !_configs.TryGetBodyPart(pair.Key, out var part) || part == null)
                {
                    continue;
                }

                if (part.BodySlot != slot)
                {
                    continue;
                }

                if (!IsApproxBodyLevel(part.BodyLevel, primary.BodyLevel))
                {
                    continue;
                }

                approx.Add(part);
            }

            if (approx.Count == 0)
            {
                reason = $"无可用 {slot}（近似品质），停造";
                Debug.LogWarning($"[AutoManufacture] {reason}");
                return false;
            }

            var withStat = new List<BodyPartConfigRow>();
            if (primary.HasBodyPrimaryStat)
            {
                for (var i = 0; i < approx.Count; i++)
                {
                    var part = approx[i];
                    if (part.HasBodyPrimaryStat && part.BodyPrimaryStat == primary.BodyPrimaryStat)
                    {
                        withStat.Add(part);
                    }
                }
            }

            var pool = withStat.Count > 0 ? withStat : approx;
            var withRace = new List<BodyPartConfigRow>();
            for (var i = 0; i < pool.Count; i++)
            {
                if (string.Equals(pool[i].RaceId, primary.RaceId, StringComparison.Ordinal))
                {
                    withRace.Add(pool[i]);
                }
            }

            var finalPool = withRace.Count > 0 ? withRace : pool;
            picked = finalPool[_rng.Next(finalPool.Count)];
            Reserve(reserved, picked.BodyPartId);
            return true;
        }

        private bool TryResolveClass(
            BodyPartConfigRow primary,
            BodyPartConfigRow secondary,
            out ClassConfigRow classRow,
            out string reason)
        {
            classRow = null;
            reason = null;
            var primarySet = ParseClassRestrict(primary.ClassRestrict);
            var secondarySet = ParseClassRestrict(secondary.ClassRestrict);
            var intersect = new List<string>();
            for (var i = 0; i < primarySet.Count; i++)
            {
                var id = primarySet[i];
                for (var j = 0; j < secondarySet.Count; j++)
                {
                    if (string.Equals(id, secondarySet[j], StringComparison.Ordinal))
                    {
                        intersect.Add(id);
                        break;
                    }
                }
            }

            var pool = intersect.Count > 0 ? intersect : primarySet;
            if (pool.Count == 0)
            {
                reason = $"职业池为空 Primary={primary.BodyPartId}，停造";
                Debug.LogError($"[AutoManufacture] {reason}");
                return false;
            }

            var classId = pool[_rng.Next(pool.Count)];
            if (!_configs.TryGetClass(classId, out classRow) || classRow == null)
            {
                reason = $"ClassConfig 缺失 ClassId={classId}，停造";
                Debug.LogError($"[AutoManufacture] {reason}");
                return false;
            }

            return true;
        }

        private BodyPartConfigRow PickByApproxLevelPreference(
            List<BodyPartConfigRow> pool,
            float anchorLevel)
        {
            if (pool == null || pool.Count == 0)
            {
                return null;
            }

            // Within approx band: higher → same → lower-by-1; within band prefer max BodyLevel.
            var higher = new List<BodyPartConfigRow>();
            var same = new List<BodyPartConfigRow>();
            var lower = new List<BodyPartConfigRow>();
            for (var i = 0; i < pool.Count; i++)
            {
                var part = pool[i];
                var delta = part.BodyLevel - anchorLevel;
                if (delta > 0.0001f)
                {
                    higher.Add(part);
                }
                else if (delta < -0.0001f)
                {
                    lower.Add(part);
                }
                else
                {
                    same.Add(part);
                }
            }

            List<BodyPartConfigRow> band;
            if (higher.Count > 0)
            {
                band = higher;
            }
            else if (same.Count > 0)
            {
                band = same;
            }
            else
            {
                band = lower;
            }

            var bestLevel = band[0].BodyLevel;
            for (var i = 1; i < band.Count; i++)
            {
                if (band[i].BodyLevel > bestLevel)
                {
                    bestLevel = band[i].BodyLevel;
                }
            }

            var top = new List<BodyPartConfigRow>();
            for (var i = 0; i < band.Count; i++)
            {
                if (Math.Abs(band[i].BodyLevel - bestLevel) <= 0.0001f)
                {
                    top.Add(band[i]);
                }
            }

            return top[_rng.Next(top.Count)];
        }

        /// <summary>
        /// After MagicBook hook: StaticStat / MaxHP / Appearance / WarriorName (SPEC_03 §3.15 §5–§7).
        /// Appearance may rewrite RaceId to Race_Undead when set A is empty.
        /// </summary>
        private void FinalizeDraft(AutoCraftDraft draft, ClassConfigRow classRow, RaceConfigRow raceRow)
        {
            var avgLevelInt = ComputeAvgLevelInt(draft.BodyLevels);
            var className = !string.IsNullOrEmpty(draft.ClassName)
                ? draft.ClassName
                : (classRow != null ? classRow.ClassName : string.Empty);
            var defaultAppearanceId = classRow != null ? classRow.DefaultAppearanceId : null;
            var raceId = draft.RaceId;
            var raceAdjust = draft.RaceAdjustCoeff;
            draft.AppearanceId = PickAppearanceMode2(
                avgLevelInt,
                ref raceId,
                ref raceRow,
                ref raceAdjust,
                className,
                defaultAppearanceId);
            draft.RaceId = raceId;
            draft.RaceAdjustCoeff = raceAdjust;

            var staticStats = WarriorStatMath.ComputeStaticStats(
                draft.BaseStats, draft.EquipStats, draft.GemMult, draft.RaceAdjustCoeff);
            draft.BodyLife = WarriorStatMath.ComputeBodyLife(draft.BaseStats, draft.EquipStats);
            draft.MaxHP = WarriorStatMath.ComputeMaxHP(draft.BodyLife, staticStats.Strength);
            draft.WarriorName = BuildWarriorName(raceRow, draft.RaceId, className);
        }

        private WarriorInstance BuildWarriorInstance(AutoCraftDraft draft)
        {
            var instance = new WarriorInstance
            {
                Id = _warriorPool.ReserveNextId(),
                WarriorName = draft.WarriorName ?? string.Empty,
                RemainingHP = draft.MaxHP,
                RaceId = draft.RaceId,
                RaceAdjustCoeff = draft.RaceAdjustCoeff,
                BaseStats = draft.BaseStats,
                AppearanceId = draft.AppearanceId,
                SoulId = draft.SoulId,
                ClassId = draft.ClassId,
                AttackMode = draft.AttackMode,
                GemMult = draft.GemMult,
                ControlPowerCost = 0f,
                EquipStats = draft.EquipStats,
                BodyLife = draft.BodyLife,
                SourceSpiritCost = 0f
            };
            instance.SourceItemIds.AddRange(draft.ConsumedBodyPartIds);
            return instance;
        }

        private static string BuildWarriorName(RaceConfigRow raceRow, string raceId, string className)
        {
            return ResolveRaceDisplayName(raceRow, raceId) + (className ?? string.Empty);
        }

        private static string ResolveRaceDisplayName(RaceConfigRow raceRow, string raceId)
        {
            if (raceRow == null)
            {
                return raceId ?? string.Empty;
            }

            return string.IsNullOrEmpty(raceRow.DisplayNameKey) ? raceRow.RaceId : raceRow.DisplayNameKey;
        }

        private static int ComputeAvgLevelInt(List<float> bodyLevels)
        {
            if (bodyLevels == null || bodyLevels.Count == 0)
            {
                return 0;
            }

            var sum = 0d;
            for (var i = 0; i < bodyLevels.Count; i++)
            {
                sum += bodyLevels[i];
            }

            var oneDecimal = Math.Round(sum / bodyLevels.Count, 1, MidpointRounding.AwayFromZero);
            return (int)Math.Round(oneDecimal, 0, MidpointRounding.AwayFromZero);
        }

        /// <summary>
        /// Mode2 appearance: A empty → Race_Undead once;
        /// B empty or A still empty after rewrite → DefaultAppearanceId → IsFallback → table.
        /// </summary>
        private string PickAppearanceMode2(
            int avgLevelInt,
            ref string raceId,
            ref RaceConfigRow raceRow,
            ref StatBlock raceAdjust,
            string className,
            string defaultAppearanceId)
        {
            return PickAppearanceMode2Core(
                avgLevelInt,
                ref raceId,
                ref raceRow,
                ref raceAdjust,
                className,
                defaultAppearanceId,
                allowUndeadRewrite: true);
        }

        private string PickAppearanceMode2Core(
            int avgLevelInt,
            ref string raceId,
            ref RaceConfigRow raceRow,
            ref StatBlock raceAdjust,
            string className,
            string defaultAppearanceId,
            bool allowUndeadRewrite)
        {
            var all = _configs.BodyAppearances;
            if (all == null || all.Count == 0)
            {
                return string.IsNullOrEmpty(defaultAppearanceId) ? null : defaultAppearanceId;
            }

            if (string.IsNullOrEmpty(raceId))
            {
                if (!string.IsNullOrWhiteSpace(defaultAppearanceId))
                {
                    return defaultAppearanceId.Trim();
                }

                return all[_rng.Next(all.Count)].AppearanceId;
            }

            var setA = new List<BodyAppearanceConfigRow>();
            for (var i = 0; i < all.Count; i++)
            {
                var row = all[i];
                if (row.AppearanceLevel == avgLevelInt
                    && string.Equals(row.RaceId, raceId, StringComparison.Ordinal))
                {
                    setA.Add(row);
                }
            }

            if (setA.Count == 0)
            {
                if (allowUndeadRewrite
                    && !string.Equals(raceId, RaceResolve.UndeadRaceId, StringComparison.Ordinal))
                {
                    raceId = RaceResolve.UndeadRaceId;
                    _configs.TryGetRace(raceId, out raceRow);
                    raceAdjust = raceRow != null ? raceRow.RaceAdjustCoeff : default;
                    return PickAppearanceMode2Core(
                        avgLevelInt,
                        ref raceId,
                        ref raceRow,
                        ref raceAdjust,
                        className,
                        defaultAppearanceId,
                        allowUndeadRewrite: false);
                }

                // Undead rewrite done or started as Undead with empty A:
                // DefaultAppearanceId before IsFallback (SPEC_03 §3.15).
                return PickDefaultThenFallbackOrTable(all, raceId, defaultAppearanceId);
            }

            var setB = new List<BodyAppearanceConfigRow>();
            for (var i = 0; i < setA.Count; i++)
            {
                if (HasClassAffinity(setA[i].ClassAffinity, className))
                {
                    setB.Add(setA[i]);
                }
            }

            if (setB.Count > 0)
            {
                return setB[_rng.Next(setB.Count)].AppearanceId;
            }

            // A non-empty, B empty: DefaultAppearanceId then IsFallback (SPEC_03 §3.15).
            return PickDefaultThenFallbackOrTable(all, raceId, defaultAppearanceId);
        }

        private string PickDefaultThenFallbackOrTable(
            IReadOnlyList<BodyAppearanceConfigRow> all,
            string raceId,
            string defaultAppearanceId)
        {
            if (!string.IsNullOrWhiteSpace(defaultAppearanceId))
            {
                return defaultAppearanceId.Trim();
            }

            var fallbackId = TryPickRaceFallback(all, raceId);
            if (!string.IsNullOrEmpty(fallbackId))
            {
                return fallbackId;
            }

            return all[_rng.Next(all.Count)].AppearanceId;
        }

        private static string TryPickRaceFallback(IReadOnlyList<BodyAppearanceConfigRow> all, string raceId)
        {
            if (string.IsNullOrEmpty(raceId) || all == null)
            {
                return null;
            }

            for (var i = 0; i < all.Count; i++)
            {
                var row = all[i];
                if (row.IsFallback && string.Equals(row.RaceId, raceId, StringComparison.Ordinal))
                {
                    return row.AppearanceId;
                }
            }

            return null;
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

        private Dictionary<string, int> CloneStock()
        {
            var map = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var pair in _warehouse.Materials)
            {
                if (pair.Value > 0)
                {
                    map[pair.Key] = pair.Value;
                }
            }

            return map;
        }

        private static void Reserve(Dictionary<string, int> reserved, string bodyPartId)
        {
            if (!reserved.TryGetValue(bodyPartId, out var count) || count < 1)
            {
                return;
            }

            if (count == 1)
            {
                reserved.Remove(bodyPartId);
            }
            else
            {
                reserved[bodyPartId] = count - 1;
            }
        }

        private static bool IsApproxBodyLevel(float candidate, float anchor)
        {
            return Math.Abs(candidate - anchor) <= 1.0001f;
        }

        private static List<string> ParseClassRestrict(string text)
        {
            var list = new List<string>();
            if (string.IsNullOrWhiteSpace(text))
            {
                return list;
            }

            var parts = text.Split('|');
            for (var i = 0; i < parts.Length; i++)
            {
                var id = parts[i] != null ? parts[i].Trim() : null;
                if (string.IsNullOrEmpty(id))
                {
                    continue;
                }

                var dup = false;
                for (var j = 0; j < list.Count; j++)
                {
                    if (string.Equals(list[j], id, StringComparison.Ordinal))
                    {
                        dup = true;
                        break;
                    }
                }

                if (!dup)
                {
                    list.Add(id);
                }
            }

            return list;
        }

        private static bool HasClassOverlap(List<string> a, List<string> b)
        {
            if (a == null || b == null || a.Count == 0 || b.Count == 0)
            {
                return false;
            }

            for (var i = 0; i < a.Count; i++)
            {
                for (var j = 0; j < b.Count; j++)
                {
                    if (string.Equals(a[i], b[j], StringComparison.Ordinal))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
