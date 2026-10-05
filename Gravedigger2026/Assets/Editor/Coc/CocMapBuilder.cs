#if UNITY_EDITOR
using Gravedigger2026.Gameplay.Coc;
using Gravedigger2026.Gameplay.Defend;
using Gravedigger2026.Gameplay.PushMap;
using UnityEditor;
using UnityEngine;

namespace Gravedigger2026.Editor.Coc
{
    /// <summary>
    /// Copies SearchExtract_Lv2_01 into Coc_Lv2_01 and places slice-02 markers
    /// (SPEC_04 §9.35). Does not run spawn, fog, or capture rules.
    /// </summary>
    public static class CocMapBuilder
    {
        private const string SourcePath = "Assets/Prefabs/Maps/SearchExtract_Lv2_01.prefab";
        private const string DestPath = "Assets/Prefabs/Maps/Coc_Lv2_01.prefab";
        private const string CatalogPath = "Assets/Settings/Defend/DefendPrefabCatalog.asset";
        private const string MapId = "Coc_Lv2_01";

        [InitializeOnLoadMethod]
        private static void AutoEnsureOnce()
        {
            EditorApplication.delayCall += () =>
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode)
                {
                    return;
                }

                if (SessionState.GetBool("CocMapBuilder.Ensured", false))
                {
                    return;
                }

                EnsureSampleMap();
                if (AssetDatabase.LoadAssetAtPath<GameObject>(DestPath) != null)
                {
                    SessionState.SetBool("CocMapBuilder.Ensured", true);
                }
            };
        }

        [MenuItem("Gravedigger2026/Coc（COC）/Ensure Sample Map（确保样例地图）")]
        public static void EnsureSampleMap()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(SourcePath) == null)
            {
                Debug.LogError($"[CocMapBuilder] Source map missing: {SourcePath}");
                return;
            }

            if (AssetDatabase.LoadAssetAtPath<GameObject>(DestPath) == null)
            {
                if (!AssetDatabase.CopyAsset(SourcePath, DestPath))
                {
                    Debug.LogError($"[CocMapBuilder] Copy failed: {SourcePath} → {DestPath}");
                    return;
                }
            }

            StripSearchExtractMarkers(DestPath);
            EnsureMarkers(DestPath);
            AssetDatabase.ImportAsset(DestPath);
            BindCatalog();
            AssetDatabase.SaveAssets();
            Debug.Log($"[CocMapBuilder] Sample map ready: {DestPath}");
        }

        private static void StripSearchExtractMarkers(string path)
        {
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                root.name = MapId;
                DestroyAll<ObjectivePoint>(root);
                DestroyAll<BossPoint>(root);
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void EnsureMarkers(string path)
        {
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var anchor = FindSpawn(root, "SP_01");
                var parent = anchor != null ? anchor.transform.parent : root.transform;
                var origin = anchor != null ? anchor.transform.localPosition : Vector3.zero;

                EnsureSpawn(parent, "SP_Mini", origin + new Vector3(4f, 0f, 2f));
                EnsureSpawn(parent, "SP_Final", origin + new Vector3(8f, 0f, 4f));
                EnsureFog(parent, origin);
                EnsureCapture(parent, "CP_Normal", origin + new Vector3(-4f, 0f, 2f));
                EnsureCapture(parent, "CP_Hard", origin + new Vector3(0f, 0f, 8f));
                EnsureCapture(parent, "CP_Hell", origin + new Vector3(6f, 0f, -2f));
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void EnsureSpawn(Transform parent, string spawnPointId, Vector3 localPosition)
        {
            if (FindSpawn(parent.gameObject, spawnPointId) != null)
            {
                return;
            }

            var go = new GameObject("SpawnPoint_" + spawnPointId);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.AddComponent<SpawnPoint>().SetSpawnPointId(spawnPointId);
        }

        private static void EnsureFog(Transform parent, Vector3 localPosition)
        {
            var existing = parent.GetComponentsInChildren<CocFogPolygon>(true);
            for (var i = 0; i < existing.Length; i++)
            {
                if (existing[i] != null && existing[i].PointCount >= 3)
                {
                    return;
                }
            }

            var fog = new GameObject("CocFog_01");
            fog.transform.SetParent(parent, false);
            fog.transform.localPosition = localPosition;
            fog.AddComponent<CocFogPolygon>();
            CreateFogPoint(fog.transform, "P0", new Vector3(-7f, 0f, -5f));
            CreateFogPoint(fog.transform, "P1", new Vector3(7f, 0f, -5f));
            CreateFogPoint(fog.transform, "P2", new Vector3(0f, 0f, 7f));
        }

        private static void CreateFogPoint(Transform parent, string pointName, Vector3 localPosition)
        {
            var point = new GameObject(pointName);
            point.transform.SetParent(parent, false);
            point.transform.localPosition = localPosition;
        }

        private static void EnsureCapture(Transform parent, string capturePointId, Vector3 localPosition)
        {
            if (FindCapture(parent.gameObject, capturePointId) != null)
            {
                return;
            }

            var go = new GameObject("CocCapture_" + capturePointId);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.AddComponent<CocCapturePoint>().SetCapturePointId(capturePointId);
        }

        private static SpawnPoint FindSpawn(GameObject root, string spawnPointId)
        {
            var points = root.GetComponentsInChildren<SpawnPoint>(true);
            for (var i = 0; i < points.Length; i++)
            {
                if (points[i] != null && points[i].SpawnPointId == spawnPointId)
                {
                    return points[i];
                }
            }

            return null;
        }

        private static CocCapturePoint FindCapture(GameObject root, string capturePointId)
        {
            var points = root.GetComponentsInChildren<CocCapturePoint>(true);
            for (var i = 0; i < points.Length; i++)
            {
                if (points[i] != null && points[i].CapturePointId == capturePointId)
                {
                    return points[i];
                }
            }

            return null;
        }

        private static void DestroyAll<T>(GameObject root) where T : Component
        {
            var found = root.GetComponentsInChildren<T>(true);
            for (var i = 0; i < found.Length; i++)
            {
                if (found[i] != null)
                {
                    Object.DestroyImmediate(found[i].gameObject);
                }
            }
        }

        private static void BindCatalog()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<DefendPrefabCatalog>(CatalogPath);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(DestPath);
            if (catalog == null || prefab == null)
            {
                Debug.LogError("[CocMapBuilder] Catalog or sample prefab missing; map was not bound.");
                return;
            }

            var so = new SerializedObject(catalog);
            var maps = so.FindProperty("_maps");
            for (var i = 0; i < maps.arraySize; i++)
            {
                var entry = maps.GetArrayElementAtIndex(i);
                if (entry.FindPropertyRelative("MapId").stringValue == MapId)
                {
                    entry.FindPropertyRelative("Prefab").objectReferenceValue = prefab;
                    so.ApplyModifiedPropertiesWithoutUndo();
                    EditorUtility.SetDirty(catalog);
                    return;
                }
            }

            var index = maps.arraySize;
            maps.InsertArrayElementAtIndex(index);
            var added = maps.GetArrayElementAtIndex(index);
            added.FindPropertyRelative("MapId").stringValue = MapId;
            added.FindPropertyRelative("Prefab").objectReferenceValue = prefab;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(catalog);
        }
    }
}
#endif
