using System;
using System.Collections.Generic;
using Gravedigger2026.Core.Config;
using UnityEngine;

namespace Gravedigger2026.Core.Shop
{
    /// <summary>
    /// Mode2 shop progress: persist snapshot + reset timing (SS-02).
    /// Offers generation & purchase loop are implemented by later slices.
    /// </summary>
    public sealed class ShopProgressService
    {
        private int _slotIndex = -1;
        private CampaignMode _campaignMode = CampaignMode.Mode1;

        private int _maxUnlockedLevelNumber;
        private bool _pendingOpenOnNewUnlock;
        private int _currentRefreshCount;

        private readonly ShopOfferSnapshot[] _offers = new ShopOfferSnapshot[6];

        public int BoundSlotIndex => _slotIndex;
        public CampaignMode BoundCampaignMode => _campaignMode;

        public int MaxUnlockedLevelNumber => _maxUnlockedLevelNumber;
        public bool PendingOpenOnNewUnlock => _pendingOpenOnNewUnlock;
        public int CurrentRefreshCount => _currentRefreshCount;

        public IReadOnlyList<ShopOfferSnapshot> CurrentOffers => _offers;

        public ShopProgressService()
        {
            ResetOffersToDefaults();
        }

        public void BindSlot(int slotIndex, CampaignMode campaignMode)
        {
            if (slotIndex < 0 || slotIndex > 2)
            {
                throw new ArgumentOutOfRangeException(nameof(slotIndex), slotIndex, "Slot index must be 0..2.");
            }

            _slotIndex = slotIndex;
            _campaignMode = campaignMode;

            _maxUnlockedLevelNumber = 0;
            _pendingOpenOnNewUnlock = false;
            _currentRefreshCount = 0;
            ResetOffersToDefaults();

            var key = ShopProgressKey(slotIndex, campaignMode);
            var raw = PlayerPrefs.GetString(key, string.Empty);
            if (string.IsNullOrEmpty(raw))
            {
                return;
            }

            try
            {
                var data = JsonUtility.FromJson<ShopProgressSaveData>(raw);
                ApplyLoaded(data);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[ShopProgress] Failed to parse JSON key='{key}': {ex.Message}. Reset to defaults.");
                ResetOffersToDefaults();
                _maxUnlockedLevelNumber = 0;
                _pendingOpenOnNewUnlock = false;
                _currentRefreshCount = 0;
            }
        }

        public void ClearBound()
        {
            _slotIndex = -1;
            _campaignMode = CampaignMode.Mode1;

            _maxUnlockedLevelNumber = 0;
            _pendingOpenOnNewUnlock = false;
            _currentRefreshCount = 0;
            ResetOffersToDefaults();
        }

        public static void DeleteSlotData(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex > 2)
            {
                return;
            }

            PlayerPrefs.DeleteKey(ShopProgressKey(slotIndex, CampaignMode.Mode1));
            PlayerPrefs.DeleteKey(ShopProgressKey(slotIndex, CampaignMode.Mode2));
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Called by meta layer when a new level unlock happens (SS-06 will wire it).
        /// If max unlocked increases:
        /// - pendingOpenOnNewUnlock=true
        /// - currentRefreshCount=0
        /// - offers cleared (empty slots) so next SS slice can regenerate safely
        /// </summary>
        public bool OnLevelCleared(int newMaxUnlockedLevelNumber)
        {
            if (_slotIndex < 0)
            {
                Debug.LogWarning("[ShopProgress] OnLevelCleared ignored — no save slot bound.");
                return false;
            }

            if (_campaignMode != CampaignMode.Mode2)
            {
                return false;
            }

            if (newMaxUnlockedLevelNumber <= _maxUnlockedLevelNumber)
            {
                return false;
            }

            _maxUnlockedLevelNumber = newMaxUnlockedLevelNumber;
            _pendingOpenOnNewUnlock = true;
            _currentRefreshCount = 0;

            ResetOffersToDefaults();

            Persist();
            return true;
        }

        /// <summary>
        /// Applies freshly generated offers into the persisted snapshot (SS-03).
        /// This method is intentionally scoped: generator decides sold/empty; this service only persists.
        /// </summary>
        public void ApplyGeneratedOffers(
            ShopOfferSnapshot[] offers,
            int newRefreshCount,
            bool clearPendingOpenOnNewUnlock)
        {
            if (offers == null || offers.Length != 6)
            {
                throw new ArgumentException("offers must be a 6-length array (slot0..5).", nameof(offers));
            }

            _currentRefreshCount = Mathf.Max(0, newRefreshCount);
            if (clearPendingOpenOnNewUnlock)
            {
                _pendingOpenOnNewUnlock = false;
            }

            for (var slot = 0; slot < 6; slot++)
            {
                var src = offers[slot];
                if (src == null)
                {
                    _offers[slot] = new ShopOfferSnapshot
                    {
                        SlotIndex = slot,
                        Category = slot <= 2 ? ShopPoolItemCategory.A : ShopPoolItemCategory.B,
                        ItemId = string.Empty,
                        PriceSpirit = 0,
                        IsSold = false
                    };
                    continue;
                }

                _offers[slot] = new ShopOfferSnapshot
                {
                    SlotIndex = slot,
                    Category = src.Category,
                    ItemId = string.IsNullOrEmpty(src.ItemId) ? string.Empty : src.ItemId.Trim(),
                    PriceSpirit = Mathf.Max(0, src.PriceSpirit),
                    IsSold = src.IsSold
                };
            }

            Persist();
        }

