using System;
using Gravedigger2026.Core.Pathing;
using UnityEngine;

namespace Gravedigger2026.Gameplay.Pathing
{
    /// <summary>
    /// Runtime TextMesh under soldier feet showing current GoalKind + effective move speed
    /// (SPEC_04 §9.7 Debug).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class WarriorTaskDebugLabelView : MonoBehaviour
    {
        private const float FootOffsetY = 0.02f;
        private const float FootOffsetZ = -0.38f;
        private const float CharacterSize = 0.12f;
        private const int FontSize = 12;
        private const float MoveSpeedDirtyEpsilon = 0.0001f;

        private MassMoveScheduler _scheduler;
        private int _moveId;
        private TextMesh _textMesh;
        private Transform _labelTransform;
        private GoalKind _lastKind;
        private bool _hasLastKind;
        private bool _visible;

        private Func<string> _extraText;
        private string _lastExtra;
        private Func<float> _resolveMoveSpeed;
        private float _lastMoveSpeed = float.NaN;

        public void Bind(
            MassMoveScheduler scheduler,
            int moveId,
            Func<string> extraText = null,
            Func<float> resolveMoveSpeed = null)
        {
            _scheduler = scheduler;
            _moveId = moveId;
            _extraText = extraText;
            _resolveMoveSpeed = resolveMoveSpeed;
            _lastExtra = null;
            _lastMoveSpeed = float.NaN;
            EnsureLabel();
            ApplyVisibility(WarriorTaskLabelSettings.Enabled);
            RefreshText(force: true);
        }

        private void OnEnable()
        {
            WarriorTaskLabelSettings.EnabledChanged += HandleEnabledChanged;
            ApplyVisibility(WarriorTaskLabelSettings.Enabled);
        }

        private void OnDisable()
        {
            WarriorTaskLabelSettings.EnabledChanged -= HandleEnabledChanged;
        }

        private void LateUpdate()
        {
            if (!_visible || _scheduler == null || _moveId == 0)
            {
                return;
            }

            RefreshText(force: false);
        }

        private void HandleEnabledChanged(bool enabled)
        {
            ApplyVisibility(enabled);
            if (enabled)
            {
                RefreshText(force: true);
            }
        }

        private void ApplyVisibility(bool enabled)
        {
            _visible = enabled;
            if (_labelTransform != null)
            {
                _labelTransform.gameObject.SetActive(enabled);
            }
        }

        private void EnsureLabel()
        {
            if (_textMesh != null)
            {
                return;
            }

            var go = new GameObject("TaskDebugLabel");
            _labelTransform = go.transform;
            _labelTransform.SetParent(transform, false);
            _labelTransform.localPosition = new Vector3(0f, FootOffsetY, FootOffsetZ);
            // Top-down Combat cameras look down -Y; face text upward.
            _labelTransform.localRotation = Quaternion.Euler(90f, 0f, 0f);

            _textMesh = go.AddComponent<TextMesh>();
            _textMesh.anchor = TextAnchor.MiddleCenter;
            _textMesh.alignment = TextAlignment.Center;
            _textMesh.characterSize = CharacterSize;
            _textMesh.fontSize = FontSize;
            _textMesh.color = Color.white;
            // Demo: OS CJK font so GoalKind ZH labels render under top-down camera.
            var font = Font.CreateDynamicFontFromOSFont(
                new[] { "Microsoft YaHei", "SimHei", "Arial Unicode MS", "Arial" },
                32);
            if (font != null)
            {
                _textMesh.font = font;
                var renderer = go.GetComponent<MeshRenderer>();
                if (renderer != null)
                {
                    if (font.material != null)
                    {
                        renderer.sharedMaterial = font.material;
                    }

                    // SPEC_04 §15.2: above the character band (200) so the camera
                    // Z-sort cannot tie it with the body sprite at the same XZ.
                    renderer.sortingOrder = 205;
                }
            }

            _textMesh.text = string.Empty;
        }

        private void RefreshText(bool force)
        {
            if (_textMesh == null || _scheduler == null || _moveId == 0)
            {
                return;
            }

            if (!_scheduler.TryGetGoal(_moveId, out var kind, out _))
            {
                if (force || _hasLastKind)
                {
                    _textMesh.text = string.Empty;
                    _hasLastKind = false;
                    _lastMoveSpeed = float.NaN;
                }

                return;
            }

            var extra = _extraText != null ? _extraText() : null;
            var hasSpeed = _resolveMoveSpeed != null;
            var speed = hasSpeed ? _resolveMoveSpeed() : 0f;
            var speedDirty = hasSpeed
                && (float.IsNaN(_lastMoveSpeed)
                    || Mathf.Abs(speed - _lastMoveSpeed) > MoveSpeedDirtyEpsilon);
            if (!force
                && _hasLastKind
                && kind == _lastKind
                && extra == _lastExtra
                && !speedDirty)
            {
                return;
            }

            _lastKind = kind;
            _hasLastKind = true;
            _lastExtra = extra;
            _lastMoveSpeed = hasSpeed ? speed : float.NaN;
            var label = ToZhLabel(kind);
            if (hasSpeed)
            {
                label += $"({speed:0.##})";
            }

            if (!string.IsNullOrEmpty(extra))
            {
                label += extra;
            }

            _textMesh.text = label;
        }

        private static string ToZhLabel(GoalKind kind)
        {
            switch (kind)
            {
                case GoalKind.Objective:
                    return "推进";
                case GoalKind.FormationHome:
                    return "回阵";
                case GoalKind.AttackSlot:
                    return "追击";
                case GoalKind.ChaseAnchor:
                    return "追击锚";
                case GoalKind.FormationSlot:
                    return "阵型";
                default:
                    return kind.ToString();
            }
        }
    }
}
