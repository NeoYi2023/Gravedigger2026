using System;
using System.Collections.Generic;
using Gravedigger2026.Core.Config;
using Gravedigger2026.Gameplay.SearchExtract;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Gravedigger2026.Gameplay.PushMap
{
    /// <summary>
    /// PushMap Combat camera follow (PM-09 Approach B / SPEC_03 §3.14).
    /// Auto: look-at CameraFollowPath at max living-loyal projection s; SmoothDamp retreat
    /// when the lead drops; freeze when none. Missing path falls back to closest loyal.
    /// Auto presentation: world-XZ FollowDeadzone + SmoothDamp; Snap on EnterAuto.
    /// Manual: LMB drag pans XZ mirrored to screen delta (grab-map); ResumeFollow returns to Auto.
    /// Scroll wheel zooms orthographicSize (forward zoom-in); clamp from CombatConstantConfig.
    /// SearchExtract HoldFraming (SE-CAM-02 / SPEC_03 §3.19): SetHoldFraming / Clear / Freeze;
    /// PushMap Stage must not call those APIs. ResumeFollow during Hold returns to Hold, not rail.
    /// Prepare path preview lives on FormationEditor (not this controller).
    /// </summary>
    public sealed class PushMapCameraFollowController : MonoBehaviour
    {
        public enum Mode
        {
            Auto = 0,
            Manual = 1,
        }

        private float _dragThresholdPixels = CombatConstantKeys.Safety.CameraDragThresholdPixels;
        private float _zoomStepPerNotch = CombatConstantKeys.Safety.CameraZoomStepPerNotch;
        private float _orthoSizeMin = CombatConstantKeys.Safety.CameraOrthoSizeMin;
        private float _orthoSizeMax = CombatConstantKeys.Safety.CameraOrthoSizeMax;
        private float _followDeadzone = CombatConstantKeys.Safety.CameraFollowDeadzone;
        private float _followSmoothTime = CombatConstantKeys.Safety.CameraFollowSmoothTime;
        private CameraPresentationConstants _presentationConstants = CameraPresentationConstants.SafetyDefaults;

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

        private readonly SearchExtractHoldFramingSolver _holdSolver = new SearchExtractHoldFramingSolver();
        private readonly List<Vector3> _holdLoyalScratch = new List<Vector3>(16);
        private bool _holdActive;
        private bool _holdFrozen;
        private bool _holdUserZoomOverride;
        private bool _holdHasTarget;
        private Vector3 _holdTargetLookAt;
        private float _holdTargetSize;
        private bool _holdWantsExpand;
        private float _sizeSmoothVelocity;

        public Mode CurrentMode => _mode;
        public bool IsHoldFramingActive => _holdActive;

        public event Action<Mode> ModeChanged;

        public void ApplyPresentationConstants(CameraPresentationConstants cam)
        {
            _presentationConstants = cam;
            _dragThresholdPixels = Mathf.Max(0f, cam.DragThresholdPixels);
            _zoomStepPerNotch = Mathf.Max(0.01f, cam.ZoomStepPerNotch);
            _orthoSizeMin = Mathf.Max(0.01f, cam.OrthoSizeMin);
            _orthoSizeMax = Mathf.Max(_orthoSizeMin, cam.OrthoSizeMax);
            _followDeadzone = Mathf.Max(0f, cam.FollowDeadzone);
            _followSmoothTime = Mathf.Max(0.01f, cam.FollowSmoothTime);
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
        /// SearchExtract-only. Starts HoldFraming without Snap; current pose is the SmoothDamp origin.
        /// </summary>
        public void SetHoldFraming()
        {
            _holdActive = true;
            _holdFrozen = false;
            _holdUserZoomOverride = false;
            _holdHasTarget = false;
            _holdWantsExpand = false;
            _sizeSmoothVelocity = 0f;
            _smoothVelocity = Vector3.zero;
            _holdSolver.Reset();
        }

        /// <summary>SearchExtract-only. Drops Hold and returns Auto follow to CameraFollowPath.</summary>
        public void ClearHoldFraming()
        {
            ResetHoldState();
        }

        /// <summary>
        /// SearchExtract-only. UI-032: keep the last Hold look-at + Size (no sudden zoom-in on clear).
        /// </summary>
        public void FreezeHoldFraming()
        {
            if (!_holdActive)
            {
                return;
            }

            if (!_holdHasTarget)
            {
                TryRefreshHoldTarget(0f);
            }

            _holdFrozen = true;
        }

        public void EnableForCombat()
        {
            ResetHoldState();
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
            _combatActive = false;
            _dragArmed = false;
            _mode = Mode.Auto;
            _smoothVelocity = Vector3.zero;
            ResetHoldState();
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
            if (_mode == Mode.Manual)
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

            HandleDragInput();
            HandleScrollZoom();

            if (_mode != Mode.Auto)
            {
                return;
            }

            if (_holdActive)
            {
                TickHoldFraming();
                return;
            }

            if (!TryGetLookAt(out var lookAt))
            {
                return;
            }

            var p = _camera.transform.position;
            var desired = _presentationConstants.ResolveCombatCameraPosition(lookAt);
            var dx = desired.x - p.x;
            var dz = desired.z - p.z;
            var distSq = dx * dx + dz * dz;
            if (distSq <= _followDeadzone * _followDeadzone)
            {
                _smoothVelocity = Vector3.zero;
                return;
            }

            var next = Vector3.SmoothDamp(
                p,
                desired,
                ref _smoothVelocity,
                _followSmoothTime);
            _camera.transform.position = next;
        }

        private void SnapFollowToLookAt()
        {
            _smoothVelocity = Vector3.zero;
            if (_camera == null || !TryGetLookAt(out var lookAt))
            {
                return;
            }

            _camera.transform.position = _presentationConstants.ResolveCombatCameraPosition(lookAt);
        }

        private bool TryGetLookAt(out Vector3 worldXz)
        {
            if (_holdActive)
            {
                if (!_holdHasTarget)
                {
                    TryRefreshHoldTarget(0f);
                }

                if (_holdHasTarget)
                {
                    worldXz = _holdTargetLookAt;
                    return true;
                }
            }

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
            if (_holdActive)
            {
                _holdUserZoomOverride = true;
                _sizeSmoothVelocity = 0f;
            }
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
            if (!_combatActive)
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

        private void TickHoldFraming()
        {
            if (_camera == null)
            {
                return;
            }

            if (!_holdFrozen || !_holdHasTarget)
            {
                TryRefreshHoldTarget(Time.deltaTime);
            }

            if (!_holdHasTarget)
            {
                return;
            }

            var hold = _presentationConstants.HoldFraming;
            var expanding = _holdWantsExpand
                             || _holdTargetSize > _camera.orthographicSize + 0.01f;
            var smoothTime = expanding ? hold.SmoothTimeOut : hold.SmoothTimeIn;
            var p = _camera.transform.position;
            var desired = _presentationConstants.ResolveCombatCameraPosition(_holdTargetLookAt);
            _camera.transform.position = Vector3.SmoothDamp(
                p,
                desired,
                ref _smoothVelocity,
                smoothTime);

            if (_holdUserZoomOverride)
            {
                return;
            }

            _camera.orthographicSize = Mathf.SmoothDamp(
                _camera.orthographicSize,
                _holdTargetSize,
                ref _sizeSmoothVelocity,
                smoothTime);
        }

        private void TryRefreshHoldTarget(float deltaTime)
        {
            if (_camera == null)
            {
                return;
            }

            CollectHoldLoyalPositions();
            var objective = _currentObjectiveProvider?.Invoke();
            var objectivePos = objective != null ? objective.transform.position : Vector3.zero;
            var currentLookAt = _presentationConstants.ResolveCombatLookAt(
                _camera.transform.position,
                objectivePos.y);
            if (!_holdSolver.TryEvaluate(
                    _camera,
                    objectivePos,
                    _holdLoyalScratch,
                    currentLookAt,
                    _camera.orthographicSize,
                    _presentationConstants,
                    deltaTime,
                    out var target))
            {
                return;
            }

            _holdTargetLookAt = target.LookAt;
            _holdTargetSize = target.OrthoSize;
            _holdWantsExpand = target.WantsExpand;
            _holdHasTarget = true;
        }

        private void CollectHoldLoyalPositions()
        {
            _holdLoyalScratch.Clear();
            if (_advanceViews == null)
            {
                return;
            }

            for (var i = 0; i < _advanceViews.Count; i++)
            {
                var view = _advanceViews[i];
                if (!IsFollowable(view))
                {
                    continue;
                }

                _holdLoyalScratch.Add(view.transform.position);
            }
        }

        private void ResetHoldState()
        {
            _holdActive = false;
            _holdFrozen = false;
            _holdUserZoomOverride = false;
            _holdHasTarget = false;
            _holdWantsExpand = false;
            _holdTargetSize = 0f;
            _sizeSmoothVelocity = 0f;
            _smoothVelocity = Vector3.zero;
            _holdLoyalScratch.Clear();
            _holdSolver.Reset();
        }
    }
}
