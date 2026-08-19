using System.Collections.Generic;
using Gravedigger2026.Core;
using Gravedigger2026.Core.AutoManufacture;
using Gravedigger2026.Core.Config;
using Gravedigger2026.Core.Dig;
using Gravedigger2026.Core.Level;
using Gravedigger2026.Core.ProtagonistEquipment;
using Gravedigger2026.Core.PushMap;
using Gravedigger2026.Core.Shop;
using Gravedigger2026.Core.Tech;
using Gravedigger2026.Core.UpgradeManufacture;
using Gravedigger2026.Gameplay.AutoManufacture;
using Gravedigger2026.Gameplay.Defend;
using Gravedigger2026.Gameplay.Dig;
using Gravedigger2026.Gameplay.Formation;
using Gravedigger2026.Gameplay.UpgradeManufacture;
using Gravedigger2026.UI;
using UnityEngine;

namespace Gravedigger2026.Meta
{
    /// <summary>
    /// Boot-scene Meta shell orchestrator: SaveSelect ↔ InSaveShell + Level driver (SPEC_03 §3.4–§3.13).
    /// </summary>
    public sealed class MetaShellController : MonoBehaviour
    {
        [SerializeField] private SaveSelectView _saveSelectView;
        [SerializeField] private InSaveShellView _inSaveShellView;
        [SerializeField] private ConfirmDialogView _confirmDialog;
        [SerializeField] private CampaignModeSelectView _campaignModeSelect;
        [SerializeField] private ToastView _toastView;
        [SerializeField] private TechTreeCanvasView _techTreeCanvasView;
        [SerializeField] private DigPrefabCatalog _digPrefabCatalog;
        [SerializeField] private Transform _digWorldParent;
        [SerializeField] private UpgradeManufacturePrefabCatalog _umPrefabCatalog;
        [SerializeField] private Transform _umWorldParent;
        [SerializeField] private FormationPrefabCatalog _formationPrefabCatalog;
        [SerializeField] private DefendPrefabCatalog _defendPrefabCatalog;
        [SerializeField] private Transform _defendWorldParent;
        [SerializeField] private AutoManufacturePrefabCatalog _autoMfgPrefabCatalog;
        [SerializeField] private Transform _autoMfgWorldParent;

        private readonly SaveSlotService _saveSlots = new SaveSlotService();
        private readonly GameplayStateService _gameplayState = new GameplayStateService();
        private readonly CampaignModeService _campaignMode = new CampaignModeService();
        private readonly ConfigCsvRepository _configs = new ConfigCsvRepository();
        private readonly WarehouseService _warehouse = new WarehouseService();
        private readonly DungeonUnlockService _dungeonUnlocks = new DungeonUnlockService();
        private readonly ProtagonistProgressService _progress = new ProtagonistProgressService();
        private readonly TechTreeService _techTree = new TechTreeService();
        private readonly WarriorPoolService _warriorPool = new WarriorPoolService();
        private readonly TempWarriorWarehouse _tempWarriorWarehouse = new TempWarriorWarehouse();
        private readonly AutoManufacturePresentationFlags _autoMfgPresentationFlags =
            new AutoManufacturePresentationFlags();
        private SpecialEquipSlotsService _specialEquipSlots;
        private ProtagonistEquipmentService _protagonistEquipment;
        private readonly ShopProgressService _shopProgress = new ShopProgressService();
        private readonly ShopOfferRefreshService _shopOfferRefresh = new ShopOfferRefreshService();
        private ShopPurchaseService _shopPurchase;
        private ShopStageRootView _shopStageRootView;
        private readonly AutoManufactureBatchRecordService _autoManufactureBatchRecord =
            new AutoManufactureBatchRecordService();
        private BattleFormationService _formation;
        private ManufactureService _manufacture;
        private AutoManufactureService _autoManufacture;
        private AutoFormationDeployService _autoDeploy;
        private GmSoldierGrantService _gmSoldierGrant;
        private UpgradeManufactureStageModule _umModule;
        private LevelOperationDriver _levelDriver;
        private readonly List<FormationClassZoneSnapshot> _gmZoneScratch =
            new List<FormationClassZoneSnapshot>();

