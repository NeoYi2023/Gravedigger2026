using System;
using System.Collections.Generic;
using System.Text;
using Gravedigger2026.Core;
using Gravedigger2026.Core.AutoManufacture;
using Gravedigger2026.Core.Config;
using Gravedigger2026.Core.Level;
using Gravedigger2026.Core.UpgradeManufacture;
using Gravedigger2026.Gameplay.Defend;
using Gravedigger2026.Gameplay.Formation;
using Gravedigger2026.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Gravedigger2026.Gameplay.UpgradeManufacture
{
    /// <summary>
    /// UM stage: upgrade + manufacture main screen; FormationEditor via 布阵 button (D-030–D-032).
    /// </summary>
    public sealed class UpgradeManufactureStageController : MonoBehaviour
    {
        [SerializeField] private UpgradeManufacturePrefabCatalog _catalog;
        [SerializeField] private UpgradePanelView _upgradePanel;
        [SerializeField] private ManufacturePanelView _manufacturePanel;
        [SerializeField] private GameObject _mainUiRoot;
        [SerializeField] private FormationPanelView _formationPanel;
        [SerializeField] private ToastView _tipsView;

        private readonly List<string> _inventoryLabels = new List<string>();
        private readonly List<string> _inventoryIds = new List<string>();
        private readonly List<string> _slotLabels = new List<string>();
        private readonly List<PoolSoldierEntry> _poolEntries = new List<PoolSoldierEntry>();

        private ConfigCsvRepository _configs;
        private FormationPrefabCatalog _formationCatalog;
        private DefendPrefabCatalog _defendCatalog;
        private ProtagonistProgressService _progress;
        private ManufactureService _manufacture;
        private WarriorPoolService _warriorPool;
        private BattleFormationService _formation;
        private AutoManufactureBatchRecordService _batchRecord;
        private LevelStageContext _stageContext;
        private Action _onComplete;
        private FormationEditorController _editor;
        private bool _active;

        public bool IsFormationEditorOpen => _editor != null && _editor.IsActive;

        public bool TryCollectFormationClassZones(List<FormationClassZoneSnapshot> into)
        {
            if (_editor == null)
            {
                if (into != null)
                {
                    into.Clear();
                }

                return false;
            }

            return _editor.TryCollectClassZones(into);
        }

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
            Action onComplete,
            AutoManufactureBatchRecordService batchRecord = null,
            bool autoOpenFormation = false)
        {
            End();
            EnsureTipsView();
            _configs = configs;
            _formationCatalog = formationCatalog;
            _defendCatalog = defendCatalog;
            _progress = progress ?? throw new ArgumentNullException(nameof(progress));
            _manufacture = manufacture;
            _warriorPool = warriorPool;
            _formation = formation ?? throw new ArgumentNullException(nameof(formation));
            _batchRecord = batchRecord;
            _stageContext = stageContext;
            _onComplete = onComplete;
            _active = true;

            _progress.Changed += HandleProgressChanged;
            if (_upgradePanel != null)
            {
                var mode = _formation != null ? _formation.BoundCampaignMode : CampaignMode.Mode1;
                if (mode == CampaignMode.Mode2)
                {
                    _upgradePanel.EnsureManufactureRecordUi();
                }

                _upgradePanel.Inject100Requested += HandleInject100;
                _upgradePanel.Inject500Requested += HandleInject500;
                _upgradePanel.CompleteRequested += HandleComplete;
                _upgradePanel.FormationRequested += HandleOpenFormation;
                _upgradePanel.OpenUpgradeModalRequested += HandleOpenUpgradeModal;
                _upgradePanel.CloseUpgradeModalRequested += HandleCloseUpgradeModal;
                _upgradePanel.OpenManufactureRecordRequested += HandleOpenManufactureRecord;
                _upgradePanel.CloseManufactureRecordRequested += HandleCloseManufactureRecord;
                _upgradePanel.Show();
            }

            if (_manufacturePanel != null && _manufacture != null && _manufacturePanel.gameObject.activeSelf)
            {
                _manufacture.Changed += HandleManufactureChanged;
                _manufacturePanel.ItemPlaceRequested += HandleItemPlaceRequested;
                _manufacturePanel.ItemPlaceAtRequested += HandleItemPlaceAtRequested;
                _manufacturePanel.SlotClearRequested += HandleSlotClearRequested;
                _manufacturePanel.GrantKitRequested += HandleGrantKitRequested;
                _manufacturePanel.ClearSlotsRequested += HandleClearSlotsRequested;
                _manufacturePanel.ManufactureRequested += HandleManufactureRequested;
                _manufacturePanel.PoolRemakeRequested += HandlePoolRemakeRequested;
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

            if (autoOpenFormation)
            {
                HandleOpenFormation();
            }
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
                _upgradePanel.OpenManufactureRecordRequested -= HandleOpenManufactureRecord;
                _upgradePanel.CloseManufactureRecordRequested -= HandleCloseManufactureRecord;
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
                _manufacturePanel.PoolRemakeRequested -= HandlePoolRemakeRequested;
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
            _batchRecord = null;
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

            var mode = _formation != null ? _formation.BoundCampaignMode : CampaignMode.Mode1;
            var rootPrefab = _formationCatalog.ResolveEditorRoot(mode);
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
            instance.name = mode == CampaignMode.Mode2
                ? "FormationEditorRoot_Mode2(Clone)"
                : "FormationEditorRoot(Clone)";
            _editor = instance.GetComponent<FormationEditorController>();
            if (_editor == null)
            {
                _editor = instance.AddComponent<FormationEditorController>();
            }

            _editor.ReturnRequested += HandleFormationReturn;
            _editor.CompleteRequested += HandleFormationComplete;
            _editor.Begin(
                FormationEditorMode.UpgradeManufacture,
                _defendCatalog,
                _warriorPool,
                _formation,
                _progress,
                _configs,
                mapPrefab,
                null,
                _formationCatalog);

            Debug.Log($"[UM Formation] Opened editor map={mapId}");
        }

        private void HandleFormationReturn()
        {
            CloseFormationEditor();
            SetMainUiVisible(true);
            RefreshManufacture();
        }

        private void HandleFormationComplete()
        {
            CloseFormationEditor();
            HandleComplete();
        }

        private void CloseFormationEditor()
        {
            if (_editor == null)
            {
                return;
            }

            _editor.ReturnRequested -= HandleFormationReturn;
            _editor.CompleteRequested -= HandleFormationComplete;
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

        private void HandleOpenManufactureRecord()
        {
            var lines = BuildManufactureRecordLines();
            _upgradePanel?.ManufactureRecordModal?.Bind(lines);
            _upgradePanel?.ShowManufactureRecordModal();
        }

        private void HandleCloseManufactureRecord()
        {
            _upgradePanel?.HideManufactureRecordModal();
        }

        private List<string> BuildManufactureRecordLines()
        {
            var lines = new List<string>();
            var ids = _batchRecord != null ? _batchRecord.WarriorIds : null;
            if (ids == null || ids.Count == 0 || _warriorPool == null)
            {
                return lines;
            }

            for (var i = 0; i < ids.Count; i++)
            {
                var id = ids[i];
                if (string.IsNullOrEmpty(id) || !_warriorPool.TryGet(id, out var warrior) || warrior == null)
                {
                    continue;
                }

                var raceName = ResolveRaceDisplayName(warrior.RaceId);
                var className = ResolveClassName(warrior.ClassId);
                var warriorName = string.IsNullOrEmpty(warrior.WarriorName) ? id : warrior.WarriorName;
                lines.Add(warriorName + "｜" + raceName + "｜" + className);
            }

            return lines;
        }

        private string ResolveRaceDisplayName(string raceId)
        {
            if (_configs != null && _configs.TryGetRace(raceId, out var raceRow) && raceRow != null)
            {
                return string.IsNullOrEmpty(raceRow.DisplayNameKey) ? raceRow.RaceId : raceRow.DisplayNameKey;
            }

            return raceId ?? "-";
        }

        private string ResolveClassName(string classId)
        {
            if (_configs != null && _configs.TryGetClass(classId, out var classRow) && classRow != null
                && !string.IsNullOrEmpty(classRow.ClassName))
            {
                return classRow.ClassName;
            }

            return classId ?? "-";
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

        private void HandlePoolRemakeRequested(string warriorId)
        {
            if (_manufacture == null || string.IsNullOrEmpty(warriorId))
            {
                return;
            }

            if (!_manufacture.TryRemanufacture(warriorId, out var instance, out var error))
            {
                if (string.Equals(error, ManufactureService.ErrorMaterialInsufficient, StringComparison.Ordinal)
                    || string.Equals(error, ManufactureService.ErrorSpiritInsufficient, StringComparison.Ordinal))
                {
                    ShowTips(error);
                }
                else
                {
                    Debug.Log($"[UM Remanufacture] 再造失败：{error}");
                }

                RefreshManufacture();
                return;
            }

            if (_catalog != null && !_catalog.TryGetWarriorAppearance(instance.AppearanceId, out _))
            {
                Debug.LogWarning(
                    $"[UM Remanufacture] 外观 Prefab 未绑定：Assets/Prefabs/Defend/Warriors/{instance.AppearanceId}.prefab");
            }
        }

        private void ShowTips(string message)
        {
            EnsureTipsView();
            if (_tipsView != null)
            {
                _tipsView.Show(message);
            }
            else
            {
                Debug.Log($"[UM Tips] {message}");
            }
        }

        private void EnsureTipsView()
        {
            if (_tipsView != null)
            {
                return;
            }

            var canvas = GetComponentInChildren<Canvas>(true);
            if (canvas == null)
            {
                return;
            }

            var existing = canvas.transform.Find("UmTips");
            if (existing != null)
            {
                _tipsView = existing.GetComponent<ToastView>();
                if (_tipsView != null)
                {
                    return;
                }
            }

            var root = new GameObject("UmTips", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image),
                typeof(CanvasGroup), typeof(ToastView));
            root.transform.SetParent(canvas.transform, false);
            var rt = root.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.82f);
            rt.anchorMax = new Vector2(0.5f, 0.82f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(480f, 52f);
            rt.anchoredPosition = Vector2.zero;
            root.GetComponent<Image>().color = new Color(0.05f, 0.05f, 0.08f, 0.9f);

            var textGo = new GameObject("Message", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            textGo.transform.SetParent(root.transform, false);
            var textRt = textGo.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = Vector2.zero;
            textRt.offsetMax = Vector2.zero;
            var text = textGo.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.fontSize = 22;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;

            var cg = root.GetComponent<CanvasGroup>();
            cg.alpha = 0f;
            cg.interactable = false;
            cg.blocksRaycasts = false;

            _tipsView = root.GetComponent<ToastView>();
            // ToastView uses SerializeField — set via reflection-free runtime API if available.
            _tipsView.RuntimeConfigure(root, text, 1f);
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
            if (_manufacturePanel == null || _manufacture == null || !_manufacturePanel.gameObject.activeSelf)
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
            _manufacturePanel.RebuildPool(BuildPoolEntries());
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

        private IReadOnlyList<PoolSoldierEntry> BuildPoolEntries()
        {
            _poolEntries.Clear();
            if (_warriorPool == null)
            {
                return _poolEntries;
            }

            var warriors = _warriorPool.Warriors;
            for (var i = 0; i < warriors.Count; i++)
            {
                var w = warriors[i];
                if (w == null)
                {
                    continue;
                }

                var deployed = _formation != null && _formation.IsDeployed(w.Id) ? "〔上阵〕" : string.Empty;
                var canRemake = w.SourceItemIds != null && w.SourceItemIds.Count > 0;
                _poolEntries.Add(new PoolSoldierEntry
                {
                    WarriorId = w.Id,
                    Summary = $"{w.Id} {w.WarriorName}{deployed}\nHP {w.RemainingHP:0}",
                    CanRemake = canRemake
                });
            }

            return _poolEntries;
        }
    }
}
