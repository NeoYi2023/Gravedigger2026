using Gravedigger2026.Core.AutoManufacture;
using Gravedigger2026.Core.Config;
using Gravedigger2026.Core.Dig;
using Gravedigger2026.Core.ProtagonistEquipment;

namespace Gravedigger2026.Core.Shop
{
    /// <summary>
    /// D-076: Sell owned equipment / MagicBook in shop UI for ItemCatalog SellPrice Spirit.
    /// </summary>
    public sealed class ShopSellService
    {
        private readonly WarehouseService _warehouse;
        private readonly ProtagonistEquipmentService _protagonistEquipment;
        private readonly SpecialEquipSlotsService _specialEquipSlots;
        private readonly ConfigCsvRepository _configs;

        public ShopSellService(
            WarehouseService warehouse,
            ProtagonistEquipmentService protagonistEquipment,
            SpecialEquipSlotsService specialEquipSlots,
            ConfigCsvRepository configs)
        {
            _warehouse = warehouse;
            _protagonistEquipment = protagonistEquipment;
            _specialEquipSlots = specialEquipSlots;
            _configs = configs;
        }

        public bool TryResolveSellPrice(string itemId, out int sellPrice, out string error)
        {
            sellPrice = 0;
            error = null;

            if (string.IsNullOrEmpty(itemId))
            {
                error = "ItemId is empty.";
                return false;
            }

            if (!_configs.TryGetItemCatalog(itemId, out var row) || row == null)
            {
                error = $"Item '{itemId}' not in ItemCatalog.";
                return false;
            }

            if (row.SellPrice < 0)
            {
                error = "Invalid SellPrice.";
                return false;
            }

            sellPrice = row.SellPrice;
            return true;
        }

        public bool TrySellEquipment(string equipId, out string error)
        {
            error = null;

            if (!_protagonistEquipment.TryGetOwned(equipId, out _))
            {
                error = "Equipment not owned.";
                return false;
            }

            if (!TryResolveSellPrice(equipId, out var price, out error))
            {
                return false;
            }

            if (!_protagonistEquipment.TryRemove(equipId, out error))
            {
                return false;
            }

            _warehouse.AddSpirit(price);
            return true;
        }

        public bool TrySellMagicBook(int slotIndex, out string error)
        {
            error = null;

            if (_specialEquipSlots == null)
            {
                error = "MagicBook slots unavailable.";
                return false;
            }

            var bookId = _specialEquipSlots.GetSlot(slotIndex);
            if (string.IsNullOrEmpty(bookId))
            {
                error = "Slot is empty.";
                return false;
            }

            if (!TryResolveSellPrice(bookId, out var price, out error))
            {
                return false;
            }

            if (!_specialEquipSlots.TryUnequip(slotIndex, out error))
            {
                return false;
            }

            _warehouse.AddSpirit(price);
            return true;
        }
    }
}
