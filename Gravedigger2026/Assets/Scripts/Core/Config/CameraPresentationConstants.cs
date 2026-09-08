using UnityEngine;

namespace Gravedigger2026.Core.Config
{
    /// <summary>
    /// Stage camera + PushMap follow/zoom/intro + SearchExtract HoldFraming tunables from CombatConstantConfig (SPEC_04 §9.20b).
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
        public SearchExtractHoldFramingConstants HoldFraming { get; }

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
            float pushMapIntroWaypointDwellSeconds,
            SearchExtractHoldFramingConstants holdFraming)
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
            HoldFraming = holdFraming;
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
                    CombatConstantKeys.Safety.PushMapCameraIntroWaypointDwellSeconds),
                SearchExtractHoldFramingConstants.FromRepository(configs));
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
            CombatConstantKeys.Safety.PushMapCameraIntroWaypointDwellSeconds,
            SearchExtractHoldFramingConstants.SafetyDefaults);

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

        /// <summary>
        /// Inverse of <see cref="ResolveCombatCameraPosition"/>: ground-Y point under the camera center ray.
        /// </summary>
        public Vector3 ResolveCombatLookAt(Vector3 cameraPosition, float groundY, float pitchDegrees = -1f)
        {
            var pitch = pitchDegrees < 0f
                ? CombatCameraPitchDegrees
                : pitchDegrees;
            pitch = Mathf.Clamp(pitch, 45f, 89.9f);
            var forward = ResolveCombatCameraRotation(pitch) * Vector3.forward;
            if (Mathf.Abs(forward.y) < 0.001f)
            {
                return new Vector3(cameraPosition.x, groundY, cameraPosition.z);
            }

            var t = (groundY - cameraPosition.y) / forward.y;
            var hit = cameraPosition + forward * t;
            return new Vector3(hit.x, groundY, hit.z);
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

    /// <summary>
    /// SearchExtract HoldFraming tunables from CombatConstantConfig (SPEC_04 §9.20b).
    /// </summary>
    public readonly struct SearchExtractHoldFramingConstants
    {
        public float OrthoSizeMin { get; }
        public float OrthoSizeMax { get; }
        public float MaxPanRadius { get; }
        public float ViewportPad { get; }
        public float TopHudPad { get; }
        public float InnerPad { get; }
        public float ZoomInDelaySeconds { get; }
        public float SmoothTimeOut { get; }
        public float SmoothTimeIn { get; }
        public float SampleInterval { get; }
        public float ObjectiveBias { get; }

        public SearchExtractHoldFramingConstants(
            float orthoSizeMin,
            float orthoSizeMax,
            float maxPanRadius,
            float viewportPad,
            float topHudPad,
            float innerPad,
            float zoomInDelaySeconds,
            float smoothTimeOut,
            float smoothTimeIn,
            float sampleInterval,
            float objectiveBias)
        {
            OrthoSizeMin = orthoSizeMin;
            OrthoSizeMax = orthoSizeMax;
            MaxPanRadius = Mathf.Max(0f, maxPanRadius);
            ViewportPad = Mathf.Clamp(viewportPad, 0f, 0.49f);
            TopHudPad = Mathf.Max(0f, topHudPad);
            InnerPad = Mathf.Clamp(innerPad, 0f, 0.49f);
            ZoomInDelaySeconds = Mathf.Max(0f, zoomInDelaySeconds);
            SmoothTimeOut = Mathf.Max(0.01f, smoothTimeOut);
            SmoothTimeIn = Mathf.Max(SmoothTimeOut, smoothTimeIn);
            SampleInterval = Mathf.Max(0f, sampleInterval);
            ObjectiveBias = Mathf.Clamp01(objectiveBias);
        }

        public static SearchExtractHoldFramingConstants FromRepository(ConfigCsvRepository configs)
        {
            if (configs == null)
            {
                return SafetyDefaults;
            }

            return new SearchExtractHoldFramingConstants(
                configs.GetCombatConstantOrFallback(
                    CombatConstantKeys.SearchExtractHoldOrthoSizeMin,
                    CombatConstantKeys.Safety.SearchExtractHoldOrthoSizeMin),
                configs.GetCombatConstantOrFallback(
                    CombatConstantKeys.SearchExtractHoldOrthoSizeMax,
                    CombatConstantKeys.Safety.SearchExtractHoldOrthoSizeMax),
                configs.GetCombatConstantOrFallback(
                    CombatConstantKeys.SearchExtractHoldMaxPanRadius,
                    CombatConstantKeys.Safety.SearchExtractHoldMaxPanRadius),
                configs.GetCombatConstantOrFallback(
                    CombatConstantKeys.SearchExtractHoldViewportPad,
                    CombatConstantKeys.Safety.SearchExtractHoldViewportPad),
                configs.GetCombatConstantOrFallback(
                    CombatConstantKeys.SearchExtractHoldTopHudPad,
                    CombatConstantKeys.Safety.SearchExtractHoldTopHudPad),
                configs.GetCombatConstantOrFallback(
                    CombatConstantKeys.SearchExtractHoldInnerPad,
                    CombatConstantKeys.Safety.SearchExtractHoldInnerPad),
                configs.GetCombatConstantOrFallback(
                    CombatConstantKeys.SearchExtractHoldZoomInDelaySeconds,
                    CombatConstantKeys.Safety.SearchExtractHoldZoomInDelaySeconds),
                configs.GetCombatConstantOrFallback(
                    CombatConstantKeys.SearchExtractHoldSmoothTimeOut,
                    CombatConstantKeys.Safety.SearchExtractHoldSmoothTimeOut),
                configs.GetCombatConstantOrFallback(
                    CombatConstantKeys.SearchExtractHoldSmoothTimeIn,
                    CombatConstantKeys.Safety.SearchExtractHoldSmoothTimeIn),
                configs.GetCombatConstantOrFallback(
                    CombatConstantKeys.SearchExtractHoldSampleInterval,
                    CombatConstantKeys.Safety.SearchExtractHoldSampleInterval),
                configs.GetCombatConstantOrFallback(
                    CombatConstantKeys.SearchExtractHoldObjectiveBias,
                    CombatConstantKeys.Safety.SearchExtractHoldObjectiveBias));
        }

        public static SearchExtractHoldFramingConstants SafetyDefaults => new SearchExtractHoldFramingConstants(
            CombatConstantKeys.Safety.SearchExtractHoldOrthoSizeMin,
            CombatConstantKeys.Safety.SearchExtractHoldOrthoSizeMax,
            CombatConstantKeys.Safety.SearchExtractHoldMaxPanRadius,
            CombatConstantKeys.Safety.SearchExtractHoldViewportPad,
            CombatConstantKeys.Safety.SearchExtractHoldTopHudPad,
            CombatConstantKeys.Safety.SearchExtractHoldInnerPad,
            CombatConstantKeys.Safety.SearchExtractHoldZoomInDelaySeconds,
            CombatConstantKeys.Safety.SearchExtractHoldSmoothTimeOut,
            CombatConstantKeys.Safety.SearchExtractHoldSmoothTimeIn,
            CombatConstantKeys.Safety.SearchExtractHoldSampleInterval,
            CombatConstantKeys.Safety.SearchExtractHoldObjectiveBias);

        public float ClampOrthoSize(float size, float globalMin, float globalMax)
        {
            var min = Mathf.Max(OrthoSizeMin, globalMin);
            var max = Mathf.Min(OrthoSizeMax, globalMax);
            if (max < min)
            {
                max = min;
            }

            return Mathf.Clamp(size, min, max);
        }

        public Vector3 ClampLookAt(Vector3 lookAt, Vector3 objective)
        {
            var flat = new Vector3(lookAt.x, objective.y, lookAt.z);
            var origin = new Vector3(objective.x, objective.y, objective.z);
            var dx = flat.x - origin.x;
            var dz = flat.z - origin.z;
            var radius = MaxPanRadius;
            if (radius <= 0f)
            {
                return origin;
            }

            var sq = dx * dx + dz * dz;
            if (sq <= radius * radius + 1e-6f)
            {
                return flat;
            }

            var mag = Mathf.Sqrt(sq);
            return new Vector3(
                origin.x + dx * (radius / mag),
                origin.y,
                origin.z + dz * (radius / mag));
        }
    }
}
