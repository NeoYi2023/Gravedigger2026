#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using SmallScaleInc.CharacterCreatorFantasy;
using UnityEditor;
using UnityEngine;

namespace Gravedigger2026.Editor.Art
{
    /// <summary>
    /// Repairs Character Creator bake folders where PNGs were sliced without
    /// TextureImporterType.Sprite, leaving zero .anim and empty BlendTree controllers.
    /// See SPEC_04 §15.3.
    /// </summary>
    public static class CharacterCreatorExportRepair
    {
        private const string MenuPath = "Tools/Gravedigger/Art/Repair Character Creator Export";
        private const int DefaultColumns = 15;
        private const int Rows = 8;

        [MenuItem(MenuPath)]
        private static void RepairFromSelectionOrDialog()
        {
            string folderAssetPath = GetSelectedCharacterFolder();
            if (string.IsNullOrEmpty(folderAssetPath))
            {
                folderAssetPath = EditorUtility.OpenFolderPanel(
                    "Select character bake folder (contains Idle.png etc.)",
                    Path.Combine(Application.dataPath, "Art/Characters"),
                    "");
                if (string.IsNullOrEmpty(folderAssetPath))
                    return;

                if (!folderAssetPath.Replace("\\", "/").StartsWith(Application.dataPath.Replace("\\", "/"), StringComparison.OrdinalIgnoreCase))
                {
                    EditorUtility.DisplayDialog("Repair failed", "Folder must be inside the project's Assets directory.", "OK");
                    return;
                }

                folderAssetPath = "Assets" + folderAssetPath.Substring(Application.dataPath.Length).Replace("\\", "/");
            }

            string resolved = ResolveCharacterBakeFolder(folderAssetPath) ?? folderAssetPath;
            if (!RepairFolder(resolved, DefaultColumns))
            {
                EditorUtility.DisplayDialog(
                    "Repair failed",
                    "Could not repair:\n" + resolved +
                    "\n\nTip: select the character folder that contains Idle.png (not Animation Clips).\nSee Console for details.",
                    "OK");
                return;
            }

            EditorUtility.DisplayDialog(
                "Repair complete",
                "Rebuilt clips/controller for:\n" + resolved,
                "OK");
        }

        [MenuItem(MenuPath, true)]
        private static bool RepairValidate()
        {
            return !EditorApplication.isPlayingOrWillChangePlaymode;
        }

        /// <summary>
        /// Batch entry for -executeMethod / tests.
        /// </summary>
        public static void RepairApp01Batch()
        {
            string[] targets =
            {
                "Assets/Art/Characters/Appearances/App_01",
                "Assets/SmallScaleInt/Character creator - Fantasy/Created Spritesheets/App_01"
            };

            bool any = false;
            foreach (string path in targets)
            {
                if (AssetDatabase.IsValidFolder(path))
                {
                    any |= RepairFolder(path, DefaultColumns);
                }
            }

            if (!any)
                Debug.LogError("[CharacterCreatorExportRepair] No App_01 folders repaired.");
        }

