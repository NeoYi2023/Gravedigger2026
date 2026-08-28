#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Gravedigger2026.Editor.Art
{
    /// <summary>
    /// Wires optional Die2 Trigger + sub-state-machine for monster revive reverse-play (SPEC_04 §15.5 D-074).
    /// </summary>
    public static class MonsterDie2AnimatorWirer
    {
        private const string MenuPath = "Tools/Gravedigger/Art/Wire Monster Die2 Animators";
        private const string MonstersRoot = "Assets/Art/Characters/Monsters";
        private const string Die2Trigger = "Die2";
        private const string DirIndexParam = "DirIndex";
        private const string DieTrigger = "Die";

        [MenuItem(MenuPath)]
        private static void WireFromMenu()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            var report = WireAllBatch();
            EditorUtility.DisplayDialog("Wire Monster Die2", report, "OK");
        }

        [MenuItem(MenuPath, true)]
        private static bool WireValidate()
        {
            return !EditorApplication.isPlayingOrWillChangePlaymode;
        }

        /// <summary>Batch entry for -executeMethod.</summary>
        public static void WireAllBatchExecute()
        {
            Debug.Log(WireAllBatch());
        }

        public static string WireAllBatch()
        {
            var report = new StringBuilder();
            var wired = 0;
            var skipped = 0;
            if (!AssetDatabase.IsValidFolder(MonstersRoot))
            {
                return "Monsters root not found.";
            }

            var modelDirs = AssetDatabase.GetSubFolders(MonstersRoot);
            for (var i = 0; i < modelDirs.Length; i++)
            {
                var artDir = modelDirs[i];
                var modelId = Path.GetFileName(artDir.Replace('\\', '/'));
                if (WireModel(modelId, artDir, report))
                {
                    wired++;
                }
                else
                {
                    skipped++;
                }
            }

            AssetDatabase.SaveAssets();
            report.Insert(0, $"Wired={wired} Skipped={skipped}\n");
            return report.ToString();
        }

        private static bool WireModel(string modelId, string artDir, StringBuilder report)
        {
            var die2Folder = artDir + "/Animation Clips/Die2";
            if (!AssetDatabase.IsValidFolder(die2Folder))
            {
                return false;
            }

            var controller = FindAnimatorController(artDir);
            if (controller == null)
            {
                report.AppendLine($"{modelId}: no AnimatorController.");
                return false;
            }

            var die2Clips = LoadDie2Clips(die2Folder);
            if (die2Clips.Count == 0)
            {
                report.AppendLine($"{modelId}: Die2 folder empty.");
                return false;
            }

            if (FindChildStateMachine(controller.layers[0].stateMachine, "Die2") != null)
            {
                report.AppendLine($"{modelId}: Die2 already wired.");
                return false;
            }

            var dieSm = FindChildStateMachine(controller.layers[0].stateMachine, "Die");
            if (dieSm == null)
            {
                report.AppendLine($"{modelId}: Die sub-state-machine missing.");
                return false;
            }

            EnsureTriggerParameter(controller, Die2Trigger);
            var root = controller.layers[0].stateMachine;
            var idleState = root.defaultState;
            var die2Sm = root.AddStateMachine("Die2", dieSm.parentStateMachinePosition + new Vector3(220f, 0f, 0f));
            var die2States = new Dictionary<string, AnimatorState>();

            foreach (var child in dieSm.states)
            {
                var dieState = child.state;
                if (dieState == null || !dieState.name.StartsWith("Die_"))
                {
                    continue;
                }

                var suffix = dieState.name.Substring("Die_".Length);
                var die2Name = "Die2_" + suffix;
                if (!die2Clips.TryGetValue(die2Name, out var clip))
                {
                    continue;
                }

                var die2State = die2Sm.AddState(die2Name, child.position);
                die2State.motion = clip;
                die2States[die2Name] = die2State;

                if (idleState != null)
                {
                    var exit = die2State.AddTransition(idleState);
                    exit.hasExitTime = true;
                    exit.exitTime = 1f;
                    exit.duration = 0f;
                    exit.hasFixedDuration = true;
                }
            }

            if (die2States.Count == 0)
            {
                Object.DestroyImmediate(die2Sm, true);
                report.AppendLine($"{modelId}: no matching Die2 clips for Die states.");
                return false;
            }

            var addedTransitions = 0;
            foreach (var transition in root.anyStateTransitions)
            {
                if (transition == null || transition.destinationState == null)
                {
                    continue;
                }

                if (!TryParseDieTransition(transition, out var dirIndex, out var dieStateName))
                {
                    continue;
                }

                var die2Name = dieStateName.Replace("Die_", "Die2_");
                if (!die2States.TryGetValue(die2Name, out var die2State))
                {
                    continue;
                }

                if (HasAnyStateTransition(root, Die2Trigger, dirIndex, die2State))
                {
                    continue;
                }

                var die2Transition = root.AddAnyStateTransition(die2State);
                die2Transition.AddCondition(AnimatorConditionMode.If, 0f, Die2Trigger);
                die2Transition.AddCondition(AnimatorConditionMode.Equals, dirIndex, DirIndexParam);
                die2Transition.duration = transition.duration;
                die2Transition.hasExitTime = false;
                die2Transition.canTransitionToSelf = transition.canTransitionToSelf;
                addedTransitions++;
            }

            EditorUtility.SetDirty(controller);
            report.AppendLine(
                $"{modelId}: Die2 states={die2States.Count} AnyState transitions={addedTransitions}");
            return true;
        }

        private static bool TryParseDieTransition(
            AnimatorStateTransition transition,
            out int dirIndex,
            out string dieStateName)
        {
            dirIndex = -1;
            dieStateName = transition.destinationState.name;
            if (!dieStateName.StartsWith("Die_"))
            {
                return false;
            }

            var hasDie = false;
            for (var i = 0; i < transition.conditions.Length; i++)
            {
                var cond = transition.conditions[i];
                if (cond.parameter == DieTrigger && cond.mode == AnimatorConditionMode.If)
                {
                    hasDie = true;
                }
                else if (cond.parameter == DirIndexParam && cond.mode == AnimatorConditionMode.Equals)
                {
                    dirIndex = (int)cond.threshold;
                }
            }

            return hasDie && dirIndex >= 0;
        }

        private static bool HasAnyStateTransition(
            AnimatorStateMachine root,
            string triggerName,
            int dirIndex,
            AnimatorState destination)
        {
            foreach (var transition in root.anyStateTransitions)
            {
                if (transition == null || transition.destinationState != destination)
                {
                    continue;
                }

                var hasTrigger = false;
                var hasDir = false;
                for (var i = 0; i < transition.conditions.Length; i++)
                {
                    var cond = transition.conditions[i];
                    if (cond.parameter == triggerName && cond.mode == AnimatorConditionMode.If)
                    {
                        hasTrigger = true;
                    }
                    else if (cond.parameter == DirIndexParam
                             && cond.mode == AnimatorConditionMode.Equals
                             && (int)cond.threshold == dirIndex)
                    {
                        hasDir = true;
                    }
                }

                if (hasTrigger && hasDir)
                {
                    return true;
                }
            }

            return false;
        }

        private static void EnsureTriggerParameter(AnimatorController controller, string triggerName)
        {
            foreach (var p in controller.parameters)
            {
                if (p.name == triggerName && p.type == AnimatorControllerParameterType.Trigger)
                {
                    return;
                }
            }

            controller.AddParameter(triggerName, AnimatorControllerParameterType.Trigger);
        }

        private static Dictionary<string, AnimationClip> LoadDie2Clips(string die2Folder)
        {
            var map = new Dictionary<string, AnimationClip>();
            var guids = AssetDatabase.FindAssets("t:AnimationClip", new[] { die2Folder });
            for (var i = 0; i < guids.Length; i++)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[i]);
                var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
                if (clip != null)
                {
                    map[clip.name] = clip;
                }
            }

            return map;
        }

        private static AnimatorStateMachine FindChildStateMachine(
            AnimatorStateMachine parent,
            string name)
        {
            foreach (var child in parent.stateMachines)
            {
                if (child.stateMachine != null && child.stateMachine.name == name)
                {
                    return child.stateMachine;
                }
            }

            return null;
        }

        private static AnimatorController FindAnimatorController(string artDir)
        {
            var guids = AssetDatabase.FindAssets("t:AnimatorController", new[] { artDir });
            for (var i = 0; i < guids.Length; i++)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[i]);
                var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
                if (controller != null)
                {
                    return controller;
                }
            }

            return null;
        }
    }
}
#endif
