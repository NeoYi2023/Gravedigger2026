using System;
using System.Collections.Generic;
using Gravedigger2026.Core.Config;
using Gravedigger2026.Core.TacticalFormation;
using UnityEngine;
using UnityEngine.UI;

namespace Gravedigger2026.Gameplay.Formation
{
    /// <summary>
    /// Top-center group cards (SPEC_03 UI-035 / D-093).
    /// The scroll shell and card template live on the formation prefab; this view only clones cards.
    /// </summary>
    public sealed class TacticalFormationGroupCardBarView : MonoBehaviour
    {
        public const int VisibleCardCount = 8;

        private static readonly Color NormalColor = new Color(0.18f, 0.22f, 0.3f, 0.95f);
        private static readonly Color SelectedColor = new Color(0.32f, 0.5f, 0.74f, 0.98f);
        private static readonly Color MissingIconColor = new Color(0.35f, 0.42f, 0.55f, 0.95f);

        [SerializeField] private RectTransform _viewport;
        [SerializeField] private RectTransform _content;
        [SerializeField] private ScrollRect _scroll;
        [SerializeField] private GameObject _cardTemplate;
        [SerializeField] private GameObject _root;

        private readonly List<GameObject> _cards = new List<GameObject>(8);
        private Action<string> _onSelect;
        private Action<string> _onDisband;
        private bool _missingShellLogged;

        public void SetHandlers(Action<string> onSelect, Action<string> onDisband)
        {
            _onSelect = onSelect;
            _onDisband = onDisband;
        }

        /// <summary>
        /// One card per group. Viewport width fits exactly <see cref="VisibleCardCount"/> cards.
        /// Horizontal scroll turns on only when there are more.
        /// </summary>
        public void Refresh(
            IReadOnlyList<TacticalFormationSquadSnapshot> groups,
            ConfigCsvRepository configs,
            string selectedGroupId)
        {
            if (!ShellReady())
            {
                return;
            }

            var count = groups != null ? groups.Count : 0;
            if (count == 0)
            {
                ClearCards();
                if (_root != null)
                {
                    _root.SetActive(false);
                }

                return;
            }

            if (_root != null && !_root.activeSelf)
            {
                _root.SetActive(true);
            }

            FitViewportToEightCards();
            var savedOffset = _scroll.horizontalNormalizedPosition;
            if (float.IsNaN(savedOffset))
            {
                savedOffset = 0f;
            }

            ClearCards();

            for (var i = 0; i < count; i++)
            {
                var squad = groups[i];
                if (squad == null || string.IsNullOrEmpty(squad.GroupInstanceId))
                {
                    continue;
                }

                CreateCard(squad, configs, selectedGroupId);
            }

            var scrollable = _cards.Count > VisibleCardCount;
            _scroll.horizontal = scrollable;
            _scroll.vertical = false;
            var background = _scroll.GetComponent<Image>();
            if (background != null)
            {
                background.raycastTarget = scrollable;
            }

            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(_content);
            _scroll.horizontalNormalizedPosition = scrollable ? savedOffset : 0f;
        }

