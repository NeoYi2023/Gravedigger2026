using Gravedigger2026.Core;
using Gravedigger2026.Core.Config;
using Gravedigger2026.Core.Dig;
using Gravedigger2026.Core.Level;
using Gravedigger2026.Core.PushMap;
using Gravedigger2026.Core.Tech;
using Gravedigger2026.Core.UpgradeManufacture;
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
        [SerializeField] private ToastView _toastView;
        [SerializeField] private TechTreeCanvasView _techTreeCanvasView;
        [SerializeField] private DigPrefabCatalog _digPrefabCatalog;
        [SerializeField] private Transform _digWorldParent;
        [SerializeField] private UpgradeManufacturePrefabCatalog _umPrefabCatalog;
        [SerializeField] private Transform _umWorldParent;
        [SerializeField] private FormationPrefabCatalog _formationPrefabCatalog;
        [SerializeField] private DefendPrefabCatalog _defendPrefabCatalog;
        [SerializeField] private Transform _defendWorldParent;

        private readonly SaveSlotService _saveSlots = new SaveSlotService();
        private readonly GameplayStateService _gameplayState = new GameplayStateService();
        private readonly ConfigCsvRepository _configs = new ConfigCsvRepository();
        private readonly WarehouseService _warehouse = new WarehouseService();
        private readonly DungeonUnlockService _dungeonUnlocks = new DungeonUnlockService();
        private readonly ProtagonistProgressService _progress = new ProtagonistProgressService();
        private readonly TechTreeService _techTree = new TechTreeService();
        private readonly WarriorPoolService _warriorPool = new WarriorPoolService();
        private BattleFormationService _formation;
        private ManufactureService _manufacture;
        private LevelOperationDriver _levelDriver;

        public SaveSlotService SaveSlots => _saveSlots;
        public GameplayStateService GameplayState => _gameplayState;
        public LevelOperationDriver LevelDriver => _levelDriver;
        public ProtagonistProgressService Progress => _progress;
        public TechTreeService TechTree => _techTree;
        public WarriorPoolService WarriorPool => _warriorPool;
        public BattleFormationService Formation => _formation;

        private void Awake()
        {
            _saveSlots.Load();

            _techTree.Bind(_configs, _progress);
            _formation = new BattleFormationService(_warriorPool);
            _manufacture = new ManufactureService(_configs, _warehouse, _warriorPool);
            _levelDriver = new LevelOperationDriver(_configs, _gameplayState);
            _levelDriver.RegisterDefaultPlaceholders();
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
                        SetStagePresentationActive));
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
                        SetStagePresentationActive));
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
            SetStagePresentationActive(false);

            if (_inSaveShellView != null)
            {
                _inSaveShellView.Hide();
            }

            if (_saveSelectView != null)
            {
                _saveSelectView.Show();
            }
        }

        private void EnterShell(int slotIndex)
        {
            _levelDriver?.StopCurrentLevel();
            _warehouse.Clear();
            _manufacture?.ClearAllSlots();
            _warriorPool.BindSlot(slotIndex);
            _formation?.BindSlot(slotIndex);
            _dungeonUnlocks.BindSlot(slotIndex);
            if (!_configs.IsLoaded)
            {
                _configs.TryLoadAll();
            }

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
            _saveSlots.Create(slotIndex);
            if (_saveSelectView != null)
            {
                _saveSelectView.RefreshAll();
            }

            EnterShell(slotIndex);
        }

        private void HandleEnter(int slotIndex)
        {
            if (!_saveSlots.IsOccupied(slotIndex))
            {
                return;
            }

            EnterShell(slotIndex);
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
            if (_levelDriver == null)
            {
                return;
            }

            if (!_configs.IsLoaded)
            {
                _configs.TryLoadAll();
            }

            _progress.EnsureLoaded(_configs);
            _progress.ResetToLevelOne(_configs);

            if (!_levelDriver.TryEnterLevel(LevelOperationDriver.DemoSampleLevelId, out var error))
            {
                if (_toastView != null)
                {
                    _toastView.Show($"关卡启动失败：{error}");
                }

                return;
            }

            if (_inSaveShellView != null)
            {
                _inSaveShellView.HideToolsPanel();
            }

            if (_toastView != null)
            {
                _toastView.Show($"已启动样例关卡 {LevelOperationDriver.DemoSampleLevelId}");
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
