#if UNITY_EDITOR
using System.Collections.Generic;
using Gravedigger2026.Gameplay.Dig;
using UnityEditor;
using UnityEngine;

namespace Gravedigger2026.Editor.Dig
{
    /// <summary>
    /// Offline-bakes DigHitShape convex hulls from Grave Prefab sprites (SPEC_04 §9.2).
    /// </summary>
    public static class DigHitShapeBaker
    {
        private const string PrefabDigDir = "Assets/Prefabs/Dig";
        private const int MaxVerts = 12;
        private const float AlphaThreshold = 0.15f;
        private const int AlphaSampleStep = 2;
        private const string BakePrefsKey = "Gravedigger2026.DigHitShape.Baked.v054";

        private static readonly string[] QualityIds =
        {
            "Q1", "Q2", "Q3", "Q4", "Q5", "Q6", "Q7", "Q8", "Q9", "Q10",
            "Q11", "Q12", "Q13", "Q14", "Q15", "Q16", "Q17", "Q18", "Q19", "Q20"
        };

        [InitializeOnLoadMethod]
        private static void AutoBakeOnceAfterCompile()
        {
            EditorApplication.delayCall += () =>
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode)
                {
                    return;
                }

                if (EditorPrefs.GetBool(BakePrefsKey, false))
                {
                    return;
                }

