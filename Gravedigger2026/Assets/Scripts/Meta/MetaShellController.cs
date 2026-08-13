using Gravedigger2026.Core;
using Gravedigger2026.Core.AutoManufacture;
using Gravedigger2026.Core.Config;
using Gravedigger2026.Core.Dig;
using Gravedigger2026.Core.Level;
using Gravedigger2026.Core.PushMap;
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
        private readonly AutoManufactureBatchRecordService _autoManufactureBatchRecord =
            new AutoManufactureBatchRecordService();
        private BattleFormationService _formation;
        private ManufactureService _manufacture;
        private AutoManufactureService _autoManufacture;
        private LevelOperationDriver _levelDriver;

        public SaveSlotService SaveSlots => _saveSlots;
        public GameplayStateService GameplayState => _gameplayState;
        public LevelOperationDriver LevelDriver => _levelDriver;
        public ProtagonistProgressService Progress => _progress;
        public TechTreeService TechTree => _techTree;
        public WarriorPoolService WarriorPool => _warriorPool;
        public BattleFormationService Formation => _formation;
        public SpecialEquipSlotsService SpecialEquipSlots => _specialEquipSlots;
        public AutoManufactureBatchRecordService AutoManufactureBatchRecord => _autoManufactureBatchRecord;

        private void Awake()
        {
            _saveSlots.Load();

            _techTree.Bind(_configs, _progress);
            _formation = new BattleFormationService(_warriorPool);
            _manufacture = new ManufactureService(_configs, _warehouse, _warriorPool);
            _specialEquipSlots = new SpecialEquipSlotsService(_configs);
            var magicBookHook = new SoldierManufactureMagicBookHook(_specialEquipSlots, _configs);
            _autoManufacture = new AutoManufactureService(
                _configs, _warehouse, _tempWarriorWarehouse, _warriorPool, magicBookHook);
            var autoDeploy = new AutoFormationDeployService(_configs, _warriorPool, _formation);
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
                        _specialEquipSlots));
            }
            else
            {
                Debug.LogWarning("[MetaShell] DigPrefabCatalog missing — Dig stage will have no module.");
            }

            if (_umPrefabCatalog != null)
            {
                _levelDriver.RegisterModule(
                    new UpgradeManufactureStageModule(
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
                        _autoMfgPresentationFlags));
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
                        _dungeonUnlocks,
                        HandleDefendVictory,
                        HandleDefendLevelFailure,
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
                _inSaveShellView.DebugCycleStateRequested += HandleDebugCycleState;
                _inSaveShellView.DebugAdvanceStageRequested += HandleDebugAdvanceStage;
                _inSaveShellView.SettingsRequested += HandleSettings;
                _inSaveShellView.LevelRequested += HandleLevel;
                _inSaveShellView.LevelSelectPicked += HandleLevelSelectPicked;
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
            if (!_configs.IsLoaded)
            {
                _configs.TryLoadAll();
            }

            if (_inSaveShellView != null)
            {
                _inSaveShellView.HideToolsPanel();
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
                _inSaveShellView.HideToolsPanel();
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