        public SaveSlotService SaveSlots => _saveSlots;
        public GameplayStateService GameplayState => _gameplayState;
        public LevelOperationDriver LevelDriver => _levelDriver;
        public ProtagonistProgressService Progress => _progress;
        public TechTreeService TechTree => _techTree;
        public WarriorPoolService WarriorPool => _warriorPool;
        public BattleFormationService Formation => _formation;
        public SpecialEquipSlotsService SpecialEquipSlots => _specialEquipSlots;
        public ProtagonistEquipmentService ProtagonistEquipment => _protagonistEquipment;
        public AutoManufactureBatchRecordService AutoManufactureBatchRecord => _autoManufactureBatchRecord;

        private void Awake()
        {
            _saveSlots.Load();

            _techTree.Bind(_configs, _progress);
            _formation = new BattleFormationService(_warriorPool);
            _manufacture = new ManufactureService(_configs, _warehouse, _warriorPool);
            _specialEquipSlots = new SpecialEquipSlotsService(_configs);
            _protagonistEquipment = new ProtagonistEquipmentService(_configs);
            _shopPurchase = new ShopPurchaseService(_warehouse, _protagonistEquipment, _specialEquipSlots);
            _techTree.BindEquipment(_protagonistEquipment);
            var magicBookHook = new SoldierManufactureMagicBookHook(_specialEquipSlots, _configs);
            _autoManufacture = new AutoManufactureService(
                _configs, _warehouse, _tempWarriorWarehouse, _warriorPool, magicBookHook);
            var autoDeploy = new AutoFormationDeployService(_configs, _warriorPool, _formation);
            _autoDeploy = autoDeploy;
            _gmSoldierGrant = new GmSoldierGrantService(_configs, _warriorPool, _autoDeploy);
            _levelDriver = new LevelOperationDriver(_configs, _gameplayState);
            _levelDriver.RegisterDefaultPlaceholders();
            _levelDriver.RegisterModule(
                new AutoManufactureStageModule(
                    _autoManufacture,
                    _formation,
                    autoDeploy,
                    _configs,
                    _defendPrefabCatalog,
                    _warriorPool,
                    _autoMfgWorldParent != null ? _autoMfgWorldParent : transform,
                    _specialEquipSlots,
                    _autoMfgPresentationFlags,
                    HandleAutoManufactureComplete,
                    HandleAutoManufactureNoSoldiers,
                    _autoManufactureBatchRecord,
                    _autoMfgPrefabCatalog));
            if (_digPrefabCatalog != null)
            {
                _levelDriver.RegisterModule(
                    new DigStageModule(
                        _configs,
                        _digPrefabCatalog,
                        _digWorldParent != null ? _digWorldParent : transform,
                        _warehouse,
                        _techTree,
                        HandleDigSummaryConfirmed,
                        SetStagePresentationActive,
                        _specialEquipSlots,
                        _protagonistEquipment));
            }
            else
            {
                Debug.LogWarning("[MetaShell] DigPrefabCatalog missing — Dig stage will have no module.");
            }

            if (_umPrefabCatalog != null)
            {
                _umModule = new UpgradeManufactureStageModule(
                    _configs,
                    _umPrefabCatalog,
                    _formationPrefabCatalog,
                    _defendPrefabCatalog,
                    _umWorldParent != null ? _umWorldParent : transform,
                    _progress,
                    _manufacture,
                    _warriorPool,
                    _formation,
                    HandleUmComplete,
                    SetStagePresentationActive,
                    _autoManufactureBatchRecord,
                    _autoMfgPresentationFlags);
                _levelDriver.RegisterModule(_umModule);
            }
            else
            {
                Debug.LogWarning("[MetaShell] UM PrefabCatalog missing — UpgradeManufacture uses placeholder.");
            }

            if (_defendPrefabCatalog != null)
            {
                _levelDriver.RegisterModule(
                    new DefendStageModule(
                        _configs,
                        _defendPrefabCatalog,
                        _formationPrefabCatalog,
                        _defendWorldParent != null ? _defendWorldParent : transform,
                        _progress,
                        _warriorPool,
                        _formation,
                        _warehouse,
                        HandleDefendVictory,
                        HandleDefendLevelFailure,
                        HandlePushMapModeConfirmed,
                        SetStagePresentationActive));
            }
            else
            {
                Debug.LogWarning("[MetaShell] Defend PrefabCatalog missing — Defend uses placeholder.");
            }

            if (_defendPrefabCatalog != null)
            {
                _levelDriver.RegisterModule(
                    new PushMapStageModule(
                        _configs,
                        _defendPrefabCatalog,
                        _formationPrefabCatalog,
                        _defendWorldParent != null ? _defendWorldParent : transform,
                        _progress,
                        _warriorPool,
                        _formation,
                        _warehouse,
                        _specialEquipSlots,
                        _protagonistEquipment,
                        _dungeonUnlocks,
                        HandlePushMapVictoryContinue,
                        HandlePushMapFailureContinue,
                        SetStagePresentationActive));
            }
            else
            {
                Debug.LogWarning("[MetaShell] Defend PrefabCatalog missing — PushMap has no module.");
            }

            _levelDriver.StageChanged += HandleStageChanged;
            _levelDriver.LevelEnded += HandleLevelEnded;

            if (_saveSelectView != null)
            {
                _saveSelectView.SetOccupiedQuery(_saveSlots.IsOccupied);
                _saveSelectView.CreateRequested += HandleCreate;
                _saveSelectView.EnterRequested += HandleEnter;
                _saveSelectView.DeleteRequested += HandleDeleteRequested;
            }

            if (_inSaveShellView != null)
            {
                _inSaveShellView.ToolsToggleRequested += HandleToolsToggle;
                _inSaveShellView.BackToSaveSelectRequested += HandleBackToSaveSelect;
                _inSaveShellView.EquipmentRequested += HandleEquipmentRequested;
                _inSaveShellView.MagicBookRequested += HandleMagicBookRequested;
                _inSaveShellView.ShopRequested += HandleShopRequested;
                _inSaveShellView.DebugCycleStateRequested += HandleDebugCycleState;
                _inSaveShellView.DebugAdvanceStageRequested += HandleDebugAdvanceStage;
                _inSaveShellView.SettingsRequested += HandleSettings;
                _inSaveShellView.LevelRequested += HandleLevel;
                _inSaveShellView.GrantProtagonistEquipmentRequested += HandleGrantProtagonistEquipment;
                _inSaveShellView.GrantMagicBookRequested += HandleGrantMagicBook;
                _inSaveShellView.GrantAddSoldierRequested += HandleGrantAddSoldier;
                _inSaveShellView.LevelSelectPicked += HandleLevelSelectPicked;
                _inSaveShellView.GmGrantItemPicked += HandleGmGrantItemPicked;
                _inSaveShellView.GmAddSoldierAddClicked += HandleGmAddSoldierAdd;
            }

            if (_techTreeCanvasView != null)
            {
                _techTreeCanvasView.CloseRequested += HandleTechTreeClose;
            }

            _gameplayState.StateChanged += HandleGameplayStateChanged;
        }

