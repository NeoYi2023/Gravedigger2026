using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Gravedigger2026.UI
{
    public readonly struct GmGrantListItem
    {
        public readonly string Id;
        public readonly string Label;

        public GmGrantListItem(string id, string label)
        {
            Id = id;
            Label = label;
        }
    }

    /// <summary>
    /// UI-019 / D-061: ToolsPanel GM grant list (layout aligned with LevelSelectPanel).
    /// </summary>
    public sealed class GmGrantListPanelView : MonoBehaviour
    {
        [SerializeField] private GameObject _root;
        [SerializeField] private Text _titleText;
        [SerializeField] private Transform _listContent;
        [SerializeField] private GameObject _rowTemplate;
        [SerializeField] private Button _closeButton;
        [SerializeField] private Text _emptyHintText;

        private readonly List<GameObject> _spawnedRows = new List<GameObject>();

        public event Action<string> ItemPicked;
        public event Action Closed;

        private void Awake()
        {
            // Prefab starts inactive. Do NOT SetActive(false) on self/_root here — see ToolsPanelView.
            if (_closeButton != null)
            {
                _closeButton.onClick.AddListener(HandleCloseClicked);
            }

            if (_rowTemplate != null)
            {
                _rowTemplate.SetActive(false);
            }
        }

        public bool IsOpen => _root != null && _root.activeSelf;

        public void BindRuntime(
            GameObject root,
            Text titleText,
            Transform listContent,
            GameObject rowTemplate,
            Button closeButton,
            Text emptyHintText)
        {
            _root = root;
            _titleText = titleText;
            _listContent = listContent;
            _rowTemplate = rowTemplate;
            _closeButton = closeButton;
            _emptyHintText = emptyHintText;
        }

        public void Show(string title, IReadOnlyList<GmGrantListItem> items)
        {
            if (_titleText != null)
            {
                _titleText.text = string.IsNullOrEmpty(title) ? "发放" : title;
            }

            RebuildRows(items);
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

        private void RebuildRows(IReadOnlyList<GmGrantListItem> items)
        {
            ClearRows();
            var count = items != null ? items.Count : 0;
            if (_emptyHintText != null)
            {
                _emptyHintText.gameObject.SetActive(count == 0);
                if (count == 0)
                {
                    _emptyHintText.text = "当前模式无可用项";
                }
            }

            if (count == 0 || _listContent == null || _rowTemplate == null)
            {
                return;
            }

            _rowTemplate.SetActive(false);
            for (var i = 0; i < count; i++)
            {
                var item = items[i];
                if (string.IsNullOrEmpty(item.Id))
                {
                    continue;
                }

                var go = Instantiate(_rowTemplate, _listContent);
                go.name = "GrantRow_" + item.Id;
                go.SetActive(true);
                var text = go.GetComponentInChildren<Text>(true);
                if (text != null)
                {
                    text.text = string.IsNullOrEmpty(item.Label) ? item.Id : item.Label;
                }

                var button = go.GetComponent<Button>();
                if (button != null)
                {
                    var captured = item.Id;
                    button.onClick.RemoveAllListeners();
                    button.onClick.AddListener(() => HandleRowClicked(captured));
                }

                _spawnedRows.Add(go);
            }
        }

        private void HandleRowClicked(string id)
        {
            ItemPicked?.Invoke(id);
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
