using System;
using System.Collections.Generic;
using Gravedigger2026.Core;
using Gravedigger2026.Core.Level;
using UnityEngine;
using UnityEngine.UI;

namespace Gravedigger2026.UI
{
    /// <summary>
    /// UI-031: vertical bottom-up Stage rows + horizontal options + unlock edges (SPEC_03 §3.9 / D-086).
    /// </summary>
    public sealed class LevelRouteSelectView : MonoBehaviour
    {
        [SerializeField] private GameObject _root;
        [SerializeField] private Text _titleText;
        [SerializeField] private Transform _stageListContent;
        [SerializeField] private GameObject _stageRowTemplate;
        [SerializeField] private GameObject _optionCardTemplate;
        [SerializeField] private RectTransform _edgeLayer;
        [SerializeField] private Button _closeButton;

        private readonly List<GameObject> _spawnedStages = new List<GameObject>();
        private readonly List<GameObject> _spawnedEdges = new List<GameObject>();
        private readonly Dictionary<string, RectTransform> _optionRects =
            new Dictionary<string, RectTransform>(StringComparer.Ordinal);
        private readonly Dictionary<string, string[]> _unlockByOption =
            new Dictionary<string, string[]>(StringComparer.Ordinal);

        private LevelRouteSnapshot _lastSnapshot;

        public event Action<string> OptionSelected;
        public event Action Closed;

        private static readonly Color Locked = new Color(0.35f, 0.35f, 0.38f, 0.85f);
        private static readonly Color Selectable = new Color(0.22f, 0.48f, 0.32f, 1f);
        private static readonly Color Cleared = new Color(0.25f, 0.35f, 0.55f, 1f);
        private static readonly Color Running = new Color(0.55f, 0.42f, 0.18f, 1f);
        private static readonly Color EdgeColor = new Color(0.75f, 0.78f, 0.55f, 0.85f);

        private void Awake()
        {
            if (_root == null && _closeButton == null)
            {
                // Boot.unity leftover was saved with no serialized refs — never show it.
                gameObject.SetActive(false);
                return;
            }

            WireClose();

            if (_stageRowTemplate != null)
            {
                _stageRowTemplate.SetActive(false);
            }

            if (_optionCardTemplate != null)
            {
                _optionCardTemplate.SetActive(false);
            }
        }

        public void BindRuntime(
            GameObject root,
            Text titleText,
            Transform stageListContent,
            GameObject stageRowTemplate,
            GameObject optionCardTemplate,
            RectTransform edgeLayer,
            Button closeButton)
        {
            _root = root;
            _titleText = titleText;
            _stageListContent = stageListContent;
            _stageRowTemplate = stageRowTemplate;
            _optionCardTemplate = optionCardTemplate;
            _edgeLayer = edgeLayer;
            _closeButton = closeButton;
            WireClose();

            if (_stageRowTemplate != null)
            {
                _stageRowTemplate.SetActive(false);
            }

            if (_optionCardTemplate != null)
            {
                _optionCardTemplate.SetActive(false);
            }
        }

        public bool IsOpen => _root != null && _root.activeSelf;

        public void ApplySnapshot(LevelRouteSnapshot snapshot)
        {
            _lastSnapshot = snapshot;
            if (snapshot == null || !snapshot.Visible)
            {
                Hide();
                return;
            }

            if (_titleText != null)
            {
                _titleText.text = $"路线选择 — {snapshot.LevelId}";
            }

            Rebuild(snapshot);
            if (_root != null)
            {
                _root.SetActive(true);
            }

            Canvas.ForceUpdateCanvases();
            RebuildEdges();
        }

        public void Hide()
        {
            if (_root != null)
            {
                _root.SetActive(false);
            }

            // Scene leftover in Boot.unity has _root unassigned; still must hide the overlay.
            if (_root == null || _root == gameObject)
            {
                gameObject.SetActive(false);
            }
        }

        private void LateUpdate()
        {
            if (IsOpen && _lastSnapshot != null && _lastSnapshot.Visible)
            {
                RebuildEdges();
            }
        }

