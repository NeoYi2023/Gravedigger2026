using System;
using System.Collections.Generic;
using System.Globalization;
using Gravedigger2026.Core.Config;
using Gravedigger2026.Core.UpgradeManufacture;
using UnityEngine;

namespace Gravedigger2026.Core.AutoManufacture
{
    /// <summary>
    /// MagicBook EffectPhase=SoldierManufacture (SPEC_03 §3.15 / SPEC_04 §9.24).
    /// Applied per equipped slot at UI-016 Step2 pulse peak (one book at a time).
    /// </summary>
    public interface ISoldierManufactureMagicBookHook
    {
        /// <summary>
        /// Apply only the book in <paramref name="slotIndex"/> to the warrior (empty / wrong phase → no-op).
        /// Caller must Refinalize + persist after.
        /// </summary>
        void ApplyEquippedBookAtSlot(WarriorInstance warrior, int slotIndex);

        /// <summary>
        /// Instant left→right apply from <paramref name="fromSlotInclusive"/> through slot 5 (fail/Exit fallback).
        /// </summary>
        void ApplyRemainingSlots(WarriorInstance warrior, int fromSlotInclusive);
    }

    public sealed class NoOpSoldierManufactureMagicBookHook : ISoldierManufactureMagicBookHook
    {
        public static readonly NoOpSoldierManufactureMagicBookHook Instance =
            new NoOpSoldierManufactureMagicBookHook();

        public void ApplyEquippedBookAtSlot(WarriorInstance warrior, int slotIndex)
        {
        }

        public void ApplyRemainingSlots(WarriorInstance warrior, int fromSlotInclusive)
        {
        }
    }

    /// <summary>
    /// Reads SpecialEquipSlots + MagicBookConfig; applies one slot token to a pool WarriorInstance.
    /// </summary>
    public sealed class SoldierManufactureMagicBookHook : ISoldierManufactureMagicBookHook
    {
        public const string PhaseSoldierManufacture = "SoldierManufacture";
        public const string PayloadStatMul = "StatMul";
        public const string PayloadForceClass = "ForceClass";
        public const string PayloadSoldierSkillLevelAdd = "SoldierSkillLevelAdd";
        public const string WarriorEnhanceBookId = "MagicBook_WarriorEnhance";
        public const string StatPrimary = "Primary";
        public const string StatAll = "All";

        private static readonly string[] StatMulAllowedKeys = { "Stat", "Mul", "ClassId" };
        private static readonly string[] ForceClassAllowedKeys = { "ClassId", "RequireClassId", "Chance" };
        private static readonly string[] SkillLevelAddAllowedKeys = { "SkillId", "Delta" };

        private readonly SpecialEquipSlotsService _slots;
        private readonly ConfigCsvRepository _configs;
        private readonly System.Random _rng;

        public SoldierManufactureMagicBookHook(SpecialEquipSlotsService slots, ConfigCsvRepository configs)
        {
            _slots = slots ?? throw new ArgumentNullException(nameof(slots));
            _configs = configs ?? throw new ArgumentNullException(nameof(configs));
            _rng = new System.Random(Environment.TickCount);
        }

        public void ApplyEquippedBookAtSlot(WarriorInstance warrior, int slotIndex)
        {
            if (warrior == null
                || slotIndex < 0
                || slotIndex >= SpecialEquipSlotsService.SlotCount)
            {
                return;
            }

            var magicBookId = _slots.GetSlot(slotIndex);
            if (string.IsNullOrEmpty(magicBookId))
            {
                return;
            }

            if (!_configs.TryGetMagicBook(magicBookId, out var row) || row == null)
            {
                Debug.LogWarning(
                    $"[MagicBook] ApplyAtSlot missing config book={magicBookId} slot={slotIndex} " +
                    $"warrior={warrior.Id}");
                return;
            }

            if (!EffectPhaseContains(row.EffectPhase, PhaseSoldierManufacture))
            {
                return;
            }

            var payload = (row.EffectPayload ?? string.Empty).Trim();
            if (string.Equals(payload, RaceResolve.RaceWeightPickPayload, StringComparison.Ordinal))
            {
                ApplyRaceWeightPick(warrior, magicBookId, row);
                return;
            }

            if (string.Equals(payload, PayloadStatMul, StringComparison.Ordinal))
            {
                ApplyStatMul(warrior, magicBookId, row);
                return;
            }

            if (string.Equals(payload, PayloadForceClass, StringComparison.Ordinal))
            {
                ApplyForceClass(warrior, magicBookId, row);
                return;
            }

            if (string.Equals(payload, PayloadSoldierSkillLevelAdd, StringComparison.Ordinal))
            {
                ApplySoldierSkillLevelAdd(warrior, magicBookId, row);
                return;
            }

            Debug.Log(
                $"[MagicBook] SoldierManufacture empty hook book={magicBookId} slot={slotIndex} " +
                $"warrior={warrior.Id} payload='{payload}'");
        }

