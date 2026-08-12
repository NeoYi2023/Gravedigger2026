using System;
using Gravedigger2026.Core.Config;
using UnityEngine;

namespace Gravedigger2026.Core.AutoManufacture
{
    /// <summary>
    /// MagicBook EffectPhase=SoldierManufacture hook (SPEC_03 §3.15 / SPEC_04 §9.24).
    /// AM-04: dispatch equipped books; empty apply (log only, no draft mutation).
    /// </summary>
    public interface ISoldierManufactureMagicBookHook
    {
        void ApplySoldierManufactureEffects(AutoCraftDraft draft);
    }

    public sealed class NoOpSoldierManufactureMagicBookHook : ISoldierManufactureMagicBookHook
    {
        public static readonly NoOpSoldierManufactureMagicBookHook Instance =
            new NoOpSoldierManufactureMagicBookHook();

        public void ApplySoldierManufactureEffects(AutoCraftDraft draft)
        {
        }
    }

    /// <summary>
    /// Reads SpecialEquipSlots + MagicBookConfig; invokes empty SoldierManufacture apply per matching book.
    /// </summary>
    public sealed class SoldierManufactureMagicBookHook : ISoldierManufactureMagicBookHook
    {
        public const string PhaseSoldierManufacture = "SoldierManufacture";

        private readonly SpecialEquipSlotsService _slots;
        private readonly ConfigCsvRepository _configs;

        public SoldierManufactureMagicBookHook(SpecialEquipSlotsService slots, ConfigCsvRepository configs)
        {
            _slots = slots ?? throw new ArgumentNullException(nameof(slots));
            _configs = configs ?? throw new ArgumentNullException(nameof(configs));
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
                    // Empty Demo table / unknown id: skip effect (no config phase).
                    return;
                }

                if (!EffectPhaseContains(row.EffectPhase, PhaseSoldierManufacture))
                {
                    return;
                }

                applied++;
                // Empty hook this round — no draft mutation.
                Debug.Log(
                    $"[MagicBook] SoldierManufacture empty hook book={magicBookId} draft={draft.TempId} " +
                    $"payload='{row.EffectPayload ?? string.Empty}'");
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
    }
}
