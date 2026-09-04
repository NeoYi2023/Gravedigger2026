using System;
using System.Collections.Generic;
using UnityEngine;

namespace Gravedigger2026.Core.Config
{
    /// <summary>
    /// One Dig Tips message segment (SPEC_04 §9.31 TipMessages).
    /// </summary>
    public readonly struct SubLevelTipMessage
    {
        public readonly string MsgType;
        public readonly string IconAssetId;
        public readonly string TypeNameKey;
        public readonly int StockScale;

        public SubLevelTipMessage(string msgType, string iconAssetId, string typeNameKey, int stockScale)
        {
            MsgType = msgType;
            IconAssetId = iconAssetId;
            TypeNameKey = typeNameKey;
            StockScale = stockScale;
        }
    }

    /// <summary>
    /// Closed MsgType → icon + LocalizedDescription Key (SPEC_04 §9.31).
    /// </summary>
    public static class SubLevelTipMessageCatalog
    {
        public const int MaxMessages = 3;
        public const int MaxScaleAbs = 3;

        private static readonly Dictionary<string, (string Icon, string TextKey)> Map =
            new Dictionary<string, (string, string)>(StringComparer.OrdinalIgnoreCase)
            {
                ["Spirit"] = ("Currency_Spirit", "TipMsg_Spirit"),
                ["Wreck"] = ("WreckWarehouse", "TipMsg_Wreck"),
                ["Warrior"] = ("WarriorIcon", "TipMsg_Warrior"),
                ["Archer"] = ("ArcherIcon", "TipMsg_Archer"),
                ["Assassin"] = ("AssassinIcon", "TipMsg_Assassin"),
                ["Mage"] = ("MageIcon", "TipMsg_Mage"),
                ["Humans"] = ("HumansIcon_1", "TipMsg_Humans"),
                ["Elves"] = ("ElvesIcon_1", "TipMsg_Elves"),
                ["Orcs"] = ("OrcIcon_1", "TipMsg_Orcs"),
                ["AllRaces"] = ("AllRacesIcon_1", "TipMsg_AllRaces"),
            };

        public static bool TryResolve(
            string msgType,
            out string iconAssetId,
            out string typeNameKey)
        {
            if (!string.IsNullOrEmpty(msgType) && Map.TryGetValue(msgType.Trim(), out var entry))
            {
                iconAssetId = entry.Icon;
                typeNameKey = entry.TextKey;
                return true;
            }

            iconAssetId = null;
            typeNameKey = null;
            return false;
        }

        /// <summary>
        /// Parse TipMessages cell: MsgType;StockScale|… (max 3). Invalid segments skipped + Warning.
        /// </summary>
        public static List<SubLevelTipMessage> Parse(string encoded, Action<string> onWarn = null)
        {
            var result = new List<SubLevelTipMessage>(MaxMessages);
            if (string.IsNullOrWhiteSpace(encoded))
            {
                return result;
            }

            var segments = encoded.Split('|');
            for (var i = 0; i < segments.Length && result.Count < MaxMessages; i++)
            {
                var seg = segments[i].Trim();
                if (seg.Length == 0)
                {
                    continue;
                }

                var semi = seg.LastIndexOf(';');
                if (semi <= 0 || semi >= seg.Length - 1)
                {
                    onWarn?.Invoke($"TipMessages segment ignored (need MsgType;Scale): '{seg}'");
                    continue;
                }

                var msgType = seg.Substring(0, semi).Trim();
                var scaleText = seg.Substring(semi + 1).Trim();
                if (!int.TryParse(scaleText, out var scale)
                    || scale == 0
                    || Math.Abs(scale) > MaxScaleAbs)
                {
                    onWarn?.Invoke($"TipMessages segment ignored (bad StockScale): '{seg}'");
                    continue;
                }

                if (!TryResolve(msgType, out var icon, out var key))
                {
                    onWarn?.Invoke($"TipMessages segment ignored (unknown MsgType): '{seg}'");
                    continue;
                }

                result.Add(new SubLevelTipMessage(msgType, icon, key, scale));
            }

            return result;
        }

        public static void WarnDefault(string message)
        {
            Debug.LogWarning("[SubLevelTipMessage] " + message);
        }
    }
}
