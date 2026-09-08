using System.Collections.Generic;
using Gravedigger2026.Core.Config;
using UnityEngine;

namespace Gravedigger2026.Gameplay.SearchExtract
{
    /// <summary>
    /// HoldFraming pure solver (SPEC_03 §3.19 Approach B / SPEC_04 §9.20b).
    /// Outputs target look-at + ortho Size. Stage on/off and SmoothDamp live on
    /// <c>PushMapCameraFollowController</c> (SE-CAM-02).
    /// </summary>
    public sealed class SearchExtractHoldFramingSolver
    {
        public readonly struct CameraBasis
        {
            public Vector3 Right { get; }
            public Vector3 Up { get; }
            public Vector3 Forward { get; }
            public Vector3 Position { get; }
            public float Aspect { get; }

            public CameraBasis(Vector3 right, Vector3 up, Vector3 forward, Vector3 position, float aspect)
            {
                Right = right.sqrMagnitude > 1e-8f ? right.normalized : Vector3.right;
                Up = up.sqrMagnitude > 1e-8f ? up.normalized : Vector3.forward;
                Forward = forward.sqrMagnitude > 1e-8f ? forward.normalized : Vector3.down;
                Position = position;
                Aspect = aspect > 0.05f ? aspect : 16f / 9f;
            }

            public static CameraBasis CreateTopDown(Vector3 lookAt, float heightY, float aspect)
            {
                return new CameraBasis(
                    Vector3.right,
                    Vector3.forward,
                    Vector3.down,
                    lookAt + Vector3.up * heightY,
                    aspect);
            }

            public static bool TryFromCamera(Camera camera, out CameraBasis basis)
            {
                if (camera == null)
                {
                    basis = default;
                    return false;
                }

                var t = camera.transform;
                basis = new CameraBasis(t.right, t.up, t.forward, t.position, camera.aspect);
                return true;
            }
        }

        public readonly struct HoldFramingTarget
        {
            public Vector3 LookAt { get; }
            public float OrthoSize { get; }
            public bool WantsExpand { get; }
            public bool AllowsTighten { get; }

            public HoldFramingTarget(Vector3 lookAt, float orthoSize, bool wantsExpand, bool allowsTighten)
            {
                LookAt = lookAt;
                OrthoSize = orthoSize;
                WantsExpand = wantsExpand;
                AllowsTighten = allowsTighten;
            }
        }

        private readonly struct ViewportInsets
        {
            public float Left { get; }
            public float Right { get; }
            public float Bottom { get; }
            public float Top { get; }

            public ViewportInsets(float left, float right, float bottom, float top)
            {
                Left = left;
                Right = right;
                Bottom = bottom;
                Top = top;
            }
        }

        private float _timeSinceSample;
        private float _innerDwellSeconds;
        private bool _hasSample;
        private bool _lastAllInsideInner;
        private HoldFramingTarget _lastTarget;

        public void Reset()
        {
            _timeSinceSample = 0f;
            _innerDwellSeconds = 0f;
            _hasSample = false;
            _lastAllInsideInner = false;
            _lastTarget = default;
        }

        public bool TryEvaluate(
            Camera camera,
            Vector3 objectiveWorld,
            IReadOnlyList<Vector3> loyalWorldPositions,
            Vector3 currentLookAt,
            float currentOrthoSize,
            in CameraPresentationConstants presentation,
            float deltaTime,
            out HoldFramingTarget target)
        {
            if (!CameraBasis.TryFromCamera(camera, out var basis))
            {
                target = default;
                return false;
            }

            target = Evaluate(
                basis,
                objectiveWorld,
                loyalWorldPositions,
                currentLookAt,
                currentOrthoSize,
                presentation,
                deltaTime);
            return true;
        }

