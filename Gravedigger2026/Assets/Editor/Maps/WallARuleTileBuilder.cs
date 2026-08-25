#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Gravedigger2026.Editor.Maps
{
    /// <summary>
    /// Ensures MapAutoTile Wall A brush: Isometric Rule Tile <c>RT_WallA</c> (SPEC_04 §13 / MA-01).
    /// Paint a filled region → interior Blank, edges/corners Wall A1 facing sprites.
    /// </summary>
    public static class WallARuleTileBuilder
    {
        public const string RuleTilesDir = "Assets/Art/Maps/RuleTiles";
        public const string RuleTilePath = RuleTilesDir + "/RT_WallA.asset";
        public const string PalettePath = "Assets/Art/Maps/Palettes/FantasyTileset_A.prefab";

        private const string TilesDir = "Assets/Art/Maps/Environment/Tiles";
        private const string WallA1N = TilesDir + "/Wall A1_N.asset";
        private const string WallA1E = TilesDir + "/Wall A1_E.asset";
        private const string WallA1S = TilesDir + "/Wall A1_S.asset";
        private const string WallA1W = TilesDir + "/Wall A1_W.asset";
        private const string BlankTile = TilesDir + "/Blank.asset";

        private static readonly Vector3Int N = new Vector3Int(0, 1, 0);
        private static readonly Vector3Int S = new Vector3Int(0, -1, 0);
        private static readonly Vector3Int W = new Vector3Int(-1, 0, 0);
        private static readonly Vector3Int E = new Vector3Int(1, 0, 0);

        private const int This = RuleTile.TilingRuleOutput.Neighbor.This;
        private const int NotThis = RuleTile.TilingRuleOutput.Neighbor.NotThis;

        /// <summary>Palette cell reserved for RT_WallA (left of grid origin).</summary>
        private static readonly Vector3Int PaletteSlot = new Vector3Int(-1, 0, 0);

        [MenuItem("Gravedigger2026/Maps/Ensure Wall A Rule Tile (RT_WallA)")]
        public static void EnsureWallARuleTile()
        {
            EnsureFolder(RuleTilesDir);

            var sprN = LoadTileSprite(WallA1N);
            var sprE = LoadTileSprite(WallA1E);
            var sprS = LoadTileSprite(WallA1S);
            var sprW = LoadTileSprite(WallA1W);
            var sprBlank = LoadTileSprite(BlankTile);

            if (sprN == null || sprE == null || sprS == null || sprW == null)
            {
                Debug.LogError(
                    "[WallARuleTileBuilder] Missing Wall A1_N/E/S/W sprites under Environment/Tiles. Abort.");
                return;
            }

            if (sprBlank == null)
            {
                Debug.LogWarning(
                    "[WallARuleTileBuilder] Blank tile sprite missing; interior cells will use Wall A1_N.");
                sprBlank = sprN;
            }

            var existing = AssetDatabase.LoadAssetAtPath<IsometricRuleTile>(RuleTilePath);
            var created = existing == null;
            var ruleTile = existing != null ? existing : ScriptableObject.CreateInstance<IsometricRuleTile>();
            ruleTile.name = "RT_WallA";
            ruleTile.m_DefaultSprite = sprN;
            ruleTile.m_DefaultColliderType = Tile.ColliderType.Sprite;
            ruleTile.m_TilingRules = BuildRules(sprBlank, sprN, sprE, sprS, sprW);
            ruleTile.UpdateNeighborPositions();

            if (created)
            {
                AssetDatabase.CreateAsset(ruleTile, RuleTilePath);
            }
            else
            {
                EditorUtility.SetDirty(ruleTile);
            }

            AssetDatabase.SaveAssets();
            EnsureOnPalette(ruleTile);
            AssetDatabase.Refresh();

            Debug.Log(
                $"[WallARuleTileBuilder] {(created ? "Created" : "Updated")} {RuleTilePath} " +
                $"({ruleTile.m_TilingRules.Count} rules) and slotted on FantasyTileset_A at {PaletteSlot}. " +
                "Tile Palette → FantasyTileset_A → RT_WallA; paint a filled region to auto-edge.");
        }

        private static List<RuleTile.TilingRule> BuildRules(
            Sprite blank,
            Sprite wallN,
            Sprite wallE,
            Sprite wallS,
            Sprite wallW)
        {
            var rules = new List<RuleTile.TilingRule>();
            var id = 0;

            // Most specific first (RuleTile uses first match).
            // Interior fill → Blank (no wall art).
            rules.Add(MakeRule(ref id, blank, Tile.ColliderType.None,
                (N, This), (S, This), (W, This), (E, This)));

            // Outer corners (two adjacent open sides).
            rules.Add(MakeRule(ref id, wallN,
                (N, NotThis), (E, NotThis), (S, This), (W, This)));
            rules.Add(MakeRule(ref id, wallE,
                (N, NotThis), (W, NotThis), (S, This), (E, This)));
            rules.Add(MakeRule(ref id, wallS,
                (S, NotThis), (E, NotThis), (N, This), (W, This)));
            rules.Add(MakeRule(ref id, wallW,
                (S, NotThis), (W, NotThis), (N, This), (E, This)));

            // Straight edges (one open side).
            rules.Add(MakeRule(ref id, wallN, (N, NotThis), (S, This)));
            rules.Add(MakeRule(ref id, wallS, (S, NotThis), (N, This)));
            rules.Add(MakeRule(ref id, wallE, (E, NotThis), (W, This)));
            rules.Add(MakeRule(ref id, wallW, (W, NotThis), (E, This)));

            // Isolated / fallback default is m_DefaultSprite (A1_N).
            return rules;
        }

        private static RuleTile.TilingRule MakeRule(
            ref int id,
            Sprite sprite,
            params (Vector3Int pos, int neighbor)[] conditions)
        {
            return MakeRule(ref id, sprite, Tile.ColliderType.Sprite, conditions);
        }

        private static RuleTile.TilingRule MakeRule(
            ref int id,
            Sprite sprite,
            Tile.ColliderType collider,
            params (Vector3Int pos, int neighbor)[] conditions)
        {
            var rule = new RuleTile.TilingRule
            {
                m_Id = id++,
                m_Sprites = new[] { sprite },
                m_Output = RuleTile.TilingRuleOutput.OutputSprite.Single,
                m_ColliderType = collider,
                m_RuleTransform = RuleTile.TilingRuleOutput.Transform.Fixed,
            };

            var dict = new Dictionary<Vector3Int, int>(conditions.Length);
            for (var i = 0; i < conditions.Length; i++)
            {
                dict[conditions[i].pos] = conditions[i].neighbor;
            }

            rule.ApplyNeighbors(dict);
            return rule;
        }

        private static Sprite LoadTileSprite(string tilePath)
        {
            var tile = AssetDatabase.LoadAssetAtPath<Tile>(tilePath);
            return tile != null ? tile.sprite : null;
        }

        private static void EnsureOnPalette(IsometricRuleTile ruleTile)
        {
            if (AssetDatabase.LoadAssetAtPath<Object>(PalettePath) == null)
            {
                Debug.LogWarning(
                    $"[WallARuleTileBuilder] Palette missing at {PalettePath}. " +
                    "Ensure FantasyTileset_A.prefab exists, then re-run Ensure Wall A Rule Tile.");
                return;
            }

            var root = PrefabUtility.LoadPrefabContents(PalettePath);
            try
            {
                var tilemap = root.GetComponentInChildren<Tilemap>();
                if (tilemap == null)
                {
                    Debug.LogError("[WallARuleTileBuilder] FantasyTileset_A has no Tilemap.");
                    return;
                }

                // Remove prior placements of this Rule Tile, then pin to reserved slot.
                var bounds = tilemap.cellBounds;
                foreach (var pos in bounds.allPositionsWithin)
                {
                    if (tilemap.GetTile(pos) == ruleTile)
                    {
                        tilemap.SetTile(pos, null);
                    }
                }

                tilemap.SetTile(PaletteSlot, ruleTile);
                PrefabUtility.SaveAsPrefabAsset(root, PalettePath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            var parent = System.IO.Path.GetDirectoryName(path)?.Replace('\\', '/');
            var name = System.IO.Path.GetFileName(path);
            if (string.IsNullOrEmpty(parent) || string.IsNullOrEmpty(name))
            {
                return;
            }

            if (!AssetDatabase.IsValidFolder(parent))
            {
                EnsureFolder(parent);
            }

            AssetDatabase.CreateFolder(parent, name);
        }
    }
}
#endif
