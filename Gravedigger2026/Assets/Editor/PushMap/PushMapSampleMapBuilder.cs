#if UNITY_EDITOR
using Gravedigger2026.Gameplay.Defend;
using Gravedigger2026.Gameplay.Maps;
using Gravedigger2026.Gameplay.PushMap;
using UnityEditor;
using UnityEngine;

namespace Gravedigger2026.Editor.PushMap
{
    /// <summary>
    /// Ensures sample PushMap map Prefab with full marker set (SPEC_04 §9.22 / PM-01 Approach A).
    /// Copies Ground_01 → PushMap_Demo_01 without rewriting Dig/Defend Ground_*.
    /// </summary>
    public static class PushMapSampleMapBuilder
    {
        public const string PrefabMapsDir = "Assets/Prefabs/Maps";
        public const string SourceMapId = "Ground_01";
        public const string SampleMapId = "PushMap_Demo_01";

        private static string SourcePrefabPath => $"{PrefabMapsDir}/{SourceMapId}.prefab";
        private static string SamplePrefabPath => $"{PrefabMapsDir}/{SampleMapId}.prefab";

        [MenuItem("Gravedigger2026/PushMap/Ensure Sample Map Prefab")]
        public static void EnsureSampleMapPrefabMenu()
        {
            if (EnsureSampleMapPrefab())
            {
                Debug.Log($"[PushMapSampleMapBuilder] Ensured {SamplePrefabPath}");
            }
        }

        /// <summary>Batchmode entry: -executeMethod Gravedigger2026.Editor.PushMap.PushMapSampleMapBuilder.EnsureSampleMapPrefabBatch</summary>
        public static void EnsureSampleMapPrefabBatch()
        {
            if (!EnsureSampleMapPrefab())
            {
                EditorApplication.Exit(1);
                return;
            }

            EditorApplication.Exit(0);
        }

        public static bool EnsureSampleMapPrefab()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(SourcePrefabPath) == null)
            {
                Debug.LogError($"[PushMapSampleMapBuilder] Missing source map: {SourcePrefabPath}");
                return false;
            }

            if (AssetDatabase.LoadAssetAtPath<GameObject>(SamplePrefabPath) == null)
            {
                if (!AssetDatabase.CopyAsset(SourcePrefabPath, SamplePrefabPath))
                {
                    Debug.LogError(
                        $"[PushMapSampleMapBuilder] CopyAsset failed: {SourcePrefabPath} → {SamplePrefabPath}");
                    return false;
                }

                AssetDatabase.ImportAsset(SamplePrefabPath);
            }

