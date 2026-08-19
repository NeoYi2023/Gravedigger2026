using System.Collections.Generic;

namespace Gravedigger2026.Core.Config
{
    /// <summary>
    /// One row of Shop_ShopPoolConfig (SPEC_04 §9.27) with parsed PoolItemsRaw.
    /// </summary>
    public sealed class ShopPoolConfigRow
    {
        public string ShopPoolId;

        public int RequiredMaxLevelNumber;

        public string ExtraUnlockCondition;

        public string PoolItemsRaw;

        /// <summary>
        /// Parsed candidates from PoolItemsRaw (category A/B + itemId + weight).
        /// weight=0 candidates are omitted.
        /// </summary>
        public List<ShopPoolItemCandidate> PoolItems;
    }

    public enum ShopPoolItemCategory
    {
        A = 0,
        B = 1
    }

    public sealed class ShopPoolItemCandidate
    {
        public string ItemId;

        public ShopPoolItemCategory Category;

        public float Weight;
    }
}

