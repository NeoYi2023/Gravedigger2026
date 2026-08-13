#if UNITY_EDITOR
using Gravedigger2026.Gameplay.PushMap;
using UnityEditor;
using UnityEngine;

namespace Gravedigger2026.Editor.PushMap
{
    [CustomEditor(typeof(PushMapCameraPath))]
    public sealed class PushMapCameraPathEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            var path = (PushMapCameraPath)target;
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Baked points", path.BakedPointCount.ToString());
            if (GUILayout.Button("Snap Waypoints to Grid"))
            {
                Undo.RecordObject(path.transform, "Snap CameraFollowPath waypoints");
                var waypoints = path.CollectWaypoints();
                for (var i = 0; i < waypoints.Length; i++)
                {
                    if (waypoints[i] != null)
                    {
                        Undo.RecordObject(waypoints[i].transform, "Snap CameraFollowPath waypoints");
                    }
                }

                if (path.SnapWaypointsToGrid())
                {
                    EditorUtility.SetDirty(path);
                    for (var i = 0; i < waypoints.Length; i++)
                    {
                        if (waypoints[i] != null)
                        {
                            EditorUtility.SetDirty(waypoints[i]);
                        }
                    }
                }
                else
                {
                    Debug.LogWarning("[PushMapCameraPath] No parent Grid found to snap to.");
                }
            }

            if (GUILayout.Button("Bake Camera Path"))
            {
                Undo.RecordObject(path, "Bake CameraFollowPath");
                if (path.TryBake(out var error))
                {
                    EditorUtility.SetDirty(path);
                    Debug.Log($"[PushMapCameraPath] Baked {path.BakedPointCount} points.");
                }
                else
                {
                    Debug.LogError($"[PushMapCameraPath] Bake failed: {error}");
                }
            }
        }

        [MenuItem("Gravedigger2026/PushMap/Bake Camera Follow Path")]
        public static void BakeSelectedOrOpenPrefabs()
        {
            var selected = Selection.GetFiltered<PushMapCameraPath>(SelectionMode.Deep);
            if (selected == null || selected.Length == 0)
            {
                Debug.LogWarning("[PushMapCameraPath] Select a CameraFollowPath (or a map Prefab containing one).");
                return;
            }

            var ok = 0;
            for (var i = 0; i < selected.Length; i++)
            {
                var path = selected[i];
                Undo.RecordObject(path, "Bake CameraFollowPath");
                if (path.TryBake(out var error))
                {
                    EditorUtility.SetDirty(path);
                    ok++;
                }
                else
                {
                    Debug.LogError($"[PushMapCameraPath] Bake failed on {path.name}: {error}");
                }
            }

            Debug.Log($"[PushMapCameraPath] Baked {ok}/{selected.Length} path(s).");
        }
    }
}
#endif
