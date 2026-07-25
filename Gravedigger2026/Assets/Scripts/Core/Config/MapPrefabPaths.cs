using System;

namespace Gravedigger2026.Core.Config
{
    /// <summary>
    /// DigMapId / BattleMapId → Assets/Prefabs/Maps/{Id}.prefab (SPEC_04 §9.2 / §9.7 / §13).
    /// </summary>
    public static class MapPrefabPaths
    {
        public const string PrefabFolder = "Assets/Prefabs/Maps";

        private static readonly string[] AllowedIds =
        {
            "Ground_01", "Ground_02", "Ground_03", "Ground_04", "Ground_05"
        };

        public static bool IsAllowed(string mapId)
        {
            if (string.IsNullOrEmpty(mapId))
            {
                return false;
            }

            for (var i = 0; i < AllowedIds.Length; i++)
            {
                if (string.Equals(AllowedIds[i], mapId, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        public static bool TryResolveAssetPath(string mapId, out string assetPath, out string error)
        {
            assetPath = null;
            if (!IsAllowed(mapId))
            {
                error = $"MapId '{mapId}' is not in Ground_01…Ground_05.";
                return false;
            }

            assetPath = $"{PrefabFolder}/{mapId}.prefab";
            error = null;
            return true;
        }
    }
}