            var contents = PrefabUtility.LoadPrefabContents(SamplePrefabPath);
            try
            {
                contents.name = SampleMapId;
                RemoveDefendSpawnPoints(contents);
                var markersRoot = EnsureChild(contents.transform, "PushMapMarkers");
                markersRoot.localPosition = Vector3.zero;
                markersRoot.localRotation = Quaternion.identity;
                markersRoot.localScale = Vector3.one;

                EnsureObjective(markersRoot, "Objective_01", 1, new Vector3(-2.5f, 0.05f, 1.5f), 2f);
                EnsureObjective(markersRoot, "Objective_02", 2, new Vector3(2f, 0.05f, -1f), 2f);
                EnsureAirWall(markersRoot, "AirWall_45", new Vector3(0f, 0.75f, 0f), 45f,
                    new Vector3(2.5f, 0.75f, 0.15f));
                EnsureSpawnPoint(markersRoot, "SpawnPoint_SP_01", "SP_01", new Vector3(-3.5f, 0.05f, -2f));
                EnsureSpawnPoint(markersRoot, "SpawnPoint_SP_02", "SP_02", new Vector3(3.5f, 0.05f, 2f));
                EnsureTrapZone(markersRoot, "TrapZone_TZ_01", "TZ_01", new Vector3(0f, 0.05f, 2.5f), 1.5f);
                EnsureBossPoint(markersRoot, "BossPoint", new Vector3(2f, 0.05f, -1.8f));

                if (contents.GetComponentInChildren<EngageZone>(true) == null)
                {
                    Debug.LogWarning(
                        "[PushMapSampleMapBuilder] EngageZone missing on sample map — expected from Ground_01.");
                }

                if (contents.GetComponentInChildren<WalkSurfaceIsoDiamond>(true) == null)
                {
                    Debug.LogWarning(
                        "[PushMapSampleMapBuilder] WalkSurface missing on sample map — expected from Ground_01.");
                }

                PrefabUtility.SaveAsPrefabAsset(contents, SamplePrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return true;
        }

        private static void RemoveDefendSpawnPoints(GameObject mapRoot)
        {
            var set = mapRoot.GetComponentInChildren<DefendSpawnPointSet>(true);
            if (set == null)
            {
                return;
            }

            Object.DestroyImmediate(set.gameObject);
        }

        private static Transform EnsureChild(Transform parent, string childName)
        {
            var existing = parent.Find(childName);
            if (existing != null)
            {
                return existing;
            }

            var go = new GameObject(childName);
            go.transform.SetParent(parent, false);
            return go.transform;
        }

        private static void EnsureObjective(
            Transform markersRoot,
            string name,
            int order,
            Vector3 localPosition,
            float captureRadius)
        {
            var t = EnsureChild(markersRoot, name);
            t.localPosition = localPosition;
            t.localRotation = Quaternion.identity;
            t.localScale = Vector3.one;

            var capture = t.GetComponent<CaptureZone>() ?? t.gameObject.AddComponent<CaptureZone>();
            capture.SetRadius(captureRadius);

            var objective = t.GetComponent<ObjectivePoint>() ?? t.gameObject.AddComponent<ObjectivePoint>();
            objective.SetObjectiveOrder(order);
            objective.SetCaptureZone(capture);

            EditorUtility.SetDirty(capture);
            EditorUtility.SetDirty(objective);
        }

        private static void EnsureAirWall(
            Transform markersRoot,
            string name,
            Vector3 localPosition,
            float yawDegrees,
            Vector3 halfExtents)
        {
            var t = EnsureChild(markersRoot, name);
            t.localPosition = localPosition;
            t.localEulerAngles = new Vector3(0f, yawDegrees, 0f);
            t.localScale = Vector3.one;

            var wall = t.GetComponent<AirWall>() ?? t.gameObject.AddComponent<AirWall>();
            wall.SetHalfExtents(halfExtents);
            EditorUtility.SetDirty(wall);
        }

        private static void EnsureSpawnPoint(
            Transform markersRoot,
            string name,
            string spawnPointId,
            Vector3 localPosition)
        {
            var t = EnsureChild(markersRoot, name);
            t.localPosition = localPosition;
            t.localRotation = Quaternion.identity;
            t.localScale = Vector3.one;

            var point = t.GetComponent<SpawnPoint>() ?? t.gameObject.AddComponent<SpawnPoint>();
            point.SetSpawnPointId(spawnPointId);
            EditorUtility.SetDirty(point);
        }

        private static void EnsureTrapZone(
            Transform markersRoot,
            string name,
            string trapZoneId,
            Vector3 localPosition,
            float radius)
        {
            var t = EnsureChild(markersRoot, name);
            t.localPosition = localPosition;
            t.localRotation = Quaternion.identity;
            t.localScale = Vector3.one;

            var zone = t.GetComponent<TrapZone>() ?? t.gameObject.AddComponent<TrapZone>();
            zone.SetTrapZoneId(trapZoneId);
            zone.SetRadius(radius);
            EditorUtility.SetDirty(zone);
        }

        private static void EnsureBossPoint(Transform markersRoot, string name, Vector3 localPosition)
        {
            var t = EnsureChild(markersRoot, name);
            t.localPosition = localPosition;
            t.localRotation = Quaternion.identity;
            t.localScale = Vector3.one;

            if (t.GetComponent<BossPoint>() == null)
            {
                t.gameObject.AddComponent<BossPoint>();
            }

            EditorUtility.SetDirty(t.gameObject);
        }
    }
}
#endif
