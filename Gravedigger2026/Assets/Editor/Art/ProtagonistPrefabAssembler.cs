#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using Gravedigger2026.Gameplay.Dig;

namespace Gravedigger2026.Editor.Art
{
    /// <summary>
    /// Assembles Digger / BattleProtagonist game Prefabs from Art bake (SPEC_04 §15.2).
    /// Root + Visual (SpriteRenderer + Animator, localEuler 90,0,0); DirIndex default South=2.
    /// </summary>
    public static class ProtagonistPrefabAssembler
    {
        private const string DiggerArtDir = "Assets/Art/Characters/Protagonist/Digger";
        private const string BattleArtDir = "Assets/Art/Characters/Protagonist/BattleProtagonist";
        private const string DiggerPrefabPath = "Assets/Prefabs/Dig/Digger.prefab";
        private const string BattlePrefabPath = "Assets/Prefabs/Defend/BattleProtagonist.prefab";
        private const string MenuPath = "Tools/Gravedigger/Art/Assemble Protagonist Prefabs";
        private const int SortingOrder = 200;
        private const int FixedDirIndexSouth = 2;
        private const float DigObstacleRadius = 0.85f;

        [MenuItem(MenuPath)]
        public static void AssembleAll()
        {
            var digOk = AssembleDigger();
            var battleOk = AssembleBattleProtagonist();
            AssetDatabase.SaveAssets();
            Debug.Log(
                $"[ProtagonistPrefabAssembler] Digger={(digOk ? "OK" : "FAIL")} BattleProtagonist={(battleOk ? "OK" : "FAIL")}");
        }

        /// <summary>Batch entry for Unity -executeMethod.</summary>
        public static void AssembleAllBatch()
        {
            AssembleAll();
        }

