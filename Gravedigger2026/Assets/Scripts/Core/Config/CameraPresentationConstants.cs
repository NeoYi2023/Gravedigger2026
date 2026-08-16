using UnityEngine;

namespace Gravedigger2026.Core.Config
{
    /// <summary>
    /// Top-down stage camera + PushMap follow/zoom/intro tunables from CombatConstantConfig (SPEC_04 §9.20b).
    /// </summary>
    public readonly struct CameraPresentationConstants
    {
        public float HeightY { get; }
        public float OrthoSizeMargin { get; }
        public float PushMapOrthoSize { get; }
        public float NearClip { get; }
        public float FarClip { get; }
        public float FollowDeadzone { get; }
        public float FollowSmoothTime { get; }
        public float ZoomStepPerNotch { get; }
        public float OrthoSizeMin { get; }
        public float OrthoSizeMax { get; }
        public float DragThresholdPixels { get; }
        public float PushMapIntroSpeed { get; }
        public float PushMapIntroWaypointDwellSeconds { get; }

        public CameraPresentationConstants(
            float heightY,
            float orthoSizeMargin,
            float pushMapOrthoSize,
            float nearClip,
            float farClip,
            float followDeadzone,
            float followSmoothTime,
            float zoomStepPerNotch,
            float orthoSizeMin,
            float orthoSizeMax,
            float dragThresholdPixels,
            float pushMapIntroSpeed,
            float pushMapIntroWaypointDwellSeconds)
        {
            HeightY = heightY;
            OrthoSizeMargin = orthoSizeMargin;
            PushMapOrthoSize = pushMapOrthoSize;
            NearClip = nearClip;
            FarClip = farClip;
            FollowDeadzone = followDeadzone;
            FollowSmoothTime = followSmoothTime;
            ZoomStepPerNotch = zoomStepPerNotch;
            OrthoSizeMin = orthoSizeMin;
            OrthoSizeMax = orthoSizeMax;
            DragThresholdPixels = dragThresholdPixels;
            PushMapIntroSpeed = pushMapIntroSpeed;
            PushMapIntroWaypointDwellSeconds = pushMapIntroWaypointDwellSeconds;
        }

        public static CameraPresentationConstants FromRepository(ConfigCsvRepository configs)
        {
            if (configs == null)
            {
                return SafetyDefaults;
            }

            return new CameraPresentationConstants(
                configs.GetCombatConstantOrFallback(
                    CombatConstantKeys.CameraHeightY,
                    CombatConstantKeys.Safety.CameraHeightY),
                configs.GetCombatConstantOrFallback(
                    CombatConstantKeys.CameraOrthoSizeMargin,
                    CombatConstantKeys.Safety.CameraOrthoSizeMargin),
                configs.GetCombatConstantOrFallback(
                    CombatConstantKeys.PushMapCameraOrthoSize,
                    CombatConstantKeys.Safety.PushMapCameraOrthoSize),
                configs.GetCombatConstantOrFallback(
                    CombatConstantKeys.CameraNearClip,
                    CombatConstantKeys.Safety.CameraNearClip),
                configs.GetCombatConstantOrFallback(
                    CombatConstantKeys.CameraFarClip,
                    CombatConstantKeys.Safety.CameraFarClip),
                configs.GetCombatConstantOrFallback(
                    CombatConstantKeys.CameraFollowDeadzone,
                    CombatConstantKeys.Safety.CameraFollowDeadzone),
                configs.GetCombatConstantOrFallback(
                    CombatConstantKeys.CameraFollowSmoothTime,
                    CombatConstantKeys.Safety.CameraFollowSmoothTime),
                configs.GetCombatConstantOrFallback(
                    CombatConstantKeys.CameraZoomStepPerNotch,
                    CombatConstantKeys.Safety.CameraZoomStepPerNotch),
                configs.GetCombatConstantOrFallback(
                    CombatConstantKeys.CameraOrthoSizeMin,
                    CombatConstantKeys.Safety.CameraOrthoSizeMin),
                configs.GetCombatConstantOrFallback(
                    CombatConstantKeys.CameraOrthoSizeMax,
                    CombatConstantKeys.Safety.CameraOrthoSizeMax),
                configs.GetCombatConstantOrFallback(
                    CombatConstantKeys.CameraDragThresholdPixels,
                    CombatConstantKeys.Safety.CameraDragThresholdPixels),
                configs.GetCombatConstantOrFallback(
                    CombatConstantKeys.PushMapCameraIntroSpeed,
                    CombatConstantKeys.Safety.PushMapCameraIntroSpeed),
                configs.GetCombatConstantOrFallback(
                    CombatConstantKeys.PushMapCameraIntroWaypointDwellSeconds,
                    CombatConstantKeys.Safety.PushMapCameraIntroWaypointDwellSeconds));
        }

        public static CameraPresentationConstants SafetyDefaults => new CameraPresentationConstants(
            CombatConstantKeys.Safety.CameraHeightY,
            CombatConstantKeys.Safety.CameraOrthoSizeMargin,
            CombatConstantKeys.Safety.PushMapCameraOrthoSize,
            CombatConstantKeys.Safety.CameraNearClip,
            CombatConstantKeys.Safety.CameraFarClip,
            CombatConstantKeys.Safety.CameraFollowDeadzone,
            CombatConstantKeys.Safety.CameraFollowSmoothTime,
            CombatConstantKeys.Safety.CameraZoomStepPerNotch,
            CombatConstantKeys.Safety.CameraOrthoSizeMin,
            CombatConstantKeys.Safety.CameraOrthoSizeMax,
            CombatConstantKeys.Safety.CameraDragThresholdPixels,
            CombatConstantKeys.Safety.PushMapCameraIntroSpeed,
            CombatConstantKeys.Safety.PushMapCameraIntroWaypointDwellSeconds);

        /// <summary>
        /// Dig / Defend / Formation: orthographicSize = max(half) − margin (clamped ≥ OrthoSizeMin).
        /// </summary>
        public float ResolveMapFitOrthoSize(Vector2 mapHalfExtents)
        {
            var raw = Mathf.Max(mapHalfExtents.x, mapHalfExtents.y) - OrthoSizeMargin;
            return Mathf.Max(OrthoSizeMin, raw);
        }

        public void ApplyTopDownPose(Camera camera, Vector3 mapCenter, float orthographicSize)
        {
            if (camera == null)
            {
                return;
            }

            camera.transform.position = mapCenter + new Vector3(0f, HeightY, 0f);
            camera.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            camera.orthographic = true;
            camera.orthographicSize = Mathf.Max(OrthoSizeMin, orthographicSize);
            camera.nearClipPlane = NearClip;
            camera.farClipPlane = FarClip;
        }
    }
}
