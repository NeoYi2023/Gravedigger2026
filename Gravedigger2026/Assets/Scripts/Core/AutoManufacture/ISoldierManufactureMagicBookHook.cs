using System;
using System.Globalization;
using Gravedigger2026.Core.Config;
using Gravedigger2026.Core.UpgradeManufacture;
using UnityEngine;

namespace Gravedigger2026.Core.AutoManufacture
{
    /// <summary>
    /// MagicBook EffectPhase=SoldierManufacture hook (SPEC_03 §3.15 / SPEC_04 §9.24).
    /// RaceWeightPick (Restore) is probed via <see cref="HasRaceWeightPick"/> before race finalize;
    /// StatMul is applied in-hook; other payloads remain empty apply + log.
    /// </summary>
    public interface ISoldierManufactureMagicBookHook
    {
        void ApplySoldierManufactureEffects(AutoCraftDraft draft);

        /// <summary>
        /// True when an equipped SoldierManufacture book has EffectPayload=RaceWeightPick.
        /// </summary>
        bool HasRaceWeightPick();
    }

    public sealed class NoOpSoldierManufactureMagicBookHook : ISoldierManufactureMagicBookHook
    {
        public static readonly NoOpSoldierManufactureMagicBookHook Instance =
            new NoOpSoldierManufactureMagicBookHook();

        public void ApplySoldierManufactureEffects(AutoCraftDraft draft)
        {
        }

        public bool HasRaceWeightPick()
        {
            return false;
        }
    }

    /// <summary>
    /// Reads SpecialEquipSlots + MagicBookConfig; probes RaceWeightPick; applies StatMul.
    /// </summary>
    public sealed class SoldierManufactureMagicBookHook : ISoldierManufactureMagicBookHook
    {
        public const string PhaseSoldierManufacture = "SoldierManufacture";
        public const string PayloadStatMul = "StatMul";
        public const string WarriorEnhanceBookId = "MagicBook_WarriorEnhance";
        public const string StatPrimary = "Primary";
        public const string StatAll = "All";

        private static readonly string[] StatMulAllowedKeys = { "Stat", "Mul", "ClassId" };

        private readonly SpecialEquipSlotsService _slots;
        private readonly ConfigCsvRepository _configs;

        public SoldierManufactureMagicBookHook(SpecialEquipSlotsService slots, ConfigCsvRepository configs)
        {
            _slots = slots ?? throw new ArgumentNullException(nameof(slots));
            _configs = configs ?? throw new ArgumentNullException(nameof(configs));
        }

        public bool HasRaceWeightPick()
        {
            var found = false;
            _slots.ForEachEquipped((_, magicBookId) =>
            {
                if (found)
                {
                    return;
                }

                if (!_configs.TryGetMagicBook(magicBookId, out var row) || row == null)
                {
                    return;
                }

                if (!EffectPhaseContains(row.EffectPhase, PhaseSoldierManufacture))
                {
                    return;
                }

                if (string.Equals(
                        row.EffectPayload?.Trim(),
                        RaceResolve.RaceWeightPickPayload,
                        StringComparison.Ordinal))
                {
                    found = true;
                }
            });
            return found;
        }