        private void Start()
        {
            ShowSaveSelect();
        }

        private void OnDestroy()
        {
            _gameplayState.StateChanged -= HandleGameplayStateChanged;
            if (_techTreeCanvasView != null)
            {
                _techTreeCanvasView.CloseRequested -= HandleTechTreeClose;
            }

            if (_levelDriver != null)
            {
                _levelDriver.StageChanged -= HandleStageChanged;
                _levelDriver.LevelEnded -= HandleLevelEnded;
            }
        }

        private void ShowSaveSelect()
        {
            _levelDriver?.StopCurrentLevel();
            _formation?.ClearBound();
            _warriorPool.ClearBound();
            _dungeonUnlocks.ClearBound();
            _specialEquipSlots?.ClearBound();
            _protagonistEquipment?.ClearBound();
            _shopProgress.ClearBound();
            _autoManufactureBatchRecord.ClearBound();
            _campaignMode.Clear();
            SetStagePresentationActive(false);

            if (_campaignModeSelect != null)
            {
                _campaignModeSelect.Hide();
            }

            if (_inSaveShellView != null)
            {
                _inSaveShellView.Hide();
            }

            if (_shopStageRootView != null)
            {
                Destroy(_shopStageRootView.gameObject);
                _shopStageRootView = null;
            }

            if (_saveSelectView != null)
            {
                _saveSelectView.Show();
            }
        }

