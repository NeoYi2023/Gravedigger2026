#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using Gravedigger2026.Gameplay.Audio;
using Gravedigger2026.Meta;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Gravedigger2026.Editor.Audio
{
    /// <summary>
    /// Builds BgmClipCatalog and wires MetaShellRoot / Boot (SPEC_04 §9.29).
    /// </summary>
    public static class BgmAssetBuilder
    {
        private const string SettingsDir = "Assets/Settings/Audio";
        private const string CatalogPath = SettingsDir + "/BgmClipCatalog.asset";
        private const string BgmFolder = "Assets/Art/Audio/BGM";
        private const string MetaRootPath = "Assets/Prefabs/Meta/MetaShellRoot.prefab";
        private const string BootScenePath = "Assets/Scenes/Boot.unity";

        [MenuItem("Gravedigger2026/Audio/Generate BGM Catalog + Wire Meta")]
        public static void GenerateAll()
        {
            EnsureCatalog();
            WireMetaShell();
            WireBootScene();
            AssetDatabase.SaveAssets();
            Debug.Log("[BgmAssetBuilder] BGM catalog generated and Meta/Boot wired.");
        }

        /// <summary>Batchmode: -executeMethod Gravedigger2026.Editor.Audio.BgmAssetBuilder.GenerateAllBatch</summary>
        public static void GenerateAllBatch()
        {
            GenerateAll();
            EditorApplication.Exit(0);
        }

        private static BgmClipCatalog EnsureCatalog()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Settings"))
            {
                AssetDatabase.CreateFolder("Assets", "Settings");
            }

            if (!AssetDatabase.IsValidFolder(SettingsDir))
            {
                AssetDatabase.CreateFolder("Assets/Settings", "Audio");
            }

            var catalog = AssetDatabase.LoadAssetAtPath<BgmClipCatalog>(CatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<BgmClipCatalog>();
                AssetDatabase.CreateAsset(catalog, CatalogPath);
            }

            var entries = new List<BgmClipCatalog.ClipEntry>();
            var guids = AssetDatabase.FindAssets("t:AudioClip", new[] { BgmFolder });
            for (var i = 0; i < guids.Length; i++)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[i]);
                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                if (clip == null)
                {
                    continue;
                }

                entries.Add(new BgmClipCatalog.ClipEntry
                {
                    ClipId = Path.GetFileNameWithoutExtension(path),
                    Clip = clip
                });
            }

            catalog.EditorSet(entries);
            EditorUtility.SetDirty(catalog);
            return catalog;
        }

        private static void WireMetaShell()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<BgmClipCatalog>(CatalogPath);
            var root = AssetDatabase.LoadAssetAtPath<GameObject>(MetaRootPath);
            if (root == null || catalog == null)
            {
                Debug.LogWarning("[BgmAssetBuilder] MetaShellRoot or catalog missing.");
                return;
            }

            var prefabPath = MetaRootPath;
            var instance = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                var controller = instance.GetComponent<MetaShellController>();
                if (controller == null)
                {
                    Debug.LogError("[BgmAssetBuilder] MetaShellController missing on MetaShellRoot.");
                    return;
                }

                var so = new SerializedObject(controller);
                so.FindProperty("_bgmClipCatalog").objectReferenceValue = catalog;

                var sourceProp = so.FindProperty("_bgmAudioSource");
                if (sourceProp.objectReferenceValue == null)
                {
                    var existing = instance.transform.Find("BgmAudioSource");
                    AudioSource source;
                    if (existing != null)
                    {
                        source = existing.GetComponent<AudioSource>();
                        if (source == null)
                        {
                            source = existing.gameObject.AddComponent<AudioSource>();
                        }
                    }
                    else
                    {
                        var go = new GameObject("BgmAudioSource");
                        go.transform.SetParent(instance.transform, false);
                        source = go.AddComponent<AudioSource>();
                    }

                    source.playOnAwake = false;
                    source.loop = true;
                    source.spatialBlend = 0f;
                    sourceProp.objectReferenceValue = source;
                }

                so.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(instance);
            }
        }

        private static void WireBootScene()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<BgmClipCatalog>(CatalogPath);
            if (catalog == null || !File.Exists(BootScenePath))
            {
                return;
            }

            var scene = EditorSceneManager.OpenScene(BootScenePath, OpenSceneMode.Single);
            var controllers = Object.FindObjectsOfType<MetaShellController>();
            for (var i = 0; i < controllers.Length; i++)
            {
                var controller = controllers[i];
                var so = new SerializedObject(controller);
                so.FindProperty("_bgmClipCatalog").objectReferenceValue = catalog;
                var sourceProp = so.FindProperty("_bgmAudioSource");
                if (sourceProp.objectReferenceValue == null)
                {
                    var t = controller.transform.Find("BgmAudioSource");
                    AudioSource source;
                    if (t != null)
                    {
                        source = t.GetComponent<AudioSource>() ?? t.gameObject.AddComponent<AudioSource>();
                    }
                    else
                    {
                        var go = new GameObject("BgmAudioSource");
                        go.transform.SetParent(controller.transform, false);
                        source = go.AddComponent<AudioSource>();
                    }

                    source.playOnAwake = false;
                    source.loop = true;
                    source.spatialBlend = 0f;
                    sourceProp.objectReferenceValue = source;
                }

                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(controller);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }
    }
}
#endif
