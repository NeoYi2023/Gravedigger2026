using Gravedigger2026.UI;
using UnityEditor;
using UnityEngine;

namespace Gravedigger2026.EditorTools.Sandbox
{
    /// <summary>Writes SandboxRoot Prefab and the Resources runtime copy (UI-036).</summary>
    public static class SandboxAssetBuilder
    {
        private const string PrefabPath = "Assets/Prefabs/Sandbox/SandboxRoot.prefab";
        private const string ResourcesPrefabPath = "Assets/Resources/Prefabs/Sandbox/SandboxRoot.prefab";
        private const string MenuPath =
            "Gravedigger2026/Sandbox（沙盘）/Ensure SandboxRoot Prefab (UI-036)（确保沙盘预制体）";

        [MenuItem(MenuPath)]
        public static void EnsurePrefab()
        {
            EnsureFolder("Assets/Prefabs");
            EnsureFolder("Assets/Prefabs/Sandbox");
            EnsureFolder("Assets/Resources");
            EnsureFolder("Assets/Resources/Prefabs");
            EnsureFolder("Assets/Resources/Prefabs/Sandbox");

            var host = new GameObject("SandboxEnsureHost");
            var view = SandboxRuntimeFactory.Create(host.transform);
            if (view == null)
            {
                Object.DestroyImmediate(host);
                Debug.LogError("[Sandbox] Failed to build SandboxRoot.");
                return;
            }

            PrefabUtility.SaveAsPrefabAsset(view.gameObject, PrefabPath);
            PrefabUtility.SaveAsPrefabAsset(view.gameObject, ResourcesPrefabPath);
            Object.DestroyImmediate(host);
            AssetDatabase.SaveAssets();
            Debug.Log("[Sandbox] Ensured Prefab at " + PrefabPath + " and Resources copy.");
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            var slash = path.LastIndexOf('/');
            var parent = path.Substring(0, slash);
            var name = path.Substring(slash + 1);
            if (!AssetDatabase.IsValidFolder(parent))
            {
                EnsureFolder(parent);
            }

            AssetDatabase.CreateFolder(parent, name);
        }
    }
}
