#if UNITY_EDITOR
using Gravedigger2026.Gameplay.Defend;
using Gravedigger2026.Gameplay.PushMap;
using UnityEditor;
using UnityEngine;

namespace Gravedigger2026.Editor.SearchExtract
{
    /// <summary>
    /// SE-02 Approach B: copy PushMap_Demo_01 → SearchExtract_Demo_01 without rewriting
    /// the PushMap source. Ensures ≥2 ObjectivePoints and AirWall OBB authoring checks.
    /// </summary>
    public static class SearchExtractSampleMapBuilder
    {
        public const string PrefabMapsDir = "Assets/Prefabs/Maps";
        public const string SourceMapId = "PushMap_Demo_01";
        public const string SampleMapId = "SearchExtract_Demo_01";
        private const string CatalogPath = "Assets/Settings/Defend/DefendPrefabCatalog.asset";

        /// <summary>Corridor-clear gather point 2 (SPEC_03 §3.14: not inside AirWall OBB).</summary>
        private static readonly Vector3 Objective02LocalPos = new Vector3(9.51f, 0.05f, 4.95f);

        private static string SourcePrefabPath => $"{PrefabMapsDir}/{SourceMapId}.prefab";
        private static string SamplePrefabPath => $"{PrefabMapsDir}/{SampleMapId}.prefab";

        [MenuItem("Gravedigger2026/SearchExtract/Ensure Sample Map Prefab")]
        public static void EnsureSampleMapPrefabMenu()
        {
            if (EnsureSampleMapPrefab())
            {
                Debug.Log($"[SearchExtractSampleMapBuilder] Ensured {SamplePrefabPath}");
            }
        }

        [MenuItem("Gravedigger2026/SearchExtract/Validate Sample Map AirWalls")]
        public static void ValidateSampleMapAirWallsMenu()
        {
            if (ValidateSampleMapAirWalls(logPass: true))
            {
                Debug.Log($"[SearchExtractSampleMapBuilder] AirWall OBB check passed: {SamplePrefabPath}");
            }
        }

        /// <summary>Batchmode: -executeMethod Gravedigger2026.Editor.SearchExtract.SearchExtractSampleMapBuilder.EnsureSampleMapPrefabBatch</summary>
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
                Debug.LogError($"[SearchExtractSampleMapBuilder] Missing source map: {SourcePrefabPath}");
                return false;
            }

            if (AssetDatabase.LoadAssetAtPath<GameObject>(SamplePrefabPath) == null)
            {
                if (!AssetDatabase.CopyAsset(SourcePrefabPath, SamplePrefabPath))
                {
                    Debug.LogError(
                        $"[SearchExtractSampleMapBuilder] CopyAsset failed: {SourcePrefabPath} → {SamplePrefabPath}");
                    return false;
                }

                AssetDatabase.ImportAsset(SamplePrefabPath);
            }

