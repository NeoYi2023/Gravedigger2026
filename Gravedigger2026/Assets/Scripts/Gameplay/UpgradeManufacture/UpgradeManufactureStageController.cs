using System;
using System.Collections.Generic;
using System.Text;
using Gravedigger2026.Core.Config;
using Gravedigger2026.Core.Level;
using Gravedigger2026.Core.UpgradeManufacture;
using Gravedigger2026.Gameplay.Defend;
using Gravedigger2026.Gameplay.Formation;
using UnityEngine;

namespace Gravedigger2026.Gameplay.UpgradeManufacture
{
    /// <summary>
    /// UM stage: upgrade + manufacture main screen; FormationEditor via 布阵 button (D-030–D-032).
    /// </summary>
    public sealed class UpgradeManufactureStageController : MonoBehaviour
    {
        private const int PoolPreviewLines = 4;

        [SerializeField] private UpgradeManufacturePrefabCatalog _catalog;
        [SerializeField] private UpgradePanelView _upgradePanel;
        [SerializeField] private ManufacturePanelView _manufacturePanel;
        [SerializeField] private GameObject _mainUiRoot;
        [SerializeField] private FormationPanelView _formationPanel;

        private readonly List<string> _inventoryLabels = new List<string>();
        private readonly List<string> _inventoryIds = new List<string>();
        private readonly List<string> _slotLabels = new List<string>();

        private ConfigCsvRepository _configs;
        private FormationPrefabCatalog _formationCatalog;
        private DefendPrefabCatalog _defendCatalog;
        private ProtagonistProgressService _progress;
        private ManufactureService _manufacture;
        private WarriorPoolService _warriorPool;
        private BattleFormationService _formation;
        private LevelStageContext _stageContext;
        private Action _onComplete;
        private FormationEditorController _editor;
        private bool _active;

        public void ConfigureCatalog(UpgradeManufacturePrefabCatalog catalog)
        {
            if (catalog != null)
            {
                _catalog = catalog;
            }
        }

        public void Begin(
            ConfigCsvRepository configs,
            FormationPrefabCatalog formationCatalog,
            DefendPrefabCatalog defendCatalog,
            ProtagonistProgressService progress,
            ManufactureService manufacture,
            WarriorPoolService warriorPool,
            BattleFormationService formation,
            LevelStageContext stageContext,
            Action onComplete)
        {
            End();
            _configs = configs;
            _formationCatalog = formationCatalog;
            _defendCatalog = defendCatalog;
            _progress = progress ?? throw new ArgumentNullException(nameof(progress));
            _manufacture = manufacture;
            _warriorPool = warriorPool;
            _formation = formation ?? throw new ArgumentNullException(nameof(formation));
            _stageContext = stageContext;
            _onComplete = onComplete;
            _active = true;

            _progress.Changed += HandleProgressChanged;
            if (_upgradePanel != null)
            {
                _upgradePanel.Inject100Requested += HandleInject100;
                _upgradePanel.Inject500Requested += HandleInject500;
                _upgradePanel.CompleteRequested += HandleComplete;
                _upgradePanel.FormationRequested += HandleOpenFormation;
                _upgradePanel.OpenUpgradeModalRequested += HandleOpenUpgradeModal;
                _upgradePanel.CloseUpgradeModalRequested += HandleCloseUpgradeModal;
                _upgradePanel.Show();
            }

            if (_manufacturePanel != null && _manufacture != null)
            {
                _manufacture.Changed += HandleManufactureChanged;
                _manufacturePanel.ItemPlaceRequested += HandleItemPlaceRequested;
                _manufacturePanel.ItemPlaceAtRequested += HandleItemPlaceAtRequested;
                _manufacturePanel.SlotClearRequested += HandleSlotClearRequested;
                _manufacturePanel.GrantKitRequested += HandleGrantKitRequested;
                _manufacturePanel.ClearSlotsRequested += HandleClearSlotsRequested;
                _manufacturePanel.ManufactureRequested += HandleManufactureRequested;
            }

            if (_warriorPool != null)
            {
                _warriorPool.Changed += HandlePoolChanged;
            }

            _formation.Changed += HandlePoolChanged;

            if (_formationPanel != null)
            {
                _formationPanel.gameObject.SetActive(false);
            }

            SetMainUiVisible(true);
            RefreshStatus();
            RefreshManufacture();
        }