        public void ApplyRemainingSlots(WarriorInstance warrior, int fromSlotInclusive)
        {
            if (warrior == null)
            {
                return;
            }

            var start = fromSlotInclusive < 0 ? 0 : fromSlotInclusive;
            for (var i = start; i < SpecialEquipSlotsService.SlotCount; i++)
            {
                ApplyEquippedBookAtSlot(warrior, i);
            }
        }

        public static bool EffectPhaseContains(string effectPhase, string phase)
        {
            if (string.IsNullOrEmpty(effectPhase) || string.IsNullOrEmpty(phase))
            {
                return false;
            }

            var parts = effectPhase.Split('|');
            for (var i = 0; i < parts.Length; i++)
            {
                var p = parts[i]?.Trim();
                if (string.Equals(p, phase, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private void ApplyRaceWeightPick(WarriorInstance warrior, string magicBookId, MagicBookConfigRow row)
        {
            var candidates = CollectSourceRaceIds(warrior);
            if (candidates.Count == 0)
            {
                Debug.LogWarning(
                    $"[MagicBook] RaceWeightPick no SourceItemIds book={magicBookId} warrior={warrior.Id}");
                return;
            }

            var raceId = RaceResolve.PickWeighted(candidates, _rng);
            if (string.IsNullOrEmpty(raceId))
            {
                return;
            }

            warrior.RaceId = raceId;
            if (_configs.TryGetRace(raceId, out var raceRow) && raceRow != null)
            {
                warrior.RaceAdjustCoeff = raceRow.RaceAdjustCoeff;
            }
            else
            {
                warrior.RaceAdjustCoeff = default;
            }

            WarriorVisualStyleBake.TryApply(warrior, row, _configs);
            Debug.Log(
                $"[MagicBook] SoldierManufacture Restore (RaceWeightPick) book={magicBookId} " +
                $"warrior={warrior.Id} Race={raceId} VisualStyle={warrior.VisualStyleId ?? ""} " +
                $"VisualModelScale={WarriorVisualModelScale.Resolve(warrior):0.###}");
        }

        private void ApplySoldierSkillLevelAdd(WarriorInstance warrior, string magicBookId, MagicBookConfigRow row)
        {
            var map = MagicBookEffectParams.Parse(row.EffectParams, SkillLevelAddAllowedKeys);
            if (!MagicBookEffectParams.TryGet(map, "SkillId", out var skillId)
                || !MagicBookEffectParams.TryGet(map, "Delta", out var deltaText))
            {
                Debug.LogWarning(
                    $"[MagicBook] SoldierSkillLevelAdd invalid (need SkillId+Delta) " +
                    $"book={magicBookId} warrior={warrior.Id}");
                return;
            }

            if (!int.TryParse(deltaText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var delta))
            {
                Debug.LogWarning(
                    $"[MagicBook] SoldierSkillLevelAdd invalid Delta='{deltaText}' " +
                    $"book={magicBookId} warrior={warrior.Id}");
                return;
            }

            var entry = FindSkill(warrior.SoldierSkills, skillId);
            if (entry == null)
            {
                Debug.Log(
                    $"[MagicBook] SoldierSkillLevelAdd skip (no such skill) book={magicBookId} " +
                    $"warrior={warrior.Id} skill={skillId} class={warrior.ClassId}");
                return;
            }

            if (!_configs.TryGetSkillLevelRange(skillId, out var minLevel, out var maxLevel))
            {
                Debug.LogWarning(
                    $"[MagicBook] SoldierSkillLevelAdd skip (no SkillConfig range) " +
                    $"book={magicBookId} warrior={warrior.Id} skill={skillId}");
                return;
            }

            var before = entry.SkillLevel;
            var next = before + delta;
            if (next < minLevel)
            {
                next = minLevel;
            }

            if (next > maxLevel)
            {
                next = maxLevel;
            }

            entry.SkillLevel = next;
            WarriorVisualStyleBake.TryApply(warrior, row, _configs);
            Debug.Log(
                $"[MagicBook] SoldierSkillLevelAdd skill={skillId} {before}{delta:+0;-0;+0}→{next} " +
                $"(clamp {minLevel}..{maxLevel}) book={magicBookId} warrior={warrior.Id} class={warrior.ClassId}");
        }

        private static SoldierSkillEntry FindSkill(List<SoldierSkillEntry> skills, string skillId)
        {
            if (skills == null || string.IsNullOrEmpty(skillId))
            {
                return null;
            }

            for (var i = 0; i < skills.Count; i++)
            {
                var entry = skills[i];
                if (entry != null && string.Equals(entry.SkillId, skillId, StringComparison.Ordinal))
                {
                    return entry;
                }
            }

            return null;
        }

        private void ApplyForceClass(WarriorInstance warrior, string magicBookId, MagicBookConfigRow row)
        {
            var map = MagicBookEffectParams.Parse(row.EffectParams, ForceClassAllowedKeys);
            if (!MagicBookEffectParams.TryGet(map, "ClassId", out var targetClassId))
            {
                Debug.LogWarning(
                    $"[MagicBook] ForceClass invalid (need ClassId) book={magicBookId} warrior={warrior.Id}");
                return;
            }

            if (!_configs.TryGetClass(targetClassId, out var targetRow) || targetRow == null)
            {
                Debug.LogWarning(
                    $"[MagicBook] ForceClass invalid ClassId='{targetClassId}' " +
                    $"book={magicBookId} warrior={warrior.Id}");
                return;
            }

            if (MagicBookEffectParams.TryGet(map, "RequireClassId", out var requireClassId))
            {
                if (!_configs.TryGetClass(requireClassId, out _))
                {
                    Debug.LogWarning(
                        $"[MagicBook] ForceClass invalid RequireClassId='{requireClassId}' " +
                        $"book={magicBookId} warrior={warrior.Id}");
                    return;
                }

                if (!string.Equals(warrior.ClassId, requireClassId, StringComparison.Ordinal))
                {
                    Debug.Log(
                        $"[MagicBook] ForceClass skip class mismatch book={magicBookId} " +
                        $"warrior={warrior.Id} class={warrior.ClassId} require={requireClassId}");
                    return;
                }
            }

            var chance = 1f;
            if (MagicBookEffectParams.TryGet(map, "Chance", out var chanceText))
            {
                if (!float.TryParse(chanceText, NumberStyles.Float, CultureInfo.InvariantCulture, out chance)
                    || chance < 0f
                    || chance > 1f)
                {
                    Debug.LogWarning(
                        $"[MagicBook] ForceClass invalid Chance='{chanceText}' " +
                        $"book={magicBookId} warrior={warrior.Id}");
                    return;
                }
            }

            var roll = chance >= 1f ? 0f : UnityEngine.Random.value;
            var hit = chance >= 1f || (chance > 0f && roll < chance);
            if (!hit)
            {
                Debug.Log(
                    $"[MagicBook] ForceClass miss roll={roll:0.###} chance={chance} " +
                    $"book={magicBookId} warrior={warrior.Id} class={warrior.ClassId} " +
                    $"target={targetClassId}");
                return;
            }

            var fromClassId = warrior.ClassId;
            warrior.ClassId = targetRow.ClassId;
            warrior.AttackMode = targetRow.AttackMode;
            SoldierSkillGrant.GrantDefaultSkillsAtLevel1(warrior, _configs);
            WarriorVisualStyleBake.TryApply(warrior, row, _configs);
            Debug.Log(
                $"[MagicBook] ForceClass hit roll={roll:0.###} chance={chance} " +
                $"book={magicBookId} warrior={warrior.Id} {fromClassId}→{warrior.ClassId} " +
                $"AttackMode={warrior.AttackMode} Skills={SoldierSkillGrant.FormatSummary(warrior.SoldierSkills)}");
        }

        private void ApplyStatMul(WarriorInstance warrior, string magicBookId, MagicBookConfigRow row)
        {
            var map = MagicBookEffectParams.Parse(row.EffectParams, StatMulAllowedKeys);
            if (!MagicBookEffectParams.TryGet(map, "Stat", out var statText)
                || !MagicBookEffectParams.TryGet(map, "Mul", out var mulText))
            {
                Debug.LogWarning(
                    $"[MagicBook] StatMul invalid (need Stat+Mul) book={magicBookId} warrior={warrior.Id}");
                return;
            }

            if (!float.TryParse(mulText, NumberStyles.Float, CultureInfo.InvariantCulture, out var mul)
                || mul < 0f)
            {
                Debug.LogWarning(
                    $"[MagicBook] StatMul invalid Mul='{mulText}' book={magicBookId} warrior={warrior.Id}");
                return;
            }

            if (MagicBookEffectParams.TryGet(map, "ClassId", out var classFilter)
                && !MatchesClassIdFilter(classFilter, warrior.ClassId, magicBookId, warrior.Id))
            {
                return;
            }

            if (string.Equals(statText, StatPrimary, StringComparison.Ordinal))
            {
                ApplyPrimaryStatMul(warrior, magicBookId, mul, row);
                return;
            }

            if (string.Equals(statText, StatAll, StringComparison.Ordinal))
            {
                var block = warrior.BaseStats;
                ScaleAll(ref block, mul);
                warrior.BaseStats = block;
                WarriorVisualStyleBake.TryApply(warrior, row, _configs);
                Debug.Log(
                    $"[MagicBook] StatMul All *={mul} book={magicBookId} warrior={warrior.Id}");
                return;
            }

            if (!Enum.TryParse(statText, false, out StatKind kind)
                || !IsFiveDim(kind))
            {
                Debug.LogWarning(
                    $"[MagicBook] StatMul invalid Stat='{statText}' book={magicBookId} warrior={warrior.Id}");
                return;
            }

            var stats = warrior.BaseStats;
            stats.Set(kind, stats.Get(kind) * mul);
            warrior.BaseStats = stats;
            WarriorVisualStyleBake.TryApply(warrior, row, _configs);
            Debug.Log(
                $"[MagicBook] StatMul {kind} *={mul} book={magicBookId} warrior={warrior.Id} " +
                $"Base={stats.Get(kind)}");
        }

        private void ApplyPrimaryStatMul(
            WarriorInstance warrior,
            string magicBookId,
            float mul,
            MagicBookConfigRow row)
        {
            if (!_configs.TryGetClass(warrior.ClassId, out var classRow) || classRow == null)
            {
                Debug.LogWarning(
                    $"[MagicBook] StatMul Primary missing ClassId='{warrior.ClassId}' " +
                    $"book={magicBookId} warrior={warrior.Id}");
                return;
            }

            var kind = classRow.PrimaryStat;
            var bodySum = SumConsumedBodyStat(warrior, kind);
            var delta = (mul - 1f) * bodySum;
            var stats = warrior.BaseStats;
            stats.Add(kind, delta);
            warrior.BaseStats = stats;
            WarriorVisualStyleBake.TryApply(warrior, row, _configs);
            Debug.Log(
                $"[MagicBook] StatMul Primary {kind} +=({mul}-1)*{bodySum}={delta} " +
                $"book={magicBookId} warrior={warrior.Id} class={warrior.ClassId} Base={stats.Get(kind)} " +
                $"VisualStyle={warrior.VisualStyleId ?? ""} " +
                $"VisualModelScale={WarriorVisualModelScale.Resolve(warrior):0.###}");
        }

        /// <summary>
        /// Comma-separated ClassId OR list. Any illegal id → book invalid (false).
        /// No match → skip (false, already logged).
        /// </summary>
        private bool MatchesClassIdFilter(string classFilter, string warriorClassId, string magicBookId, string warriorId)
        {
            var matched = false;
            var any = false;
            var parts = classFilter.Split(',');
            for (var i = 0; i < parts.Length; i++)
            {
                var id = parts[i] != null ? parts[i].Trim() : string.Empty;
                if (id.Length == 0)
                {
                    continue;
                }

                if (!_configs.TryGetClass(id, out _))
                {
                    Debug.LogWarning(
                        $"[MagicBook] StatMul invalid ClassId='{id}' book={magicBookId} warrior={warriorId}");
                    return false;
                }

                any = true;
                if (string.Equals(warriorClassId, id, StringComparison.Ordinal))
                {
                    matched = true;
                }
            }

            if (!any)
            {
                Debug.LogWarning(
                    $"[MagicBook] StatMul invalid ClassId='{classFilter}' book={magicBookId} warrior={warriorId}");
                return false;
            }

            if (!matched)
            {
                Debug.Log(
                    $"[MagicBook] StatMul skip class mismatch book={magicBookId} " +
                    $"warrior={warriorId} class={warriorClassId} filter={classFilter}");
                return false;
            }

            return true;
        }

        private float SumConsumedBodyStat(WarriorInstance warrior, StatKind kind)
        {
            var sum = 0f;
            var ids = warrior.SourceItemIds;
            if (ids == null)
            {
                return 0f;
            }

            for (var i = 0; i < ids.Count; i++)
            {
                if (!_configs.TryGetBodyPart(ids[i], out var part) || part == null)
                {
                    continue;
                }

                sum += part.StatBonus.Get(kind);
            }

            return sum;
        }

        private List<string> CollectSourceRaceIds(WarriorInstance warrior)
        {
            var list = new List<string>(6);
            var ids = warrior.SourceItemIds;
            if (ids == null)
            {
                return list;
            }

            for (var i = 0; i < ids.Count; i++)
            {
                if (!_configs.TryGetBodyPart(ids[i], out var part) || part == null)
                {
                    continue;
                }

                if (!string.IsNullOrEmpty(part.RaceId))
                {
                    list.Add(part.RaceId);
                }
            }

            return list;
        }

        private static void ScaleAll(ref StatBlock block, float mul)
        {
            block.MaxHP *= mul;
            block.MoveSpeed *= mul;
            block.Strength *= mul;
            block.Agility *= mul;
            block.Intelligence *= mul;
        }

        private static bool IsFiveDim(StatKind kind)
        {
            return kind >= StatKind.MaxHP && kind <= StatKind.Intelligence;
        }
    }
}
