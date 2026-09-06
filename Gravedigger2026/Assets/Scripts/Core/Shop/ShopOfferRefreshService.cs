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
        /// 方案 B：打开商店时若 6 槽皆空，按 floor=max(1, activeLevelNumberFloor) 抬门槛/置 pending，再生成一次。
        /// 任一槽已有 itemId（含已售）则不重刷。
        /// </summary>
        public bool TryEnsureOffersIfAllEmpty(
            ShopProgressService progress,
            ConfigCsvRepository configs,
            int activeLevelNumberFloor)
        {
            if (progress == null || configs == null)
            {
                return false;
            }

            if (!progress.AreAllOffersEmpty())
            {
                return false;
            }

            var floor = Math.Max(1, activeLevelNumberFloor);
            if (progress.MaxUnlockedLevelNumber < floor)
            {
                progress.OnLevelCleared(floor);
            }
            else
            {
                progress.ForcePendingOpenForEmptyOffers();
            }

            var generated = TryAutoRefreshOnceIfPending(progress, configs);
            if (generated)
            {
                Debug.Log(
                    $"[ShopRefresh] Empty-shelf ensure done (slot={progress.BoundSlotIndex}, floor={floor}, maxUnlocked={progress.MaxUnlockedLevelNumber}).");
            }

            return generated;
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

        /// <summary>Parse trailing digits of LevelId (e.g. Level_01 → 1).</summary>
        public static bool TryParseLevelNumberSuffix(string levelId, out int number)
        {
            number = 0;
            if (string.IsNullOrEmpty(levelId))
            {
                return false;
            }

            var i = levelId.Length - 1;
            while (i >= 0 && char.IsDigit(levelId[i]))
            {
                i--;
            }

            if (i == levelId.Length - 1)
            {
                return false;
            }

            var digits = levelId.Substring(i + 1);
            return int.TryParse(digits, out number);
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

