using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Gravedigger2026.UI
{
    /// <summary>
    /// UI-008: Level pick list — Hub-embedded select + Enter → Stage 1 (SPEC_03 §3.5 / D-081).
    /// </summary>
    public sealed class LevelSelectPanelView : MonoBehaviour
    {
        [SerializeField] private GameObject _root;
        [SerializeField] private Text _titleText;
        [SerializeField] private Transform _levelListContent;
        [SerializeField] private GameObject _levelRowTemplate;
        [SerializeField] private Button _closeButton;
        [SerializeField] private Button _enterButton;
        [SerializeField] private Text _emptyHintText;
        [SerializeField] private Image _backdropImage;
        [SerializeField] private bool _hubEmbedded;

        private readonly List<GameObject> _spawnedRows = new List<GameObject>();
        private readonly List<string> _rowLevelIds = new List<string>();
        private string _selectedLevelId;
        private Color _rowDefault = new Color(0.28f, 0.38f, 0.52f, 1f);
        private Color _rowSelected = new Color(0.35f, 0.55f, 0.75f, 1f);

        public event Action<string> LevelPicked;
        public event Action Closed;

        private void Awake()
        {
            if (_closeButton != null)
            {
                _closeButton.onClick.AddListener(HandleCloseClicked);
            }

            if (_enterButton != null)
            {
                _enterButton.onClick.AddListener(HandleEnterClicked);
            }

            if (_levelRowTemplate != null)
            {
                _levelRowTemplate.SetActive(false);
            }

            ApplyEmbedChrome();
        }

        public bool IsOpen => _root != null && _root.activeSelf;

        public string SelectedLevelId => _selectedLevelId;

        public void ConfigureHubEmbedded(bool embedded)
        {
            _hubEmbedded = embedded;
            ApplyEmbedChrome();
        }

        public void BindRuntime(
            GameObject root,
            Text titleText,
            Transform levelListContent,
            GameObject levelRowTemplate,
            Button closeButton,
            Text emptyHintText,
            Button enterButton = null,
            Image backdropImage = null)
        {
            _root = root;
            _titleText = titleText;
            _levelListContent = levelListContent;
            _levelRowTemplate = levelRowTemplate;
            _closeButton = closeButton;
            _emptyHintText = emptyHintText;
            _enterButton = enterButton;
            _backdropImage = backdropImage;

            if (_closeButton != null)
            {
                _closeButton.onClick.RemoveAllListeners();
                _closeButton.onClick.AddListener(HandleCloseClicked);
            }

            if (_enterButton != null)
            {
                _enterButton.onClick.RemoveAllListeners();
                _enterButton.onClick.AddListener(HandleEnterClicked);
            }

            ApplyEmbedChrome();
        }

        public void Show(IReadOnlyList<string> levelIds)
        {
            RebuildRows(levelIds);
            AutoSelectMax(levelIds);
            if (_root != null)
            {
                _root.SetActive(true);
            }

            ApplyEmbedChrome();
        }

        public void Hide()
        {
            if (_root != null)
            {
                _root.SetActive(false);
            }
        }

        private void ApplyEmbedChrome()
        {
            if (_hubEmbedded)
            {
                if (_backdropImage != null)
                {
                    _backdropImage.color = new Color(1f, 1f, 1f, 0.92f);
                    _backdropImage.raycastTarget = true;
                }

                if (_closeButton != null)
                {
                    _closeButton.gameObject.SetActive(false);
                }

                if (_enterButton != null)
                {
                    _enterButton.gameObject.SetActive(true);
                }

                if (_titleText != null)
                {
                    _titleText.text = "关卡选择";
                }
            }
            else
            {
                if (_enterButton != null)
                {
                    _enterButton.gameObject.SetActive(false);
                }

                if (_closeButton != null)
                {
                    _closeButton.gameObject.SetActive(true);
                }
            }
        }

        private void HandleCloseClicked()
        {
            Hide();
            Closed?.Invoke();
        }

        private void HandleEnterClicked()
        {
            if (string.IsNullOrEmpty(_selectedLevelId))
            {
                return;
            }

            LevelPicked?.Invoke(_selectedLevelId);
        }

        private void AutoSelectMax(IReadOnlyList<string> levelIds)
        {
            _selectedLevelId = null;
            if (levelIds == null || levelIds.Count == 0)
            {
                RefreshRowHighlights();
                return;
            }

            for (var i = levelIds.Count - 1; i >= 0; i--)
            {
                if (!string.IsNullOrEmpty(levelIds[i]))
                {
                    _selectedLevelId = levelIds[i];
                    break;
                }
            }

            RefreshRowHighlights();
        }

        private void RebuildRows(IReadOnlyList<string> levelIds)
        {
            ClearRows();
            var count = levelIds != null ? levelIds.Count : 0;
            if (_emptyHintText != null)
            {
                _emptyHintText.gameObject.SetActive(count == 0);
                if (count == 0)
                {
                    _emptyHintText.text = "当前模式无可用关卡";
                }
            }

            if (count == 0 || _levelListContent == null || _levelRowTemplate == null)
            {
                return;
            }

            _levelRowTemplate.SetActive(false);
            for (var i = 0; i < count; i++)
            {
                var levelId = levelIds[i];
                if (string.IsNullOrEmpty(levelId))
                {
                    continue;
                }

                var go = Instantiate(_levelRowTemplate, _levelListContent);
                go.name = "LevelRow_" + levelId;
                go.SetActive(true);
                var text = go.GetComponentInChildren<Text>(true);
                if (text != null)
                {
                    text.text = levelId;
                }

                var image = go.GetComponent<Image>();
                if (image != null && i == 0)
                {
                    _rowDefault = image.color;
                }

                var button = go.GetComponent<Button>();
                if (button != null)
                {
                    var captured = levelId;
                    button.onClick.RemoveAllListeners();
                    button.onClick.AddListener(() => HandleLevelRowClicked(captured));
                }

                _spawnedRows.Add(go);
                _rowLevelIds.Add(levelId);
            }
        }

        private void HandleLevelRowClicked(string levelId)
        {
            if (_hubEmbedded)
            {
                _selectedLevelId = levelId;
                RefreshRowHighlights();
                return;
            }

            LevelPicked?.Invoke(levelId);
        }

        private void RefreshRowHighlights()
        {
            for (var i = 0; i < _spawnedRows.Count; i++)
            {
                var go = _spawnedRows[i];
                if (go == null)
                {
                    continue;
                }

                var image = go.GetComponent<Image>();
                if (image == null)
                {
                    continue;
                }

                var selected = i < _rowLevelIds.Count && _rowLevelIds[i] == _selectedLevelId;
                image.color = selected ? _rowSelected : _rowDefault;
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
            _rowLevelIds.Clear();
        }
    }
}
