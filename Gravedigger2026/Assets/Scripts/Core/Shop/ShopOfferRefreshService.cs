using System;
using Gravedigger2026.Core.Config;
using Gravedigger2026.Core.Dig;
using UnityEngine;

namespace Gravedigger2026.Core.Shop
{
    /// <summary>
    /// SS-03：auto refresh once + 手动刷新（扣 refresh price，但不处理购买扣款）。
    /// </summary>
    public sealed class ShopOfferRefreshService
    {
        private readonly ShopOfferGenerator _generator;

        public ShopOfferRefreshService(ShopOfferGenerator generator = null)
        {
            _generator = generator ?? new ShopOfferGenerator();
        }

        /// <summary>
        /// 当 pendingOpenOnNewUnlock=true 时，自动生成 offers 一次（不扣 refresh price）。
        /// </summary>
        public bool TryAutoRefreshOnceIfPending(
            ShopProgressService progress,
            ConfigCsvRepository configs)
        {
            if (progress == null || configs == null)
            {
                return false;
            }

            if (!progress.PendingOpenOnNewUnlock)
            {
                return false;
            }

            var offers = _generator.GenerateOffers(configs, progress.MaxUnlockedLevelNumber);
            progress.ApplyGeneratedOffers(
                offers,
                progress.CurrentRefreshCount,
                clearPendingOpenOnNewUnlock: true);

            Debug.Log($"[ShopRefresh] Auto refresh once done (slot={progress.BoundSlotIndex}, mode={progress.BoundCampaignMode}).");
            return true;
        }

        /// <summary>
        /// 手动刷新：若下一行 RefreshCount 配置缺失 → 返回 false（不改变 offers，不扣款）。
        /// 若存在 → 尝试从 WarehouseService 扣款 RefreshPrice，成功则刷新 offers，并推进 currentRefreshCount。
        /// </summary>
        public bool TryManualRefresh(
            ShopProgressService progress,
            WarehouseService warehouse,
            ConfigCsvRepository configs)
        {
            if (progress == null || warehouse == null || configs == null)
            {
                return false;
            }

            var nextRefreshCount = progress.CurrentRefreshCount + 1;
            if (!configs.TryGetShopRefreshPrice(nextRefreshCount, out var refreshRow) || refreshRow == null)
            {
                return false;
            }

            var price = refreshRow.RefreshPrice;
            if (price > 0f && !warehouse.TrySpendSpirit(price))
            {
                return false;
            }

            var offers = _generator.GenerateOffers(configs, progress.MaxUnlockedLevelNumber);
            progress.ApplyGeneratedOffers(
                offers,
                nextRefreshCount,
                clearPendingOpenOnNewUnlock: true);

            Debug.Log(
                $"[ShopRefresh] Manual refresh ok (slot={progress.BoundSlotIndex}, mode={progress.BoundCampaignMode}, refreshCount={nextRefreshCount}, price={price}).");
            return true;
        }
    }
}

