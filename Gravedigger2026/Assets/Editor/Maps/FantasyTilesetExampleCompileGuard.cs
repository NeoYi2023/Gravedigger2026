#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Gravedigger2026.Editor.Maps
{
    /// <summary>
    /// Fantasy kingdom Tileset 1.1.0 Example scene scripts use Unity 6 Rigidbody2D/TMP APIs
    /// that do not compile on project Unity 2021.3. SmallScaleInt is gitignored, so this
    /// Editor guard writes a local asmdef with an unsatisfied defineConstraints to skip
    /// compiling that folder after import (SPEC_04 §2 / v0.83.06).
    /// </summary>
    public static class FantasyTilesetExampleCompileGuard
    {
        private const string ScriptsDir =
            "Assets/SmallScaleInt/Fantasy kingdom Tileset/Example scene/Scripts";
        private const string AsmdefAssetPath =
            ScriptsDir + "/SmallScaleInt.FantasyKingdomTileset.Example.asmdef";
        private const string AsmdefName = "SmallScaleInt.FantasyKingdomTileset.Example";
        private const string NeverDefine = "GRAVEDIGGER_NEVER_COMPILE_FANTASY_TILESET_EXAMPLE";

        private static readonly string ExpectedAsmdefJson =
            "{\n" +
            "    \"name\": \"" + AsmdefName + "\",\n" +
            "    \"rootNamespace\": \"\",\n" +
            "    \"references\": [],\n" +
            "    \"includePlatforms\": [],\n" +
            "    \"excludePlatforms\": [],\n" +
            "    \"allowUnsafeCode\": false,\n" +
            "    \"overrideReferences\": true,\n" +
            "    \"precompiledReferences\": [],\n" +
            "    \"autoReferenced\": false,\n" +
            "    \"defineConstraints\": [\n" +
            "        \"" + NeverDefine + "\"\n" +
            "    ],\n" +
            "    \"versionDefines\": [],\n" +
            "    \"noEngineReferences\": false\n" +
            "}\n";

        [InitializeOnLoadMethod]
        private static void AutoEnsureOnLoad()
        {
            EditorApplication.delayCall += EnsureCompileGuard;
        }

        [MenuItem("Gravedigger2026/Maps/Ensure Fantasy Tileset Example Compile Guard")]
        public static void EnsureCompileGuardMenu()
        {
            EnsureCompileGuard();
        }

        public static void EnsureCompileGuard()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            if (!AssetDatabase.IsValidFolder(ScriptsDir))
            {
                return;
            }

            var absolutePath = Path.GetFullPath(
                Path.Combine(Application.dataPath, "..", AsmdefAssetPath));
            if (File.Exists(absolutePath))
            {
                var existing = File.ReadAllText(absolutePath);
                if (IsExpectedContent(existing))
                {
                    return;
                }
            }

            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(absolutePath) ?? ScriptsDir);
                File.WriteAllText(absolutePath, ExpectedAsmdefJson);
                AssetDatabase.ImportAsset(AsmdefAssetPath);
                AssetDatabase.Refresh();
                Debug.Log(
                    "[FantasyTilesetExampleCompileGuard] Wrote stop-compile asmdef at " +
                    AsmdefAssetPath);
            }
            catch (System.Exception ex)
            {
                Debug.LogError(
                    "[FantasyTilesetExampleCompileGuard] Failed to write asmdef: " + ex);
            }
        }

        private static bool IsExpectedContent(string content)
        {
            if (string.IsNullOrEmpty(content))
            {
                return false;
            }

            return content.Contains("\"name\": \"" + AsmdefName + "\"")
                   && content.Contains("\"autoReferenced\": false")
                   && content.Contains("\"overrideReferences\": true")
                   && content.Contains("\"" + NeverDefine + "\"");
        }
    }

    /// <summary>
    /// Re-applies the compile guard when the vendor Tileset Example Scripts folder is imported.
    /// </summary>
    public sealed class FantasyTilesetExampleCompileGuardPostprocessor : AssetPostprocessor
    {
        private static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            if (!ShouldEnsure(importedAssets)
                && !ShouldEnsure(movedAssets)
                && !ShouldEnsure(movedFromAssetPaths))
            {
                return;
            }

            EditorApplication.delayCall += FantasyTilesetExampleCompileGuard.EnsureCompileGuard;
        }

        private static bool ShouldEnsure(string[] paths)
        {
            if (paths == null || paths.Length == 0)
            {
                return false;
            }

            const string marker = "Assets/SmallScaleInt/Fantasy kingdom Tileset/Example scene";
            for (var i = 0; i < paths.Length; i++)
            {
                var path = paths[i];
                if (!string.IsNullOrEmpty(path)
                    && path.StartsWith(marker, System.StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
#endif
