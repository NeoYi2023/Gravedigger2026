using Gravedigger2026.Core.Pathing;
using UnityEngine;

namespace Gravedigger2026.Gameplay.Pathing
{
    /// <summary>
    /// Runtime TextMesh under soldier feet showing current GoalKind (SPEC_04 §9.7 Debug).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class WarriorTaskDebugLabelView : MonoBehaviour
    {
        private const float FootOffsetY = 0.02f;
        private const float CharacterSize = 0.12f;

        private MassMoveScheduler _scheduler;
        private int _moveId;
        private TextMesh _textMesh;
        private Transform _labelTransform;
        private GoalKind _lastKind;
        private bool _hasLastKind;
        private bool _visible;

        public void Bind(MassMoveScheduler scheduler, int moveId)
        {
            _scheduler = scheduler;
            _moveId = moveId;
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
            _labelTransform.localPosition = new Vector3(0f, FootOffsetY, 0f);
            // Top-down Combat cameras look down -Y; face text upward.
            _labelTransform.localRotation = Quaternion.Euler(90f, 0f, 0f);

            _textMesh = go.AddComponent<TextMesh>();
            _textMesh.anchor = TextAnchor.MiddleCenter;
            _textMesh.alignment = TextAlignment.Center;
            _textMesh.characterSize = CharacterSize;
            _textMesh.fontSize = 32;
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

                    renderer.sortingOrder = 200;
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
                }

                return;
            }

            if (!force && _hasLastKind && kind == _lastKind)
            {
                return;
            }

            _lastKind = kind;
            _hasLastKind = true;
            _textMesh.text = ToZhLabel(kind);
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
                default:
                    return kind.ToString();
            }
        }
    }
}
