#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Gravedigger2026.Editor.Art
{
    /// <summary>
    /// Assembles Defend Monsters/{ModelId}.prefab from Art bake (SPEC_04 §15.2).
    /// Root + Visual (SpriteRenderer + Animator, localEuler 90,0,0); removes placeholder Body Mesh.
    /// Only ModelIds with Art Controller are assembled; others keep temp cubes.
    /// </summary>
    public static class MonsterModelPrefabAssembler
    {
        private const string ArtMonstersDir = "Assets/Art/Characters/Monsters";
        private const string PrefabMonstersDir = "Assets/Prefabs/Defend/Monsters";
        private const string MenuPath = "Tools/Gravedigger/Art/Assemble Monster Model Prefabs";
        private const int SortingOrder = 200;
        private const int FixedDirIndexSouth = 2;

        [MenuItem(MenuPath)]
        public static void AssembleAll()
        {
            if (!AssetDatabase.IsValidFolder(ArtMonstersDir))
            {
                Debug.LogWarning($"[MonsterModelPrefabAssembler] Missing folder: {ArtMonstersDir}");
                return;
            }

            EnsureFolder(PrefabMonstersDir);
            var modelIds = ListArtReadyModelIds();
            var ok = 0;
            for (var i = 0; i < modelIds.Count; i++)
            {
                if (TryAssemble(modelIds[i]))
                {
                    ok++;
                }
            }

            AssetDatabase.SaveAssets();
            Debug.Log(
                $"[MonsterModelPrefabAssembler] Assembled {ok}/{modelIds.Count} Art-ready ModelIds under {PrefabMonstersDir}.");
        }

        /// <summary>Batch entry for Unity -executeMethod.</summary>
        public static void AssembleAllBatch()
        {
            AssembleAll();
        }

        /// <summary>
        /// Assembles one ModelId when Art has AnimatorController. Returns false if Art not ready.
        /// </summary>
        public static bool TryAssemble(string modelId)
        {
            if (string.IsNullOrEmpty(modelId))
            {
                return false;
            }

            var artDir = $"{ArtMonstersDir}/{modelId}";
            if (!AssetDatabase.IsValidFolder(artDir))
            {
                return false;
            }

            var controller = FindAnimatorController(artDir);
            if (controller == null)
            {
                return false;
            }

            var sprite = FindIdleSprite(artDir);
            EnsureFolder(PrefabMonstersDir);
            var prefabPath = $"{PrefabMonstersDir}/{modelId}.prefab";

            var root = new GameObject(modelId);
            try
            {
                BuildVisualChild(root, controller, sprite, FixedDirIndexSouth);
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                return true;
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        public static bool HasArtReady(string modelId)
        {
            if (string.IsNullOrEmpty(modelId))
            {
                return false;
            }

            var artDir = $"{ArtMonstersDir}/{modelId}";
            return AssetDatabase.IsValidFolder(artDir) && FindAnimatorController(artDir) != null;
        }

        private static List<string> ListArtReadyModelIds()
        {
            var result = new List<string>();
            var subFolders = AssetDatabase.GetSubFolders(ArtMonstersDir);
            for (var i = 0; i < subFolders.Length; i++)
            {
                var path = subFolders[i].Replace("\\", "/");
                var modelId = Path.GetFileName(path);
                if (string.IsNullOrEmpty(modelId))
                {
                    continue;
                }

                if (HasArtReady(modelId))
                {
                    result.Add(modelId);
                }
            }

            result.Sort(System.StringComparer.Ordinal);
            return result;
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