        /// <summary>
        /// SS-05：购买后将指定 slot 标记 sold 并持久化。
        /// </summary>
        public bool TryMarkSlotSold(int slotIndex, out string error)
        {
            error = null;

            if (_slotIndex < 0)
            {
                error = "No save slot bound.";
                return false;
            }

            if (slotIndex < 0 || slotIndex > 5)
            {
                error = $"Slot index must be 0..5 (got {slotIndex}).";
                return false;
            }

            var offer = _offers[slotIndex];
            if (offer == null || string.IsNullOrEmpty(offer.ItemId))
            {
                error = "Offer slot is empty.";
                return false;
            }

            if (offer.IsSold)
            {
                error = "Offer slot already sold.";
                return false;
            }

            offer.IsSold = true;
            Persist();
            return true;
        }

        private void ApplyLoaded(ShopProgressSaveData data)
        {
            if (data == null)
            {
                return;
            }

            _maxUnlockedLevelNumber = Mathf.Max(0, data.maxUnlockedLevelNumber);
            _pendingOpenOnNewUnlock = data.pendingOpenOnNewUnlock;
            _currentRefreshCount = Mathf.Max(0, data.currentRefreshCount);

            if (data.currentOffers == null || data.currentOffers.Length == 0)
            {
                ResetOffersToDefaults();
                return;
            }

            ResetOffersToDefaults();

            for (var i = 0; i < data.currentOffers.Length; i++)
            {
                var dto = data.currentOffers[i];
                if (dto == null)
                {
                    continue;
                }

                var slot = dto.slotIndex;
                if (slot < 0 || slot > 5)
                {
                    continue;
                }

                var category = ParseCategory(dto.category, slot);

                _offers[slot] = new ShopOfferSnapshot
                {
                    SlotIndex = slot,
                    Category = category,
                    ItemId = string.IsNullOrEmpty(dto.itemId) ? string.Empty : dto.itemId.Trim(),
                    PriceSpirit = Mathf.Max(0, dto.priceSpirit),
                    IsSold = dto.isSold
                };
            }
        }

        private static ShopPoolItemCategory ParseCategory(string categoryText, int slotIndex)
        {
            if (string.Equals(categoryText, "A", StringComparison.Ordinal))
            {
                return ShopPoolItemCategory.A;
            }

            if (string.Equals(categoryText, "B", StringComparison.Ordinal))
            {
                return ShopPoolItemCategory.B;
            }

            // fallback: derive from slot
            return slotIndex <= 2 ? ShopPoolItemCategory.A : ShopPoolItemCategory.B;
        }

        private void Persist()
        {
            if (_slotIndex < 0)
            {
                return;
            }

            var data = new ShopProgressSaveData
            {
                maxUnlockedLevelNumber = Mathf.Max(0, _maxUnlockedLevelNumber),
                pendingOpenOnNewUnlock = _pendingOpenOnNewUnlock,
                currentRefreshCount = Mathf.Max(0, _currentRefreshCount),
                currentOffers = new ShopOfferSaveData[6]
            };

            for (var slot = 0; slot < 6; slot++)
            {
                var offer = _offers[slot];
                var categoryText = offer.Category == ShopPoolItemCategory.A ? "A" : "B";

                data.currentOffers[slot] = new ShopOfferSaveData
                {
                    slotIndex = offer.SlotIndex,
                    itemId = offer.ItemId,
                    category = categoryText,
                    priceSpirit = offer.PriceSpirit,
                    isSold = offer.IsSold
                };
            }

            var key = ShopProgressKey(_slotIndex, _campaignMode);
            PlayerPrefs.SetString(key, JsonUtility.ToJson(data));
            PlayerPrefs.Save();
        }

        private void ResetOffersToDefaults()
        {
            for (var slot = 0; slot < 6; slot++)
            {
                _offers[slot] = new ShopOfferSnapshot
                {
                    SlotIndex = slot,
                    Category = slot <= 2 ? ShopPoolItemCategory.A : ShopPoolItemCategory.B,
                    ItemId = string.Empty,
                    PriceSpirit = 0,
                    IsSold = false
                };
            }
        }

        private static string ShopProgressKey(int slotIndex, CampaignMode mode)
        {
            return SaveSlotPrefsKeys.DataKey(slotIndex, mode, SaveSlotPrefsKeys.ShopProgressSuffix);
        }

        [Serializable]
        public sealed class ShopOfferSnapshot
        {
            public int SlotIndex;
            public ShopPoolItemCategory Category;
            public string ItemId;
            public int PriceSpirit;
            public bool IsSold;
        }
    }
}