        public void End()
        {
            CloseFormationEditor();

            if (!_active)
            {
                return;
            }

            if (_progress != null)
            {
                _progress.Changed -= HandleProgressChanged;
            }

            if (_upgradePanel != null)
            {
                _upgradePanel.Inject100Requested -= HandleInject100;
                _upgradePanel.Inject500Requested -= HandleInject500;
                _upgradePanel.CompleteRequested -= HandleComplete;
                _upgradePanel.FormationRequested -= HandleOpenFormation;
                _upgradePanel.OpenUpgradeModalRequested -= HandleOpenUpgradeModal;
                _upgradePanel.CloseUpgradeModalRequested -= HandleCloseUpgradeModal;
                _upgradePanel.Hide();
            }

            if (_manufacturePanel != null && _manufacture != null)
            {
                _manufacture.Changed -= HandleManufactureChanged;
                _manufacturePanel.ItemPlaceRequested -= HandleItemPlaceRequested;
                _manufacturePanel.ItemPlaceAtRequested -= HandleItemPlaceAtRequested;
                _manufacturePanel.SlotClearRequested -= HandleSlotClearRequested;
                _manufacturePanel.GrantKitRequested -= HandleGrantKitRequested;
                _manufacturePanel.ClearSlotsRequested -= HandleClearSlotsRequested;
                _manufacturePanel.ManufactureRequested -= HandleManufactureRequested;
            }

            if (_warriorPool != null)
            {
                _warriorPool.Changed -= HandlePoolChanged;
            }

            if (_formation != null)
            {
                _formation.Changed -= HandlePoolChanged;
            }

            _configs = null;
            _formationCatalog = null;
            _defendCatalog = null;
            _progress = null;
            _manufacture = null;
            _warriorPool = null;
            _formation = null;
            _stageContext = null;
            _onComplete = null;
            _active = false;
        }

        private void HandleOpenFormation()
        {
            if (_editor != null || _formationCatalog == null || _defendCatalog == null)
            {
                Debug.LogWarning("[UM] FormationEditor catalog missing.");
                return;
            }

            var rootPrefab = _formationCatalog.FormationEditorRootPrefab;
            if (rootPrefab == null)
            {
                Debug.LogError("[UM] FormationEditorRoot prefab missing.");
                return;
            }

            var mapId = FormationMapResolver.ResolveUmBattleMapId(
                _configs,
                _stageContext != null ? _stageContext.LevelId : null,
                _stageContext != null ? _stageContext.StageNumber : 0);

            if (!_defendCatalog.TryGetMap(mapId, out var mapPrefab))
            {
                Debug.LogError($"[UM] BattleMap prefab missing for '{mapId}'.");
                return;
            }

            SetMainUiVisible(false);
            var instance = Instantiate(rootPrefab, transform);
            instance.name = "FormationEditorRoot(Clone)";
            _editor = instance.GetComponent<FormationEditorController>();
            if (_editor == null)
            {
                _editor = instance.AddComponent<FormationEditorController>();
            }

            _editor.ReturnRequested += HandleFormationReturn;
            _editor.Begin(
                FormationEditorMode.UpgradeManufacture,
                _defendCatalog,
                _warriorPool,
                _formation,
                _progress,
                mapPrefab,
                null);

            Debug.Log($"[UM Formation] Opened editor map={mapId}");
        }

        private void HandleFormationReturn()
        {
            CloseFormationEditor();
            SetMainUiVisible(true);
            RefreshManufacture();
        }

        private void CloseFormationEditor()
        {
            if (_editor == null)
            {
                return;
            }

            _editor.ReturnRequested -= HandleFormationReturn;
            _editor.End();
            Destroy(_editor.gameObject);
            _editor = null;
        }

        private void SetMainUiVisible(bool visible)
        {
            if (_mainUiRoot != null)
            {
                _mainUiRoot.SetActive(visible);
                return;
            }

            if (_upgradePanel != null)
            {
                if (visible)
                {
                    _upgradePanel.Show();
                }
                else
                {
                    _upgradePanel.Hide();
                }
            }
        }

