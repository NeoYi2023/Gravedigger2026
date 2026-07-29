#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SmallScaleInc.CharacterCreatorFantasy;
using UnityEditor;
using UnityEngine;

namespace Gravedigger2026.Editor.Art
{
    /// <summary>
    /// Repairs Character Creator bake folders where PNGs were sliced without
    /// TextureImporterType.Sprite, or sliced with NPOT-padded width (e.g. 2048→136.53
    /// cells instead of 1920→128), leaving empty clips or frame-drift sprites.
    /// See SPEC_04 §15.3.
    /// </summary>
    public static class CharacterCreatorExportRepair
    {
        private const string MenuPath = "Tools/Gravedigger/Art/Repair Character Creator Export";
        private const string MenuPathAll = "Tools/Gravedigger/Art/Repair All Character Creator Exports (Art/Characters)";
        private const string ArtCharactersRoot = "Assets/Art/Characters";
        private const int DefaultColumns = 15;
        private const int Rows = 8;
        private const float SliceWidthTolerance = 0.5f;

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

        [MenuItem(MenuPathAll)]
        private static void RepairAllUnderArtCharacters()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            if (!AssetDatabase.IsValidFolder(ArtCharactersRoot))
            {
                EditorUtility.DisplayDialog("Repair failed", "Missing folder:\n" + ArtCharactersRoot, "OK");
                return;
            }

            if (!EditorUtility.DisplayDialog(
                    "Repair all character exports?",
                    "Reslice spritesheets (force correct cell size) and rebuild .anim/.controller under:\n" +
                    ArtCharactersRoot +
                    "\n\nThis may take several minutes.",
                    "Repair all",
                    "Cancel"))
                return;

            int ok = RepairAllArtCharactersBatch();
            EditorUtility.DisplayDialog(
                "Repair complete",
                "Repaired " + ok + " character bake folder(s) under " + ArtCharactersRoot +
                ".\nSee Console for details.",
                "OK");
        }

        [MenuItem(MenuPathAll, true)]
        private static bool RepairAllValidate()
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

        /// <summary>
        /// Batch entry: repair every character bake folder under Art/Characters.
        /// </summary>
        public static int RepairAllArtCharactersBatch()
        {
            if (!AssetDatabase.IsValidFolder(ArtCharactersRoot))
            {
                Debug.LogError("[CharacterCreatorExportRepair] Missing " + ArtCharactersRoot);
                return 0;
            }

            var folders = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            string[] pngGuids = AssetDatabase.FindAssets("t:Texture2D", new[] { ArtCharactersRoot });
            foreach (string guid in pngGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (string.IsNullOrEmpty(path) || !path.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                    continue;

                string dir = Path.GetDirectoryName(path)?.Replace("\\", "/");
                string resolved = ResolveCharacterBakeFolder(dir);
                if (!string.IsNullOrEmpty(resolved))
                    folders.Add(resolved);
            }

            int ok = 0;
            int i = 0;
            foreach (string folder in folders.OrderBy(f => f, StringComparer.OrdinalIgnoreCase))
            {
                i++;
                EditorUtility.DisplayProgressBar(
                    "Repair Character Creator Exports",
                    folder + " (" + i + "/" + folders.Count + ")",
                    (float)i / folders.Count);
                try
                {
                    if (RepairFolder(folder, DefaultColumns))
                        ok++;
                }
                catch (Exception ex)
                {
                    Debug.LogError("[CharacterCreatorExportRepair] Exception on " + folder + ": " + ex);
                }
            }

            EditorUtility.ClearProgressBar();
            Debug.Log("[CharacterCreatorExportRepair] Batch done: ok=" + ok + " / " + folders.Count);
            return ok;
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
            ti.npotScale = TextureImporterNPOTScale.None;

            // Apply importer settings before reading dimensions (avoid NPOT-padded size).
            EditorUtility.SetDirty(ti);
            ti.SaveAndReimport();

            ti.GetSourceTextureWidthAndHeight(out int sourceWidth, out int sourceHeight);
            if (sourceWidth <= 0 || sourceHeight <= 0)
            {
                var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
                if (tex == null)
                {
                    Debug.LogError("[CharacterCreatorExportRepair] Failed to load texture: " + assetPath);
                    return false;
                }

                sourceWidth = tex.width;
                sourceHeight = tex.height;
            }

            float expectedSliceWidth = sourceWidth / (float)columns;
            float expectedSliceHeight = sourceHeight / (float)Rows;
            var existing = ti.spritesheet;
            bool countMismatch = existing == null || existing.Length != columns * Rows;
            bool sizeMismatch = false;
            if (!countMismatch)
            {
                float actualW = existing[0].rect.width;
                float actualH = existing[0].rect.height;
                sizeMismatch =
                    Mathf.Abs(actualW - expectedSliceWidth) > SliceWidthTolerance ||
                    Mathf.Abs(actualH - expectedSliceHeight) > SliceWidthTolerance ||
                    existing[0].rect.xMax > sourceWidth + SliceWidthTolerance ||
                    existing[0].rect.yMax > sourceHeight + SliceWidthTolerance;
            }

            bool needRebuild = countMismatch || sizeMismatch;
            string baseName = Path.GetFileNameWithoutExtension(assetPath);

            if (!needRebuild)
            {
                // Still rewrite names to {base}_{row}_{col} expected by AnimatorClipBuilder.
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
                if (sizeMismatch)
                {
                    Debug.LogWarning(
                        $"[CharacterCreatorExportRepair] Reslicing {assetPath}: " +
                        $"cell was {existing[0].rect.width}x{existing[0].rect.height}, " +
                        $"expected {expectedSliceWidth}x{expectedSliceHeight} " +
                        $"(source {sourceWidth}x{sourceHeight}).");
                }

                var metaData = new SpriteMetaData[columns * Rows];
                for (int y = 0; y < Rows; y++)
                {
                    for (int x = 0; x < columns; x++)
                    {
                        metaData[y * columns + x] = new SpriteMetaData
                        {
                            name = $"{baseName}_{y}_{x}",
                            rect = new Rect(
                                x * expectedSliceWidth,
                                y * expectedSliceHeight,
                                expectedSliceWidth,
                                expectedSliceHeight),
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
