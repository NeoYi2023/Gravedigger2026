using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Gravedigger2026.Gameplay.UpgradeManufacture
{
    /// <summary>
    /// Rough formation panel UI (SPEC_03 UI-010 / D-032): pool deploy, formation select, nudge, undeploy.
    /// </summary>
    public sealed class FormationPanelView : MonoBehaviour
    {
        [SerializeField] private RectTransform _poolContent;
        [SerializeField] private Button _poolRowTemplate;
        [SerializeField] private RectTransform _formationContent;
        [SerializeField] private Button _formationRowTemplate;
        [SerializeField] private Text _statusText;
        [SerializeField] private Button _undeployButton;
        [SerializeField] private Button _nudgeNegXButton;
        [SerializeField] private Button _nudgePosXButton;
        [SerializeField] private Button _nudgeNegZButton;
        [SerializeField] private Button _nudgePosZButton;

        private readonly List<Button> _poolRows = new List<Button>();
        private readonly List<Button> _formationRows = new List<Button>();

        public event Action<string> DeployRequested;
        public event Action<string> SelectRequested;
        public event Action UndeployRequested;
        public event Action NudgeNegXRequested;
        public event Action NudgePosXRequested;
        public event Action NudgeNegZRequested;
        public event Action NudgePosZRequested;

        private void OnEnable()
        {
            if (_undeployButton != null)
            {
                _undeployButton.onClick.AddListener(HandleUndeploy);
            }

            if (_nudgeNegXButton != null)
            {
                _nudgeNegXButton.onClick.AddListener(HandleNudgeNegX);
            }

            if (_nudgePosXButton != null)
            {
                _nudgePosXButton.onClick.AddListener(HandleNudgePosX);
            }

            if (_nudgeNegZButton != null)
            {
                _nudgeNegZButton.onClick.AddListener(HandleNudgeNegZ);
            }

            if (_nudgePosZButton != null)
            {
                _nudgePosZButton.onClick.AddListener(HandleNudgePosZ);
            }
        }

        private void OnDisable()
        {
            if (_undeployButton != null)
            {
                _undeployButton.onClick.RemoveListener(HandleUndeploy);
            }

            if (_nudgeNegXButton != null)
            {
                _nudgeNegXButton.onClick.RemoveListener(HandleNudgeNegX);
            }

            if (_nudgePosXButton != null)
            {
                _nudgePosXButton.onClick.RemoveListener(HandleNudgePosX);
            }

            if (_nudgeNegZButton != null)
            {
                _nudgeNegZButton.onClick.RemoveListener(HandleNudgeNegZ);
            }

            if (_nudgePosZButton != null)
            {
                _nudgePosZButton.onClick.RemoveListener(HandleNudgePosZ);
            }
        }

        public void SetPoolLines(IReadOnlyList<string> labels, IReadOnlyList<string> warriorIds)
        {
            EnsureRowCount(_poolRows, _poolRowTemplate, _poolContent, labels.Count);
            for (var i = 0; i < _poolRows.Count; i++)
            {
                var row = _poolRows[i];
                if (i >= labels.Count)
                {
                    row.gameObject.SetActive(false);
                    continue;
                }

                var warriorId = warriorIds[i];
                row.gameObject.SetActive(true);
                SetRowLabel(row, labels[i]);
                row.onClick.RemoveAllListeners();
                row.onClick.AddListener(() => DeployRequested?.Invoke(warriorId));
            }
        }

        public void SetFormationLines(IReadOnlyList<string> labels, IReadOnlyList<string> warriorIds)
        {
            EnsureRowCount(_formationRows, _formationRowTemplate, _formationContent, labels.Count);
            for (var i = 0; i < _formationRows.Count; i++)
            {
                var row = _formationRows[i];
                if (i >= labels.Count)
                {
                    row.gameObject.SetActive(false);
                    continue;
                }

                var warriorId = warriorIds[i];
                row.gameObject.SetActive(true);
                SetRowLabel(row, labels[i]);
                row.onClick.RemoveAllListeners();
                row.onClick.AddListener(() => SelectRequested?.Invoke(warriorId));
            }
        }

        public void SetStatusText(string text)
        {
            if (_statusText != null)
            {
                _statusText.text = text ?? string.Empty;
            }
        }

        public void SetActionInteractable(bool hasSelection)
        {
            if (_undeployButton != null)
            {
                _undeployButton.interactable = hasSelection;
            }

            if (_nudgeNegXButton != null)
            {
                _nudgeNegXButton.interactable = hasSelection;
            }

            if (_nudgePosXButton != null)
            {
                _nudgePosXButton.interactable = hasSelection;
            }

            if (_nudgeNegZButton != null)
            {
                _nudgeNegZButton.interactable = hasSelection;
            }

            if (_nudgePosZButton != null)
            {
                _nudgePosZButton.interactable = hasSelection;
            }
        }

        private void EnsureRowCount(List<Button> rows, Button template, RectTransform content, int required)
        {
            if (template == null || content == null)
            {
                return;
            }

            while (rows.Count < required)
            {
                var clone = Instantiate(template, content);
                clone.gameObject.SetActive(true);
                rows.Add(clone);
            }
        }

        private static void SetRowLabel(Button row, string label)
        {
            var text = row.GetComponentInChildren<Text>(true);
            if (text != null)
            {
                text.text = label ?? string.Empty;
            }
        }

        private void HandleUndeploy()
        {
            UndeployRequested?.Invoke();
        }

        private void HandleNudgeNegX()
        {
            NudgeNegXRequested?.Invoke();
        }

        private void HandleNudgePosX()
        {
            NudgePosXRequested?.Invoke();
        }

        private void HandleNudgeNegZ()
        {
            NudgeNegZRequested?.Invoke();
        }

        private void HandleNudgePosZ()
        {
            NudgePosZRequested?.Invoke();
        }
    }
}
