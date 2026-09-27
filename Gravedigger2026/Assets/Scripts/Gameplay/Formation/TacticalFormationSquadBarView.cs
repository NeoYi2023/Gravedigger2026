using System;
using System.Collections.Generic;
using Gravedigger2026.Core.Config;
using UnityEngine;
using UnityEngine.UI;

namespace Gravedigger2026.Gameplay.Formation
{
    /// <summary>
    /// Left-edge catalog buttons, one per distinct FormationId (SPEC_03 UI-030 / D-093).
    /// Click tries to create a group; the bar does not select.
    /// </summary>
    public sealed class TacticalFormationSquadBarView : MonoBehaviour
    {
        private const float ButtonSize = 56f;

        private static readonly Color NormalColor = new Color(0.22f, 0.28f, 0.38f, 0.95f);
        private static readonly Color MissingIconColor = new Color(0.35f, 0.42f, 0.55f, 0.95f);

        [SerializeField] private RectTransform _buttonColumn;
        [SerializeField] private GameObject _root;

        private readonly List<GameObject> _buttonInstances = new List<GameObject>(4);
        private readonly List<string> _formationIds = new List<string>(4);

        private ConfigCsvRepository _configs;
        private Action<string> _onFormationClicked;

        public void Configure(RectTransform buttonColumn, GameObject root = null)
        {
            _buttonColumn = buttonColumn;
            _root = root != null ? root : gameObject;
        }

        public void SetClickHandler(Action<string> onFormationClicked)
        {
            _onFormationClicked = onFormationClicked;
        }

        /// <summary>
        /// Show one button per catalog id. Duplicate ids are skipped.
        /// The strip stays visible when no groups exist, as long as the catalog is non-empty.
        /// </summary>
        public void Refresh(IReadOnlyList<string> formationIds, ConfigCsvRepository configs)
        {
            _configs = configs;
            ClearButtons();

            var count = formationIds != null ? formationIds.Count : 0;
            if (count == 0 || _buttonColumn == null)
            {
                if (_root != null)
                {
                    _root.SetActive(false);
                }

                return;
            }

            for (var i = 0; i < count; i++)
            {
                var formationId = formationIds[i];
                if (string.IsNullOrEmpty(formationId) || ContainsFormationId(formationId))
                {
                    continue;
                }

                CreateButton(formationId);
            }

            if (_root != null)
            {
                _root.SetActive(_formationIds.Count > 0);
            }
        }

        private void CreateButton(string formationId)
        {
            TacticalFormationConfigRow row = null;
            if (_configs != null)
            {
                _configs.TryGetTacticalFormation(formationId, out row);
            }

            var go = new GameObject(
                $"CatalogBtn_{formationId}",
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
            button.onClick.AddListener(() => _onFormationClicked?.Invoke(capturedId));

            _buttonInstances.Add(go);
            _formationIds.Add(formationId);
        }

        private bool ContainsFormationId(string formationId)
        {
            for (var i = 0; i < _formationIds.Count; i++)
            {
                if (string.Equals(_formationIds[i], formationId, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
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
        }

        private void OnDestroy()
        {
            ClearButtons();
            _onFormationClicked = null;
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
