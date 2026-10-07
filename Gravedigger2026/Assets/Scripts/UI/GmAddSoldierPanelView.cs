using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Gravedigger2026.UI
{
    public readonly struct GmDropdownOption
    {
        public readonly string Id;
        public readonly string Label;

        public GmDropdownOption(string id, string label)
        {
            Id = id;
            Label = label;
        }
    }

    /// <summary>
    /// UI-020 / D-064: left-docked Tools GM add-soldier settings panel.
    /// Root Canvas overrideSorting=100 so it draws above FormationCanvas (70) / squad bar;
    /// Sandbox path uses ≥220 above SandboxRoot (200).
    /// </summary>
    public sealed class GmAddSoldierPanelView : MonoBehaviour
    {
        /// <summary>Above FormationCanvas (70); aligned with other InSaveShell modals.</summary>
        public const int PanelSortingOrder = 100;

        /// <summary>Above SandboxRoot (200) and ToolsPanel (220).</summary>
        public const int SandboxElevatedSortingOrder = 230;

        private int _activeSortingOrder = PanelSortingOrder;

        [SerializeField] private GameObject _root;
        [SerializeField] private Dropdown _classDropdown;
        [SerializeField] private Dropdown _raceDropdown;
        [SerializeField] private InputField _countInput;
        [SerializeField] private Toggle _autoDeployToggle;
        [SerializeField] private Button _closeButton;
        [SerializeField] private Button _addButton;

        private readonly List<string> _classIds = new List<string>();
        private readonly List<string> _raceIds = new List<string>();

        public event Action AddClicked;
        public event Action Closed;

        private void Awake()
        {
            WireButtons();
            ApplyPanelSorting();
            if (_countInput != null && string.IsNullOrEmpty(_countInput.text))
            {
                _countInput.text = "1";
            }

            if (_autoDeployToggle != null)
            {
                _autoDeployToggle.isOn = true;
            }
        }

        public bool IsOpen => _root != null && _root.activeSelf;

        public void BindRuntime(
            GameObject root,
            Dropdown classDropdown,
            Dropdown raceDropdown,
            InputField countInput,
            Toggle autoDeployToggle,
            Button closeButton,
            Button addButton)
        {
            _root = root;
            _classDropdown = classDropdown;
            _raceDropdown = raceDropdown;
            _countInput = countInput;
            _autoDeployToggle = autoDeployToggle;
            _closeButton = closeButton;
            _addButton = addButton;
            WireButtons();
            ApplyPanelSorting();
            if (_countInput != null && string.IsNullOrEmpty(_countInput.text))
            {
                _countInput.text = "1";
            }

            if (_autoDeployToggle != null)
            {
                _autoDeployToggle.isOn = true;
            }
        }

        private void WireButtons()
        {
            if (_closeButton != null)
            {
                _closeButton.onClick.RemoveListener(HandleCloseClicked);
                _closeButton.onClick.AddListener(HandleCloseClicked);
            }

            if (_addButton != null)
            {
                _addButton.onClick.RemoveAllListeners();
                _addButton.onClick.AddListener(() => AddClicked?.Invoke());
            }
        }

        public void Show(
            IReadOnlyList<GmDropdownOption> classes,
            IReadOnlyList<GmDropdownOption> races,
            int? sortingOrder = null)
        {
            FillDropdown(_classDropdown, _classIds, classes);
            FillDropdown(_raceDropdown, _raceIds, races);
            if (_countInput != null && string.IsNullOrEmpty(_countInput.text))
            {
                _countInput.text = "1";
            }

            var sandboxElevated = sortingOrder.HasValue
                && sortingOrder.Value >= SandboxElevatedSortingOrder;
            if (_autoDeployToggle != null)
            {
                // Sandbox gate: pool only — leave toggle off so UI matches forced grant path.
                _autoDeployToggle.isOn = !sandboxElevated;
            }

            _activeSortingOrder = sortingOrder ?? PanelSortingOrder;
            if (_root != null)
            {
                ApplyPanelSorting();
                _root.SetActive(true);
                _root.transform.SetAsLastSibling();
            }
        }

        public void Hide()
        {
            if (_root != null)
            {
                _root.SetActive(false);
            }
        }

        private void ApplyPanelSorting()
        {
            var target = _root != null ? _root : gameObject;
            if (target == null)
            {
                return;
            }

            var canvas = target.GetComponent<Canvas>();
            if (canvas == null)
            {
                canvas = target.AddComponent<Canvas>();
            }

            canvas.overrideSorting = true;
            canvas.sortingOrder = _activeSortingOrder;
            if (target.GetComponent<GraphicRaycaster>() == null)
            {
                target.AddComponent<GraphicRaycaster>();
            }
        }

        public bool TryGetSelection(out string classId, out string raceId, out int count, out bool autoDeploy)
        {
            classId = null;
            raceId = null;
            count = 1;
            autoDeploy = true;

            if (_classDropdown == null || _raceDropdown == null)
            {
                return false;
            }

            var ci = _classDropdown.value;
            var ri = _raceDropdown.value;
            if (ci < 0 || ci >= _classIds.Count || ri < 0 || ri >= _raceIds.Count)
            {
                return false;
            }

            classId = _classIds[ci];
            raceId = _raceIds[ri];
            autoDeploy = _autoDeployToggle == null || _autoDeployToggle.isOn;
            count = ParseCount(_countInput != null ? _countInput.text : "1");
            return !string.IsNullOrEmpty(classId) && !string.IsNullOrEmpty(raceId);
        }

        private void HandleCloseClicked()
        {
            Hide();
            Closed?.Invoke();
        }

        private static int ParseCount(string text)
        {
            if (!int.TryParse(text, out var n) || n < 1)
            {
                return 1;
            }

            return Mathf.Clamp(n, 1, 999);
        }

        private static void FillDropdown(
            Dropdown dropdown,
            List<string> idStore,
            IReadOnlyList<GmDropdownOption> options)
        {
            idStore.Clear();
            if (dropdown == null)
            {
                return;
            }

            dropdown.ClearOptions();
            var labels = new List<string>();
            var count = options != null ? options.Count : 0;
            for (var i = 0; i < count; i++)
            {
                var opt = options[i];
                if (string.IsNullOrEmpty(opt.Id))
                {
                    continue;
                }

                idStore.Add(opt.Id);
                labels.Add(string.IsNullOrEmpty(opt.Label) ? opt.Id : opt.Label);
            }

            dropdown.AddOptions(labels);
            dropdown.value = 0;
            dropdown.RefreshShownValue();
        }
    }
}