        private void EnterShell(int slotIndex, CampaignMode mode)
        {
            _levelDriver?.StopCurrentLevel();
            _warehouse.Clear();
            _manufacture?.ClearAllSlots();
            _campaignMode.Set(mode);
            _warriorPool.BindSlot(slotIndex, mode);
            _formation?.BindSlot(slotIndex, mode);
            _dungeonUnlocks.BindSlot(slotIndex, mode);
            _specialEquipSlots?.BindSlot(slotIndex, mode);
            _protagonistEquipment?.BindSlot(slotIndex, mode);
            _shopProgress.BindSlot(slotIndex, mode);
            _autoManufactureBatchRecord.BindSlot(slotIndex, mode);
            if (!_configs.TryLoadAll(mode))
            {
                Debug.LogError(
                    $"[MetaShell] Config load failed for CampaignMode={mode}: {_configs.LastError}");
            }

            // Legacy JsonUtility dropped non-[Serializable] StatBlock; rebuild from SourceItemIds.
            _manufacture?.RepairMissingStatSnapshots();

            _progress.ResetToLevelOne(_configs);
            _techTree.ResetForNewSave();
            if (_techTreeCanvasView != null)
            {
                _techTreeCanvasView.Bind(_techTree, _progress, _configs);
                _techTreeCanvasView.Hide();
            }

            _gameplayState.ResetToDefaultDig();
            SetStagePresentationActive(false);

            if (_saveSelectView != null)
            {
                _saveSelectView.Hide();
            }

            if (_inSaveShellView != null)
            {
                _inSaveShellView.Show(slotIndex);
                _inSaveShellView.BindEquipmentWarehouse(_protagonistEquipment, _configs);
                _inSaveShellView.BindMagicBookSlots(_specialEquipSlots, _configs, _confirmDialog);
                _inSaveShellView.ShowGameplayState(_gameplayState.Current);
                _inSaveShellView.ShowStageInfo(null);
            }
        }

        private void HandleCreate(int slotIndex)
        {
            PromptCampaignMode(
                slotIndex,
                isCreate: true,
                $"选择玩法模式（新建存档槽 {slotIndex + 1}）");
        }

        private void HandleEnter(int slotIndex)
        {
            if (!_saveSlots.IsOccupied(slotIndex))
            {
                return;
            }

            PromptCampaignMode(
                slotIndex,
                isCreate: false,
                $"选择玩法模式（进入存档槽 {slotIndex + 1}）");
        }

        private void PromptCampaignMode(int slotIndex, bool isCreate, string message)
        {
            if (_campaignModeSelect == null)
            {
                Debug.LogWarning("[MetaShell] CampaignModeSelectView missing — defaulting to Mode1.");
                if (isCreate)
                {
                    _saveSlots.Create(slotIndex);
                    if (_saveSelectView != null)
                    {
                        _saveSelectView.RefreshAll();
                    }
                }

                EnterShell(slotIndex, CampaignMode.Mode1);
                return;
            }

            _campaignModeSelect.Show(
                message,
                mode =>
                {
                    if (isCreate)
                    {
                        _saveSlots.Create(slotIndex);
                        if (_saveSelectView != null)
                        {
                            _saveSelectView.RefreshAll();
                        }
                    }

                    EnterShell(slotIndex, mode);
                });
        }

        private void HandleDeleteRequested(int slotIndex)
        {
            if (!_saveSlots.IsOccupied(slotIndex))
            {
                return;
            }

            if (_confirmDialog == null)
            {
                DeleteSlot(slotIndex);
                return;
            }

            _confirmDialog.Show(
                $"确认删除存档槽 {slotIndex + 1}？此操作不可恢复。",
                () => DeleteSlot(slotIndex));
        }

        private void DeleteSlot(int slotIndex)
        {
            _saveSlots.Delete(slotIndex);
            WarriorPoolService.DeleteSlotData(slotIndex);
            BattleFormationService.DeleteSlotData(slotIndex);
            DungeonUnlockService.DeleteSlotData(slotIndex);
            SpecialEquipSlotsService.DeleteSlotData(slotIndex);
            ProtagonistEquipmentService.DeleteSlotData(slotIndex);
            AutoManufactureBatchRecordService.DeleteSlotData(slotIndex);
            if (_saveSelectView != null)
            {
                _saveSelectView.RefreshAll();
            }

            if (_toastView != null)
            {
                _toastView.Show($"已删除存档槽 {slotIndex + 1}");
            }
        }

        private void HandleToolsToggle()
        {
            if (_inSaveShellView != null)
            {
                _inSaveShellView.ToggleToolsPanel();
            }
        }

        private void HandleBackToSaveSelect()
        {
            ShowSaveSelect();
        }

        private void HandleEquipmentRequested()
        {
            if (_inSaveShellView == null)
            {
                return;
            }

            HideSiblingInSaveOverlays();
            _inSaveShellView.HideMagicBookSlotsPanel();
            _inSaveShellView.BindEquipmentWarehouse(_protagonistEquipment, _configs);
            _inSaveShellView.ShowEquipmentWarehousePanel();
        }