        public void ApplySoldierManufactureEffects(AutoCraftDraft draft)
        {
            if (draft == null)
            {
                return;
            }

            var applied = 0;
            _slots.ForEachEquipped((_, magicBookId) =>
            {
                if (!_configs.TryGetMagicBook(magicBookId, out var row) || row == null)
                {
                    return;
                }

                if (!EffectPhaseContains(row.EffectPhase, PhaseSoldierManufacture))
                {
                    return;
                }

                applied++;
                var payload = (row.EffectPayload ?? string.Empty).Trim();
                if (string.Equals(payload, RaceResolve.RaceWeightPickPayload, StringComparison.Ordinal))
                {
                    Debug.Log(
                        $"[MagicBook] SoldierManufacture Restore (RaceWeightPick) book={magicBookId} " +
                        $"draft={draft.TempId} (race mode already applied at finalize)");
                    return;
                }

                if (string.Equals(payload, PayloadStatMul, StringComparison.Ordinal))
                {
                    ApplyStatMul(draft, magicBookId, row);
                    return;
                }

                Debug.Log(
                    $"[MagicBook] SoldierManufacture empty hook book={magicBookId} draft={draft.TempId} " +
                    $"payload='{payload}'");
            });

            if (applied == 0)
            {
                // No equipped books with this phase — skip silently (avoid craft-loop spam).
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

        private void ApplyStatMul(AutoCraftDraft draft, string magicBookId, MagicBookConfigRow row)
        {
            var map = MagicBookEffectParams.Parse(row.EffectParams, StatMulAllowedKeys);
            if (!MagicBookEffectParams.TryGet(map, "Stat", out var statText)
                || !MagicBookEffectParams.TryGet(map, "Mul", out var mulText))
            {
                Debug.LogWarning(
                    $"[MagicBook] StatMul invalid (need Stat+Mul) book={magicBookId} draft={draft.TempId}");
                return;
            }

            if (!float.TryParse(mulText, NumberStyles.Float, CultureInfo.InvariantCulture, out var mul)
                || mul < 0f)
            {
                Debug.LogWarning(
                    $"[MagicBook] StatMul invalid Mul='{mulText}' book={magicBookId} draft={draft.TempId}");
                return;
            }

            if (MagicBookEffectParams.TryGet(map, "ClassId", out var classFilter))
            {
                if (!_configs.TryGetClass(classFilter, out _))
                {
                    Debug.LogWarning(
                        $"[MagicBook] StatMul invalid ClassId='{classFilter}' book={magicBookId} draft={draft.TempId}");
                    return;
                }

                if (!string.Equals(draft.ClassId, classFilter, StringComparison.Ordinal))
                {
                    Debug.Log(
                        $"[MagicBook] StatMul skip class mismatch book={magicBookId} " +
                        $"draft={draft.TempId} class={draft.ClassId} filter={classFilter}");
                    return;
                }
            }

            if (string.Equals(statText, StatPrimary, StringComparison.Ordinal))
            {
                ApplyPrimaryStatMul(draft, magicBookId, mul);
                return;
            }

            if (string.Equals(statText, StatAll, StringComparison.Ordinal))
            {
                var block = draft.BaseStats;
                ScaleAll(ref block, mul);
                draft.BaseStats = block;
                Debug.Log(
                    $"[MagicBook] StatMul All *={mul} book={magicBookId} draft={draft.TempId}");
                return;
            }

            if (!Enum.TryParse(statText, false, out StatKind kind)
                || !IsFiveDim(kind))
            {
                Debug.LogWarning(
                    $"[MagicBook] StatMul invalid Stat='{statText}' book={magicBookId} draft={draft.TempId}");
                return;
            }

            var stats = draft.BaseStats;
            stats.Set(kind, stats.Get(kind) * mul);
            draft.BaseStats = stats;
            Debug.Log(
                $"[MagicBook] StatMul {kind} *={mul} book={magicBookId} draft={draft.TempId} " +
                $"Base={stats.Get(kind)}");
        }

        private void ApplyPrimaryStatMul(AutoCraftDraft draft, string magicBookId, float mul)
        {
            if (!_configs.TryGetClass(draft.ClassId, out var classRow) || classRow == null)
            {
                Debug.LogWarning(
                    $"[MagicBook] StatMul Primary missing ClassId='{draft.ClassId}' " +
                    $"book={magicBookId} draft={draft.TempId}");
                return;
            }

            var kind = classRow.PrimaryStat;
            var bodySum = SumConsumedBodyStat(draft, kind);
            var delta = (mul - 1f) * bodySum;
            var stats = draft.BaseStats;
            stats.Add(kind, delta);
            draft.BaseStats = stats;
            Debug.Log(
                $"[MagicBook] StatMul Primary {kind} +=({mul}-1)*{bodySum}={delta} " +
                $"book={magicBookId} draft={draft.TempId} class={draft.ClassId} Base={stats.Get(kind)}");
        }

        private float SumConsumedBodyStat(AutoCraftDraft draft, StatKind kind)
        {
            var sum = 0f;
            var ids = draft.ConsumedBodyPartIds;
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