        private void HandleOpenUpgradeModal()
        {
            _upgradePanel?.ShowUpgradeModal();
        }

        private void HandleCloseUpgradeModal()
        {
            _upgradePanel?.HideUpgradeModal();
        }

        private void HandleInject100()
        {
            Inject(100);
        }

        private void HandleInject500()
        {
            Inject(500);
        }

        private void Inject(long amount)
        {
            if (_progress == null)
            {
                return;
            }

            var gained = _progress.AddExperience(amount);
            Debug.Log(
                $"[UM Upgrade] Debug +{amount} Exp → Lifetime={_progress.LifetimeExperience} Level={_progress.Level} (+{gained}) Tech={_progress.TechPoints} Cap={_progress.ControlPowerCap} MaxHP={_progress.ProtagonistMaxHP}");
        }

        private void HandleComplete()
        {
            if (_formation != null)
            {
                var sb = new StringBuilder();
                sb.Append($"[UM Formation] Complete snapshot count={_formation.Entries.Count}");
                for (var i = 0; i < _formation.Entries.Count; i++)
                {
                    var e = _formation.Entries[i];
                    sb.Append($" | {e.WarriorId}@({e.PositionX:0.##},{e.PositionZ:0.##}) HP={e.RemainingHP:0}");
                }

                Debug.Log(sb.ToString());
            }

            _onComplete?.Invoke();
        }

        private void HandleProgressChanged()
        {
            RefreshStatus();
        }

        private void HandleManufactureChanged()
        {
            RefreshManufacture();
        }

        private void HandlePoolChanged()
        {
            RefreshManufacture();
        }

        private void HandleItemPlaceRequested(string itemId)
        {
            if (_manufacture == null)
            {
                return;
            }

            if (!_manufacture.TryPlace(itemId, out var error))
            {
                Debug.Log($"[UM Manufacture] 放入失败：{error}");
                RefreshManufacture();
            }
        }

        private void HandleItemPlaceAtRequested(int slotIndex, string itemId)
        {
            if (_manufacture == null)
            {
                return;
            }

            if (!_manufacture.TryPlaceAt(slotIndex, itemId, out var error))
            {
                Debug.Log($"[UM Manufacture] 放入失败：{error}");
                RefreshManufacture();
            }
        }

        private void HandleSlotClearRequested(int slotIndex)
        {
            _manufacture?.TryClearSlot(slotIndex);
        }

        private void HandleGrantKitRequested()
        {
            _manufacture?.GrantDebugStarterKit();
        }

        private void HandleClearSlotsRequested()
        {
            _manufacture?.ClearAllSlots();
        }

        private void HandleManufactureRequested()
        {
            if (_manufacture == null)
            {
                return;
            }

            if (!_manufacture.TryManufacture(out var instance, out var error))
            {
                Debug.Log($"[UM Manufacture] 制造失败：{error}");
                RefreshManufacture();
                return;
            }

            if (_catalog != null && !_catalog.TryGetWarriorAppearance(instance.AppearanceId, out _))
            {
                Debug.LogWarning(
                    $"[UM Manufacture] 外观 Prefab 未绑定：Assets/Prefabs/Defend/Warriors/{instance.AppearanceId}.prefab");
            }
        }

        private void RefreshStatus()
        {
            if (_upgradePanel == null || _progress == null)
            {
                return;
            }

            var next = _progress.IsMaxLevel
                ? "已满级"
                : $"下一级累计阈值 {_progress.GetNextRequiredTotalExperience()}";
            _upgradePanel.SetStatus(
                $"升级区\n" +
                $"等级 Level = {_progress.Level}\n" +
                $"生涯经验 LifetimeExperience = {_progress.LifetimeExperience}\n" +
                $"科技点 TechPoints = {_progress.TechPoints}\n" +
                $"控制力上限 ControlPowerCap = {_progress.ControlPowerCap}\n" +
                $"护盾上限语义 ProtagonistMaxHP = {_progress.ProtagonistMaxHP}\n" +
                $"{next}");
        }

