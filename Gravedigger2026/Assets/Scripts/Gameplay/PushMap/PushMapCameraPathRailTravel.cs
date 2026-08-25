using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Gravedigger2026.Gameplay.PushMap
{
    /// <summary>
    /// Shared CameraFollowPath rail travel helpers (SPEC_03 §3.14 Prepare Quick Preview).
    /// Arc-length progress s∈[0,1]: WP_Start=0, WP_End=1.
    /// </summary>
    public static class PushMapCameraPathRailTravel
    {
        public static void SnapCameraToProgress(Camera camera, PushMapCameraPath path, float s)
        {
            if (camera == null || path == null || !path.TryEvaluate(s, out var lookAt))
            {
                return;
            }

            var p = camera.transform.position;
            camera.transform.position = new Vector3(lookAt.x, p.y, lookAt.z);
        }

        /// <summary>
        /// Reverse sweep along author waypoints (WP_End → WP_Start) at constant world-XZ speed,
        /// dwelling at each author waypoint. Invokes <paramref name="onProgress"/> whenever s changes.
        /// </summary>
        public static IEnumerator ReverseAuthorWaypointSweep(
            Camera camera,
            PushMapCameraPath path,
            IReadOnlyList<float> progresses,
            float speed,
            float dwellSeconds,
            Action<float> onProgress)
        {
            if (camera == null || path == null || progresses == null || progresses.Count < 2)
            {
                yield break;
            }

            var totalLength = Mathf.Max(1e-4f, path.TotalLength);
            var speedSafe = Mathf.Max(0.01f, speed);
            var dwell = Mathf.Max(0f, dwellSeconds);

            var index = progresses.Count - 1;
            ApplyProgress(camera, path, progresses[index], onProgress);
            if (dwell > 0f)
            {
                yield return new WaitForSeconds(dwell);
            }

            while (index > 0)
            {
                var fromS = progresses[index];
                var toS = progresses[index - 1];
                var distance = Mathf.Abs(fromS - toS) * totalLength;
                var duration = distance / speedSafe;
                if (duration <= 1e-4f)
                {
                    ApplyProgress(camera, path, toS, onProgress);
                }
                else
                {
                    var elapsed = 0f;
                    while (elapsed < duration)
                    {
                        elapsed += Time.deltaTime;
                        var t = Mathf.Clamp01(elapsed / duration);
                        var s = Mathf.Lerp(fromS, toS, t);
                        ApplyProgress(camera, path, s, onProgress);
                        yield return null;
                    }

                    ApplyProgress(camera, path, toS, onProgress);
                }

                index--;
                if (dwell > 0f)
                {
                    yield return new WaitForSeconds(dwell);
                }
            }
        }

        private static void ApplyProgress(
            Camera camera,
            PushMapCameraPath path,
            float s,
            Action<float> onProgress)
        {
            var clamped = Mathf.Clamp01(s);
            SnapCameraToProgress(camera, path, clamped);
            onProgress?.Invoke(clamped);
        }
    }
}