        private void HandleMagicBookRequested()
        {
            if (_inSaveShellView == null)
            {
                return;
            }

            HideSiblingInSaveOverlays();
            _inSaveShellView.HideEquipmentWarehousePanel();
            _inSaveShellView.BindMagicBookSlots(_specialEquipSlots, _configs, _confirmDialog);
            _inSaveShellView.ShowMagicBookSlotsPanel();
        }

        private void HandleShopRequested()
        {
            if (_inSaveShellView == null)
            {
                return;
            }

            if (_campaignMode.Current != CampaignMode.Mode2)
            {
                _toastView?.Show("Mode2 才能使用商店");
                return;
            }

            if (_shopStageRootView != null)
            {
                return;
            }

            HideSiblingInSaveOverlays();
            _inSaveShellView.HideEquipmentWarehousePanel();
            _inSaveShellView.HideMagicBookSlotsPanel();

            var go = new GameObject("ShopStageRoot");
            go.transform.SetParent(_inSaveShellView.transform, false);
            _shopStageRootView = go.AddComponent<ShopStageRootView>();

            _shopStageRootView.Bind(
                _shopProgress,
                _protagonistEquipment,
                _specialEquipSlots,
                _warehouse,
                _configs,
                _shopOfferRefresh,
                _shopPurchase,
                _toastView);

            _shopStageRootView.Closed += () => { _shopStageRootView = null; };
            _shopStageRootView.Open();
        }

        private void HideSiblingInSaveOverlays()
        {
            if (_inSaveShellView == null)
            {
                return;
            }

            _inSaveShellView.HideToolsPanel();
            _inSaveShellView.HideLevelSelectPanel();
            _inSaveShellView.HideGmGrantListPanel();
            _inSaveShellView.HideGmAddSoldierPanel();
        }

        private void HandleDebugCycleState()
        {
            if (_levelDriver != null && _levelDriver.IsRunning)
            {
                if (_toastView != null)
                {
                    _toastView.Show("关卡运行中：请用「推进阶段」，勿用切态");
                }

                return;
            }

            _gameplayState.CycleNextForDemoDebug();
        }

        private void HandleDebugAdvanceStage()
        {
            if (_levelDriver == null)
            {
                return;
            }

            if (!_levelDriver.TryAdvanceStage(out var message))
            {
                if (_toastView != null)
                {
                    _toastView.Show(message);
                }

                return;
            }

            if (_toastView != null)
            {
                _toastView.Show(message);
            }
        }

        private void HandleDigSummaryConfirmed()
        {
            AdvanceStageFromGameplay();
        }

        private void HandleAutoManufactureComplete()
        {
            // Defer: StageModule.Enter must finish before Exit/Advance (re-entrancy).
            StartCoroutine(CoAdvanceAfterAutoManufacture());
        }

        private void HandleAutoManufactureNoSoldiers()
        {
            // SPEC_03 §3.15: batch crafted==0 → Tips「无士兵可制造」~1s; does not block advance.
            if (_toastView != null)
            {
                _toastView.Show("无士兵可制造", 1f);
            }
        }

        private System.Collections.IEnumerator CoAdvanceAfterAutoManufacture()
        {
            yield return null;
            AdvanceStageFromGameplay();
        }

        private void HandleUmComplete()
        {
            AdvanceStageFromGameplay();
        }

        private void HandleDefendVictory()
        {
            AdvanceStageFromGameplay();
        }

        private void HandlePushMapVictoryContinue()
        {
            var levelId = _levelDriver != null ? _levelDriver.ActiveLevelId : null;

            // SS-06：Mode2 新关卡解锁 → OnLevelCleared(pending) → 立刻 TryAutoRefreshOnceIfPending 生成 offers once。
            if (_campaignMode.HasMode && _campaignMode.Current == CampaignMode.Mode2)
            {
                if (!string.IsNullOrEmpty(levelId) && TryExtractTrailingNumber(levelId, out var levelMaxNumber))
                {
                    var updated = _shopProgress.OnLevelCleared(levelMaxNumber);
                    if (updated)
                    {
                        _shopOfferRefresh.TryAutoRefreshOnceIfPending(_shopProgress, _configs);
                    }
                }
            }

            if (_levelDriver != null)
            {
                _levelDriver.CompleteLevelAfterBattleSettlement();
            }

            OpenLevelSelectPanel();
        }

