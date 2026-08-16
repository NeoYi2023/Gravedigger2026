using System;
using System.Collections;
using System.Collections.Generic;
using Gravedigger2026.Core.Config;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Gravedigger2026.Gameplay.PushMap
{
    /// <summary>
    /// PushMap Combat camera follow (PM-09 Approach B / SPEC_03 §3.14).
    /// Intro: reverse author-waypoint sweep WP_End→WP_Start before Auto.
    /// Auto: look-at CameraFollowPath at max living-loyal projection s; SmoothDamp retreat
    /// when the lead drops; freeze when none. Missing path falls back to closest loyal.
    /// Auto presentation: world-XZ FollowDeadzone + SmoothDamp; Snap on EnterAuto.
    /// Manual: LMB drag pans XZ mirrored to screen delta (grab-map); ResumeFollow returns to Auto.
    /// Scroll wheel zooms orthographicSize (forward zoom-in); clamp from CombatConstantConfig.
    /// </summary>
    public sealed class PushMapCameraFollowController : MonoBehaviour
    {
        public enum Mode
        {
            Auto = 0,
            Manual = 1,
            Intro = 2,
        }

        private float _dragThresholdPixels = CombatConstantKeys.Safety.CameraDragThresholdPixels;
        private float _zoomStepPerNotch = CombatConstantKeys.Safety.CameraZoomStepPerNotch;
        private float _orthoSizeMin = CombatConstantKeys.Safety.CameraOrthoSizeMin;
        private float _orthoSizeMax = CombatConstantKeys.Safety.CameraOrthoSizeMax;
        private float _followDeadzone = CombatConstantKeys.Safety.CameraFollowDeadzone;
        private float _followSmoothTime = CombatConstantKeys.Safety.CameraFollowSmoothTime;
        private float _introSpeed = CombatConstantKeys.Safety.PushMapCameraIntroSpeed;
        private float _introWaypointDwell =
            CombatConstantKeys.Safety.PushMapCameraIntroWaypointDwellSeconds;

        private Camera _camera;
        private IReadOnlyList<PushMapAdvanceView> _advanceViews;
        private Func<ObjectivePoint> _currentObjectiveProvider;
        private PushMapCameraPath _followPath;
        private GameObject _resumeButtonRoot;
        private Button _resumeButton;
        private bool _combatActive;
        private Mode _mode = Mode.Auto;
        private bool _dragArmed;
        private Vector3 _lastMousePosition;
        private float _dragAccumPixels;
        private Vector3 _smoothVelocity;
        private bool _loggedMissingPath;
        private Coroutine _introRoutine;
        private Action _introComplete;
        private readonly List<float> _introWaypointProgress = new List<float>(8);

        public Mode CurrentMode => _mode;

        public event Action<Mode> ModeChanged;

        public void ApplyPresentationConstants(CameraPresentationConstants cam)
        {
            _dragThresholdPixels = Mathf.Max(0f, cam.DragThresholdPixels);
            _zoomStepPerNotch = Mathf.Max(0.01f, cam.ZoomStepPerNotch);
            _orthoSizeMin = Mathf.Max(0.01f, cam.OrthoSizeMin);
            _orthoSizeMax = Mathf.Max(_orthoSizeMin, cam.OrthoSizeMax);
            _followDeadzone = Mathf.Max(0f, cam.FollowDeadzone);
            _followSmoothTime = Mathf.Max(0.01f, cam.FollowSmoothTime);
            _introSpeed = Mathf.Max(0.01f, cam.PushMapIntroSpeed);
            _introWaypointDwell = Mathf.Max(0f, cam.PushMapIntroWaypointDwellSeconds);
        }

        public void Bind(
            Camera camera,
            IReadOnlyList<PushMapAdvanceView> advanceViews,
            Func<ObjectivePoint> currentObjectiveProvider,
            PushMapCameraPath followPath = null)
        {
            _camera = camera;
            _advanceViews = advanceViews;
            _currentObjectiveProvider = currentObjectiveProvider;
            _followPath = followPath;
            _loggedMissingPath = false;
        }

        public void BindResumeButton(GameObject root, Button button)
        {
            if (_resumeButton != null)
            {
                _resumeButton.onClick.RemoveListener(HandleResumeClicked);
            }

            _resumeButtonRoot = root;
            _resumeButton = button;
            if (_resumeButton != null)
            {
                _resumeButton.onClick.AddListener(HandleResumeClicked);
            }

            RefreshResumeButtonVisibility();
        }

        /// <summary>
        /// Play StartBattle rail intro (WP_End → WP_Start). Invokes onComplete when done or skipped.
        /// Returns false if intro cannot run (onComplete still invoked synchronously).
        /// </summary>
        public bool TryPlayCombatIntro(Action onComplete)
        {
            StopIntroRoutine();
            _introComplete = onComplete;
            _combatActive = true;
            _dragArmed = false;
            _dragAccumPixels = 0f;

            if (_followPath != null && !_followPath.HasBakedPath)
            {
                if (!_followPath.TryBake(out var error) && !_loggedMissingPath)
                {
                    Debug.LogWarning(
                        $"[PushMapCameraFollow] CameraFollowPath bake empty at Intro: {error}.");
                    _loggedMissingPath = true;
                }
            }

            if (_followPath == null ||
                !_followPath.HasBakedPath ||
                !_followPath.TryBuildAuthorWaypointProgresses(_introWaypointProgress))
            {
                if (!_loggedMissingPath)
                {
                    Debug.LogWarning(
                        "[PushMapCameraFollow] Intro skipped — missing CameraFollowPath or <2 author waypoints.");
                    _loggedMissingPath = true;
                }

                CompleteIntro(skipped: true);
                return false;
            }

            _mode = Mode.Intro;
            _smoothVelocity = Vector3.zero;
            RefreshResumeButtonVisibility();
            ModeChanged?.Invoke(_mode);
            _introRoutine = StartCoroutine(IntroRoutine());
            return true;
        }

        public void EnableForCombat()
        {
            StopIntroRoutine();
            _combatActive = true;
            _dragArmed = false;
            _dragAccumPixels = 0f;
            if (_followPath != null && !_followPath.HasBakedPath)
            {
                if (!_followPath.TryBake(out var error) && !_loggedMissingPath)
                {
                    Debug.LogWarning(
                        $"[PushMapCameraFollow] CameraFollowPath bake empty at StartBattle: {error}. " +
                        "Falling back to closest loyal soldier.");
                    _loggedMissingPath = true;
                }
            }
            else if (_followPath == null && !_loggedMissingPath)
            {
                Debug.LogWarning(
                    "[PushMapCameraFollow] No CameraFollowPath on map. " +
                    "Falling back to closest loyal soldier.");
                _loggedMissingPath = true;
            }

            EnterAuto(silent: true);
        }

        public void Disable()
        {
            StopIntroRoutine();
            _combatActive = false;
            _dragArmed = false;
            _mode = Mode.Auto;
            _smoothVelocity = Vector3.zero;
            _introComplete = null;
            RefreshResumeButtonVisibility();
        }

        public void EnterAuto()
        {
            EnterAuto(silent: false);
        }

        private void EnterAuto(bool silent)
        {
            _mode = Mode.Auto;
            SnapFollowToLookAt();
            RefreshResumeButtonVisibility();
            if (!silent)
            {
                ModeChanged?.Invoke(_mode);
            }
        }

        private void EnterManual()
        {
            if (_mode == Mode.Manual || _mode == Mode.Intro)
            {
                return;
            }

            _mode = Mode.Manual;
            _smoothVelocity = Vector3.zero;
            RefreshResumeButtonVisibility();
            ModeChanged?.Invoke(_mode);
        }

        private void OnDisable()
        {
            StopIntroRoutine();
            if (_resumeButton != null)
            {
                _resumeButton.onClick.RemoveListener(HandleResumeClicked);
            }
        }

        private void LateUpdate()
        {
            if (!_combatActive || _camera == null)
            {
                return;
            }

            if (_mode == Mode.Intro)
            {
                HandleScrollZoom();
                return;
            }

            HandleDragInput();
            HandleScrollZoom();

            if (_mode != Mode.Auto)
            {
                return;
            }

            if (!TryGetLookAt(out var lookAt))
            {
                return;
            }

            var p = _camera.transform.position;
            var dx = lookAt.x - p.x;
            var dz = lookAt.z - p.z;
            var distSq = dx * dx + dz * dz;
            if (distSq <= _followDeadzone * _followDeadzone)
            {
                _smoothVelocity = Vector3.zero;
                return;
            }

            var desired = new Vector3(lookAt.x, p.y, lookAt.z);
            var next = Vector3.SmoothDamp(
                p,
                desired,
                ref _smoothVelocity,
                _followSmoothTime);
            _smoothVelocity.y = 0f;
            _camera.transform.position = new Vector3(next.x, p.y, next.z);
        }

        private IEnumerator IntroRoutine()
        {
            var path = _followPath;
            var progresses = _introWaypointProgress;
            var totalLength = Mathf.Max(1e-4f, path.TotalLength);
            var speed = Mathf.Max(0.01f, _introSpeed);
            var dwell = Mathf.Max(0f, _introWaypointDwell);

            // progresses: ascending WP_Start..WP_End; traverse reverse.
            var index = progresses.Count - 1;
            SnapCameraToProgress(path, progresses[index]);
            if (dwell > 0f)
            {
                yield return new WaitForSeconds(dwell);
            }

            while (index > 0)
            {
                var fromS = progresses[index];
                var toS = progresses[index - 1];
                var distance = Mathf.Abs(fromS - toS) * totalLength;
                var duration = distance / speed;
                if (duration <= 1e-4f)
                {
                    SnapCameraToProgress(path, toS);
                }
                else
                {
                    var elapsed = 0f;
                    while (elapsed < duration)
                    {
                        elapsed += Time.deltaTime;
                        var t = Mathf.Clamp01(elapsed / duration);
                        var s = Mathf.Lerp(fromS, toS, t);
                        SnapCameraToProgress(path, s);
                        yield return null;
                    }

                    SnapCameraToProgress(path, toS);
                }

                index--;
                if (dwell > 0f)
                {
                    yield return new WaitForSeconds(dwell);
                }
            }

            _introRoutine = null;
            CompleteIntro(skipped: false);
        }

        private void SnapCameraToProgress(PushMapCameraPath path, float s)
        {
            if (_camera == null || path == null || !path.TryEvaluate(s, out var lookAt))
            {
                return;
            }

            var p = _camera.transform.position;
            _camera.transform.position = new Vector3(lookAt.x, p.y, lookAt.z);
            _smoothVelocity = Vector3.zero;
        }

        private void CompleteIntro(bool skipped)
        {
            var cb = _introComplete;
            _introComplete = null;
            _introRoutine = null;
            if (skipped)
            {
                Debug.Log("[PushMapCameraFollow] Combat intro skipped.");
            }
            else
            {
                Debug.Log("[PushMapCameraFollow] Combat intro complete.");
            }

            cb?.Invoke();
        }

        private void StopIntroRoutine()
        {
            if (_introRoutine != null)
            {
                StopCoroutine(_introRoutine);
                _introRoutine = null;
            }
        }

        private void SnapFollowToLookAt()
        {
            _smoothVelocity = Vector3.zero;
            if (_camera == null || !TryGetLookAt(out var lookAt))
            {
                return;
            }

            var p = _camera.transform.position;
            _camera.transform.position = new Vector3(lookAt.x, p.y, lookAt.z);
        }

        private bool TryGetLookAt(out Vector3 worldXz)
        {
            if (_followPath != null && _followPath.HasBakedPath)
            {
                return TryGetPathLookAt(out worldXz);
            }

            return TryGetClosestLoyalLookAt(out worldXz);
        }

        private bool TryGetPathLookAt(out Vector3 worldXz)
        {
            worldXz = default;
            if (_advanceViews == null)
            {
                return false;
            }

            var any = false;
            var sMax = 0f;
            for (var i = 0; i < _advanceViews.Count; i++)
            {
                var view = _advanceViews[i];
                if (!IsFollowable(view))
                {
                    continue;
                }

                if (!_followPath.TryProjectProgress(view.transform.position, out var s))
                {
                    continue;
                }

                any = true;
                if (s > sMax)
                {
                    sMax = s;
                }
            }

            if (!any)
            {
                return false;
            }

            return _followPath.TryEvaluate(sMax, out worldXz);
        }

        private bool TryGetClosestLoyalLookAt(out Vector3 worldXz)
        {
            worldXz = default;
            if (_advanceViews == null || _currentObjectiveProvider == null)
            {
                return false;
            }

            var objective = _currentObjectiveProvider();
            var objectivePos = objective != null
                ? objective.transform.position
                : Vector3.zero;
            var bestDistSq = float.MaxValue;
            PushMapAdvanceView best = null;

            for (var i = 0; i < _advanceViews.Count; i++)
            {
                var view = _advanceViews[i];
                if (!IsFollowable(view))
                {
                    continue;
                }

                var pos = view.transform.position;
                var dx = pos.x - objectivePos.x;
                var dz = pos.z - objectivePos.z;
                var distSq = dx * dx + dz * dz;
                if (distSq < bestDistSq)
                {
                    bestDistSq = distSq;
                    best = view;
                }
            }

            if (best == null)
            {
                return false;
            }

            worldXz = best.transform.position;
            return true;
        }

        private void HandleScrollZoom()
        {
            if (IsPointerOverUi())
            {
                return;
            }

            var scroll = Input.mouseScrollDelta.y;
            if (Mathf.Abs(scroll) < 0.01f)
            {
                return;
            }

            var size = _camera.orthographicSize - scroll * _zoomStepPerNotch;
            _camera.orthographicSize = Mathf.Clamp(size, _orthoSizeMin, _orthoSizeMax);
        }

        private void HandleDragInput()
        {
            if (Input.GetMouseButtonDown(0))
            {
                if (IsPointerOverUi())
                {
                    _dragArmed = false;
                    return;
                }

                _dragArmed = true;
                _dragAccumPixels = 0f;
                _lastMousePosition = Input.mousePosition;
                return;
            }

            if (!_dragArmed)
            {
                return;
            }

            if (Input.GetMouseButtonUp(0))
            {
                _dragArmed = false;
                _dragAccumPixels = 0f;
                return;
            }

            if (!Input.GetMouseButton(0))
            {
                return;
            }

            var mouse = Input.mousePosition;
            var screenDelta = mouse - _lastMousePosition;
            _lastMousePosition = mouse;
            _dragAccumPixels += screenDelta.magnitude;

            if (_dragAccumPixels < _dragThresholdPixels && _mode != Mode.Manual)
            {
                return;
            }

            if (_mode != Mode.Manual)
            {
                EnterManual();
            }

            ApplyPan(screenDelta);
        }

        private void ApplyPan(Vector3 screenDelta)
        {
            if (_camera == null || !_camera.orthographic)
            {
                return;
            }

            var unitsPerPixelY = (2f * _camera.orthographicSize) / Mathf.Max(1f, Screen.height);
            var unitsPerPixelX = (2f * _camera.orthographicSize * _camera.aspect) / Mathf.Max(1f, Screen.width);
            var worldDelta = new Vector3(-screenDelta.x * unitsPerPixelX, 0f, -screenDelta.y * unitsPerPixelY);
            _camera.transform.position += worldDelta;
        }

        private static bool IsFollowable(PushMapAdvanceView view)
        {
            return view != null
                   && view.isActiveAndEnabled
                   && view.gameObject.activeInHierarchy
                   && !view.IsRebel
                   && view.IsCombatActive;
        }

        private void HandleResumeClicked()
        {
            if (!_combatActive || _mode == Mode.Intro)
            {
                return;
            }

            EnterAuto();
        }

        private void RefreshResumeButtonVisibility()
        {
            if (_resumeButtonRoot == null)
            {
                return;
            }

            var show = _combatActive && _mode == Mode.Manual;
            if (_resumeButtonRoot.activeSelf != show)
            {
                _resumeButtonRoot.SetActive(show);
            }
        }

        private static bool IsPointerOverUi()
        {
            if (EventSystem.current == null)
            {
                return false;
            }

            return EventSystem.current.IsPointerOverGameObject();
        }
    }
}
