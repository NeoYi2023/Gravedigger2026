using Gravedigger2026.Core.UpgradeManufacture;
using UnityEngine;

namespace Gravedigger2026.Gameplay.Defend
{
    /// <summary>
    /// Applies Mode2 VisualStyle to a spawned warrior Visual (SPEC_03 §3.15 6b / SPEC_04 §15.2).
    /// Swaps sharedMaterial; writes intensity via MaterialPropertyBlock; never clones materials.
    /// Scale channel sets Visual.localScale to (k,k,k).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class WarriorAllIn1StyleView : MonoBehaviour
    {
        public static void ApplyTo(
            GameObject root,
            WarriorVisualStyleCatalog catalog,
            WarriorInstance warrior)
        {
            if (root == null)
            {
                return;
            }

            var styleId = warrior != null ? warrior.VisualStyleId : null;
            var intensity = warrior != null ? warrior.VisualIntensity : 0f;
            ApplyTo(root, catalog, styleId, intensity);
            ApplyModelScale(root, WarriorVisualModelScale.Resolve(warrior));
        }

        public static void ApplyTo(
            GameObject root,
            WarriorVisualStyleCatalog catalog,
            string styleId,
            float intensity)
        {
            if (root == null)
            {
                return;
            }

            var visual = FindVisualRenderer(root);
            if (visual == null)
            {
                return;
            }

            EnsureAtlasDriver(visual.gameObject);

            WarriorVisualStyleCatalog.Entry entry = null;
            var hasStyle = !string.IsNullOrWhiteSpace(styleId);
            var resolved = hasStyle
                && catalog != null
                && catalog.TryGet(styleId.Trim(), out entry)
                && entry != null
                && entry.Kind != WarriorVisualStyleCatalog.StyleKind.ScaleModel
                && entry.Material != null;
            if (!resolved)
            {
                if (hasStyle && !WarriorVisualModelScale.IsScaleStyle(styleId))
                {
                    Debug.LogWarning(
                        $"[VisualStyle] Apply skipped style={styleId} catalog={(catalog != null)} " +
                        $"root={root.name}");
                }

                RefreshAtlas(visual.gameObject);
                return;
            }

            visual.sharedMaterial = entry.Material;
            ApplyIntensity(visual, entry, intensity <= 0f ? 1f : intensity);
            RefreshAtlas(visual.gameObject);
        }

        private static void ApplyModelScale(GameObject root, float scale)
        {
            var visual = root.transform.Find("Visual");
            if (visual == null)
            {
                return;
            }

            var k = scale <= 0f ? 1f : scale;
            visual.localScale = new Vector3(k, k, k);
        }

        private static SpriteRenderer FindVisualRenderer(GameObject root)
        {
            var visual = root.transform.Find("Visual");
            if (visual != null)
            {
                var sr = visual.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    return sr;
                }
            }

            return root.GetComponentInChildren<SpriteRenderer>(true);
        }

        private static void EnsureAtlasDriver(GameObject visualGo)
        {
            if (visualGo.GetComponent<AllIn1AtlasUvDriver>() == null)
            {
                visualGo.AddComponent<AllIn1AtlasUvDriver>();
            }
        }

        private static void RefreshAtlas(GameObject visualGo)
        {
            var driver = visualGo.GetComponent<AllIn1AtlasUvDriver>();
            if (driver != null)
            {
                driver.Refresh();
            }
        }

        private static void ApplyIntensity(
            SpriteRenderer renderer,
            WarriorVisualStyleCatalog.Entry entry,
            float intensity)
        {
            var names = entry.IntensityFloatProperties;
            if (names == null || names.Length == 0)
            {
                return;
            }

            var mat = renderer.sharedMaterial;
            if (mat == null)
            {
                return;
            }

            var block = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(block);
            for (var i = 0; i < names.Length; i++)
            {
                var name = names[i];
                if (string.IsNullOrEmpty(name) || !mat.HasProperty(name))
                {
                    continue;
                }

                block.SetFloat(name, mat.GetFloat(name) * intensity);
            }

            renderer.SetPropertyBlock(block);
        }
    }
}