        private static bool TryExtractTrailingNumber(string levelId, out int number)
        {
            number = 0;
            if (string.IsNullOrEmpty(levelId))
            {
                return false;
            }

            var i = levelId.Length - 1;
            while (i >= 0 && char.IsDigit(levelId[i]))
            {
                i--;
            }

            if (i == levelId.Length - 1)
            {
                return false; // no trailing digits
            }

            var digits = levelId.Substring(i + 1);
            return int.TryParse(digits, out number);
        }

        private void HandlePushMapFailureContinue(string reason)
        {
            if (_levelDriver != null)
            {
                _levelDriver.AbortLevelAsFailure(reason);
            }

            OpenLevelSelectPanel();
        }

        private void OpenLevelSelectPanel()
        {
            if (!_configs.IsLoaded)
            {
                _configs.TryLoadAll();
            }

            if (_inSaveShellView != null)
            {
                _inSaveShellView.HideToolsPanel();
                _inSaveShellView.HideGmGrantListPanel();
                _inSaveShellView.HideGmAddSoldierPanel();
                _inSaveShellView.HideEquipmentWarehousePanel();
                _inSaveShellView.HideMagicBookSlotsPanel();
            }

            var levelIds = _configs.GetDistinctLevelIds();
            if (levelIds.Count == 0)
            {
                if (_toastView != null)
                {
                    _toastView.Show("当前模式无可用关卡");
                }

                return;
            }

            if (_inSaveShellView != null)
            {
                _inSaveShellView.ShowLevelSelectPanel(levelIds);
            }
            else if (_toastView != null)
            {
                _toastView.Show("关卡列表 Prefab 未绑定");
            }
        }

        private void HandlePushMapModeConfirmed(string gameplayConfigId)
        {
            if (_levelDriver == null)
            {
                return;
            }

            if (!_levelDriver.TryHandoffModeSelectToPushMap(gameplayConfigId, out var error))
            {
                Debug.LogError($"[MetaShell] PushMap ModeSelect handoff failed: {error}");
                if (_toastView != null)
                {
                    _toastView.Show($"推图战进入失败：{error}");
                }
            }
        }

        private void HandleDefendLevelFailure(string reason)
        {
            if (_levelDriver == null)
            {
                return;
            }

            _levelDriver.AbortLevelAsFailure(reason);
        }

        private void AdvanceStageFromGameplay()
        {
            if (_levelDriver == null)
            {
                return;
            }

            if (!_levelDriver.TryAdvanceStage(out var message))
            {
                if (_toastView != null)
                {
                    _toastView.Show(message);
                }

                return;
            }

            if (_toastView != null)
            {
                _toastView.Show(message);
            }
        }

        private void SetStagePresentationActive(bool active)
        {
            if (_inSaveShellView != null)
            {
                _inSaveShellView.SetModePanelsSuppressed(active);
            }
        }

        private void HandleSettings()
        {
            if (!_configs.IsLoaded)
            {
                _configs.TryLoadAll();
            }

            _progress.EnsureLoaded(_configs);
            _techTree.Bind(_configs, _progress);
            if (_techTree.LearnedTechIds.Count == 0)
            {
                _techTree.ResetForNewSave();
            }

            if (_techTreeCanvasView != null)
            {
                _techTreeCanvasView.Bind(_techTree, _progress, _configs);
                _techTreeCanvasView.Show();
                if (_inSaveShellView != null)
                {
                    _inSaveShellView.HideToolsPanel();
                    _inSaveShellView.HideGmGrantListPanel();
                    _inSaveShellView.HideEquipmentWarehousePanel();
                    _inSaveShellView.HideMagicBookSlotsPanel();
                }

                return;
            }

            if (_toastView != null)
            {
                _toastView.Show("设置（科技树 Prefab 未绑定）");
            }
        }

        private void HandleTechTreeClose()
        {
            if (_techTreeCanvasView != null)
            {
                _techTreeCanvasView.Hide();
            }
        }

        private void HandleLevel()
        {
            OpenLevelSelectPanel();
        }

        private enum GmGrantKind
        {
            Equip,
            MagicBook
        }

        private GmGrantKind _gmGrantKind;

        private void HandleGrantProtagonistEquipment()
        {
            OpenGmGrantList(GmGrantKind.Equip);
        }

        private void HandleGrantMagicBook()
        {
            OpenGmGrantList(GmGrantKind.MagicBook);
        }