        public HoldFramingTarget Evaluate(
            in CameraBasis camera,
            Vector3 objectiveWorld,
            IReadOnlyList<Vector3> loyalWorldPositions,
            Vector3 currentLookAt,
            float currentOrthoSize,
            in CameraPresentationConstants presentation,
            float deltaTime)
        {
            var hold = presentation.HoldFraming;
            var dt = Mathf.Max(0f, deltaTime);
            var size = Mathf.Max(0.01f, currentOrthoSize);
            var lookAt = FlattenY(currentLookAt, objectiveWorld.y);

            _timeSinceSample += dt;
            var interval = hold.SampleInterval;
            var due = !_hasSample || interval <= 0f || _timeSinceSample >= interval;
            if (!due)
            {
                if (_lastAllInsideInner)
                {
                    _innerDwellSeconds += dt;
                }
                else
                {
                    _innerDwellSeconds = 0f;
                }

                return _lastTarget;
            }

            _timeSinceSample = 0f;
            _hasSample = true;
            ResolveInsets(hold, out var outer, out var inner);

            var inRadiusCount = 0;
            var anyOuter = false;
            var allInner = true;
            var minX = 1f;
            var maxX = 0f;
            var minY = 1f;
            var maxY = 0f;
            var hasViewport = false;
            var radius = hold.MaxPanRadius;
            var radiusSq = radius * radius;

            if (loyalWorldPositions != null)
            {
                for (var i = 0; i < loyalWorldPositions.Count; i++)
                {
                    var world = loyalWorldPositions[i];
                    var dx = world.x - objectiveWorld.x;
                    var dz = world.z - objectiveWorld.z;
                    if (dx * dx + dz * dz > radiusSq + 1e-6f)
                    {
                        continue;
                    }

                    inRadiusCount++;
                    if (!TryWorldToViewport(camera, world, lookAt, size, out var vp, out var inFront)
                        || !inFront
                        || !IsInside(vp, outer))
                    {
                        anyOuter = true;
                        allInner = false;
                    }
                    else if (!IsInside(vp, inner))
                    {
                        allInner = false;
                    }

                    if (inFront)
                    {
                        if (!hasViewport)
                        {
                            minX = maxX = vp.x;
                            minY = maxY = vp.y;
                            hasViewport = true;
                        }
                        else
                        {
                            if (vp.x < minX) minX = vp.x;
                            if (vp.x > maxX) maxX = vp.x;
                            if (vp.y < minY) minY = vp.y;
                            if (vp.y > maxY) maxY = vp.y;
                        }
                    }
                }
            }

            if (inRadiusCount == 0)
            {
                allInner = false;
            }

            if (anyOuter)
            {
                _innerDwellSeconds = 0f;
            }
            else if (allInner)
            {
                _innerDwellSeconds += dt;
            }
            else
            {
                _innerDwellSeconds = 0f;
            }

            _lastAllInsideInner = allInner && !anyOuter;

            var allowsTighten = !anyOuter && allInner && _innerDwellSeconds >= hold.ZoomInDelaySeconds;
            Vector3 targetLook;
            float targetSize;
            if (inRadiusCount == 0)
            {
                targetLook = hold.ClampLookAt(objectiveWorld, objectiveWorld);
                targetSize = hold.ClampOrthoSize(size, presentation.OrthoSizeMin, presentation.OrthoSizeMax);
                _lastTarget = new HoldFramingTarget(targetLook, targetSize, false, false);
                return _lastTarget;
            }

            var soldierCenter = hasViewport
                ? UnprojectViewport(
                    camera,
                    lookAt,
                    size,
                    0.5f * (minX + maxX),
                    0.5f * (minY + maxY),
                    objectiveWorld.y)
                : FlattenY(objectiveWorld, objectiveWorld.y);
            targetLook = Vector3.Lerp(
                soldierCenter,
                FlattenY(objectiveWorld, objectiveWorld.y),
                hold.ObjectiveBias);
            targetLook = hold.ClampLookAt(targetLook, objectiveWorld);

            if (anyOuter)
            {
                var fit = FitOrthoSize(camera, loyalWorldPositions, objectiveWorld, radiusSq, targetLook, outer);
                targetSize = Mathf.Max(size, fit);
                targetSize = hold.ClampOrthoSize(targetSize, presentation.OrthoSizeMin, presentation.OrthoSizeMax);
                _lastTarget = new HoldFramingTarget(targetLook, targetSize, true, false);
                return _lastTarget;
            }

            if (allowsTighten)
            {
                var fit = FitOrthoSize(camera, loyalWorldPositions, objectiveWorld, radiusSq, targetLook, inner);
                targetSize = hold.ClampOrthoSize(fit, presentation.OrthoSizeMin, presentation.OrthoSizeMax);
                _lastTarget = new HoldFramingTarget(targetLook, targetSize, false, true);
                return _lastTarget;
            }

            targetLook = hold.ClampLookAt(lookAt, objectiveWorld);
            targetSize = hold.ClampOrthoSize(size, presentation.OrthoSizeMin, presentation.OrthoSizeMax);
            _lastTarget = new HoldFramingTarget(targetLook, targetSize, false, false);
            return _lastTarget;
        }

