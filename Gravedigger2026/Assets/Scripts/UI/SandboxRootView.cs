using System;
using System.Collections.Generic;
using Gravedigger2026.Core.Config;
using UnityEngine;
using UnityEngine.UI;

namespace Gravedigger2026.UI
{
    /// <summary>
    /// UI-036: horizontal Sandbox cells from Level_SandboxNodeConfig (SPEC_03 §3.20).
    /// View only displays rows and raises clicks.
    /// </summary>
    public sealed class SandboxRootView : MonoBehaviour
    {
        [SerializeField] private GameObject _root;
        [SerializeField] private Text _title;
        [SerializeField] private Button _backButton;
        [SerializeField] private RectTransform _content;
        [SerializeField] private Text _emptyLabel;

        public delegate bool SandboxCountReader(string nodeId, out int remainingEnterCount, out bool cleared);

        public event Action BackRequested;
        public event Action<string> NodeClicked;

        public bool IsOpen => _root != null && _root.activeSelf;

        public void BindRuntime(GameObject root, Text title, Button backButton, RectTransform content, Text emptyLabel)
        {
            _root = root;
            _title = title;
            _backButton = backButton;
            _content = content;
            _emptyLabel = emptyLabel;
            WireBack();
        }

        private void Awake()
        {
            WireBack();
        }

        public void Show(string title, IReadOnlyList<SandboxNodeConfigRow> nodes, SandboxCountReader readCount)
        {
            if (_root != null)
            {
                _root.SetActive(true);
                _root.transform.SetAsLastSibling();
            }

            if (_title != null)
            {
                _title.text = string.IsNullOrEmpty(title) ? "沙盘" : title;
            }

            RebuildCells(nodes, readCount);
        }

        public void Hide()
        {
            if (_root != null)
            {
                _root.SetActive(false);
            }
        }

        private void WireBack()
        {
            if (_backButton == null)
            {
                return;
            }

            _backButton.onClick.RemoveAllListeners();
            _backButton.onClick.AddListener(() => BackRequested?.Invoke());
        }

        private void RebuildCells(IReadOnlyList<SandboxNodeConfigRow> nodes, SandboxCountReader readCount)
        {
            if (_content == null)
            {
                return;
            }

            for (var i = _content.childCount - 1; i >= 0; i--)
            {
                var child = _content.GetChild(i);
                if (child != null)
                {
                    Destroy(child.gameObject);
                }
            }

            var count = nodes != null ? nodes.Count : 0;
            if (_emptyLabel != null)
            {
                _emptyLabel.gameObject.SetActive(count == 0);
            }

            for (var i = 0; i < count; i++)
            {
                var row = nodes[i];
                if (row == null || string.IsNullOrEmpty(row.NodeId))
                {
                    continue;
                }

                var remaining = 0;
                var cleared = false;
                if (readCount != null)
                {
                    readCount(row.NodeId, out remaining, out cleared);
                }

                var cell = SandboxCellView.Create(_content, row, remaining, cleared, HandleNodeClicked);
                cell.transform.SetSiblingIndex(i);
            }
        }

        private void HandleNodeClicked(string nodeId)
        {
            if (!string.IsNullOrEmpty(nodeId))
            {
                NodeClicked?.Invoke(nodeId);
            }
        }
    }
}