        public static bool AssembleDigger()
        {
            EnsureFolder(Path.GetDirectoryName(DiggerPrefabPath)?.Replace("\\", "/"));
            var controller = FindAnimatorController(DiggerArtDir);
            var sprite = FindIdleSprite(DiggerArtDir);
            if (controller == null)
            {
                Debug.LogError($"[ProtagonistPrefabAssembler] Missing AnimatorController under {DiggerArtDir}");
                return false;
            }

            var root = new GameObject("Digger");
            try
            {
                var obstacle = root.AddComponent<DigObstacleRadius>();
                var oso = new SerializedObject(obstacle);
                var radiusProp = oso.FindProperty("_radius");
                if (radiusProp != null)
                {
                    radiusProp.floatValue = DigObstacleRadius;
                    oso.ApplyModifiedPropertiesWithoutUndo();
                }

                root.AddComponent<DigDiggerView>();
                BuildVisualChild(root, controller, sprite, FixedDirIndexSouth);

                PrefabUtility.SaveAsPrefabAsset(root, DiggerPrefabPath);
                return true;
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        public static bool AssembleBattleProtagonist()
        {
            EnsureFolder(Path.GetDirectoryName(BattlePrefabPath)?.Replace("\\", "/"));
            var controller = FindAnimatorController(BattleArtDir);
            var sprite = FindIdleSprite(BattleArtDir);
            if (controller == null)
            {
                Debug.LogError($"[ProtagonistPrefabAssembler] Missing AnimatorController under {BattleArtDir}");
                return false;
            }

            var root = new GameObject("BattleProtagonist");
            try
            {
                BuildVisualChild(root, controller, sprite, FixedDirIndexSouth);
                PrefabUtility.SaveAsPrefabAsset(root, BattlePrefabPath);
                return true;
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static void BuildVisualChild(
            GameObject root,
            RuntimeAnimatorController controller,
            Sprite sprite,
            int dirIndex)
        {
            var visualGo = new GameObject("Visual");
            var visual = visualGo.transform;
            visual.SetParent(root.transform, false);
            visual.localPosition = Vector3.zero;
            visual.localRotation = Quaternion.Euler(90f, 0f, 0f);
            visual.localScale = Vector3.one;

            var sr = visualGo.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = SortingOrder;

            var animator = visualGo.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            ApplyControllerDefaultDirIndex(controller, dirIndex);

            // Wire DigDiggerView._animator when present on root.
            var digView = root.GetComponent<DigDiggerView>();
            if (digView != null)
            {
                var so = new SerializedObject(digView);
                var animProp = so.FindProperty("_animator");
                if (animProp != null)
                {
                    animProp.objectReferenceValue = animator;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }

                var dirProp = so.FindProperty("_fixedDirIndex");
                if (dirProp != null)
                {
                    dirProp.intValue = dirIndex;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }
            }
        }

        private static void ApplyControllerDefaultDirIndex(RuntimeAnimatorController controller, int dirIndex)
        {
            var path = AssetDatabase.GetAssetPath(controller);
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            var ac = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            if (ac == null)
            {
                return;
            }

            var so = new SerializedObject(ac);
            var paramsProp = so.FindProperty("m_AnimatorParameters");
            if (paramsProp == null || !paramsProp.isArray)
            {
                return;
            }

            for (var i = 0; i < paramsProp.arraySize; i++)
            {
                var p = paramsProp.GetArrayElementAtIndex(i);
                var nameProp = p.FindPropertyRelative("m_Name");
                var typeProp = p.FindPropertyRelative("m_Type");
                if (nameProp == null || typeProp == null)
                {
                    continue;
                }

                // AnimatorControllerParameterType.Int == 3
                if (nameProp.stringValue != "DirIndex" || typeProp.intValue != 3)
                {
                    continue;
                }

                var defaultInt = p.FindPropertyRelative("m_DefaultInt");
                if (defaultInt != null)
                {
                    defaultInt.intValue = dirIndex;
                    so.ApplyModifiedPropertiesWithoutUndo();
                    EditorUtility.SetDirty(ac);
                }

                return;
            }
        }

        private static AnimatorController FindAnimatorController(string artDir)
        {
            var guids = AssetDatabase.FindAssets("t:AnimatorController", new[] { artDir });
            for (var i = 0; i < guids.Length; i++)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[i]).Replace("\\", "/");
                var ac = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
                if (ac != null)
                {
                    return ac;
                }
            }

            return null;
        }

        private static Sprite FindIdleSprite(string artDir)
        {
            var idlePath = $"{artDir}/Idle.png".Replace("\\", "/");
            var sprites = AssetDatabase.LoadAllAssetsAtPath(idlePath);
            Sprite first = null;
            if (sprites != null)
            {
                for (var i = 0; i < sprites.Length; i++)
                {
                    if (sprites[i] is Sprite s)
                    {
                        // Prefer a south-facing frame name if present; else first.
                        if (s.name.IndexOf("_S", System.StringComparison.OrdinalIgnoreCase) >= 0
                            || s.name.EndsWith("S", System.StringComparison.Ordinal))
                        {
                            return s;
                        }

                        if (first == null)
                        {
                            first = s;
                        }
                    }
                }
            }

            if (first != null)
            {
                return first;
            }

            // Fallback: any Sprite under art dir.
            var guids = AssetDatabase.FindAssets("t:Sprite", new[] { artDir });
            if (guids.Length > 0)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[0]).Replace("\\", "/");
                var all = AssetDatabase.LoadAllAssetsAtPath(path);
                for (var i = 0; i < all.Length; i++)
                {
                    if (all[i] is Sprite s)
                    {
                        return s;
                    }
                }
            }

            return null;
        }

        private static void EnsureFolder(string folder)
        {
            if (string.IsNullOrEmpty(folder) || AssetDatabase.IsValidFolder(folder))
            {
                return;
            }

            var parts = folder.Split('/');
            var current = parts[0];
            for (var i = 1; i < parts.Length; i++)
            {
                var next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }

                current = next;
            }
        }
    }
}
#endif