        private void Rebuild(LevelRouteSnapshot snapshot)
        {
            ClearSpawned();
            if (_stageListContent == null || _stageRowTemplate == null || _optionCardTemplate == null)
            {
                return;
            }

            var stages = snapshot.Stages;
            if (stages == null || stages.Length == 0)
            {
                return;
            }

            // Top = highest StageNumber; bottom = Stage1 (bottom-up map).
            for (var i = stages.Length - 1; i >= 0; i--)
            {
                var stage = stages[i];
                var rowGo = Instantiate(_stageRowTemplate, _stageListContent);
                rowGo.name = $"StageRow_{stage.StageNumber}";
                rowGo.SetActive(true);
                _spawnedStages.Add(rowGo);

                var label = rowGo.transform.Find("StageLabel")?.GetComponent<Text>();
                if (label != null)
                {
                    label.text = $"Stage {stage.StageNumber}";
                }

                var optionsHost = rowGo.transform.Find("OptionsHost");
                if (optionsHost == null)
                {
                    optionsHost = rowGo.transform;
                }

                var options = stage.Options;
                if (options == null)
                {
                    continue;
                }

                for (var j = 0; j < options.Length; j++)
                {
                    var opt = options[j];
                    var card = Instantiate(_optionCardTemplate, optionsHost);
                    card.name = opt.GameplayOptionId;
                    card.SetActive(true);
                    _spawnedStages.Add(card);

                    var rt = card.GetComponent<RectTransform>();
                    if (rt != null)
                    {
                        _optionRects[opt.GameplayOptionId] = rt;
                    }

                    _unlockByOption[opt.GameplayOptionId] = SplitPipe(opt.UnlockNextOptionIds);

                    var title = card.transform.Find("Title")?.GetComponent<Text>();
                    if (title != null)
                    {
                        title.text = string.IsNullOrEmpty(opt.Title) ? opt.GameplayOptionId : opt.Title;
                    }

                    var desc = card.transform.Find("Description")?.GetComponent<Text>();
                    if (desc != null)
                    {
                        desc.text = opt.Description ?? string.Empty;
                    }

                    var reward = card.transform.Find("Reward")?.GetComponent<Text>();
                    if (reward != null)
                    {
                        reward.text = string.IsNullOrEmpty(opt.Reward) ? "奖励：—" : $"奖励：{opt.Reward}";
                    }

                    var typeText = card.transform.Find("Type")?.GetComponent<Text>();
                    if (typeText != null)
                    {
                        typeText.text = opt.GameplayType.ToString();
                    }

                    var icon = card.transform.Find("Icon")?.GetComponent<Image>();
                    if (icon != null)
                    {
                        icon.sprite = LevelRouteIconLoader.Load(opt.IconAssetId);
                        icon.enabled = icon.sprite != null;
                    }

                    var bg = card.GetComponent<Image>();
                    if (bg != null)
                    {
                        bg.color = ColorFor(opt.UiState);
                    }

                    var button = card.GetComponent<Button>();
                    if (button != null)
                    {
                        var selectable = opt.UiState == LevelRouteOptionUiState.Selectable;
                        button.interactable = selectable;
                        var capturedId = opt.GameplayOptionId;
                        button.onClick.RemoveAllListeners();
                        if (selectable)
                        {
                            button.onClick.AddListener(() => OptionSelected?.Invoke(capturedId));
                        }
                    }
                }
            }
        }

        private void RebuildEdges()
        {
            ClearEdges();
            if (_edgeLayer == null)
            {
                return;
            }

            foreach (var kv in _unlockByOption)
            {
                if (!_optionRects.TryGetValue(kv.Key, out var from) || from == null)
                {
                    continue;
                }

                var targets = kv.Value;
                if (targets == null)
                {
                    continue;
                }

                for (var i = 0; i < targets.Length; i++)
                {
                    if (!_optionRects.TryGetValue(targets[i], out var to) || to == null)
                    {
                        continue;
                    }

                    SpawnEdge(from, to);
                }
            }
        }

        private void SpawnEdge(RectTransform from, RectTransform to)
        {
            var go = new GameObject("Edge", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(_edgeLayer, false);
            var img = go.GetComponent<Image>();
            img.color = EdgeColor;
            img.raycastTarget = false;

            var rt = go.GetComponent<RectTransform>();
            var a = WorldToLayerLocal(from.position);
            var b = WorldToLayerLocal(to.position);
            var mid = (a + b) * 0.5f;
            var dir = b - a;
            var len = dir.magnitude;
            rt.anchoredPosition = mid;
            rt.sizeDelta = new Vector2(Mathf.Max(4f, len), 4f);
            rt.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
            _spawnedEdges.Add(go);
        }

        private Vector2 WorldToLayerLocal(Vector3 world)
        {
            var canvas = _edgeLayer.GetComponentInParent<Canvas>();
            var cam = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? canvas.worldCamera
                : null;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _edgeLayer,
                RectTransformUtility.WorldToScreenPoint(cam, world),
                cam,
                out var local);
            return local;
        }

        private void ClearSpawned()
        {
            for (var i = 0; i < _spawnedStages.Count; i++)
            {
                if (_spawnedStages[i] != null)
                {
                    Destroy(_spawnedStages[i]);
                }
            }

            _spawnedStages.Clear();
            _optionRects.Clear();
            _unlockByOption.Clear();
            ClearEdges();
        }

        private void ClearEdges()
        {
            for (var i = 0; i < _spawnedEdges.Count; i++)
            {
                if (_spawnedEdges[i] != null)
                {
                    Destroy(_spawnedEdges[i]);
                }
            }

            _spawnedEdges.Clear();
        }

        private void WireClose()
        {
            if (_closeButton == null)
            {
                return;
            }

            _closeButton.onClick.RemoveListener(HandleClose);
            _closeButton.onClick.AddListener(HandleClose);
        }

        private void HandleClose()
        {
            Hide();
            Closed?.Invoke();
        }

        private static Color ColorFor(LevelRouteOptionUiState state)
        {
            switch (state)
            {
                case LevelRouteOptionUiState.Selectable:
                    return Selectable;
                case LevelRouteOptionUiState.Cleared:
                    return Cleared;
                case LevelRouteOptionUiState.Running:
                    return Running;
                default:
                    return Locked;
            }
        }

        private static string[] SplitPipe(string encoded)
        {
            if (string.IsNullOrWhiteSpace(encoded))
            {
                return Array.Empty<string>();
            }

            var parts = encoded.Split('|');
            var list = new List<string>(parts.Length);
            for (var i = 0; i < parts.Length; i++)
            {
                var p = parts[i] != null ? parts[i].Trim() : string.Empty;
                if (p.Length > 0)
                {
                    list.Add(p);
                }
            }

            return list.ToArray();
        }
    }

    public static class LevelRouteIconLoader
    {
        public static Sprite Load(string iconAssetId)
        {
            if (string.IsNullOrEmpty(iconAssetId))
            {
                return null;
            }

            return Resources.Load<Sprite>("UI/Levels/" + iconAssetId);
        }
    }
}