        private void HandleGrantAddSoldier()
        {
            if (!_configs.IsLoaded)
            {
                _configs.TryLoadAll();
            }

            if (_inSaveShellView != null)
            {
                _inSaveShellView.HideToolsPanel();
                _inSaveShellView.HideLevelSelectPanel();
                _inSaveShellView.HideGmGrantListPanel();
                _inSaveShellView.HideEquipmentWarehousePanel();
                _inSaveShellView.HideMagicBookSlotsPanel();
            }

            if (_umModule == null || !_umModule.IsFormationEditorOpen)
            {
                if (_toastView != null)
                {
                    _toastView.Show("请先打开布阵界面");
                }

                return;
            }

            var classes = BuildClassDropdownOptions();
            var races = BuildRaceDropdownOptions();
            if (classes.Count == 0 || races.Count == 0)
            {
                if (_toastView != null)
                {
                    _toastView.Show("当前模式无职业或种族配置");
                }

                return;
            }

            if (_inSaveShellView != null)
            {
                _inSaveShellView.ShowGmAddSoldierPanel(classes, races);
            }
        }

        private void HandleGmAddSoldierAdd()
        {
            if (_inSaveShellView == null
                || !_inSaveShellView.TryGetGmAddSoldierSelection(
                    out var classId, out var raceId, out var count, out var autoDeploy))
            {
                return;
            }

            if (_umModule == null || !_umModule.IsFormationEditorOpen)
            {
                if (_toastView != null)
                {
                    _toastView.Show("请先打开布阵界面");
                }

                return;
            }

            _gmZoneScratch.Clear();
            _umModule.TryCollectFormationClassZones(_gmZoneScratch);

            if (!_gmSoldierGrant.TryAdd(
                    classId,
                    raceId,
                    count,
                    autoDeploy,
                    _gmZoneScratch,
                    out var added,
                    out var deployed,
                    out var error))
            {
                if (error == GmSoldierGrantError.SoldierNotFound && _toastView != null)
                {
                    _toastView.Show("找不到此种士兵！");
                }

                return;
            }

            if (_toastView != null)
            {
                _toastView.Show(autoDeploy
                    ? $"已添加 {added}，上阵 {deployed}"
                    : $"已添加 {added}");
            }
        }

        private List<GmDropdownOption> BuildClassDropdownOptions()
        {
            var list = new List<GmDropdownOption>();
            foreach (var row in _configs.Classes)
            {
                if (row == null || string.IsNullOrEmpty(row.ClassId))
                {
                    continue;
                }

                var label = string.IsNullOrEmpty(row.ClassName) ? row.ClassId : row.ClassName;
                list.Add(new GmDropdownOption(row.ClassId, label));
            }

            list.Sort((a, b) => string.CompareOrdinal(a.Label, b.Label));
            return list;
        }

        private List<GmDropdownOption> BuildRaceDropdownOptions()
        {
            var list = new List<GmDropdownOption>();
            foreach (var row in _configs.Races)
            {
                if (row == null || string.IsNullOrEmpty(row.RaceId))
                {
                    continue;
                }

                var label = string.IsNullOrEmpty(row.DisplayNameKey) ? row.RaceId : row.DisplayNameKey;
                list.Add(new GmDropdownOption(row.RaceId, label));
            }

            list.Sort((a, b) => string.CompareOrdinal(a.Label, b.Label));
            return list;
        }

        private void OpenGmGrantList(GmGrantKind kind)
        {
            if (!_configs.IsLoaded)
            {
                _configs.TryLoadAll();
            }

            if (_inSaveShellView != null)
            {
                _inSaveShellView.HideToolsPanel();
                _inSaveShellView.HideLevelSelectPanel();
                _inSaveShellView.HideGmAddSoldierPanel();
                _inSaveShellView.HideEquipmentWarehousePanel();
                _inSaveShellView.HideMagicBookSlotsPanel();
            }

            _gmGrantKind = kind;
            var title = kind == GmGrantKind.Equip ? "增加主角装备" : "增加魔法书";
            var items = kind == GmGrantKind.Equip
                ? BuildProtagonistEquipmentGrantItems()
                : BuildMagicBookGrantItems();

            if (items.Count == 0)
            {
                if (_toastView != null)
                {
                    _toastView.Show(kind == GmGrantKind.Equip
                        ? "当前模式无主角装备"
                        : "当前模式无魔法书");
                }

                return;
            }

            if (_inSaveShellView != null && _inSaveShellView.HasGmGrantListPanel)
            {
                _inSaveShellView.ShowGmGrantListPanel(title, items);
            }
            else if (_toastView != null)
            {
                _toastView.Show("发放列表 Prefab 未绑定");
            }
        }

