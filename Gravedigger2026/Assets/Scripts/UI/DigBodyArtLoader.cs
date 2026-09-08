using Gravedigger2026.Core.Config;
using UnityEngine;

namespace Gravedigger2026.UI
{
    /// <summary>
    /// Loads UI-016 Dig presentation sprites from Resources/UI/Dig (SPEC_04 §2).
    /// Falls back to Texture2D→Sprite if import type is not Sprite yet.
    /// </summary>
    public static class DigBodyArtLoader
    {
        private const string DigResourceRoot = "UI/Dig/";
        private const float PixelsPerUnit = 100f;

        public static Sprite LoadBodyPart(string artAssetId, BodySlot slot)
        {
            var sprite = LoadByAssetId(artAssetId);
            if (sprite != null)
            {
                return sprite;
            }

            return LoadFallback(slot);
        }

        public static Sprite LoadMagicCircle()
        {
            return LoadSprite(DigResourceRoot + "MagicCircle_1");
        }

        public static Sprite LoadUnknownSoldier()
        {
            return LoadSprite("UI/Icons/UnknownSoldier_1");
        }

        private static Sprite LoadByAssetId(string artAssetId)
        {
            if (string.IsNullOrWhiteSpace(artAssetId))
            {
                return null;
            }

            var id = artAssetId.Trim();
            if (id.EndsWith(".png", System.StringComparison.OrdinalIgnoreCase))
            {
                id = id.Substring(0, id.Length - 4);
            }

            if (id.Contains("/"))
            {
                return LoadSprite(id);
            }

            return LoadSprite(DigResourceRoot + id);
        }

        private static Sprite LoadFallback(BodySlot slot)
        {
            switch (slot)
            {
                case BodySlot.Head:
                    return LoadSprite(DigResourceRoot + "Art_BP_Head_Undead_1");
                case BodySlot.Torso:
                    return LoadSprite(DigResourceRoot + "Art_BP_Torso_Undead_1");
                case BodySlot.Arm:
                    return LoadSprite(DigResourceRoot + "Art_BP_Arm_Undead_1");
                case BodySlot.Leg:
                    return LoadSprite(DigResourceRoot + "Art_BP_Leg_Undead_1");
                default:
                    return LoadSprite(DigResourceRoot + "Art_BP_Torso_Undead_1");
            }
        }

        private static Sprite LoadSprite(string resourcesPath)
        {
            if (string.IsNullOrEmpty(resourcesPath))
            {
                return null;
            }

            var sprite = Resources.Load<Sprite>(resourcesPath);
            if (sprite != null)
            {
                return sprite;
            }

            var sprites = Resources.LoadAll<Sprite>(resourcesPath);
            if (sprites != null && sprites.Length > 0 && sprites[0] != null)
            {
                return sprites[0];
            }

            var tex = Resources.Load<Texture2D>(resourcesPath);
            if (tex == null)
            {
                return null;
            }

            return Sprite.Create(
                tex,
                new Rect(0f, 0f, tex.width, tex.height),
                new Vector2(0.5f, 0.5f),
                PixelsPerUnit);
        }
    }
}
