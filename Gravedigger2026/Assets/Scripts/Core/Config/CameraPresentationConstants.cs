using UnityEngine;

namespace Gravedigger2026.Core.Config
{
    /// <summary>
    /// Stage camera + PushMap follow/zoom/intro tunables from CombatConstantConfig (SPEC_04 §9.20b).
    /// </summary>
    public readonly struct CameraPresentationConstants
    {
        public float HeightY { get; }
        public float CombatCameraPitchDegrees { get; }
        public float OrthoSizeMargin { get; }
        public float PushMapPrepareOrthoSize { get; }
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
            float combatCameraPitchDegrees,
            float orthoSizeMargin,
            float pushMapPrepareOrthoSize,
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
            CombatCameraPitchDegrees = combatCameraPitchDegrees;
            OrthoSizeMargin = orthoSizeMargin;
            PushMapPrepareOrthoSize = pushMapPrepareOrthoSize;
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
                    CombatConstantKeys.CombatCameraPitchDegrees,
                    CombatConstantKeys.Safety.CombatCameraPitchDegrees),
                configs.GetCombatConstantOrFallback(
                    CombatConstantKeys.CameraOrthoSizeMargin,
                    CombatConstantKeys.Safety.CameraOrthoSizeMargin),
                configs.GetCombatConstantOrFallback(
                    CombatConstantKeys.PushMapPrepareOrthoSize,
                    CombatConstantKeys.Safety.PushMapPrepareOrthoSize),
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
            CombatConstantKeys.Safety.CombatCameraPitchDegrees,
            CombatConstantKeys.Safety.CameraOrthoSizeMargin,
            CombatConstantKeys.Safety.PushMapPrepareOrthoSize,
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
        /// Dig / Defend / UM·Defend formation: orthographicSize = max(half) − margin (clamped ≥ OrthoSizeMin).
        /// PushMap Prepare uses <see cref="PushMapPrepareOrthoSize"/> instead.
        /// </summary>
        public float ResolveMapFitOrthoSize(Vector2 mapHalfExtents)
        {
            var raw = Mathf.Max(mapHalfExtents.x, mapHalfExtents.y) - OrthoSizeMargin;
            return Mathf.Max(OrthoSizeMin, raw);
        }

        /// <summary>
        /// Pure top-down (Euler 90°). Prepare FormationCamera, Dig, and map-fit Defend formation framing.
        /// </summary>
        public void ApplyTopDownPose(Camera camera, Vector3 mapCenter, float orthographicSize)
        {
            ApplyCameraPose(camera, mapCenter, orthographicSize, 90f);
        }

        /// <summary>
        /// Defend+PushMap Combat oblique ortho (SPEC_04 §9.20b / §15.5 CP-CAM).
        /// Viewport center ray hits <paramref name="lookAt"/>; world Y arcs project onto screen.
        /// </summary>
        public void ApplyCombatCameraPose(Camera camera, Vector3 lookAt, float orthographicSize)
        {
            ApplyCameraPose(camera, lookAt, orthographicSize, CombatCameraPitchDegrees);
        }

        /// <summary>
        /// World position for a combat camera whose center ray hits <paramref name="lookAt"/>.
        /// Used by PushMap follow so XZ-only offsets stay valid under oblique pitch.
        /// </summary>
        public Vector3 ResolveCombatCameraPosition(Vector3 lookAt, float pitchDegrees = -1f)
        {
            var pitch = pitchDegrees < 0f
                ? CombatCameraPitchDegrees
                : pitchDegrees;
            return ResolveCameraPosition(lookAt, pitch);
        }

        /// <summary>Euler rotation for combat pitch (Y=0).</summary>
        public Quaternion ResolveCombatCameraRotation(float pitchDegrees = -1f)
        {
            var pitch = pitchDegrees < 0f
                ? CombatCameraPitchDegrees
                : pitchDegrees;
            pitch = Mathf.Clamp(pitch, 45f, 89.9f);
            return Quaternion.Euler(pitch, 0f, 0f);
        }

        private void ApplyCameraPose(
            Camera camera,
            Vector3 lookAt,
            float orthographicSize,
            float pitchDegrees)
        {
            if (camera == null)
            {
                return;
            }

            var pitch = Mathf.Clamp(pitchDegrees, 45f, 89.9f);
            camera.transform.rotation = ResolveCombatCameraRotation(pitch);
            camera.transform.position = ResolveCameraPosition(lookAt, pitch);
            camera.orthographic = true;
            camera.orthographicSize = Mathf.Max(OrthoSizeMin, orthographicSize);
            camera.nearClipPlane = NearClip;
            camera.farClipPlane = FarClip;
        }

        private Vector3 ResolveCameraPosition(Vector3 lookAt, float pitchDegrees)
        {
            var pitch = Mathf.Clamp(pitchDegrees, 45f, 89.9f);
            var forward = ResolveCombatCameraRotation(pitch) * Vector3.forward;
            if (forward.y >= -0.001f)
            {
                return lookAt + new Vector3(0f, HeightY, 0f);
            }

            var distanceAlongForward = HeightY / -forward.y;
            return lookAt - forward * distanceAlongForward;
        }
    }
}
