using System;
using Gravedigger2026.Core.AutoManufacture;
using Gravedigger2026.Core.Config;
using Gravedigger2026.Core.Dig;
using Gravedigger2026.Core.ProtagonistEquipment;

namespace Gravedigger2026.Core.Shop
{
    /// <summary>
    /// SS-05：购买扣款 + 入账/入仓 + slot sold 状态（真正逻辑在后续切片实现）。
    /// SS-04 只需要它的接口存在，以便 ShopStageRoot UI 能把点击派发到 service。
    /// </summary>
    public sealed class ShopPurchaseService
    {
        private readonly WarehouseService _warehouse;
        private readonly ProtagonistEquipmentService _protagonistEquipment;
        private readonly SpecialEquipSlotsService _specialEquipSlots;

        public ShopPurchaseService(
            WarehouseService warehouse,
            ProtagonistEquipmentService protagonistEquipment,
            SpecialEquipSlotsService specialEquipSlots)
        {
            _warehouse = warehouse;
            _protagonistEquipment = protagonistEquipment;
            _specialEquipSlots = specialEquipSlots;
        }

        public bool TryPurchase(
            ShopProgressService progress,
            int slotIndex,
            out string error)
        {
            error = null;

            if (progress == null)
            {
                error = "ShopProgress is null.";
                return false;
            }

            if (slotIndex < 0 || slotIndex > 5)
            {
                error = $"Slot index must be 0..5 (got {slotIndex}).";
                return false;
            }

            if (_warehouse == null || _protagonistEquipment == null || _specialEquipSlots == null)
            {
                error = "ShopPurchaseService dependencies missing.";
                return false;
            }

            var offer = progress.CurrentOffers[slotIndex];
            if (offer == null || string.IsNullOrEmpty(offer.ItemId))
            {
                error = "This offer slot is empty.";
                return false;
            }

            if (offer.IsSold)
            {
                error = "This offer has already been sold.";
                return false;
            }

            var price = offer.PriceSpirit;
            if (price <= 0)
            {
                error = "Invalid offer price.";
                return false;
            }

            if (_warehouse.SpiritEssence < price)
            {
                error = "精魂不足";
                return false;
            }

            // 先扣款，入仓/入账后再标 sold；如果入仓失败则补回精魂。
            if (!_warehouse.TrySpendSpirit(price))
            {
                error = "精魂扣款失败（可能并发或余额不足）";
                return false;
            }

            string equipError;
            bool ok;
            switch (offer.Category)
            {
                case ShopPoolItemCategory.A:
                    ok = _protagonistEquipment.TryAcquire(offer.ItemId, out equipError);
                    break;

                case ShopPoolItemCategory.B:
                    ok = _specialEquipSlots.TryEquip(offer.ItemId, out equipError);
                    break;

                default:
                    ok = false;
                    equipError = "Invalid offer category.";
                    break;
            }

            if (!ok)
            {
                // Refund spirit on failure to keep shop state consistent.
                _warehouse.AddSpirit(price);
                error = equipError ?? "入账失败";
                return false;
            }

            if (!progress.TryMarkSlotSold(slotIndex, out error))
            {
                // State marking failed: refund spirit (inventory already mutated, but this prevents price inconsistency).
                _warehouse.AddSpirit(price);
                return false;
            }

            return true;
        }
    }
}

