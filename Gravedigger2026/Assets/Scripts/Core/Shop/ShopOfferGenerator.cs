using System;
using System.Collections.Generic;
using Gravedigger2026.Core.Config;

namespace Gravedigger2026.Core.Shop
{
    /// <summary>
    /// SS-03：基于已解锁关卡的池筛选 + A/B 类别权重汇总 + 类内无重复抽样（最多 3 个 itemId）。
    /// </summary>
    public sealed class ShopOfferGenerator
    {
        private readonly System.Random _rng;

        public ShopOfferGenerator(int? seed = null)
        {
            _rng = new System.Random(seed ?? Environment.TickCount);
        }

        public ShopProgressService.ShopOfferSnapshot[] GenerateOffers(
            ConfigCsvRepository configs,
            int highestClearedMaxLevelNumber)
        {
            if (configs == null)
            {
                throw new ArgumentNullException(nameof(configs));
            }

            var offers = new ShopProgressService.ShopOfferSnapshot[6];
            for (var slot = 0; slot < 6; slot++)
            {
                offers[slot] = new ShopProgressService.ShopOfferSnapshot
                {
                    SlotIndex = slot,
                    Category = slot <= 2 ? ShopPoolItemCategory.A : ShopPoolItemCategory.B,
                    ItemId = string.Empty,
                    PriceSpirit = 0,
                    IsSold = false
                };
            }

            var weightByItemA = new Dictionary<string, float>(StringComparer.Ordinal);
            var weightByItemB = new Dictionary<string, float>(StringComparer.Ordinal);

            // 1) unlocked pools
            var pools = configs.ShopPoolRows;
            for (var i = 0; i < pools.Count; i++)
            {
                var pool = pools[i];
                if (pool == null)
                {
                    continue;
                }

                if (pool.RequiredMaxLevelNumber > highestClearedMaxLevelNumber)
                {
                    continue;
                }

                // ExtraUnlockCondition is reserved (SPEC_04 §9.27): always-true for this version.

                // 2) parse + sum weights into A/B dicts
                var items = pool.PoolItems;
                if (items == null)
                {
                    continue;
                }

                for (var n = 0; n < items.Count; n++)
                {
                    var c = items[n];
                    if (c == null || string.IsNullOrEmpty(c.ItemId))
                    {
                        continue;
                    }

                    if (c.Category == ShopPoolItemCategory.A)
                    {
                        if (weightByItemA.TryGetValue(c.ItemId, out var cur))
                        {
                            weightByItemA[c.ItemId] = cur + c.Weight;
                        }
                        else
                        {
                            weightByItemA[c.ItemId] = c.Weight;
                        }
                    }
                    else
                    {
                        if (weightByItemB.TryGetValue(c.ItemId, out var cur))
                        {
                            weightByItemB[c.ItemId] = cur + c.Weight;
                        }
                        else
                        {
                            weightByItemB[c.ItemId] = c.Weight;
                        }
                    }
                }
            }

            // 3) pick up to 3 distinct itemIds per category by weight
            var pickedA = PickDistinctItemIdsByWeight(weightByItemA, 3);
            var pickedB = PickDistinctItemIdsByWeight(weightByItemB, 3);

            // 4) write offers (no sold; price from ItemCatalog SellPrice)
            for (var idx = 0; idx < pickedA.Count && idx < 3; idx++)
            {
                var itemId = pickedA[idx];
                offers[idx].ItemId = itemId;
                offers[idx].IsSold = false;

                if (configs.TryGetItemCatalog(itemId, out var catalog) && catalog != null)
                {
                    offers[idx].PriceSpirit = Math.Max(0, catalog.SellPrice);
                }
            }

            for (var idx = 0; idx < pickedB.Count && idx < 3; idx++)
            {
                var itemId = pickedB[idx];
                var slot = 3 + idx;
                offers[slot].ItemId = itemId;
                offers[slot].IsSold = false;

                if (configs.TryGetItemCatalog(itemId, out var catalog) && catalog != null)
                {
                    offers[slot].PriceSpirit = Math.Max(0, catalog.SellPrice);
                }
            }

            return offers;
        }

        private List<string> PickDistinctItemIdsByWeight(
            Dictionary<string, float> weightByItemId,
            int count)
        {
            var result = new List<string>();
            if (weightByItemId == null || weightByItemId.Count == 0 || count <= 0)
            {
                return result;
            }

            // Copy because we need remove picked items to ensure "no duplicate itemId".
            var remaining = new Dictionary<string, float>(weightByItemId, StringComparer.Ordinal);

            while (result.Count < count && remaining.Count > 0)
            {
                var total = 0f;
                foreach (var kv in remaining)
                {
                    total += kv.Value;
                }

                if (total <= 0f)
                {
                    break;
                }

                var r = (float)(_rng.NextDouble() * total);
                var cumulative = 0f;
                string chosen = null;

                foreach (var kv in remaining)
                {
                    cumulative += kv.Value;
                    if (r <= cumulative)
                    {
                        chosen = kv.Key;
                        break;
                    }
                }

                if (string.IsNullOrEmpty(chosen))
                {
                    // Numerical fallback: pick the first item.
                    foreach (var kv in remaining)
                    {
                        chosen = kv.Key;
                        break;
                    }
                }

                if (string.IsNullOrEmpty(chosen))
                {
                    break;
                }

                result.Add(chosen);
                remaining.Remove(chosen);
            }

            return result;
        }
    }
}