        private static void ResolveInsets(
            in SearchExtractHoldFramingConstants hold,
            out ViewportInsets outer,
            out ViewportInsets inner)
        {
            var outerTop = Mathf.Min(0.49f, hold.ViewportPad + hold.TopHudPad);
            outer = new ViewportInsets(
                hold.ViewportPad,
                hold.ViewportPad,
                hold.ViewportPad,
                outerTop);

            inner = new ViewportInsets(
                Mathf.Max(hold.InnerPad, outer.Left),
                Mathf.Max(hold.InnerPad, outer.Right),
                Mathf.Max(hold.InnerPad, outer.Bottom),
                Mathf.Max(hold.InnerPad, outer.Top));
        }

        private static bool TryWorldToViewport(
            in CameraBasis camera,
            Vector3 world,
            Vector3 lookAt,
            float orthoSize,
            out Vector2 viewport,
            out bool inFront)
        {
            var d = world - lookAt;
            var rx = Vector3.Dot(d, camera.Right);
            var uy = Vector3.Dot(d, camera.Up);
            var depth = Vector3.Dot(world - camera.Position, camera.Forward);
            inFront = depth > 0.001f;
            var halfH = 2f * Mathf.Max(0.01f, orthoSize);
            viewport = new Vector2(
                0.5f + rx / (halfH * camera.Aspect),
                0.5f + uy / halfH);
            return true;
        }

        private static bool IsInside(Vector2 viewport, in ViewportInsets insets)
        {
            return viewport.x >= insets.Left
                && viewport.x <= 1f - insets.Right
                && viewport.y >= insets.Bottom
                && viewport.y <= 1f - insets.Top;
        }

        private static Vector3 UnprojectViewport(
            in CameraBasis camera,
            Vector3 lookAt,
            float orthoSize,
            float viewportX,
            float viewportY,
            float groundY)
        {
            var halfH = 2f * Mathf.Max(0.01f, orthoSize);
            var world = lookAt
                + camera.Right * ((viewportX - 0.5f) * halfH * camera.Aspect)
                + camera.Up * ((viewportY - 0.5f) * halfH);
            return FlattenY(world, groundY);
        }

        private static float FitOrthoSize(
            in CameraBasis camera,
            IReadOnlyList<Vector3> loyalWorldPositions,
            Vector3 objectiveWorld,
            float radiusSq,
            Vector3 lookAt,
            in ViewportInsets insets)
        {
            var roomLeft = Mathf.Max(0.02f, 0.5f - insets.Left);
            var roomRight = Mathf.Max(0.02f, 0.5f - insets.Right);
            var roomDown = Mathf.Max(0.02f, 0.5f - insets.Bottom);
            var roomUp = Mathf.Max(0.02f, 0.5f - insets.Top);
            var needed = 0f;
            for (var i = 0; i < loyalWorldPositions.Count; i++)
            {
                var world = loyalWorldPositions[i];
                var dx = world.x - objectiveWorld.x;
                var dz = world.z - objectiveWorld.z;
                if (dx * dx + dz * dz > radiusSq + 1e-6f)
                {
                    continue;
                }

                var d = world - lookAt;
                var rx = Vector3.Dot(d, camera.Right);
                var uy = Vector3.Dot(d, camera.Up);
                if (rx >= 0f)
                {
                    needed = Mathf.Max(needed, rx / (2f * camera.Aspect * roomRight));
                }
                else
                {
                    needed = Mathf.Max(needed, -rx / (2f * camera.Aspect * roomLeft));
                }

                if (uy >= 0f)
                {
                    needed = Mathf.Max(needed, uy / (2f * roomUp));
                }
                else
                {
                    needed = Mathf.Max(needed, -uy / (2f * roomDown));
                }
            }

            return needed;
        }

        private static Vector3 FlattenY(Vector3 p, float y)
        {
            return new Vector3(p.x, y, p.z);
        }
    }
}
