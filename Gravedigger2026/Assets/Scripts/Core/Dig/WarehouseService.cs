using System;
using System.Collections.Generic;
using Gravedigger2026.Core.Config;
using UnityEngine;

namespace Gravedigger2026.Core.Dig
{
    /// <summary>
    /// Per-save-slot item warehouse + SpiritEssence (SPEC_03 §3.10). Demo: in-memory only.
    /// Holds materials and manufacture items (BodyPart / Soul / Gem / ExtraEquipment) in one Id namespace
    /// (SPEC_04 §9.12: BodyPartId shares the MaterialId namespace; Soul/Gem/Equip acquisition is TBD).
    /// </summary>
    public sealed class WarehouseService
    {
        public const int MaterialStackCap = 10000;

        private readonly Dictionary<string, int> _materials = new Dictionary<string, int>(StringComparer.Ordinal);
        private float _spiritEssence;

        public float SpiritEssence => _spiritEssence;
        public IReadOnlyDictionary<string, int> Materials => _materials;

        public void Clear()
        {
            _materials.Clear();
            _spiritEssence = 0f;
        }

        /// <summary>
        /// Credits initial Spirit on new SaveSlot create (SPEC_03 §3.4 / SPEC_04 §9.20b).
        /// </summary>
        public void ApplyNewSaveGrants(ConfigCsvRepository configs)
        {
            if (configs == null)
            {
                return;
            }

            var count = (int)configs.GetCombatConstantOrFallback(
                CombatConstantKeys.NewSaveInitialSpiritCount,
                CombatConstantKeys.Safety.NewSaveInitialSpiritCount);
            if (count <= 0)
            {
                return;
            }

            CreditLootEntry(
                new LootDropEntry(LootDropParser.SpiritId, count),
                configs,
                (_, __) => { },
                _ => { });
        }

        public void AddSpirit(float amount)
        {
            if (amount > 0f)
            {
                _spiritEssence += amount;
            }
        }

        public bool TrySpendSpirit(float amount)
        {
            if (amount < 0f || _spiritEssence < amount)
            {
                return false;
            }

            _spiritEssence -= amount;
            return true;
        }

        public int GetCount(string itemId)
        {
            if (string.IsNullOrEmpty(itemId))
            {
                return 0;
            }

            return _materials.TryGetValue(itemId, out var count) ? count : 0;
        }

        /// <summary>
        /// Adds items without AutoConvert handling; used by manufacture-kit Debug grants.
        /// </summary>
        public void AddItem(string itemId, int count)
        {
            if (string.IsNullOrEmpty(itemId) || count < 1)
            {
                return;
            }

            var current = GetCount(itemId);
            _materials[itemId] = Math.Min(MaterialStackCap, current + count);
        }

        public bool TryConsume(string itemId, int count)
        {
            if (string.IsNullOrEmpty(itemId) || count < 1)
            {
                return false;
            }

            var current = GetCount(itemId);
            if (current < count)
            {
                return false;
            }

            var remaining = current - count;
            if (remaining > 0)
            {
                _materials[itemId] = remaining;
            }
            else
            {
                _materials.Remove(itemId);
            }

            return true;
        }

        /// <summary>
        /// Credits one LootDrop entry; returns display lines for stage aggregate.
        /// </summary>
        public void CreditLootEntry(
            LootDropEntry entry,
            ConfigCsvRepository configs,
            Action<string, int> onMaterialGained,
            Action<float> onSpiritGained)
        {
            if (entry.Count < 1 || string.IsNullOrEmpty(entry.Id))
            {
                return;
            }

            if (string.Equals(entry.Id, LootDropParser.SpiritId, StringComparison.Ordinal))
            {
                AddSpirit(entry.Count);
                onSpiritGained?.Invoke(entry.Count);
                return;
            }

            if (configs != null && configs.TryGetMaterial(entry.Id, out var material))
            {
                CreditMaterial(entry.Id, entry.Count, material.AutoConvert, onMaterialGained, onSpiritGained);
                return;
            }

            if (configs != null && configs.TryGetBodyPart(entry.Id, out var bodyPart))
            {
                CreditMaterial(entry.Id, entry.Count, bodyPart.AutoConvert, onMaterialGained, onSpiritGained);
                return;
            }

            Debug.LogWarning($"[Warehouse] Unknown LootDrop Id '{entry.Id}' — ignored.");
        }

        private void CreditMaterial(
            string materialId,
            int count,
            float autoConvert,
            Action<string, int> onMaterialGained,
            Action<float> onSpiritGained)
        {
            _materials.TryGetValue(materialId, out var current);
            var space = MaterialStackCap - current;
            var toStack = Math.Min(count, Math.Max(0, space));
            var excess = count - toStack;

            if (toStack > 0)
            {
                _materials[materialId] = current + toStack;
                onMaterialGained?.Invoke(materialId, toStack);
            }

            if (excess > 0 && autoConvert > 0f)
            {
                var spiritGain = excess * autoConvert;
                AddSpirit(spiritGain);
                onSpiritGained?.Invoke(spiritGain);
            }
        }
    }
}
