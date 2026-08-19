using System;

namespace Gravedigger2026.Core.Shop
{
    /// <summary>
    /// PlayerPrefs JSON DTO for Mode2 shop progress (SPEC_04 §6).
    /// </summary>
    [Serializable]
    public sealed class ShopProgressSaveData
    {
        public int maxUnlockedLevelNumber;

        /// <summary>
        /// Pending "auto open + auto-refresh-on-next-open" after a new level unlock.
        /// </summary>
        public bool pendingOpenOnNewUnlock;

        public int currentRefreshCount;

        public ShopOfferSaveData[] currentOffers = Array.Empty<ShopOfferSaveData>();
    }

    [Serializable]
    public sealed class ShopOfferSaveData
    {
        /// <summary>0..5</summary>
        public int slotIndex;

        public string itemId;

        /// <summary>"A" or "B" (SPEC_03 §3.5)</summary>
        public string category;

        public int priceSpirit;

        public bool isSold;
    }
}

