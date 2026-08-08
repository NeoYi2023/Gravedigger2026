#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Gravedigger2026.Editor.Art
{
    /// <summary>
    /// Forces IdleBT/RunBT (and Direction-blend locomotion trees) child thresholds to
    /// SPEC_04 §15.5 DirIndex order: 0E 1W 2S 3N 4NE 5NW 6SE 7SW.
    /// </summary>
    public static class DirectionBlendLayoutNormalizer
    {
        private const string MenuPath =
            "Tools/Gravedigger/Art/Normalize Direction Blend Layout (Appearances+Monsters)";
        private const string AppearancesRoot = "Assets/Art/Characters/Appearances";
        private const string MonstersRoot = "Assets/Art/Characters/Monsters";
        private const string DirectionParam = "Direction";

        /// <summary>DirIndex order suffixes (longest first for matching).</summary>
        private static readonly string[] DirSuffixLongestFirst =
        {
            "_NE", "_NW", "_SE", "_SW", "_E", "_W", "_S", "_N"
        };

        private static readonly Dictionary<string, int> SuffixToDirIndex =
            new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
            {
                { "_E", 0 },
                { "_W", 1 },
                { "_S", 2 },
                { "_N", 3 },
                { "_NE", 4 },
                { "_NW", 5 },
                { "_SE", 6 },
                { "_SW", 7 }
            };

        [MenuItem(MenuPath)]
        private static void NormalizeFromMenu()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            if (!EditorUtility.DisplayDialog(
                    "Normalize Direction blend layout?",
                    "Reorder IdleBT/RunBT (and Direction-blend locomotion) children to DirIndex order " +
                    "(0E…7SW) under:\n" + AppearancesRoot + "\n" + MonstersRoot +
                    "\n\nAfter this, run Assemble Warrior/Monster Prefabs if Prefabs need refresh.",
                    "Normalize",
                    "Cancel"))
            {
                return;
            }

            var report = NormalizeAllBatch();
            EditorUtility.DisplayDialog(
                "Normalize complete",
                report,
                "OK");
        }

        [MenuItem(MenuPath, true)]
        private static bool NormalizeValidate()
        {
            return !EditorApplication.isPlayingOrWillChangePlaymode;
        }

        /// <summary>Batch entry for -executeMethod / tests.</summary>
        public static string NormalizeAllBatch()
        {
            var guids = AssetDatabase.FindAssets(
                "t:AnimatorController",
                new[] { AppearancesRoot, MonstersRoot });
            var changedControllers = 0;
            var changedTrees = 0;
            var skippedTrees = 0;
            var warnings = new StringBuilder();

            for (var i = 0; i < guids.Length; i++)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[i]);
                var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
                if (controller == null)
                {
                    continue;
                }

                var treeChanged = 0;
                var treeSkipped = 0;
                if (NormalizeController(controller, path, warnings, ref treeChanged, ref treeSkipped))
                {
                    changedControllers++;
                    EditorUtility.SetDirty(controller);
                }

                changedTrees += treeChanged;
                skippedTrees += treeSkipped;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            var summary =
                $"Controllers changed: {changedControllers}/{guids.Length}; " +
                $"BlendTrees rewritten: {changedTrees}; skipped: {skippedTrees}.";
            Debug.Log("[DirectionBlendLayoutNormalizer] " + summary);
            if (warnings.Length > 0)
            {
                Debug.LogWarning("[DirectionBlendLayoutNormalizer] Warnings:\n" + warnings);
            }

            return summary + (warnings.Length > 0 ? "\nSee Console for warnings." : string.Empty);
        }

        private static bool NormalizeController(
            AnimatorController controller,
            string path,
            StringBuilder warnings,
            ref int treeChanged,
            ref int treeSkipped)
        {
            var any = false;
            var seen = new HashSet<BlendTree>();
            var layers = controller.layers;
            for (var li = 0; li < layers.Length; li++)
            {
                var sm = layers[li].stateMachine;
                if (sm == null)
                {
                    continue;
                }

                if (NormalizeStateMachine(sm, path, warnings, seen, ref treeChanged, ref treeSkipped))
                {
                    any = true;
                }
            }

            return any;
        }

        private static bool NormalizeStateMachine(
            AnimatorStateMachine sm,
            string path,
            StringBuilder warnings,
            HashSet<BlendTree> seen,
            ref int treeChanged,
            ref int treeSkipped)
        {
            var any = false;
            var states = sm.states;
            for (var i = 0; i < states.Length; i++)
            {
                if (NormalizeMotion(states[i].state.motion, path, warnings, seen, ref treeChanged, ref treeSkipped))
                {
                    any = true;
                }
            }

            var children = sm.stateMachines;
            for (var i = 0; i < children.Length; i++)
            {
                if (children[i].stateMachine != null &&
                    NormalizeStateMachine(
                        children[i].stateMachine,
                        path,
                        warnings,
                        seen,
                        ref treeChanged,
                        ref treeSkipped))
                {
                    any = true;
                }
            }

            return any;
        }

        private static bool NormalizeMotion(
            Motion motion,
            string path,
            StringBuilder warnings,
            HashSet<BlendTree> seen,
            ref int treeChanged,
            ref int treeSkipped)
        {
            var tree = motion as BlendTree;
            if (tree == null || !seen.Add(tree))
            {
                return false;
            }

            var any = false;
            if (IsDirectionLocomotionTree(tree))
            {
                if (TryNormalizeDirectionTree(tree, path, warnings))
                {
                    treeChanged++;
                    any = true;
                }
                else
                {
                    treeSkipped++;
                }
            }

            var children = tree.children;
            for (var i = 0; i < children.Length; i++)
            {
                if (NormalizeMotion(children[i].motion, path, warnings, seen, ref treeChanged, ref treeSkipped))
                {
                    any = true;
                }
            }

            return any;
        }

        private static bool IsDirectionLocomotionTree(BlendTree tree)
        {
            if (tree == null)
            {
                return false;
            }

            if (!string.Equals(tree.blendParameter, DirectionParam, StringComparison.Ordinal))
            {
                return false;
            }

            // IdleBT / RunBT / WalkBT / *BT_Tree locomotion with Direction axis.
            var name = tree.name ?? string.Empty;
            return name.IndexOf("BT", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool TryNormalizeDirectionTree(BlendTree tree, string path, StringBuilder warnings)
        {
            var children = tree.children;
            if (children == null || children.Length == 0)
            {
                return false;
            }

            var slots = new ChildMotion[8];
            var filled = new bool[8];
            var unresolved = 0;

            for (var i = 0; i < children.Length; i++)
            {
                var child = children[i];
                var clipName = ResolveMotionName(child.motion);
                if (!TryParseDirIndex(clipName, out var dirIndex))
                {
                    unresolved++;
                    warnings.AppendLine(
                        path + " / " + tree.name + ": cannot parse direction from '" + clipName + "'");
                    continue;
                }

                if (filled[dirIndex])
                {
                    warnings.AppendLine(
                        path + " / " + tree.name + ": duplicate dir " + dirIndex + " ('" + clipName + "')");
                    continue;
                }

                child.threshold = dirIndex;
                child.timeScale = child.timeScale <= 0f ? 1f : child.timeScale;
                slots[dirIndex] = child;
                filled[dirIndex] = true;
            }

            var count = 0;
            for (var d = 0; d < 8; d++)
            {
                if (filled[d])
                {
                    count++;
                }
            }

            if (count == 0)
            {
                return false;
            }

            if (count < 8)
            {
                warnings.AppendLine(
                    path + " / " + tree.name + ": only " + count + "/8 directions resolved" +
                    (unresolved > 0 ? " (" + unresolved + " unresolved)" : string.Empty));
            }

            // Already in DirIndex order with matching thresholds?
            if (children.Length == count && IsAlreadyNormalized(children, filled))
            {
                return false;
            }

            var ordered = new ChildMotion[count];
            var write = 0;
            for (var d = 0; d < 8; d++)
            {
                if (!filled[d])
                {
                    continue;
                }

                ordered[write++] = slots[d];
            }

            tree.useAutomaticThresholds = false;
            tree.children = ordered;
            tree.minThreshold = 0f;
            tree.maxThreshold = 7f;
            return true;
        }

        private static bool IsAlreadyNormalized(ChildMotion[] children, bool[] filled)
        {
            var expected = 0;
            for (var i = 0; i < children.Length; i++)
            {
                while (expected < 8 && !filled[expected])
                {
                    expected++;
                }

                if (expected >= 8)
                {
                    return false;
                }

                if (!Mathf.Approximately(children[i].threshold, expected))
                {
                    return false;
                }

                var name = ResolveMotionName(children[i].motion);
                if (!TryParseDirIndex(name, out var dir) || dir != expected)
                {
                    return false;
                }

                expected++;
            }

            return true;
        }

        private static string ResolveMotionName(Motion motion)
        {
            if (motion == null)
            {
                return string.Empty;
            }

            return motion.name ?? string.Empty;
        }

        private static bool TryParseDirIndex(string clipName, out int dirIndex)
        {
            dirIndex = -1;
            if (string.IsNullOrEmpty(clipName))
            {
                return false;
            }

            // Strip common trailing noise; match longest suffix first (_NE before _N).
            for (var i = 0; i < DirSuffixLongestFirst.Length; i++)
            {
                var suffix = DirSuffixLongestFirst[i];
                if (clipName.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                {
                    dirIndex = SuffixToDirIndex[suffix];
                    return true;
                }
            }

            return false;
        }
    }
}
#endif
