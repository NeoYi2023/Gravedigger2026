using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Gravedigger2026.Gameplay.PushMap
{
    /// <summary>
    /// PushMap Combat camera follow (PM-09 Approach A / SPEC_03 §3.14).
    /// Auto: sticky-follow closest loyal AdvanceView to CurrentObjective; freeze when none.
    /// Auto presentation: world-XZ FollowDeadzone + SmoothDamp; Snap on EnterAuto / repick.
    /// Manual: LMB drag pans XZ; ResumeFollow returns to Auto.
    /// Scroll wheel zooms orthographicSize in [0.5, 20] (forward zoom-in).
    /// </summary>
    public sealed class PushMapCameraFollowController : MonoBehaviour
    {
        public enum Mode
        {
            Auto = 0,
            Manual = 1,
        }

        private const float DragThresholdPixels = 4f;
        private const float ZoomStepPerNotch = 0.5f;
        private const float OrthoSizeMin = 0.5f;
        private const float OrthoSizeMax = 20f;
        private const float FollowDeadzone = 0.15f;
        private const float FollowSmoothTime = 0.25f;

        private Camera _camera;
        private IReadOnlyList<PushMapAdvanceView> _advanceViews;
        private Func<ObjectivePoint> _currentObjectiveProvider;
        private GameObject _resumeButtonRoot;
        private Button _resumeButton;
        private bool _combatActive;
        private Mode _mode = Mode.Auto;
        private PushMapAdvanceView _followTarget;
        private bool _dragArmed;
        private Vector3 _lastMousePosition;
        private float _dragAccumPixels;
        private Vector3 _smoothVelocity;

        public Mode CurrentMode => _mode;

        public event Action<Mode> ModeChanged;

        public void Bind(
            Camera camera,
            IReadOnlyList<PushMapAdvanceView> advanceViews,
            Func<ObjectivePoint> currentObjectiveProvider)
        {
            _camera = camera;
            _advanceViews = advanceViews;
            _currentObjectiveProvider = currentObjectiveProvider;
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

        public void EnableForCombat()
        {
            _combatActive = true;
            _dragArmed = false;
            _dragAccumPixels = 0f;
            EnterAuto(silent: true);
        }

        public void Disable()
        {
            _combatActive = false;
            _followTarget = null;
            _dragArmed = false;
            _mode = Mode.Auto;
            _smoothVelocity = Vector3.zero;
            RefreshResumeButtonVisibility();
        }

        public void EnterAuto()
        {
            EnterAuto(silent: false);
        }

        private void EnterAuto(bool silent)
        {
            _mode = Mode.Auto;
            _followTarget = null;
            TryPickClosestLoyal();
            SnapFollowToTarget();
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
            _followTarget = null;
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

            if (!IsFollowable(_followTarget))
            {
                var previous = _followTarget;
                TryPickClosestLoyal();
                if (_followTarget != null && _followTarget != previous)
                {
                    SnapFollowToTarget();
                }
            }

            if (_followTarget == null)
            {
                return;
            }

            var p = _camera.transform.position;
            var t = _followTarget.transform.position;
            var dx = t.x - p.x;
            var dz = t.z - p.z;
            var distSq = dx * dx + dz * dz;
            if (distSq <= FollowDeadzone * FollowDeadzone)
            {
                _smoothVelocity = Vector3.zero;
                return;
            }

            var desired = new Vector3(t.x, p.y, t.z);
            var next = Vector3.SmoothDamp(
                p,
                desired,
                ref _smoothVelocity,
                FollowSmoothTime);
            _smoothVelocity.y = 0f;
            _camera.transform.position = new Vector3(next.x, p.y, next.z);
        }

        private void SnapFollowToTarget()
        {
            _smoothVelocity = Vector3.zero;
            if (_camera == null || !IsFollowable(_followTarget))
            {
                return;
            }

            var p = _camera.transform.position;
            var t = _followTarget.transform.position;
            _camera.transform.position = new Vector3(t.x, p.y, t.z);
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

            var size = _camera.orthographicSize - scroll * ZoomStepPerNotch;
            _camera.orthographicSize = Mathf.Clamp(size, OrthoSizeMin, OrthoSizeMax);
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

            if (_dragAccumPixels < DragThresholdPixels && _mode != Mode.Manual)
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
            var worldDelta = new Vector3(screenDelta.x * unitsPerPixelX, 0f, screenDelta.y * unitsPerPixelY);
            _camera.transform.position += worldDelta;
        }

        private void TryPickClosestLoyal()
        {
            _followTarget = null;
            if (_advanceViews == null || _currentObjectiveProvider == null)
            {
                return;
            }

            var objective = _currentObjectiveProvider();
            var objectivePos = objective != null
                ? objective.transform.position
                : Vector3.zero;
            var bestDistSq = float.MaxValue;

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
                    _followTarget = view;
                }
            }
        }

        private static bool IsFollowable(PushMapAdvanceView view)
        {
            return view != null
                   && view.isActiveAndEnabled
                   && view.gameObject.activeInHierarchy
                   && !view.IsRebel;
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
    }
}