        private void CreateCard(
            TacticalFormationSquadSnapshot squad,
            ConfigCsvRepository configs,
            string selectedGroupId)
        {
            var go = Instantiate(_cardTemplate, _content, false);
            go.name = "GroupCard_" + squad.GroupInstanceId;
            go.SetActive(true);

            TacticalFormationConfigRow identity = null;
            if (configs != null)
            {
                configs.TryGetTacticalFormation(squad.FormationId, out identity);
            }

            var selected = string.Equals(squad.GroupInstanceId, selectedGroupId, StringComparison.Ordinal);
            var background = go.GetComponent<Image>();
            if (background != null)
            {
                background.color = selected ? SelectedColor : NormalColor;
            }

            var icon = FindChildImage(go.transform, "Icon");
            if (icon != null)
            {
                var iconId = identity != null && !string.IsNullOrEmpty(identity.IconAssetId)
                    ? identity.IconAssetId
                    : squad.FormationId;
                var sprite = TacticalFormationIconLoader.Load(iconId);
                icon.sprite = sprite;
                icon.color = sprite != null ? Color.white : MissingIconColor;
                icon.preserveAspect = true;
            }

            var display = identity != null && !string.IsNullOrEmpty(identity.DisplayName)
                ? identity.DisplayName
                : squad.FormationId;
            SetChildText(go.transform, "Name", display);
            SetChildText(go.transform, "Level", FormatLevel(squad));
            var members = squad.MemberIds != null ? squad.MemberIds.Length : 0;
            SetChildText(go.transform, "Count", members + "人");

            var capturedId = squad.GroupInstanceId;
            var cardButton = go.GetComponent<Button>();
            if (cardButton != null)
            {
                cardButton.onClick.AddListener(() => _onSelect?.Invoke(capturedId));
            }

            var disband = FindChild(go.transform, "Disband");
            if (disband != null)
            {
                disband.gameObject.SetActive(selected);
                var disbandButton = disband.GetComponent<Button>();
                if (disbandButton != null)
                {
                    disbandButton.onClick.AddListener(() => _onDisband?.Invoke(capturedId));
                }
            }

            _cards.Add(go);
        }

        /// <summary>
        /// Matched table level when a row exists. Otherwise the computed level plus 无加成,
        /// so two groups with no stat row stay distinguishable.
        /// </summary>
        public static string FormatLevel(TacticalFormationSquadSnapshot squad)
        {
            if (squad == null)
            {
                return string.Empty;
            }

            if (squad.MatchedLevelRow != null)
            {
                return "Lv" + squad.MatchedLevelRow.FormationLevel;
            }

            return "Lv" + squad.ComputedLevel + " 无加成";
        }

        private void FitViewportToEightCards()
        {
            var cardRt = _cardTemplate.GetComponent<RectTransform>();
            if (cardRt == null)
            {
                return;
            }

            var spacing = 0f;
            var layout = _content.GetComponent<HorizontalLayoutGroup>();
            if (layout != null)
            {
                spacing = layout.spacing;
            }

            var width = cardRt.sizeDelta.x * VisibleCardCount + spacing * (VisibleCardCount - 1);
            var height = cardRt.sizeDelta.y;
            var rootRt = _root != null ? _root.GetComponent<RectTransform>() : transform as RectTransform;
            if (rootRt != null)
            {
                rootRt.sizeDelta = new Vector2(width, height);
            }
        }

        private bool ShellReady()
        {
            if (_content != null && _cardTemplate != null && _scroll != null && _viewport != null)
            {
                return true;
            }

            if (!_missingShellLogged)
            {
                _missingShellLogged = true;
                Debug.LogError("[TacticalFormationGroupCardBar] Prefab shell is incomplete.");
            }

            return false;
        }

        private void ClearCards()
        {
            for (var i = 0; i < _cards.Count; i++)
            {
                if (_cards[i] != null)
                {
                    Destroy(_cards[i]);
                }
            }

            _cards.Clear();
        }

        private void OnDestroy()
        {
            ClearCards();
            _onSelect = null;
            _onDisband = null;
        }

        private static void SetChildText(Transform root, string childName, string value)
        {
            var child = FindChild(root, childName);
            if (child == null)
            {
                return;
            }

            var text = child.GetComponent<Text>();
            if (text != null)
            {
                text.text = value ?? string.Empty;
            }
        }

        private static Image FindChildImage(Transform root, string childName)
        {
            var child = FindChild(root, childName);
            return child != null ? child.GetComponent<Image>() : null;
        }

        private static Transform FindChild(Transform root, string childName)
        {
            for (var i = 0; i < root.childCount; i++)
            {
                var child = root.GetChild(i);
                if (child.name == childName)
                {
                    return child;
                }
            }

            return null;
        }
    }
}
