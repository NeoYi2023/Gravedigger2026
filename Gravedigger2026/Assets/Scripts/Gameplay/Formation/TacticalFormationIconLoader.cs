using System;
using System.Collections.Generic;
using UnityEngine;

namespace Gravedigger2026.Gameplay.Formation
{
    /// <summary>
    /// Loads tactical-formation icons from Resources/UI/Formations/{IconAssetId} (SPEC_04 §9.30 / UI-030).
    /// </summary>
    public static class TacticalFormationIconLoader
    {
        public const string ResourcesFolder = "UI/Formations";
        private static readonly Dictionary<string, Sprite> Cache =
            new Dictionary<string, Sprite>(StringComparer.Ordinal);

        public static Sprite Load(string iconAssetId)
        {
            if (string.IsNullOrEmpty(iconAssetId))
            {
                return null;
            }

            if (Cache.TryGetValue(iconAssetId, out var cached))
            {
                return cached;
            }

            var sprite = Resources.Load<Sprite>($"{ResourcesFolder}/{iconAssetId}");
            Cache[iconAssetId] = sprite;
            return sprite;
        }
    }
}
