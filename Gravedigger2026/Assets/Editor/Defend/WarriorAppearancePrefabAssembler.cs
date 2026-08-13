#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Gravedigger2026.Editor.Defend
{
    /// <summary>
    /// Assembles Warriors/{AppearanceId}.prefab (SPEC_04 §15.2 / D-056 Approach B).
    /// Missing Prefab + Art ready → create root + Visual from Art.
    /// Existing Prefab → ensure Visual layout only (no Capsule overwrite).
    /// Refreshes Defend/UM warrior catalog bindings; does not call GenerateAll.
    /// </summary>
    public static class WarriorAppearancePrefabAssembler
    {
        private const string ArtAppearancesDir = "Assets/Art/Characters/Appearances";
        private const string PrefabDir = "Assets/Prefabs/Defend/Warriors";
        private const string MenuPath = "Tools/Gravedigger/Defend/Assemble Warrior Appearance Prefabs";
        private const int SortingOrder = 200;
        private const int FixedDirIndexSouth = 2;

        [MenuItem(MenuPath)]
        public static void AssembleAll()
        {
            if (!AssetDatabase.IsValidFolder(ArtAppearancesDir))
            {
                Debug.LogWarning($"[WarriorAppearancePrefabAssembler] Missing folder: {ArtAppearancesDir}");
            }

            EnsureFolder(PrefabDir);

            var created = 0;
            var artReadyIds = ListArtReadyAppearanceIds();
            var processedPaths = new HashSet<string>();
            for (var i = 0; i < artReadyIds.Count; i++)
            {
                var appearanceId = artReadyIds[i];
                var prefabPath = $"{PrefabDir}/{appearanceId}.prefab";
                if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) == null)
                {
                    if (TryAssembleFromArt(appearanceId))
                    {
                        created++;
                        processedPaths.Add(prefabPath);
                    }

                    continue;
                }

                if (EnsureVisualLayout(prefabPath))
                {
                    processedPaths.Add(prefabPath);
                }
            }

            var guids = AssetDatabase.FindAssets("t:Prefab", new[] { PrefabDir });
            var ensured = 0;
            for (var i = 0; i < guids.Length; i++)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[i]).Replace("\\", "/");
                if (processedPaths.Contains(path))
                {
                    ensured++;
                    continue;
                }

                if (EnsureVisualLayout(path))
                {
                    ensured++;
                }
            }

            AssetDatabase.SaveAssets();
            CloneApp02FallbackAppearances.RefreshWarriorCatalogBindings();
            Debug.Log(
                $"[WarriorAppearancePrefabAssembler] Created {created} From-Art Prefabs; ensured Visual on {ensured}/{guids.Length}; catalogs refreshed.");
        }

        /// <summary>Batch entry for Unity -executeMethod.</summary>
        public static void AssembleAllBatch()
        {
            AssembleAll();
        }

        /// <summary>
        /// Creates one AppearanceId Prefab when Art has AnimatorController + folder.
        /// Returns false if Art is not ready or Prefab already exists.
        /// </summary>
        public static bool TryAssembleFromArt(string appearanceId)
        {
            if (string.IsNullOrEmpty(appearanceId) || !HasArtReady(appearanceId))
            {
                return false;
            }

            var prefabPath = $"{PrefabDir}/{appearanceId}.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null)
            {
                return false;
            }

            var artDir = $"{ArtAppearancesDir}/{appearanceId}";
            var controller = FindAnimatorController(artDir);
            var sprite = FindIdleSprite(artDir);
            EnsureFolder(PrefabDir);

            var root = new GameObject(appearanceId);
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

        public static bool HasArtReady(string appearanceId)
        {
            if (string.IsNullOrEmpty(appearanceId))
            {
                return false;
            }

            var artDir = $"{ArtAppearancesDir}/{appearanceId}";
            return AssetDatabase.IsValidFolder(artDir) && FindAnimatorController(artDir) != null;
        }

        private static List<string> ListArtReadyAppearanceIds()
        {
            var result = new List<string>();
            if (!AssetDatabase.IsValidFolder(ArtAppearancesDir))
            {
                return result;
            }

            var subFolders = AssetDatabase.GetSubFolders(ArtAppearancesDir);
            for (var i = 0; i < subFolders.Length; i++)
            {
                var path = subFolders[i].Replace("\\", "/");
                var appearanceId = Path.GetFileName(path);
                if (string.IsNullOrEmpty(appearanceId))
                {
                    continue;
                }

                if (HasArtReady(appearanceId))
                {
                    result.Add(appearanceId);
                }
            }

            result.Sort(System.StringComparer.Ordinal);
            return result;
        }

        private static bool EnsureVisualLayout(string prefabPath)
        {
            var root = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                var visual = root.transform.Find("Visual");
                if (visual == null)
                {
                    var visualGo = new GameObject("Visual");
                    visual = visualGo.transform;
                    visual.SetParent(root.transform, false);
                }

                visual.localPosition = Vector3.zero;
                visual.localRotation = Quaternion.Euler(90f, 0f, 0f);
                visual.localScale = Vector3.one;

                MoveComponentIfNeeded<SpriteRenderer>(root, visual.gameObject);
                MoveComponentIfNeeded<Animator>(root, visual.gameObject);

                var sr = visual.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    // SPEC_04 §15.2 — above GroundTilemap (order 0).
                    sr.sortingOrder = 200;
                }

                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                return true;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void MoveComponentIfNeeded<T>(GameObject fromRoot, GameObject toVisual)
            where T : Component
        {
            var onVisual = toVisual.GetComponent<T>();
            var onRoot = fromRoot.GetComponent<T>();
            if (onVisual != null)
            {
                if (onRoot != null && onRoot.gameObject != toVisual)
                {
                    Object.DestroyImmediate(onRoot);
                }

                // Keep Visual rotation even when components already present.
                return;
            }

            if (onRoot == null)
            {
                toVisual.AddComponent<T>();
                return;
            }

            var copy = toVisual.AddComponent<T>();
            EditorUtility.CopySerialized(onRoot, copy);
            Object.DestroyImmediate(onRoot);
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
            if (controller == null)
            {
                return;
            }

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
