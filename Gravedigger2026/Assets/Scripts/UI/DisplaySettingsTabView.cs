using System;
using System.Collections.Generic;
using Gravedigger2026.Core.Settings;
using UnityEngine;
using UnityEngine.UI;

namespace Gravedigger2026.UI
{
    /// <summary>
    /// UI-028 Display tab: resolution list + window mode + Apply (SPEC_03 §3.6).
    /// </summary>
    public sealed class DisplaySettingsTabView : MonoBehaviour
    {
        private static readonly Color ModeIdle = new Color(0.28f, 0.38f, 0.52f, 1f);
        private static readonly Color ModeSelected = new Color(0.35f, 0.55f, 0.38f, 1f);
        private static readonly Color ResIdle = new Color(0.22f, 0.28f, 0.36f, 1f);
        private static readonly Color ResSelected = new Color(0.30f, 0.48f, 0.42f, 1f);

        [SerializeField] private GameObject _root;
        [SerializeField] private Transform _resolutionListContent;
        [SerializeField] private GameObject _resolutionRowTemplate;
        [SerializeField] private Button _modeWindowedButton;
        [SerializeField] private Button _modeBorderlessButton;
        [SerializeField] private Button _modeExclusiveButton;
        [SerializeField] private Button _applyButton;

        private DisplaySettingsService _service;
        private readonly List<GameObject> _spawnedRows = new List<GameObject>();
        private readonly List<DisplayResolutionOption> _options = new List<DisplayResolutionOption>();
        private int _draftWidth;
        private int _draftHeight;
        private DisplayWindowMode _draftMode;

        public event Action Applied;

        private void Awake()
        {
            if (_resolutionRowTemplate != null)
            {
                _resolutionRowTemplate.SetActive(false);
            }

            WireModeButton(_modeWindowedButton, DisplayWindowMode.Windowed);
            WireModeButton(_modeBorderlessButton, DisplayWindowMode.Borderless);
            WireModeButton(_modeExclusiveButton, DisplayWindowMode.Exclusive);

            if (_applyButton != null)
            {
                _applyButton.onClick.AddListener(HandleApplyClicked);
            }
        }

        public void Bind(DisplaySettingsService service)
        {
            _service = service;
        }

        public void BindRuntime(
            GameObject root,
            Transform resolutionListContent,
            GameObject resolutionRowTemplate,
            Button modeWindowedButton,
            Button modeBorderlessButton,
            Button modeExclusiveButton,
            Button applyButton)
        {
            _root = root;
            _resolutionListContent = resolutionListContent;
            _resolutionRowTemplate = resolutionRowTemplate;
            _modeWindowedButton = modeWindowedButton;
            _modeBorderlessButton = modeBorderlessButton;
            _modeExclusiveButton = modeExclusiveButton;
            _applyButton = applyButton;

            if (_resolutionRowTemplate != null)
            {
                _resolutionRowTemplate.SetActive(false);
            }

            WireModeButton(_modeWindowedButton, DisplayWindowMode.Windowed);
            WireModeButton(_modeBorderlessButton, DisplayWindowMode.Borderless);
            WireModeButton(_modeExclusiveButton, DisplayWindowMode.Exclusive);

            if (_applyButton != null)
            {
                _applyButton.onClick.RemoveListener(HandleApplyClicked);
                _applyButton.onClick.AddListener(HandleApplyClicked);
            }
        }

        public void Show()
        {
            if (_root != null)
            {
                _root.SetActive(true);
            }

            RefreshFromService();
        }

        public void Hide()
        {
            if (_root != null)
            {
                _root.SetActive(false);
            }
        }

        public void RefreshFromService()
        {
            if (_service == null)
            {
                return;
            }

            _draftWidth = _service.Width;
            _draftHeight = _service.Height;
            _draftMode = _service.WindowMode;
            RebuildResolutionRows();
            RefreshModeButtons();
        }

        private void WireModeButton(Button button, DisplayWindowMode mode)
        {
            if (button == null)
            {
                return;
            }

            button.onClick.AddListener(() =>
            {
                _draftMode = mode;
                RefreshModeButtons();
            });
        }

        private void HandleApplyClicked()
        {
            if (_service == null)
            {
                return;
            }

            if (!_service.TryApply(_draftWidth, _draftHeight, _draftMode))
            {
                return;
            }

            RefreshFromService();
            Applied?.Invoke();
        }

        private void RebuildResolutionRows()
        {
            ClearRows();
            _options.Clear();
            if (_service == null || _resolutionListContent == null || _resolutionRowTemplate == null)
            {
                return;
            }

            var opts = _service.GetResolutionOptions();
            for (var i = 0; i < opts.Count; i++)
            {
                _options.Add(opts[i]);
            }

            _resolutionRowTemplate.SetActive(false);
            for (var i = 0; i < _options.Count; i++)
            {
                var opt = _options[i];
                var go = Instantiate(_resolutionRowTemplate, _resolutionListContent);
                go.name = $"Res_{opt.Width}x{opt.Height}";
                go.SetActive(true);
                var text = go.GetComponentInChildren<Text>(true);
                if (text != null)
                {
                    text.text = opt.Label;
                }

                var button = go.GetComponent<Button>();
                if (button != null)
                {
                    var captured = opt;
                    button.onClick.RemoveAllListeners();
                    button.onClick.AddListener(() =>
                    {
                        _draftWidth = captured.Width;
                        _draftHeight = captured.Height;
                        RefreshResolutionHighlights();
                    });
                }

                _spawnedRows.Add(go);
            }

            RefreshResolutionHighlights();
        }

        private void RefreshResolutionHighlights()
        {
            for (var i = 0; i < _spawnedRows.Count; i++)
            {
                var selected = i < _options.Count
                    && _options[i].Width == _draftWidth
                    && _options[i].Height == _draftHeight;
                var image = _spawnedRows[i].GetComponent<Image>();
                if (image != null)
                {
                    image.color = selected ? ResSelected : ResIdle;
                }
            }
        }

        private void RefreshModeButtons()
        {
            SetModeButtonColor(_modeWindowedButton, _draftMode == DisplayWindowMode.Windowed);
            SetModeButtonColor(_modeBorderlessButton, _draftMode == DisplayWindowMode.Borderless);
            SetModeButtonColor(_modeExclusiveButton, _draftMode == DisplayWindowMode.Exclusive);
        }

        private static void SetModeButtonColor(Button button, bool selected)
        {
            if (button == null)
            {
                return;
            }

            var image = button.GetComponent<Image>();
            if (image != null)
            {
                image.color = selected ? ModeSelected : ModeIdle;
            }
        }

        private void ClearRows()
        {
            for (var i = 0; i < _spawnedRows.Count; i++)
            {
                if (_spawnedRows[i] != null)
                {
                    Destroy(_spawnedRows[i]);
                }
            }

            _spawnedRows.Clear();
        }
    }
}
