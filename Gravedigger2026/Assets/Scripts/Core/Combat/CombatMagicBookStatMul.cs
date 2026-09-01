using System;
using System.Globalization;
using Gravedigger2026.Core.AutoManufacture;
using Gravedigger2026.Core.Config;
using UnityEngine;

namespace Gravedigger2026.Core.Combat
{
    /// <summary>
    /// Aggregates protagonist equipped Combat-phase StatMul MagicBooks (SPEC_03 §3.15 4b / SPEC_04 §9.24).
    /// Applied at StartBattle warrior registration; does not mutate WarriorInstance.BaseStats.
    /// </summary>
    public readonly struct CombatStatMulBuff
    {
        public static readonly CombatStatMulBuff Identity = new CombatStatMulBuff(1f, 1f, 1f, 1f);

        public CombatStatMulBuff(
            float maxHpBodyLifeMul,
            float strengthMul,
            float agilityMul,
            float intelligenceMul)
        {
            MaxHpBodyLifeMul = maxHpBodyLifeMul;
            StrengthMul = strengthMul;
            AgilityMul = agilityMul;
            IntelligenceMul = intelligenceMul;
        }

        public float MaxHpBodyLifeMul { get; }
        public float StrengthMul { get; }
        public float AgilityMul { get; }
        public float IntelligenceMul { get; }

        public bool IsIdentity =>
            MaxHpBodyLifeMul == 1f
            && StrengthMul == 1f
            && AgilityMul == 1f
            && IntelligenceMul == 1f;

        public void ApplyToBattleStats(ref StatBlock battleStats)
        {
            if (StrengthMul != 1f)
            {
                battleStats.Strength *= StrengthMul;
            }

            if (AgilityMul != 1f)
            {
                battleStats.Agility *= AgilityMul;
            }

            if (IntelligenceMul != 1f)
            {
                battleStats.Intelligence *= IntelligenceMul;
            }
        }

        public float ApplyToBodyLife(float bodyLife)
        {
            return bodyLife * MaxHpBodyLifeMul;
        }

        public override string ToString()
        {
            return $"HP×{MaxHpBodyLifeMul:0.###} Str×{StrengthMul:0.###} Agi×{AgilityMul:0.###} Int×{IntelligenceMul:0.###}";
        }
    }

    public static class CombatMagicBookStatMul
    {
        public const string PhaseCombat = "Combat";
        public const string PayloadStatMul = "StatMul";

        private static readonly string[] StatMulAllowedKeys = { "Stat", "Mul" };

        public static CombatStatMulBuff Aggregate(
            SpecialEquipSlotsService slots,
            ConfigCsvRepository configs)
        {
            if (slots == null || configs == null)
            {
                return CombatStatMulBuff.Identity;
            }

            var maxHpMul = 1f;
            var strengthMul = 1f;
            var agilityMul = 1f;
            var intelligenceMul = 1f;

            for (var i = 0; i < SpecialEquipSlotsService.SlotCount; i++)
            {
                var magicBookId = slots.GetSlot(i);
                if (string.IsNullOrEmpty(magicBookId))
                {
                    continue;
                }

                if (!configs.TryGetMagicBook(magicBookId, out var row) || row == null)
                {
                    Debug.LogWarning($"[CombatMagicBook] missing config book={magicBookId} slot={i}");
                    continue;
                }

                if (!SoldierManufactureMagicBookHook.EffectPhaseContains(row.EffectPhase, PhaseCombat))
                {
                    continue;
                }

                var payload = (row.EffectPayload ?? string.Empty).Trim();
                if (!string.Equals(payload, PayloadStatMul, StringComparison.Ordinal))
                {
                    continue;
                }

                if (!TryParseStatMul(row, magicBookId, out var kind, out var mul))
                {
                    continue;
                }

                switch (kind)
                {
                    case StatKind.MaxHP:
                        maxHpMul *= mul;
                        break;
                    case StatKind.Strength:
                        strengthMul *= mul;
                        break;
                    case StatKind.Agility:
                        agilityMul *= mul;
                        break;
                    case StatKind.Intelligence:
                        intelligenceMul *= mul;
                        break;
                }
            }

            return new CombatStatMulBuff(maxHpMul, strengthMul, agilityMul, intelligenceMul);
        }

        private static bool TryParseStatMul(
            MagicBookConfigRow row,
            string magicBookId,
            out StatKind kind,
            out float mul)
        {
            kind = default;
            mul = 0f;

            var map = MagicBookEffectParams.Parse(row.EffectParams, StatMulAllowedKeys);
            if (!MagicBookEffectParams.TryGet(map, "Stat", out var statText)
                || !MagicBookEffectParams.TryGet(map, "Mul", out var mulText))
            {
                Debug.LogWarning(
                    $"[CombatMagicBook] StatMul invalid (need Stat+Mul) book={magicBookId}");
                return false;
            }

            if (!float.TryParse(mulText, NumberStyles.Float, CultureInfo.InvariantCulture, out mul)
                || mul < 0f)
            {
                Debug.LogWarning(
                    $"[CombatMagicBook] StatMul invalid Mul='{mulText}' book={magicBookId}");
                return false;
            }

            if (!Enum.TryParse(statText, false, out kind)
                || !IsCombatStat(kind))
            {
                Debug.LogWarning(
                    $"[CombatMagicBook] StatMul invalid Stat='{statText}' book={magicBookId}");
                return false;
            }

            return true;
        }

        private static bool IsCombatStat(StatKind kind)
        {
            return kind == StatKind.MaxHP
                || kind == StatKind.Strength
                || kind == StatKind.Agility
                || kind == StatKind.Intelligence;
        }
    }
}
