using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Gravedigger2026.UI
{
    /// <summary>
    /// UI-008: Tools Level pick list — distinct LevelIds → enter Stage 1 (SPEC_03 §3.5).
    /// </summary>
    public sealed class LevelSelectPanelView : MonoBehaviour
    {
        [SerializeField] private GameObject _root;
        [SerializeField] private Text _titleText;
        [SerializeField] private Transform _levelListContent;
        [SerializeField] private GameObject _levelRowTemplate;
        [SerializeField] private Button _closeButton;
        [SerializeField] private Text _emptyHintText;

        private readonly List<GameObject> _spawnedRows = new List<GameObject>();

        public event Action<string> LevelPicked;
        public event Action Closed;

        private void Awake()
        {
            // Prefab starts inactive. Do NOT SetActive(false) on self/_root here — see ToolsPanelView.
            if (_closeButton != null)
            {
                _closeButton.onClick.AddListener(HandleCloseClicked);
            }

            if (_levelRowTemplate != null)
            {
                _levelRowTemplate.SetActive(false);
            }
        }

        public bool IsOpen => _root != null && _root.activeSelf;

        public void Show(IReadOnlyList<string> levelIds)
        {
            RebuildRows(levelIds);
            if (_root != null)
            {
                _root.SetActive(true);
            }
        }

        public void Hide()
        {
            if (_root != null)
            {
                _root.SetActive(false);
            }
        }

        private void HandleCloseClicked()
        {
            Hide();
            Closed?.Invoke();
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

                var button = go.GetComponent<Button>();
                if (button != null)
                {
                    var captured = levelId;
                    button.onClick.RemoveAllListeners();
                    button.onClick.AddListener(() => HandleLevelRowClicked(captured));
                }

                _spawnedRows.Add(go);
            }
        }

        private void HandleLevelRowClicked(string levelId)
        {
            LevelPicked?.Invoke(levelId);
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