        public static bool RepairFolder(string folderAssetPath, int columns)
        {
            folderAssetPath = folderAssetPath.Replace("\\", "/").TrimEnd('/');
            folderAssetPath = ResolveCharacterBakeFolder(folderAssetPath);
            if (string.IsNullOrEmpty(folderAssetPath) || !AssetDatabase.IsValidFolder(folderAssetPath))
            {
                Debug.LogError("[CharacterCreatorExportRepair] Not a valid character bake folder (need top-level Idle.png etc.).");
                return false;
            }

            var sheetPaths = GetTopLevelPngSheets(folderAssetPath);
            if (sheetPaths.Length == 0)
            {
                Debug.LogError("[CharacterCreatorExportRepair] No top-level PNG spritesheets in " + folderAssetPath);
                return false;
            }

            string absoluteFolder = Path.GetFullPath(Path.Combine(Application.dataPath, "..", folderAssetPath));
            // Capture name before deleting the broken controller.
            string characterName = DeriveCharacterName(folderAssetPath, absoluteFolder);

            DeleteBrokenAnimArtifacts(folderAssetPath);

            foreach (string assetPath in sheetPaths)
            {
                if (!EnsureSlicedSpriteSheet(assetPath, columns))
                    return false;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // Verify sprites exist before clip generation.
            int spriteCount = 0;
            foreach (string assetPath in sheetPaths)
            {
                spriteCount += AssetDatabase.LoadAllAssetRepresentationsAtPath(assetPath).OfType<Sprite>().Count();
            }

            if (spriteCount == 0)
            {
                Debug.LogError(
                    "[CharacterCreatorExportRepair] Still zero Sprite sub-assets after reimport in " +
                    folderAssetPath);
                return false;
            }

            string[] absoluteSheets = sheetPaths
                .Select(p => Path.GetFullPath(Path.Combine(Application.dataPath, "..", p)))
                .ToArray();
            var host = new GameObject("~CharacterCreatorExportRepair");
            try
            {
                var builder = host.AddComponent<AnimatorClipBuilder>();
                builder.GenerateClipsForSpritesheets(absoluteSheets, absoluteFolder, columns, characterName);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(host);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            string clipsFolder = folderAssetPath + "/Animation Clips";
            int animCount = AssetDatabase.FindAssets("t:AnimationClip", new[] { clipsFolder }).Length;
            int controllerCount = AssetDatabase.FindAssets("t:AnimatorController", new[] { clipsFolder }).Length;
            Debug.Log(
                $"[CharacterCreatorExportRepair] Done {folderAssetPath}: sprites={spriteCount}, " +
                $".anim={animCount}, .controller={controllerCount}, name={characterName}");

            return animCount > 0 && controllerCount > 0;
        }

        private static string GetSelectedCharacterFolder()
        {
            foreach (var obj in Selection.objects)
            {
                string path = AssetDatabase.GetAssetPath(obj);
                if (string.IsNullOrEmpty(path))
                    continue;

                string start = AssetDatabase.IsValidFolder(path)
                    ? path.Replace("\\", "/")
                    : Path.GetDirectoryName(path)?.Replace("\\", "/");

                string resolved = ResolveCharacterBakeFolder(start);
                if (!string.IsNullOrEmpty(resolved))
                    return resolved;
            }

            return null;
        }

        /// <summary>
        /// Walks up from Animation Clips / a selected .controller until a folder with top-level PNGs is found.
        /// </summary>
        private static string ResolveCharacterBakeFolder(string startFolder)
        {
            if (string.IsNullOrEmpty(startFolder))
                return null;

            string current = startFolder.Replace("\\", "/").TrimEnd('/');
            while (!string.IsNullOrEmpty(current) && current.StartsWith("Assets", StringComparison.Ordinal))
            {
                if (AssetDatabase.IsValidFolder(current) && GetTopLevelPngSheets(current).Length > 0)
                    return current;

                int slash = current.LastIndexOf('/');
                if (slash <= 0)
                    break;
                current = current.Substring(0, slash);
            }

            return null;
        }

        private static string[] GetTopLevelPngSheets(string folderAssetPath)
        {
            folderAssetPath = folderAssetPath.Replace("\\", "/").TrimEnd('/');
            string[] pngGuids = AssetDatabase.FindAssets("t:Texture2D", new[] { folderAssetPath });
            return pngGuids
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(p => p.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                .Where(p => Path.GetDirectoryName(p)?.Replace("\\", "/") == folderAssetPath)
                .OrderBy(p => p, StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        private static void DeleteBrokenAnimArtifacts(string folderAssetPath)
        {
            string clipsFolder = folderAssetPath + "/Animation Clips";
            if (!AssetDatabase.IsValidFolder(clipsFolder))
                return;

            string[] oldGuids = AssetDatabase.FindAssets("", new[] { clipsFolder });
            foreach (string guid in oldGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (string.IsNullOrEmpty(path) || AssetDatabase.IsValidFolder(path))
                    continue;
                if (path.EndsWith(".anim", StringComparison.OrdinalIgnoreCase) ||
                    path.EndsWith(".controller", StringComparison.OrdinalIgnoreCase))
                {
                    AssetDatabase.DeleteAsset(path);
                }
            }

            // Remove empty per-animation subfolders left behind.
            string absoluteClips = Path.GetFullPath(Path.Combine(Application.dataPath, "..", clipsFolder));
            if (Directory.Exists(absoluteClips))
            {
                foreach (string sub in Directory.GetDirectories(absoluteClips))
                {
                    if (Directory.GetFiles(sub).Length == 0 && Directory.GetDirectories(sub).Length == 0)
                    {
                        string rel = ("Assets" + sub.Substring(Application.dataPath.Length)).Replace("\\", "/");
                        if (AssetDatabase.IsValidFolder(rel))
                            AssetDatabase.DeleteAsset(rel);
                    }
                }
            }
        }

        private static bool EnsureSlicedSpriteSheet(string assetPath, int columns)
        {
            var ti = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (ti == null)
            {
                Debug.LogError("[CharacterCreatorExportRepair] No TextureImporter: " + assetPath);
                return false;
            }

            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Multiple;
            ti.spritePixelsPerUnit = 100;
            ti.filterMode = FilterMode.Point;
            ti.textureCompression = TextureImporterCompression.Uncompressed;
            ti.mipmapEnabled = false;
            ti.alphaIsTransparency = true;
            ti.isReadable = false;

            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
            if (tex == null)
            {
                Debug.LogError("[CharacterCreatorExportRepair] Failed to load texture: " + assetPath);
                return false;
            }

            // Prefer existing rects if count matches; otherwise rebuild grid.
            var existing = ti.spritesheet;
            bool needRebuild = existing == null || existing.Length != columns * Rows;
            if (!needRebuild)
            {
                // Still rewrite names to {base}_{row}_{col} expected by AnimatorClipBuilder.
                string baseName = Path.GetFileNameWithoutExtension(assetPath);
                for (int i = 0; i < existing.Length; i++)
                {
                    int y = i / columns;
                    int x = i % columns;
                    existing[i].name = $"{baseName}_{y}_{x}";
                    existing[i].alignment = (int)SpriteAlignment.Center;
                    existing[i].pivot = new Vector2(0.5f, 0.5f);
                }

                ti.spritesheet = existing;
            }
            else
            {
                float sliceWidth = tex.width / (float)columns;
                float sliceHeight = tex.height / (float)Rows;
                var metaData = new SpriteMetaData[columns * Rows];
                string baseName = Path.GetFileNameWithoutExtension(assetPath);
                for (int y = 0; y < Rows; y++)
                {
                    for (int x = 0; x < columns; x++)
                    {
                        metaData[y * columns + x] = new SpriteMetaData
                        {
                            name = $"{baseName}_{y}_{x}",
                            rect = new Rect(x * sliceWidth, y * sliceHeight, sliceWidth, sliceHeight),
                            pivot = new Vector2(0.5f, 0.5f),
                            alignment = (int)SpriteAlignment.Center
                        };
                    }
                }

                ti.spritesheet = metaData;
            }

            EditorUtility.SetDirty(ti);
            ti.SaveAndReimport();
            return true;
        }

        private static string DeriveCharacterName(string folderAssetPath, string absoluteFolder)
        {
            string clipsDir = Path.Combine(absoluteFolder, "Animation Clips");
            if (Directory.Exists(clipsDir))
            {
                var existing = Directory.GetFiles(clipsDir, "*_animator.controller");
                if (existing.Length > 0)
                {
                    string file = Path.GetFileNameWithoutExtension(existing[0]);
                    if (file.EndsWith("_animator", StringComparison.OrdinalIgnoreCase))
                        return file.Substring(0, file.Length - "_animator".Length);
                }
            }

            string folderName = Path.GetFileName(folderAssetPath.TrimEnd('/'));
            string stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            return folderName + "_" + stamp;
        }
    }
}
#endif