            var contents = PrefabUtility.LoadPrefabContents(SamplePrefabPath);
            try
            {
                contents.name = SampleMapId;
                var markersRoot = FindMarkersRoot(contents.transform);
                if (markersRoot == null)
                {
                    Debug.LogError(
                        "[SearchExtractSampleMapBuilder] PushMapMarkers missing on copied map.");
                    return false;
                }

                EnsureObjective(markersRoot, "Objective_02", 2, Objective02LocalPos, 2f);
                NudgeAuthoringMarkersOutOfAirWalls(contents);

                if (!ValidateAirWallsOnMap(contents, logPass: false))
                {
                    return false;
                }

                PrefabUtility.SaveAsPrefabAsset(contents, SamplePrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return BindCatalogMap();
        }

        public static bool ValidateSampleMapAirWalls(bool logPass)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(SamplePrefabPath);
            if (prefab == null)
            {
                Debug.LogError($"[SearchExtractSampleMapBuilder] Missing sample map: {SamplePrefabPath}");
                return false;
            }

            var contents = PrefabUtility.LoadPrefabContents(SamplePrefabPath);
            try
            {
                return ValidateAirWallsOnMap(contents, logPass);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }

        private static Transform FindMarkersRoot(Transform mapRoot)
        {
            var named = mapRoot.Find("PushMapMarkers");
            if (named != null)
            {
                return named;
            }

            var objectives = mapRoot.GetComponentsInChildren<ObjectivePoint>(true);
            return objectives.Length > 0 ? objectives[0].transform.parent : null;
        }

        private static void EnsureObjective(
            Transform markersRoot,
            string name,
            int order,
            Vector3 localPosition,
            float captureRadius)
        {
            var existing = markersRoot.Find(name);
            var created = existing == null;
            var t = existing != null ? existing : CreateChild(markersRoot, name);
            if (created)
            {
                t.localPosition = localPosition;
                t.localRotation = Quaternion.identity;
                t.localScale = Vector3.one;
            }

            var capture = t.GetComponent<CaptureZone>() ?? t.gameObject.AddComponent<CaptureZone>();
            if (created)
            {
                capture.SetRadius(captureRadius);
            }

            var objective = t.GetComponent<ObjectivePoint>() ?? t.gameObject.AddComponent<ObjectivePoint>();
            objective.SetObjectiveOrder(order);
            objective.SetCaptureZone(capture);
            EditorUtility.SetDirty(capture);
            EditorUtility.SetDirty(objective);
        }

        private static Transform CreateChild(Transform parent, string childName)
        {
            var go = new GameObject(childName);
            go.transform.SetParent(parent, false);
            return go.transform;
        }

        private static void NudgeAuthoringMarkersOutOfAirWalls(GameObject mapRoot)
        {
            var walls = mapRoot.GetComponentsInChildren<AirWall>(true);
            NudgeIfInside(mapRoot.GetComponentsInChildren<ObjectivePoint>(true), walls, t => t.transform);
            NudgeIfInside(mapRoot.GetComponentsInChildren<SpawnPoint>(true), walls, t => t.transform);
            NudgeIfInside(mapRoot.GetComponentsInChildren<BossPoint>(true), walls, t => t.transform);
        }

        private static void NudgeIfInside<T>(T[] markers, AirWall[] walls, System.Func<T, Transform> transformOf)
            where T : Component
        {
            for (var i = 0; i < markers.Length; i++)
            {
                var t = transformOf(markers[i]);
                if (t == null || !IsInsideAnyAirWall(t.position, walls))
                {
                    continue;
                }

                if (!TryFindClearLocal(t, walls, out var clearLocal))
                {
                    Debug.LogWarning(
                        $"[SearchExtractSampleMapBuilder] Could not nudge '{t.name}' out of AirWall OBB.");
                    continue;
                }

                t.localPosition = clearLocal;
                EditorUtility.SetDirty(t);
                Debug.Log(
                    $"[SearchExtractSampleMapBuilder] Nudged '{t.name}' to {clearLocal} (was inside AirWall).");
            }
        }

        private static bool TryFindClearLocal(Transform marker, AirWall[] walls, out Vector3 clearLocal)
        {
            var parent = marker.parent;
            var start = marker.localPosition;
            for (var step = 1; step <= 24; step++)
            {
                for (var ix = -step; ix <= step; ix++)
                {
                    for (var iz = -step; iz <= step; iz++)
                    {
                        if (Mathf.Abs(ix) != step && Mathf.Abs(iz) != step)
                        {
                            continue;
                        }

                        var candidateLocal = start + new Vector3(ix * 0.25f, 0f, iz * 0.25f);
                        var world = parent != null
                            ? parent.TransformPoint(candidateLocal)
                            : candidateLocal;
                        if (!IsInsideAnyAirWall(world, walls))
                        {
                            clearLocal = candidateLocal;
                            return true;
                        }
                    }
                }
            }

            clearLocal = start;
            return false;
        }

        private static bool ValidateAirWallsOnMap(GameObject mapRoot, bool logPass)
        {
            var walls = mapRoot.GetComponentsInChildren<AirWall>(true);
            var failed = 0;
            failed += ReportIfInside("ObjectivePoint", mapRoot.GetComponentsInChildren<ObjectivePoint>(true), walls);
            failed += ReportIfInside("SpawnPoint", mapRoot.GetComponentsInChildren<SpawnPoint>(true), walls);
            failed += ReportIfInside("BossPoint", mapRoot.GetComponentsInChildren<BossPoint>(true), walls);

            var objectives = mapRoot.GetComponentsInChildren<ObjectivePoint>(true);
            if (objectives.Length < 2)
            {
                Debug.LogError(
                    $"[SearchExtractSampleMapBuilder] Need ≥2 ObjectivePoint, found {objectives.Length}.");
                failed++;
            }

            var hasSp01 = false;
            var hasSp02 = false;
            var spawns = mapRoot.GetComponentsInChildren<SpawnPoint>(true);
            for (var i = 0; i < spawns.Length; i++)
            {
                var id = spawns[i].SpawnPointId;
                if (id == "SP_01")
                {
                    hasSp01 = true;
                }
                else if (id == "SP_02")
                {
                    hasSp02 = true;
                }
            }

            if (!hasSp01 || !hasSp02)
            {
                Debug.LogError(
                    "[SearchExtractSampleMapBuilder] Missing SpawnPointId SP_01 and/or SP_02.");
                failed++;
            }

            if (failed > 0)
            {
                return false;
            }

            if (logPass)
            {
                Debug.Log(
                    $"[SearchExtractSampleMapBuilder] Markers OK — Objectives={objectives.Length} " +
                    $"SpawnPoints={spawns.Length} AirWalls={walls.Length}.");
            }

            return true;
        }

        private static int ReportIfInside<T>(string label, T[] markers, AirWall[] walls)
            where T : Component
        {
            var failed = 0;
            for (var i = 0; i < markers.Length; i++)
            {
                var t = markers[i].transform;
                if (!IsInsideAnyAirWall(t.position, walls))
                {
                    continue;
                }

                Debug.LogError(
                    $"[SearchExtractSampleMapBuilder] {label} '{t.name}' world XZ is inside an AirWall OBB.");
                failed++;
            }

            return failed;
        }

        private static bool IsInsideAnyAirWall(Vector3 worldPosition, AirWall[] walls)
        {
            for (var i = 0; i < walls.Length; i++)
            {
                if (walls[i] != null && walls[i].ContainsXZ(worldPosition))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool BindCatalogMap()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<DefendPrefabCatalog>(CatalogPath);
            if (catalog == null)
            {
                Debug.LogError(
                    $"[SearchExtractSampleMapBuilder] Catalog missing: {CatalogPath} — run Defend build first.");
                return false;
            }

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(SamplePrefabPath);
            if (prefab == null)
            {
                Debug.LogError($"[SearchExtractSampleMapBuilder] Sample prefab missing after save: {SamplePrefabPath}");
                return false;
            }

            var so = new SerializedObject(catalog);
            var mapsProp = so.FindProperty("_maps");
            if (mapsProp == null || !mapsProp.isArray)
            {
                Debug.LogError("[SearchExtractSampleMapBuilder] DefendPrefabCatalog._maps not found.");
                return false;
            }

            UpsertMapEntry(mapsProp, SampleMapId, prefab);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            return true;
        }

        private static void UpsertMapEntry(SerializedProperty mapsProp, string mapId, GameObject prefab)
        {
            var mapIdPropPath = nameof(DefendPrefabCatalog.MapEntry.MapId);
            var prefabPropPath = nameof(DefendPrefabCatalog.MapEntry.Prefab);
            for (var i = 0; i < mapsProp.arraySize; i++)
            {
                var entry = mapsProp.GetArrayElementAtIndex(i);
                var idProp = entry.FindPropertyRelative(mapIdPropPath);
                if (idProp != null && idProp.stringValue == mapId)
                {
                    entry.FindPropertyRelative(prefabPropPath).objectReferenceValue = prefab;
                    return;
                }
            }

            mapsProp.InsertArrayElementAtIndex(mapsProp.arraySize);
            var newEntry = mapsProp.GetArrayElementAtIndex(mapsProp.arraySize - 1);
            newEntry.FindPropertyRelative(mapIdPropPath).stringValue = mapId;
            newEntry.FindPropertyRelative(prefabPropPath).objectReferenceValue = prefab;
        }
    }
}
#endif
