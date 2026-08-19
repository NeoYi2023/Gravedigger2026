using System.Collections.Generic;
using Gravedigger2026.Core.Combat;
using Gravedigger2026.Core.Config;
using Gravedigger2026.Core.UpgradeManufacture;
using UnityEngine;
using UnityEngine.UI;

namespace Gravedigger2026.Gameplay.Formation
{
    /// <summary>
    /// Formation / Combat top-left bond HUD (SPEC_03 §3.17).
    /// </summary>
    public sealed class FormationBondHudView : MonoBehaviour
    {
        private const float IconSize = 40f;

        [SerializeField] private Button _viewButton;
        [SerializeField] private Text _viewButtonLabel;
        [SerializeField] private RectTransform _iconRow;
        [SerializeField] private FormationBondDetailView _detailView;

        private readonly List<GameObject> _iconInstances = new List<GameObject>();
        private BattleFormationService _formation;
        private WarriorPoolService _pool;
        private ConfigCsvRepository _configs;
        private IReadOnlyList<ActiveFormationBond> _snapshot;

        public void Configure(Button viewButton, RectTransform iconRow, FormationBondDetailView detailView)
        {
            _viewButton = viewButton;
            _iconRow = iconRow;
            _detailView = detailView;
            if (_viewButton != null)
            {
                _viewButton.onClick.RemoveListener(HandleViewClicked);
                _viewButton.onClick.AddListener(HandleViewClicked);
            }
        }

        private void Awake()
        {
            if (_viewButton != null)
            {
                _viewButton.onClick.AddListener(HandleViewClicked);
            }
        }

        private void OnDestroy()
        {
            if (_viewButton != null)
            {
                _viewButton.onClick.RemoveListener(HandleViewClicked);
            }
        }

        public void BindServices(
            BattleFormationService formation,
            WarriorPoolService pool,
            ConfigCsvRepository configs)
        {
            _formation = formation;
            _pool = pool;
            _configs = configs;
            _snapshot = null;
        }

        public void SetSnapshot(IReadOnlyList<ActiveFormationBond> allBonds)
        {
            _formation = null;
            _pool = null;
            _snapshot = allBonds;
            if (allBonds == null)
            {
                RefreshIconRow(null);
                return;
            }

            var active = new List<ActiveFormationBond>();
            for (var i = 0; i < allBonds.Count; i++)
            {
                if (allBonds[i].IsActive)
                {
                    active.Add(allBonds[i]);
                }
            }

            RefreshIconRow(active);
        }

        public void RefreshLive()
        {
            _snapshot = null;
            if (_formation == null || _pool == null || _configs == null)
            {
                RefreshIconRow(null);
                return;
            }

            var active = FormationBondEvaluator.EvaluateActiveOnly(_formation, _pool, _configs);
            RefreshIconRow(active);
        }

        private void HandleViewClicked()
        {
            if (_detailView == null)
            {
                return;
            }

            if (_snapshot != null)
            {
                _detailView.ShowEvaluated(_snapshot, _configs, combatSnapshot: true);
                return;
            }

            _detailView.ShowLive(_formation, _pool, _configs);
        }

        private void RefreshIconRow(IReadOnlyList<ActiveFormationBond> activeBonds)
        {
            ClearIcons();
            if (_iconRow == null || activeBonds == null || activeBonds.Count == 0)
            {
                return;
            }

            for (var i = 0; i < activeBonds.Count; i++)
            {
                var bond = activeBonds[i];
                if (bond.Row == null)
                {
                    continue;
                }

                _iconInstances.Add(CreateIcon(bond.Row));
            }
        }

        private GameObject CreateIcon(FormationBondConfigRow row)
        {
            var go = new GameObject(
                "BondIcon",
                typeof(RectTransform),
                typeof(Image),
                typeof(LayoutElement));
            go.transform.SetParent(_iconRow, false);

            var layout = go.GetComponent<LayoutElement>();
            layout.preferredWidth = IconSize;
            layout.preferredHeight = IconSize;
            layout.minWidth = IconSize;
            layout.minHeight = IconSize;

            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(IconSize, IconSize);

            var image = go.GetComponent<Image>();
            image.preserveAspect = true;
            image.raycastTarget = false;
            var sprite = FormationBondIconLoader.Load(row.IconAssetId);
            if (sprite != null)
            {
                image.sprite = sprite;
                image.color = Color.white;
            }
            else
            {
                image.color = new Color(0.35f, 0.42f, 0.55f, 0.95f);
            }

            var labelGo = new GameObject("Label", typeof(RectTransform), typeof(Text));
            labelGo.transform.SetParent(go.transform, false);
            var labelRt = labelGo.GetComponent<RectTransform>();
            Stretch(labelRt);
            var label = labelGo.GetComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            label.fontSize = 10;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Color.white;
            label.raycastTarget = false;
            label.text = string.IsNullOrEmpty(row.DisplayName)
                ? row.BondId
                : row.DisplayName.Substring(0, Mathf.Min(2, row.DisplayName.Length));

            return go;
        }

        private void ClearIcons()
        {
            for (var i = 0; i < _iconInstances.Count; i++)
            {
                if (_iconInstances[i] != null)
                {
                    Destroy(_iconInstances[i]);
                }
            }

            _iconInstances.Clear();
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
    }
}
