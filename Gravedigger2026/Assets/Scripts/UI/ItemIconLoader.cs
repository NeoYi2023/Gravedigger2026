using System;
using Gravedigger2026.Core.Config;
using UnityEngine;

namespace Gravedigger2026.UI
{
    /// <summary>
    /// Loads item sprites from Resources folders (SPEC_04 §2 / UI-022 / UI-023 / UI-026).
    /// </summary>
    public static class ItemIconLoader
    {
        public const string EquipmentResourcesFolder = "UI/Equipment";
        public const string MagicBookResourcesFolder = "UI/MagicBooks";

        public static Sprite LoadForShopOffer(
            ConfigCsvRepository configs,
            string itemId,
            ShopPoolItemCategory category)
        {
            var iconAssetId = ResolveIconAssetId(configs, itemId, category);
            var folder = category == ShopPoolItemCategory.A
                ? EquipmentResourcesFolder
                : MagicBookResourcesFolder;
            return Load(iconAssetId, folder);
        }

        public static Sprite Load(string iconAssetId, string resourcesFolder)
        {
            var id = Normalize(iconAssetId);
            if (string.IsNullOrEmpty(id))
            {
                return null;
            }

            if (id.IndexOf('/') >= 0)
            {
                return Resources.Load<Sprite>(id);
            }

            if (string.IsNullOrEmpty(resourcesFolder))
            {
                return Resources.Load<Sprite>(id);
            }

            return Resources.Load<Sprite>($"{resourcesFolder}/{id}");
        }

        private static string ResolveIconAssetId(
            ConfigCsvRepository configs,
            string itemId,
            ShopPoolItemCategory category)
        {
            if (configs != null && configs.TryGetItemCatalog(itemId, out var catalog) && catalog != null
                && !string.IsNullOrEmpty(catalog.IconAssetId))
            {
                return catalog.IconAssetId;
            }

            if (configs == null || string.IsNullOrEmpty(itemId))
            {
                return null;
            }

            if (category == ShopPoolItemCategory.A)
            {
                if (configs.TryGetProtagonistEquipment(itemId, 1, out var equip) && equip != null)
                {
                    return equip.IconAssetId;
                }
            }
            else if (configs.TryGetMagicBook(itemId, out var book) && book != null)
            {
                return book.IconAssetId;
            }

            return null;
        }

        private static string Normalize(string iconAssetId)
        {
            if (string.IsNullOrEmpty(iconAssetId))
            {
                return null;
            }

            var id = iconAssetId.Trim();
            if (id.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
            {
                id = id.Substring(0, id.Length - 4);
            }

            return id;
        }
    }
}
