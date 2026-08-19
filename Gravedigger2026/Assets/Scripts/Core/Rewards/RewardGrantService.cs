using System;
using System.Collections.Generic;
using Gravedigger2026.Core.AutoManufacture;
using Gravedigger2026.Core.Config;
using Gravedigger2026.Core.Dig;
using Gravedigger2026.Core.ProtagonistEquipment;
using UnityEngine;

namespace Gravedigger2026.Core.Rewards
{
    /// <summary>
    /// Grants unified reward items by first resolving ItemCatalogConfig, then dispatching to
    /// Warehouse / SpecialEquipSlots / ProtagonistEquipment services.
    /// </summary>
    public sealed class RewardGrantService
    {
        private readonly ConfigCsvRepository _configs;
        private readonly WarehouseService _warehouse;
        private readonly SpecialEquipSlotsService _specialEquipSlots;
        private readonly ProtagonistEquipmentService _protagonistEquipment;

        public RewardGrantService(
            ConfigCsvRepository configs,
            WarehouseService warehouse,
            SpecialEquipSlotsService specialEquipSlots,
            ProtagonistEquipmentService protagonistEquipment)
        {
            _configs = configs ?? throw new ArgumentNullException(nameof(configs));
            _warehouse = warehouse;
            _specialEquipSlots = specialEquipSlots;
            _protagonistEquipment = protagonistEquipment;
        }

        public List<LootDropEntry> GrantEntries(
            IReadOnlyList<LootDropEntry> entries,
            Action<string> onGranted = null,
            Action<string> onWarning = null)
        {
            var granted = new List<LootDropEntry>();
            if (entries == null || entries.Count == 0)
            {
                return granted;
            }

            for (var i = 0; i < entries.Count; i++)
            {
                if (TryGrantEntry(entries[i], out var grantedEntry, out var message))
                {
                    if (grantedEntry.Count > 0)
                    {
                        granted.Add(grantedEntry);
                    }

                    if (!string.IsNullOrEmpty(message))
                    {
                        onGranted?.Invoke(message);
                    }
                }
                else if (!string.IsNullOrEmpty(message))
                {
                    onWarning?.Invoke(message);
                }
            }

            return granted;
        }

        private bool TryGrantEntry(LootDropEntry entry, out LootDropEntry grantedEntry, out string message)
        {
            grantedEntry = default;
            message = null;
            if (string.IsNullOrEmpty(entry.Id) || entry.Count < 1)
            {
                message = "[RewardGrant] Empty ItemId or non-positive Count — ignored.";
                return false;
            }

            if (!_configs.TryGetItemCatalog(entry.Id, out var item) || item == null)
            {
                message = $"[RewardGrant] ItemCatalog missing ItemId '{entry.Id}' — ignored.";
                return false;
            }

            var displayName = string.IsNullOrEmpty(item.DisplayName) ? item.ItemId : item.DisplayName;
            if (string.Equals(item.ItemType, "Currency", StringComparison.Ordinal) ||
                string.Equals(item.ItemType, "Material", StringComparison.Ordinal) ||
                string.Equals(item.ItemType, "BodyPart", StringComparison.Ordinal))
            {
                if (_warehouse == null)
                {
                    message = $"[RewardGrant] Warehouse unavailable for {item.ItemType} '{item.ItemId}'.";
                    return false;
                }

                _warehouse.CreditLootEntry(
                    entry,
                    _configs,
                    (id, count) => { },
                    spirit => { });
                grantedEntry = entry;
                message = $"[RewardGrant] +{entry.Count} {displayName}";
                return true;
            }

            if (string.Equals(item.ItemType, "MagicBook", StringComparison.Ordinal))
            {
                if (_specialEquipSlots == null)
                {
                    message = $"[RewardGrant] SpecialEquipSlots unavailable for MagicBook '{item.ItemId}'.";
                    return false;
                }

                var grantedCount = 0;
                for (var i = 0; i < entry.Count; i++)
                {
                    if (_specialEquipSlots.TryEquip(item.ItemId, out var error))
                    {
                        grantedCount++;
                        continue;
                    }

                    Debug.LogWarning($"[RewardGrant] MagicBook '{item.ItemId}' grant stopped: {error}");
                    break;
                }

                if (grantedCount <= 0)
                {
                    message = $"[RewardGrant] MagicBook '{item.ItemId}' grant failed.";
                    return false;
                }

                grantedEntry = new LootDropEntry(item.ItemId, grantedCount);
                message = $"[RewardGrant] +{grantedCount} {displayName}";
                return true;
            }

            if (string.Equals(item.ItemType, "ProtagonistEquipment", StringComparison.Ordinal))
            {
                if (_protagonistEquipment == null)
                {
                    message = $"[RewardGrant] ProtagonistEquipment unavailable for '{item.ItemId}'.";
                    return false;
                }

                var grantedCount = 0;
                for (var i = 0; i < entry.Count; i++)
                {
                    if (_protagonistEquipment.TryAcquire(item.ItemId, out var error))
                    {
                        grantedCount++;
                        continue;
                    }

                    Debug.LogWarning($"[RewardGrant] ProtagonistEquipment '{item.ItemId}' grant stopped: {error}");
                    break;
                }

                if (grantedCount <= 0)
                {
                    message = $"[RewardGrant] ProtagonistEquipment '{item.ItemId}' grant failed.";
                    return false;
                }

                grantedEntry = new LootDropEntry(item.ItemId, grantedCount);
                message = $"[RewardGrant] +{grantedCount} {displayName}";
                return true;
            }

            message = $"[RewardGrant] Unsupported ItemType '{item.ItemType}' for '{item.ItemId}'.";
            return false;
        }
    }
}
