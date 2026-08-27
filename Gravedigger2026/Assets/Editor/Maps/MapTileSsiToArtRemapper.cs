#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Gravedigger2026.Editor.Maps
{
    /// <summary>
    /// Remaps SmallScaleInt (vendor FantasyTileset) Tile GUIDs on PushMap_Demo_03
    /// to Art same-name tiles used by FantasyTileset_A (SPEC_04 §13).
    /// Does not touch WalkSurface / NavMesh / AirWall / EngageZone.
    /// </summary>
    public static class MapTileSsiToArtRemapper
    {
        public const string PrefabPath = "Assets/Prefabs/Maps/PushMap_Demo_03.prefab";
        private const string GridChildName = "GroundTilemap";
        private const string SsiPrefix = "Assets/SmallScaleInt/";

        private static readonly string[] ArtTileSearchFolders =
        {
            "Assets/Art/Maps/Environment/Tiles",
            "Assets/Art/Maps/Environment/Animated tiles",
            "Assets/Art/Maps/RuleTiles"
        };

        [MenuItem("Gravedigger2026/Maps/Remap PushMap_Demo_03 SSI Tiles To Art (FantasyTileset_A names)")]
        public static void RemapPushMapDemo03Menu()
        {
            RemapPushMapDemo03();
        }

        /// <summary>Batch entry: -executeMethod Gravedigger2026.Editor.Maps.MapTileSsiToArtRemapper.RemapPushMapDemo03</summary>
        public static void RemapPushMapDemo03()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null)
            {
                Debug.LogError($"[MapTileSsiToArtRemapper] Prefab not found: {PrefabPath}");
                return;
            }

            var artByName = BuildArtTileLookup();
            var root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                var grid = FindChildRecursive(root.transform, GridChildName);
                if (grid == null)
                {
                    Debug.LogError($"[MapTileSsiToArtRemapper] Missing child '{GridChildName}' on {PrefabPath}");
                    return;
                }

                var maps = grid.GetComponentsInChildren<Tilemap>(true);
                int replacedTypes = 0;
                int alreadyArt = 0;
                int missing = 0;
                var missingNames = new List<string>();

                foreach (var map in maps)
                {
                    if (map == null)
                    {
                        continue;
                    }

                    var used = new HashSet<TileBase>();
                    foreach (var pos in map.cellBounds.allPositionsWithin)
                    {
                        var t = map.GetTile(pos);
                        if (t != null)
                        {
                            used.Add(t);
                        }
                    }

                    foreach (var oldTile in used)
                    {
                        var path = AssetDatabase.GetAssetPath(oldTile);
                        if (string.IsNullOrEmpty(path))
                        {
                            continue;
                        }

                        if (!path.StartsWith(SsiPrefix))
                        {
                            alreadyArt++;
                            continue;
                        }

                        if (!artByName.TryGetValue(oldTile.name, out var artTile) || artTile == null)
                        {
                            missing++;
                            if (!missingNames.Contains(oldTile.name))
                            {
                                missingNames.Add(oldTile.name);
                            }

                            Debug.LogError(
                                $"[MapTileSsiToArtRemapper] No Art tile named '{oldTile.name}' " +
                                $"(SSI path={path}) on layer '{map.name}'");
                            continue;
                        }

                        map.SwapTile(oldTile, artTile);
                        replacedTypes++;
                    }
                }

                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                AssetDatabase.SaveAssets();

                var missingMsg = missingNames.Count > 0
                    ? $" missingNames=[{string.Join(", ", missingNames)}]"
                    : string.Empty;
                Debug.Log(
                    $"[MapTileSsiToArtRemapper] {PrefabPath}: replacedTypes={replacedTypes}, " +
                    $"alreadyArtRefs={alreadyArt}, missing={missing}.{missingMsg}");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static Dictionary<string, TileBase> BuildArtTileLookup()
        {
            var map = new Dictionary<string, TileBase>();
            foreach (var folder in ArtTileSearchFolders)
            {
                if (!AssetDatabase.IsValidFolder(folder))
                {
                    continue;
                }

                var guids = AssetDatabase.FindAssets("t:TileBase", new[] { folder });
                foreach (var guid in guids)
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    var tile = AssetDatabase.LoadAssetAtPath<TileBase>(path);
                    if (tile == null)
                    {
                        continue;
                    }

                    // Prefer first hit; Environment/Tiles listed before RuleTiles.
                    if (!map.ContainsKey(tile.name))
                    {
                        map[tile.name] = tile;
                    }
                }
            }

            return map;
        }

        private static Transform FindChildRecursive(Transform parent, string name)
        {
            if (parent.name == name)
            {
                return parent;
            }

            for (var i = 0; i < parent.childCount; i++)
            {
                var found = FindChildRecursive(parent.GetChild(i), name);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }
    }
}
#endif
