using System;
using System.Collections.Generic;
using Gravedigger2026.Core.Config;
using Gravedigger2026.Core.Dig;
using Gravedigger2026.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Gravedigger2026.Gameplay.Dig
{
    /// <summary>DigStageSummary (UI-011): icon+name+qty grid on scroll Content, max 5 columns, ~3 rows visible.</summary>
    public sealed class DigStageSummaryView : MonoBehaviour
    {
        private const string EmptyMessage = "本阶段未获得奖励。";

        [SerializeField] private GameObject _root;
        [SerializeField] private RectTransform _itemGrid;
        [SerializeField] private DigSummaryItemCell _itemCellPrefab;
        [SerializeField] private Text _emptyText;
        [SerializeField] private Button _confirmButton;

        private readonly List<GameObject> _spawnedCells = new List<GameObject>(16);
        private Action _onConfirm;

        private void Awake()
        {
            // Prefab starts inactive. Do NOT SetActive(false) on self/_root here — see ToolsPanelView.
            if (_confirmButton != null)
            {
                _confirmButton.onClick.AddListener(HandleConfirm);
            }
        }

        public void Show(
            IReadOnlyList<DigStageSummaryEntry> entries,
            ConfigCsvRepository configs,
            Action onConfirm)
        {
            _onConfirm = onConfirm;
            ClearCells();

            var hasItems = entries != null && entries.Count > 0;
            if (_emptyText != null)
            {
                _emptyText.gameObject.SetActive(!hasItems);
                if (!hasItems)
                {
                    _emptyText.text = EmptyMessage;
                }
            }

            if (_itemGrid != null)
            {
                _itemGrid.gameObject.SetActive(hasItems);
            }

            if (hasItems && _itemCellPrefab != null && _itemGrid != null)
            {
                for (var i = 0; i < entries.Count; i++)
                {
                    var entry = entries[i];
                    var cell = Instantiate(_itemCellPrefab, _itemGrid);
                    cell.gameObject.SetActive(true);
                    cell.Bind(
                        ResolveIcon(entry, configs),
                        entry.DisplayName,
                        DigStageRewardLedger.FormatAmount(entry.Amount));
                    _spawnedCells.Add(cell.gameObject);
                }
            }

            if (_root != null)
            {
                _root.SetActive(true);
            }
        }

        public void Hide()
        {
            ClearCells();
            if (_root != null)
            {
                _root.SetActive(false);
            }

            _onConfirm = null;
        }

        private void HandleConfirm()
        {
            var cb = _onConfirm;
            Hide();
            cb?.Invoke();
        }

        private void ClearCells()
        {
            for (var i = 0; i < _spawnedCells.Count; i++)
            {
                if (_spawnedCells[i] != null)
                {
                    Destroy(_spawnedCells[i]);
                }
            }

            _spawnedCells.Clear();
        }

        private static Sprite ResolveIcon(DigStageSummaryEntry entry, ConfigCsvRepository configs)
        {
            if (entry.IsBodyPart && configs != null && configs.TryGetBodyPart(entry.RewardId, out var part))
            {
                return DigBodyArtLoader.LoadBodyPart(part.ArtAssetId, part.BodySlot);
            }

            return ItemIconLoader.LoadFromCatalog(configs, entry.RewardId);
        }
    }
}
