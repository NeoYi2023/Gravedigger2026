using System;
using System.Collections.Generic;
using Gravedigger2026.Core.Config;
using Gravedigger2026.Core.TacticalFormation;
using UnityEngine;
using UnityEngine.UI;

namespace Gravedigger2026.Gameplay.Formation
{
    /// <summary>
    /// Left-edge vertical buttons for active Prepare tactical squads (SPEC_03 UI-030 / D-085).
    /// </summary>
    public sealed class TacticalFormationSquadBarView : MonoBehaviour
    {
        private const float ButtonSize = 56f;

        private static readonly Color NormalColor = new Color(0.22f, 0.28f, 0.38f, 0.95f);
        private static readonly Color SelectedColor = new Color(0.55f, 0.72f, 0.95f, 1f);
        private static readonly Color MissingIconColor = new Color(0.35f, 0.42f, 0.55f, 0.95f);

        [SerializeField] private RectTransform _buttonColumn;
        [SerializeField] private GameObject _root;

        private readonly List<GameObject> _buttonInstances = new List<GameObject>(4);
        private readonly List<string> _formationIds = new List<string>(4);
        private readonly List<Image> _buttonBackgrounds = new List<Image>(4);

        private ConfigCsvRepository _configs;
        private string _selectedFormationId;
        private Action<string> _onSquadClicked;

        public void Configure(RectTransform buttonColumn, GameObject root = null)
        {
            _buttonColumn = buttonColumn;
            _root = root != null ? root : gameObject;
        }

        public void SetClickHandler(Action<string> onSquadClicked)
        {
            _onSquadClicked = onSquadClicked;
        }

        public void Refresh(
            IReadOnlyList<TacticalFormationSquadSnapshot> squads,
            ConfigCsvRepository configs)
        {
            _configs = configs;
            ClearButtons();

            var hasAny = squads != null && squads.Count > 0;
            if (_root != null)
            {
                _root.SetActive(hasAny);
            }

            if (!hasAny || _buttonColumn == null)
            {
                _selectedFormationId = null;
                return;
            }

            var selectionStillValid = false;
            for (var i = 0; i < squads.Count; i++)
            {
                var squad = squads[i];
                if (squad == null || string.IsNullOrEmpty(squad.FormationId))
                {
                    continue;
                }

                CreateButton(squad.FormationId);
                if (string.Equals(squad.FormationId, _selectedFormationId, StringComparison.Ordinal))
                {
                    selectionStillValid = true;
                }
            }

            if (!selectionStillValid)
            {
                _selectedFormationId = null;
            }

            ApplySelectedVisuals();
        }

        public void SetSelectedFormationId(string formationId)
        {
            _selectedFormationId = formationId;
            ApplySelectedVisuals();
        }

        public string SelectedFormationId => _selectedFormationId;

        public bool ContainsFormationId(string formationId)
        {
            if (string.IsNullOrEmpty(formationId))
            {
                return false;
            }

            for (var i = 0; i < _formationIds.Count; i++)
            {
                if (string.Equals(_formationIds[i], formationId, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private void CreateButton(string formationId)
        {
            TacticalFormationConfigRow row = null;
            if (_configs != null)
            {
                _configs.TryGetTacticalFormation(formationId, out row);
            }

            var go = new GameObject(
                $"SquadBtn_{formationId}",
                typeof(RectTransform),
                typeof(Image),
                typeof(Button),
                typeof(LayoutElement));
            go.transform.SetParent(_buttonColumn, false);

            var layout = go.GetComponent<LayoutElement>();
            layout.preferredWidth = ButtonSize;
            layout.preferredHeight = ButtonSize;
            layout.minWidth = ButtonSize;
            layout.minHeight = ButtonSize;

            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(ButtonSize, ButtonSize);

            var bg = go.GetComponent<Image>();
            bg.color = NormalColor;
            bg.raycastTarget = true;

            var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconGo.transform.SetParent(go.transform, false);
            var iconRt = iconGo.GetComponent<RectTransform>();
            Stretch(iconRt, 4f);
            var icon = iconGo.GetComponent<Image>();
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            var sprite = TacticalFormationIconLoader.Load(row != null ? row.IconAssetId : formationId);
            if (sprite != null)
            {
                icon.sprite = sprite;
                icon.color = Color.white;
            }
            else
            {
                icon.color = MissingIconColor;
            }

            var labelGo = new GameObject("Label", typeof(RectTransform), typeof(Text));
            labelGo.transform.SetParent(go.transform, false);
            var labelRt = labelGo.GetComponent<RectTransform>();
            Stretch(labelRt, 0f);
            var label = labelGo.GetComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            label.fontSize = 11;
            label.alignment = TextAnchor.LowerCenter;
            label.color = Color.white;
            label.raycastTarget = false;
            var display = row != null && !string.IsNullOrEmpty(row.DisplayName)
                ? row.DisplayName
                : formationId;
            label.text = sprite != null
                ? string.Empty
                : display.Substring(0, Mathf.Min(2, display.Length));

            var capturedId = formationId;
            var button = go.GetComponent<Button>();
            button.targetGraphic = bg;
            button.onClick.AddListener(() => _onSquadClicked?.Invoke(capturedId));

            _buttonInstances.Add(go);
            _formationIds.Add(formationId);
            _buttonBackgrounds.Add(bg);
        }

        private void ApplySelectedVisuals()
        {
            for (var i = 0; i < _buttonBackgrounds.Count; i++)
            {
                var bg = _buttonBackgrounds[i];
                if (bg == null)
                {
                    continue;
                }

                var selected = string.Equals(_formationIds[i], _selectedFormationId, StringComparison.Ordinal);
                bg.color = selected ? SelectedColor : NormalColor;
            }
        }

        private void ClearButtons()
        {
            for (var i = 0; i < _buttonInstances.Count; i++)
            {
                if (_buttonInstances[i] != null)
                {
                    Destroy(_buttonInstances[i]);
                }
            }

            _buttonInstances.Clear();
            _formationIds.Clear();
            _buttonBackgrounds.Clear();
        }

        private void OnDestroy()
        {
            ClearButtons();
            _onSquadClicked = null;
        }

        private static void Stretch(RectTransform rt, float inset)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(inset, inset);
            rt.offsetMax = new Vector2(-inset, -inset);
        }
    }
}