        private List<GmGrantListItem> BuildProtagonistEquipmentGrantItems()
        {
            var byId = new Dictionary<string, GmGrantListItem>(System.StringComparer.Ordinal);
            var order = new List<string>();
            foreach (var row in _configs.ProtagonistEquipmentRows)
            {
                if (row == null || string.IsNullOrEmpty(row.EquipId))
                {
                    continue;
                }

                var id = row.EquipId.Trim();
                if (id.Length == 0)
                {
                    continue;
                }

                var label = string.IsNullOrEmpty(row.DisplayName) ? id : row.DisplayName;
                if (!byId.ContainsKey(id))
                {
                    byId[id] = new GmGrantListItem(id, label);
                    order.Add(id);
                    continue;
                }

                if (row.EquipLevel == 1)
                {
                    byId[id] = new GmGrantListItem(id, label);
                }
            }

            var items = new List<GmGrantListItem>(order.Count);
            for (var i = 0; i < order.Count; i++)
            {
                items.Add(byId[order[i]]);
            }

            return items;
        }

        private List<GmGrantListItem> BuildMagicBookGrantItems()
        {
            var items = new List<GmGrantListItem>();
            foreach (var row in _configs.MagicBooks)
            {
                if (row == null || string.IsNullOrEmpty(row.MagicBookId))
                {
                    continue;
                }

                var id = row.MagicBookId.Trim();
                if (id.Length == 0)
                {
                    continue;
                }

                var label = string.IsNullOrEmpty(row.DisplayName) ? id : row.DisplayName;
                items.Add(new GmGrantListItem(id, label));
            }

            return items;
        }

        private void HandleGmGrantItemPicked(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return;
            }

            if (_gmGrantKind == GmGrantKind.Equip)
            {
                if (_protagonistEquipment == null)
                {
                    return;
                }

                if (!_protagonistEquipment.TryAcquire(id, out var error))
                {
                    if (_toastView != null)
                    {
                        _toastView.Show(string.IsNullOrEmpty(error) ? "获得装备失败" : error);
                    }

                    return;
                }

                if (_toastView != null)
                {
                    _toastView.Show($"已获得装备 {id}");
                }

                return;
            }

            if (_specialEquipSlots == null)
            {
                return;
            }

            if (!_specialEquipSlots.TryEquip(id, out var equipError))
            {
                if (_toastView != null)
                {
                    _toastView.Show(string.IsNullOrEmpty(equipError) ? "装备魔法书失败" : equipError);
                }

                return;
            }

            if (_toastView != null)
            {
                _toastView.Show($"已装备魔法书 {id}");
            }
        }

        private void HandleLevelSelectPicked(string levelId)
        {
            if (_levelDriver == null)
            {
                return;
            }

            if (string.IsNullOrEmpty(levelId))
            {
                return;
            }

            if (!_configs.IsLoaded)
            {
                _configs.TryLoadAll();
            }

            _progress.EnsureLoaded(_configs);
            _progress.ResetToLevelOne(_configs);

            if (!_levelDriver.TryEnterLevel(levelId, out var error))
            {
                if (_toastView != null)
                {
                    _toastView.Show($"关卡启动失败：{error}");
                }

                return;
            }

            if (_inSaveShellView != null)
            {
                _inSaveShellView.HideLevelSelectPanel();
                _inSaveShellView.HideGmGrantListPanel();
                _inSaveShellView.HideToolsPanel();
                _inSaveShellView.HideEquipmentWarehousePanel();
                _inSaveShellView.HideMagicBookSlotsPanel();
            }

            if (_toastView != null)
            {
                _toastView.Show($"已启动关卡 {levelId}");
            }
        }

        private void HandleGameplayStateChanged(GameplayState state)
        {
            if (_inSaveShellView != null)
            {
                _inSaveShellView.ShowGameplayState(state);
            }
        }

        private void HandleStageChanged(LevelStageContext context)
        {
            if (_inSaveShellView != null)
            {
                _inSaveShellView.ShowStageInfo(context);
                if (context != null)
                {
                    _inSaveShellView.ShowGameplayState(context.GameplayType);
                }
            }
        }

        private void HandleLevelEnded(string message)
        {
            SetStagePresentationActive(false);

            if (_toastView != null && !string.IsNullOrEmpty(message))
            {
                _toastView.Show(message);
            }

            if (_inSaveShellView != null)
            {
                _inSaveShellView.ShowStageInfo(null);
            }
        }
    }
}