        private void RefreshManufacture()
        {
            if (_manufacturePanel == null || _manufacture == null)
            {
                return;
            }

            _inventoryLabels.Clear();
            _inventoryIds.Clear();
            var inventory = _manufacture.BuildInventory();
            for (var i = 0; i < inventory.Count; i++)
            {
                var entry = inventory[i];
                _inventoryLabels.Add($"×{entry.Available}  {entry.Label}");
                _inventoryIds.Add(entry.ItemId);
            }

            if (_inventoryLabels.Count == 0)
            {
                _inventoryLabels.Add("（无可用制造材料，可先挖坟或注入 Debug 套件）");
                _inventoryIds.Add(string.Empty);
            }

            _manufacturePanel.SetInventoryLines(_inventoryLabels, _inventoryIds);

            _slotLabels.Clear();
            var slots = _manufacture.Slots;
            for (var i = 0; i < slots.Count; i++)
            {
                var slot = slots[i];
                var name = ManufactureService.DescribeSlotKind(slot.Kind);
                _slotLabels.Add(slot.IsEmpty ? $"{name}：（空）" : $"{name}：{slot.ItemId}");
            }

            _manufacturePanel.SetSlotLines(_slotLabels);

            var preview = _manufacture.GetPreview();
            _manufacturePanel.SetPreviewText(BuildPreviewText(preview));
            _manufacturePanel.SetManufactureInteractable(preview.CanManufacture);
            _manufacturePanel.SetPoolText(BuildPoolText());
            RefreshWarriorVisual(preview);
        }

        private void RefreshWarriorVisual(ManufacturePreview preview)
        {
            if (_manufacturePanel == null || _manufacture == null)
            {
                return;
            }

            var show = _manufacture.AreNonGemSlotsFilled();
            GameObject prefab = null;
            if (show
                && !string.IsNullOrEmpty(preview.TrialAppearanceId)
                && _catalog != null)
            {
                _catalog.TryGetWarriorAppearance(preview.TrialAppearanceId, out prefab);
            }

            _manufacturePanel.SetWarriorVisualPreview(show, prefab, preview.TrialAppearanceId);
        }

        private static string BuildPreviewText(ManufacturePreview preview)
        {
            var sb = new StringBuilder();
            sb.AppendLine("制造区预览（StaticStat；不含运行时 Buff）");
            sb.AppendLine(
                $"生命 {preview.StaticStats.MaxHP:0.##}｜移速 {preview.StaticStats.MoveSpeed:0.##}｜力量 {preview.StaticStats.Strength:0.##}｜敏捷 {preview.StaticStats.Agility:0.##}｜智力 {preview.StaticStats.Intelligence:0.##}");
            sb.AppendLine($"BodyLife {preview.BodyLife:0.##}｜静态 MaxHP {preview.StaticMaxHP}");
            sb.AppendLine(
                $"精魂消耗 {preview.TotalSpiritCost:0.##}｜控制力占用 {preview.ControlPowerCost:0.##}");
            sb.AppendLine(
                $"试算种族 {preview.TrialRaceId ?? "-"}｜职业 {(string.IsNullOrEmpty(preview.ClassName) ? "-" : preview.ClassName)}｜外观 {preview.TrialAppearanceId ?? "-"}");
            sb.AppendLine($"试算命名 {(string.IsNullOrEmpty(preview.TrialWarriorName) ? "-" : preview.TrialWarriorName)}");
            sb.Append(preview.CanManufacture ? "可制造" : preview.BlockReason);
            return sb.ToString();
        }

        private string BuildPoolText()
        {
            if (_warriorPool == null || _warriorPool.Warriors.Count == 0)
            {
                return "士兵池：空";
            }

            var warriors = _warriorPool.Warriors;
            var sb = new StringBuilder();
            sb.AppendLine($"士兵池：{warriors.Count} 名");
            var start = Math.Max(0, warriors.Count - PoolPreviewLines);
            for (var i = start; i < warriors.Count; i++)
            {
                var w = warriors[i];
                var deployed = _formation != null && _formation.IsDeployed(w.Id) ? "〔上阵〕" : string.Empty;
                sb.AppendLine(
                    $"{w.Id} {w.WarriorName}{deployed}｜HP {w.RemainingHP:0}｜{w.RaceId}／{w.ClassId}｜{w.AppearanceId}｜控 {w.ControlPowerCost:0.##}");
            }

            return sb.ToString();
        }
    }
}
