using System;
using System.Collections.Generic;
using Gravedigger2026.Core.Config;
using Gravedigger2026.Core.Tech;
using Gravedigger2026.Core.UpgradeManufacture;
using UnityEngine;
using UnityEngine.UI;

namespace Gravedigger2026.UI
{
    /// <summary>
    /// Settings TechTree pannable canvas (SPEC_03 §3.13 / UI-012 Approach A).
    /// </summary>
    public sealed class TechTreeCanvasView : MonoBehaviour
    {
        [SerializeField] private GameObject _root;
        [SerializeField] private RectTransform _content;
        [SerializeField] private RectTransform _edgesParent;
        [SerializeField] private Text _techPointsLabel;
        [SerializeField] private Text _capsLabel;
        [SerializeField] private Text _tooltipTitle;
        [SerializeField] private Text _tooltipBody;
        [SerializeField] private GameObject _tooltipRoot;
        [SerializeField] private Button _closeButton;
        [SerializeField] private Button _debugGrantTechPointsButton;
        [SerializeField] private TechTreeNodeView[] _nodes;
        [SerializeField] private GameObject _panLayerRoot;

        private TechTreeService _techTree;
        private ProtagonistProgressService _progress;
        private readonly List<Image> _edgeImages = new List<Image>();
        private TechTreePanLayer _panLayerRuntime;
        private bool _bound;

        public event Action CloseRequested;

        private void Awake()
        {
            if (_closeButton != null)
            {
                _closeButton.onClick.AddListener(() => CloseRequested?.Invoke());
            }

            if (_debugGrantTechPointsButton != null)
            {
                _debugGrantTechPointsButton.onClick.AddListener(HandleDebugGrant);
            }

            EnsurePanLayerRuntime();
        }

        private void OnDestroy()
        {
            UnbindEvents();
            if (_panLayerRuntime != null)
            {
                _panLayerRuntime.DragDelta -= HandlePan;
                _panLayerRuntime = null;
            }
        }

        private void EnsurePanLayerRuntime()
        {
            if (_panLayerRoot == null)
            {
                return;
            }

            // Prefab only stores Image on PanLayer; add drag handler at runtime to avoid
            // Editor-time MonoScript/fileID:0 missing-script Prefab saves.
            _panLayerRuntime = _panLayerRoot.GetComponent<TechTreePanLayer>();
            if (_panLayerRuntime == null)
            {
                _panLayerRuntime = _panLayerRoot.AddComponent<TechTreePanLayer>();
            }

            _panLayerRuntime.DragDelta -= HandlePan;
            _panLayerRuntime.DragDelta += HandlePan;
        }

        public void Bind(TechTreeService techTree, ProtagonistProgressService progress, ConfigCsvRepository configs)
        {
            if (_bound)
            {
                UnbindEvents();
            }

            _techTree = techTree;
            _progress = progress;
            _bound = true;
            if (_techTree != null)
            {
                _techTree.Changed += RefreshAll;
            }

            if (_progress != null)
            {
                _progress.Changed += RefreshAll;
            }

            ConfigureNodes(configs);
            RebuildEdgesFromConfig(configs);
            RefreshAll();
        }

        public void Show()
        {
            if (_root != null)
            {
                _root.SetActive(true);
            }

            FocusDefaultNode();
            RefreshAll();
        }

        public void Hide()
        {
            HideTooltip();
            if (_root != null)
            {
                _root.SetActive(false);
            }
        }

        public void ShowTooltip(TechTreeConfigRow row)
        {
            if (row == null)
            {
                return;
            }

            if (_tooltipTitle != null)
            {
                _tooltipTitle.text = string.IsNullOrEmpty(row.DisplayName) ? row.TechId : row.DisplayName;
            }

            if (_tooltipBody != null)
            {
                _tooltipBody.text = $"{row.EffectDescription}\n学习费用: {row.LearnCost}";
            }

            if (_tooltipRoot != null)
            {
                _tooltipRoot.SetActive(true);
            }
        }

        public void HideTooltip()
        {
            if (_tooltipRoot != null)
            {
                _tooltipRoot.SetActive(false);
            }
        }

        public void TryLearnNode(string techId)
        {
            if (_techTree == null)
            {
                return;
            }

            var result = _techTree.TryLearn(techId);
            if (!result.Success)
            {
                Debug.Log($"[TechTreeUI] Learn failed {techId}: {result.FailReason}");
            }
        }