                BakeAllGraves();
                EditorPrefs.SetBool(BakePrefsKey, true);
            };
        }

        [MenuItem("Gravedigger2026/Dig/Bake All Grave Hit Shapes")]
        public static void BakeAllGravesMenu()
        {
            BakeAllGraves();
        }

        /// <summary>Batchmode entry: -executeMethod Gravedigger2026.Editor.Dig.DigHitShapeBaker.BakeAllGraves</summary>
        public static void BakeAllGraves()
        {
            var ok = 0;
            var fail = 0;
            for (var i = 0; i < QualityIds.Length; i++)
            {
                var path = $"{PrefabDigDir}/Grave_{QualityIds[i]}.prefab";
                if (BakePrefabPath(path))
                {
                    ok++;
                }
                else
                {
                    fail++;
                }
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[DigHitShapeBaker] Done. ok={ok} fail={fail}");
        }

        public static bool BakePrefabPath(string prefabPath)
        {
            var root = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                if (!BakeOnRoot(root, out var note))
                {
                    Debug.LogWarning($"[DigHitShapeBaker] {prefabPath}: {note}");
                    return false;
                }

                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                Debug.Log($"[DigHitShapeBaker] Baked {prefabPath} ({note})");
                return true;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        public static bool BakeOnRoot(GameObject root, out string note)
        {
            note = "ok";
            if (root == null)
            {
                note = "null root";
                return false;
            }

            var sr = root.GetComponentInChildren<SpriteRenderer>(true);
            if (sr == null || sr.sprite == null)
            {
                note = "no SpriteRenderer/sprite — leave empty DigHitShape";
                EnsureHitShape(root);
                return false;
            }

            var hit = EnsureHitShape(root);
            if (!TryExtractSpriteLocalPoints(sr.sprite, out var spriteLocal))
            {
                note = "outline extract failed — AABB fallback";
                spriteLocal = SpriteAabbCorners(sr.sprite);
            }

            var rootLocal = new List<Vector2>(spriteLocal.Count);
            var rootT = root.transform;
            var spriteT = sr.transform;
            for (var i = 0; i < spriteLocal.Count; i++)
            {
                var p = spriteLocal[i];
                var world = spriteT.TransformPoint(new Vector3(p.x, p.y, 0f));
                var local = rootT.InverseTransformPoint(world);
                rootLocal.Add(new Vector2(local.x, local.z));
            }

            var hull = BuildConvexHull(rootLocal);
            if (hull.Count < 3)
            {
                note = "hull < 3 verts";
                return false;
            }

            var simplified = SimplifyRadial(hull, MaxVerts);
            var radius = 0f;
            for (var i = 0; i < simplified.Count; i++)
            {
                radius = Mathf.Max(radius, simplified[i].magnitude);
            }

            hit.SetBaked(simplified.ToArray(), Mathf.Max(0.05f, radius));
            note = $"verts={simplified.Count} r={radius:0.###}";
            return true;
        }

        private static DigHitShape EnsureHitShape(GameObject root)
        {
            var hit = root.GetComponent<DigHitShape>();
            if (hit == null)
            {
                hit = root.AddComponent<DigHitShape>();
            }

            return hit;
        }

        private static bool TryExtractSpriteLocalPoints(Sprite sprite, out List<Vector2> points)
        {
            points = new List<Vector2>(64);
            var shapeCount = sprite.GetPhysicsShapeCount();
            if (shapeCount > 0)
            {
                var shape = new List<Vector2>(64);
                for (var s = 0; s < shapeCount; s++)
                {
                    shape.Clear();
                    sprite.GetPhysicsShape(s, shape);
                    points.AddRange(shape);
                }

                if (points.Count >= 3)
                {
                    return true;
                }
            }

            return TryAlphaOutline(sprite, points);
        }

        private static bool TryAlphaOutline(Sprite sprite, List<Vector2> into)
        {
            into.Clear();
            var path = AssetDatabase.GetAssetPath(sprite.texture);
            if (string.IsNullOrEmpty(path))
            {
                return false;
            }

            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            var restored = false;
            var wasReadable = true;
            if (importer != null)
            {
                wasReadable = importer.isReadable;
                if (!wasReadable)
                {
                    importer.isReadable = true;
                    importer.SaveAndReimport();
                    restored = true;
                    sprite = AssetDatabase.LoadAssetAtPath<Sprite>(AssetDatabase.GetAssetPath(sprite));
                }
            }

            try
            {
                var tex = sprite.texture;
                if (tex == null)
                {
                    return false;
                }

                var rect = sprite.textureRect;
                var ppu = sprite.pixelsPerUnit;
                var pivot = sprite.pivot;
                var minX = Mathf.FloorToInt(rect.x);
                var minY = Mathf.FloorToInt(rect.y);
                var w = Mathf.FloorToInt(rect.width);
                var h = Mathf.FloorToInt(rect.height);
                if (w < 2 || h < 2)
                {
                    return false;
                }

                for (var y = 0; y < h; y += AlphaSampleStep)
                {
                    for (var x = 0; x < w; x += AlphaSampleStep)
                    {
                        var c = tex.GetPixel(minX + x, minY + y);
                        if (c.a < AlphaThreshold)
                        {
                            continue;
                        }

                        if (!IsAlphaEdge(tex, minX, minY, w, h, x, y))
                        {
                            continue;
                        }

                        var lx = (x - pivot.x) / ppu;
                        var ly = (y - pivot.y) / ppu;
                        into.Add(new Vector2(lx, ly));
                    }
                }

                return into.Count >= 3;
            }
            finally
            {
                if (restored && importer != null)
                {
                    importer.isReadable = wasReadable;
                    importer.SaveAndReimport();
                }
            }
        }

        private static bool IsAlphaEdge(Texture2D tex, int minX, int minY, int w, int h, int x, int y)
        {
            for (var oy = -1; oy <= 1; oy++)
            {
                for (var ox = -1; ox <= 1; ox++)
                {
                    if (ox == 0 && oy == 0)
                    {
                        continue;
                    }

                    var nx = x + ox;
                    var ny = y + oy;
                    if (nx < 0 || ny < 0 || nx >= w || ny >= h)
                    {
                        return true;
                    }

                    if (tex.GetPixel(minX + nx, minY + ny).a < AlphaThreshold)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static List<Vector2> SpriteAabbCorners(Sprite sprite)
        {
            var b = sprite.bounds;
            return new List<Vector2>(4)
            {
                new Vector2(b.min.x, b.min.y),
                new Vector2(b.max.x, b.min.y),
                new Vector2(b.max.x, b.max.y),
                new Vector2(b.min.x, b.max.y)
            };
        }

        private static List<Vector2> BuildConvexHull(List<Vector2> points)
        {
            if (points == null || points.Count == 0)
            {
                return new List<Vector2>();
            }

            var pts = new List<Vector2>(points);
            pts.Sort((a, b) =>
            {
                var c = a.x.CompareTo(b.x);
                return c != 0 ? c : a.y.CompareTo(b.y);
            });

            // Dedup nearly identical
            for (var i = pts.Count - 1; i > 0; i--)
            {
                if ((pts[i] - pts[i - 1]).sqrMagnitude < 1e-10f)
                {
                    pts.RemoveAt(i);
                }
            }

            if (pts.Count <= 2)
            {
                return pts;
            }

            var lower = new List<Vector2>();
            for (var i = 0; i < pts.Count; i++)
            {
                while (lower.Count >= 2 && Cross(lower[lower.Count - 2], lower[lower.Count - 1], pts[i]) <= 0f)
                {
                    lower.RemoveAt(lower.Count - 1);
                }

                lower.Add(pts[i]);
            }

            var upper = new List<Vector2>();
            for (var i = pts.Count - 1; i >= 0; i--)
            {
                while (upper.Count >= 2 && Cross(upper[upper.Count - 2], upper[upper.Count - 1], pts[i]) <= 0f)
                {
                    upper.RemoveAt(upper.Count - 1);
                }

                upper.Add(pts[i]);
            }

            lower.RemoveAt(lower.Count - 1);
            upper.RemoveAt(upper.Count - 1);
            lower.AddRange(upper);
            return lower;
        }

        private static float Cross(Vector2 o, Vector2 a, Vector2 b)
        {
            return (a.x - o.x) * (b.y - o.y) - (a.y - o.y) * (b.x - o.x);
        }

        private static List<Vector2> SimplifyRadial(List<Vector2> hull, int maxVerts)
        {
            if (hull.Count <= maxVerts)
            {
                return new List<Vector2>(hull);
            }

            var centroid = Vector2.zero;
            for (var i = 0; i < hull.Count; i++)
            {
                centroid += hull[i];
            }

            centroid /= hull.Count;

            var indexed = new List<(float ang, Vector2 p)>(hull.Count);
            for (var i = 0; i < hull.Count; i++)
            {
                var d = hull[i] - centroid;
                indexed.Add((Mathf.Atan2(d.y, d.x), hull[i]));
            }

            indexed.Sort((a, b) => a.ang.CompareTo(b.ang));

            var result = new List<Vector2>(maxVerts);
            for (var i = 0; i < maxVerts; i++)
            {
                var t = i / (float)maxVerts;
                var targetAng = -Mathf.PI + t * (Mathf.PI * 2f);
                var best = 0;
                var bestDiff = float.MaxValue;
                for (var j = 0; j < indexed.Count; j++)
                {
                    var diff = Mathf.Abs(Mathf.DeltaAngle(targetAng * Mathf.Rad2Deg, indexed[j].ang * Mathf.Rad2Deg));
                    if (diff < bestDiff)
                    {
                        bestDiff = diff;
                        best = j;
                    }
                }

                var p = indexed[best].p;
                    if (result.Count == 0 || (result[result.Count - 1] - p).sqrMagnitude > 1e-8f)
                {
                    result.Add(p);
                }
            }

            if (result.Count >= 2 && (result[0] - result[result.Count - 1]).sqrMagnitude < 1e-8f)
            {
                result.RemoveAt(result.Count - 1);
            }

            if (result.Count < 3)
            {
                return new List<Vector2>(hull);
            }

            return result;
        }
    }
}
#endif
