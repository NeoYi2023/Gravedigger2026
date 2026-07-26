#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Gravedigger2026.Editor.Defend
{
    /// <summary>
    /// Ensures Warriors/{AppearanceId}.prefab use root + Visual child (SPEC_04 §15.2).
    /// Visual holds SpriteRenderer + Animator at localEuler (90,0,0) for top-down cameras.
    /// </summary>
    public static class WarriorAppearancePrefabAssembler
    {
        private const string PrefabDir = "Assets/Prefabs/Defend/Warriors";
        private const string MenuPath = "Tools/Gravedigger/Defend/Assemble Warrior Appearance Prefabs";

        [MenuItem(MenuPath)]
        public static void AssembleAll()
        {
            if (!AssetDatabase.IsValidFolder(PrefabDir))
            {
                Debug.LogError($"[WarriorAppearancePrefabAssembler] Missing folder: {PrefabDir}");
                return;
            }

            var guids = AssetDatabase.FindAssets("t:Prefab", new[] { PrefabDir });
            var updated = 0;
            for (var i = 0; i < guids.Length; i++)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (AssembleOne(path))
                {
                    updated++;
                }
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[WarriorAppearancePrefabAssembler] Updated {updated}/{guids.Length} Prefabs under {PrefabDir}.");
        }

        /// <summary>Batch entry for Unity -executeMethod.</summary>
        public static void AssembleAllBatch()
        {
            AssembleAll();
        }

        private static bool AssembleOne(string prefabPath)
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
    }
}
#endif