        private void UnbindEvents()
        {
            if (_techTree != null)
            {
                _techTree.Changed -= RefreshAll;
            }

            if (_progress != null)
            {
                _progress.Changed -= RefreshAll;
            }

            _bound = false;
        }

        private void ConfigureNodes(ConfigCsvRepository configs)
        {
            if (_nodes == null || configs == null)
            {
                return;
            }

            for (var i = 0; i < _nodes.Length; i++)
            {
                var node = _nodes[i];
                if (node == null || string.IsNullOrEmpty(node.TechId))
                {
                    continue;
                }

                if (configs.TryGetTechTree(node.TechId, out var row))
                {
                    node.Configure(this, row);
                }
            }
        }

        private void RebuildEdgesFromConfig(ConfigCsvRepository configs)
        {
            if (_edgesParent == null || _nodes == null || configs == null)
            {
                return;
            }

            for (var i = _edgeImages.Count - 1; i >= 0; i--)
            {
                if (_edgeImages[i] != null)
                {
                    Destroy(_edgeImages[i].gameObject);
                }
            }

            _edgeImages.Clear();

            var byId = new Dictionary<string, TechTreeNodeView>(StringComparer.Ordinal);
            for (var i = 0; i < _nodes.Length; i++)
            {
                var node = _nodes[i];
                if (node != null && !string.IsNullOrEmpty(node.TechId))
                {
                    byId[node.TechId] = node;
                }
            }

            foreach (var pair in byId)
            {
                if (!configs.TryGetTechTree(pair.Key, out var row) || row.UnlockNextTechIds == null)
                {
                    continue;
                }

                var fromRt = pair.Value.RectTransform;
                for (var n = 0; n < row.UnlockNextTechIds.Length; n++)
                {
                    var nextId = row.UnlockNextTechIds[n];
                    if (string.IsNullOrEmpty(nextId) || !byId.TryGetValue(nextId, out var toNode))
                    {
                        continue;
                    }

                    _edgeImages.Add(CreateEdge(fromRt, toNode.RectTransform));
                }
            }
        }

        private Image CreateEdge(RectTransform from, RectTransform to)
        {
            var go = new GameObject("Edge", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(_edgesParent, false);
            go.transform.SetAsFirstSibling();
            var image = go.GetComponent<Image>();
            image.color = new Color(0.55f, 0.58f, 0.62f, 0.85f);
            image.raycastTarget = false;

            var rt = go.GetComponent<RectTransform>();
            var a = from.anchoredPosition;
            var b = to.anchoredPosition;
            var dir = b - a;
            var length = dir.magnitude;
            rt.sizeDelta = new Vector2(Mathf.Max(4f, length), 4f);
            rt.anchoredPosition = (a + b) * 0.5f;
            rt.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
            return image;
        }

        private void FocusDefaultNode()
        {
            if (_content == null || _techTree == null || _nodes == null)
            {
                return;
            }

            var focusId = _techTree.GetDefaultFocusTechId();
            for (var i = 0; i < _nodes.Length; i++)
            {
                var node = _nodes[i];
                if (node == null || !string.Equals(node.TechId, focusId, StringComparison.Ordinal))
                {
                    continue;
                }

                _content.anchoredPosition = -node.RectTransform.anchoredPosition;
                return;
            }
        }

        private void RefreshAll()
        {
            if (_techPointsLabel != null && _progress != null)
            {
                _techPointsLabel.text = $"科技点: {_progress.TechPoints}";
            }

            if (_capsLabel != null && _techTree != null)
            {
                var c = _techTree.Capabilities;
                _capsLabel.text =
                    $"DigDamage={c.DigDamage:0.##}  单次时长={c.DigActionDuration:0.##}s  光标={c.DigCursorRadius:0.##}  阶段+={c.DigStageDurationBonus:0.##}s";
            }

            if (_nodes != null && _techTree != null)
            {
                for (var i = 0; i < _nodes.Length; i++)
                {
                    _nodes[i]?.RefreshVisual(_techTree);
                }
            }
        }

        private void HandlePan(Vector2 delta)
        {
            if (_content != null)
            {
                _content.anchoredPosition += delta;
            }
        }

        private void HandleDebugGrant()
        {
            _progress?.DebugGrantTechPoints(5);
            Debug.Log($"[TechTreeUI] Debug +5 TechPoints → {_progress?.TechPoints}");
        }
    }
}
