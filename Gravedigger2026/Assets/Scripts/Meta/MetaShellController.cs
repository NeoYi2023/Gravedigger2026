using System.Collections.Generic;
using Gravedigger2026.Core;
using Gravedigger2026.Core.Audio;
using Gravedigger2026.Core.AutoManufacture;
using Gravedigger2026.Core.Config;
using Gravedigger2026.Core.Dig;
using Gravedigger2026.Core.Level;
using Gravedigger2026.Core.ProtagonistEquipment;
using Gravedigger2026.Core.PushMap;
using Gravedigger2026.Core.Settings;
using Gravedigger2026.Core.Shop;
using Gravedigger2026.Core.Tech;
using Gravedigger2026.Core.UpgradeManufacture;
using Gravedigger2026.Gameplay.Audio;
using Gravedigger2026.Gameplay.AutoManufacture;
using Gravedigger2026.Gameplay.Defend;
using Gravedigger2026.Gameplay.Dig;
using Gravedigger2026.Gameplay.Formation;
using Gravedigger2026.Gameplay.Shop;
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
        [SerializeField] private TitleMenuView _titleMenuView;
        [SerializeField] private TitleSettingsPanelView _titleSettingsPanelView;
        [SerializeField] private GameObject _titleScreenBackground;
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
        [SerializeField] private ShopPrefabCatalog _shopPrefabCatalog;
        [SerializeField] private BgmClipCatalog _bgmClipCatalog;
        [SerializeField] private AudioSource _bgmAudioSource;

        private readonly SaveSlotService _saveSlots = new SaveSlotService();
        private readonly GameplayStateService _gameplayState = new GameplayStateService();
        private readonly CampaignModeService _campaignMode = new CampaignModeService();
        private readonly ConfigCsvRepository _configs = new ConfigCsvRepository();
        private readonly BgmService _bgm = new BgmService();
        private readonly DisplaySettingsService _displaySettings = new DisplaySettingsService();
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
        private ShopSellService _shopSell;
        private ShopStageRootView _shopStageRootView;
        private ShopStageModule _shopModule;
        private readonly AutoManufactureBatchRecordService _autoManufactureBatchRecord =
            new AutoManufactureBatchRecordService();
        private BattleFormationService _formation;
        private ManufactureService _manufacture;
        private AutoManufactureService _autoManufacture;
        private AutoFormationDeployService _autoDeploy;
        private GmSoldierGrantService _gmSoldierGrant;
        private UpgradeManufactureStageModule _umModule;
        private LevelOperationDriver _levelDriver;
        private CameraFogService _cameraFog;
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
            EnsureCameraFogService();
            EnsureBgmAudioSource();
            _displaySettings.ApplySavedOrCurrent();
            _bgm.Bind(_configs, _bgmClipCatalog, _bgmAudioSource);
            _saveSlots.Load();

            _techTree.Bind(_configs, _progress);
            _formation = new BattleFormationService(_warriorPool);
            _manufacture = new ManufactureService(_configs, _warehouse, _warriorPool);
            _specialEquipSlots = new SpecialEquipSlotsService(_configs);
            _protagonistEquipment = new ProtagonistEquipmentService(_configs);
            _shopPurchase = new ShopPurchaseService(_warehouse, _protagonistEquipment, _specialEquipSlots);
            _shopSell = new ShopSellService(_warehouse, _protagonistEquipment, _specialEquipSlots, _configs);
            _techTree.BindEquipment(_protagonistEquipment);
            var magicBookHook = new SoldierManufactureMagicBookHook(_specialEquipSlots, _configs);
            _autoManufacture = new AutoManufactureService(
                _configs, _warehouse, _tempWarriorWarehouse, _warriorPool, magicBookHook);
            var autoDeploy = new AutoFormationDeployService(_configs, _warriorPool, _formation);
            _autoDeploy = autoDeploy;
            _gmSoldierGrant = new GmSoldierGrantService(_configs, _warriorPool, _autoDeploy);
            _levelDriver = new LevelOperationDriver(_configs, _gameplayState);
            _levelDriver.RegisterDefaultPlaceholders();
            _shopModule = new ShopStageModule(
                _shopPrefabCatalog,
                transform,
                _shopProgress,
                _protagonistEquipment,
                _specialEquipSlots,
                _warehouse,
                _configs,
                _shopOfferRefresh,
                _shopPurchase,
                _shopSell,
                _confirmDialog,
                _toastView,
                HandleShopStageComplete,
                SetStagePresentationActive);
            _levelDriver.RegisterModule(_shopModule);
            if (_shopPrefabCatalog == null)
            {
                Debug.LogWarning("[MetaShell] ShopPrefabCatalog missing — Shop stage uses runtime full-screen fallback.");
            }

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
                        _protagonistEquipment,
                        _gmSoldierGrant,
                        _defendPrefabCatalog,
                        _bgm));
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
                        _specialEquipSlots,
                        HandleDefendVictory,
                        HandleDefendLevelFailure,
                        HandlePushMapModeConfirmed,
                        SetStagePresentationActive,
                        _bgm));
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
                        SetStagePresentationActive,
                        _bgm));
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
                _saveSelectView.BackRequested += HandleSaveSelectBack;
            }

            if (_titleMenuView != null)
            {
                _titleMenuView.PrimaryClicked += HandleTitlePrimary;
                _titleMenuView.LoadSaveClicked += HandleTitlePlaceholder;
                _titleMenuView.SettingsClicked += HandleTitleSettings;
                _titleMenuView.CreditsClicked += HandleTitlePlaceholder;
            }

            EnsureTitleSettingsPanelBound();
            if (_titleSettingsPanelView != null)
            {
                _titleSettingsPanelView.DisplayTab?.Bind(_displaySettings);
                _titleSettingsPanelView.Closed += HandleTitleSettingsClosed;
                _titleSettingsPanelView.Applied += HandleTitleSettingsApplied;
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
                _inSaveShellView.LockedDifficultyClicked += HandleLockedDifficultyClicked;
                _inSaveShellView.GmGrantItemPicked += HandleGmGrantItemPicked;
                _inSaveShellView.GmGrantLevelPicked += HandleGmGrantLevelPicked;
                _inSaveShellView.GmAddSoldierAddClicked += HandleGmAddSoldierAdd;
                _inSaveShellView.ToolsClosed += HandleMetaOverlayClosed;
                _inSaveShellView.LevelSelectClosed += HandleMetaOverlayClosed;
                _inSaveShellView.GmGrantListClosed += HandleMetaOverlayClosed;
                _inSaveShellView.GmAddSoldierClosed += HandleMetaOverlayClosed;
                _inSaveShellView.EquipmentWarehouseClosed += HandleMetaOverlayClosed;
                _inSaveShellView.MagicBookSlotsClosed += HandleMetaOverlayClosed;
            }

            if (_techTreeCanvasView != null)
            {
                _techTreeCanvasView.CloseRequested += HandleTechTreeClose;
            }

            _gameplayState.StateChanged += HandleGameplayStateChanged;
            RefreshCameraFogMetaBlocking();
        }

        private void Start()
        {
            if (_titleMenuView == null)
            {
                Debug.LogWarning(
                    "[MetaShell] TitleMenuView missing — run menu Gravedigger2026/Meta/Ensure TitleMenu (UI-027). Falling back to SaveSelect.");
                ShowSaveSelect();
                return;
            }

            ShowTitleMenu();
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

        private void ShowTitleMenu()
        {
            ResetMetaShellForTitleScreen();
            SetTitleScreenBackgroundActive(true);

            if (_titleSettingsPanelView != null)
            {
                _titleSettingsPanelView.Hide();
            }

            if (_titleMenuView != null)
            {
                _titleMenuView.Show(_saveSlots.HasAnyOccupied());
            }

            if (_saveSelectView != null)
            {
                _saveSelectView.Hide();
            }

            PlayTitleBgm();
        }

        private void ShowSaveSelect()
        {
            ResetMetaShellForTitleScreen();
            SetTitleScreenBackgroundActive(true);

            if (_titleSettingsPanelView != null)
            {
                _titleSettingsPanelView.Hide();
            }

            if (_titleMenuView != null)
            {
                _titleMenuView.Hide();
            }

            if (_saveSelectView != null)
            {
                _saveSelectView.Show();
            }

            PlayTitleBgm();
        }

        private void ResetMetaShellForTitleScreen()
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
                DestroyShopOverlay();
            }
        }

        private void SetTitleScreenBackgroundActive(bool active)
        {
            if (_titleScreenBackground != null)
            {
                _titleScreenBackground.SetActive(active);
            }
        }

        private void HandleTitlePrimary()
        {
            ShowSaveSelect();
        }

        private void HandleSaveSelectBack()
        {
            ShowTitleMenu();
        }

        private void HandleTitlePlaceholder()
        {
            if (_toastView != null)
            {
                _toastView.Show("还未制作");
            }
        }

        private void HandleTitleSettings()
        {
            var wasMissing = _titleSettingsPanelView == null;
            EnsureTitleSettingsPanelBound();
            if (_titleSettingsPanelView == null)
            {
                if (_toastView != null)
                {
                    _toastView.Show("设置 Prefab 未绑定");
                }

                return;
            }

            if (wasMissing)
            {
                _titleSettingsPanelView.Closed += HandleTitleSettingsClosed;
                _titleSettingsPanelView.Applied += HandleTitleSettingsApplied;
            }

            _titleSettingsPanelView.DisplayTab?.Bind(_displaySettings);
            _titleSettingsPanelView.Show();
            RefreshCameraFogMetaBlocking();
        }

        private void EnsureTitleSettingsPanelBound()
        {
            if (_titleSettingsPanelView != null)
            {
                return;
            }

            Transform canvas = null;
            if (_titleMenuView != null)
            {
                canvas = _titleMenuView.transform.parent;
            }

            if (canvas == null)
            {
                canvas = transform.Find("MetaCanvas");
            }

            if (canvas == null)
            {
                return;
            }

            _titleSettingsPanelView = TitleSettingsPanelFactory.Create(canvas);
            if (_titleSettingsPanelView != null)
            {
                Debug.LogWarning(
                    "[MetaShell] TitleSettingsPanel was missing — created runtime fallback. " +
                    "Run menu Gravedigger2026/Meta/Ensure TitleSettingsPanel (UI-028) to bake Prefab.");
            }
        }

        private void HandleTitleSettingsClosed()
        {
            RefreshCameraFogMetaBlocking();
        }

        private void HandleTitleSettingsApplied()
        {
            if (_toastView != null)
            {
                _toastView.Show("显示设置已应用");
            }
        }

        private void EnterShell(int slotIndex, CampaignMode mode, bool isNewSave)
        {
            _bgm.Stop();
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

            if (isNewSave)
            {
                _warehouse.ApplyNewSaveGrants(_configs);
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
            SetTitleScreenBackgroundActive(false);

            if (_titleMenuView != null)
            {
                _titleMenuView.Hide();
            }

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

            OpenLevelSelectPanel();
        }

        private void HandleCreate(int slotIndex)
        {
            _saveSlots.Create(slotIndex);
            if (_saveSelectView != null)
            {
                _saveSelectView.RefreshAll();
            }

            EnterShell(slotIndex, CampaignMode.Mode2, isNewSave: true);
        }

        private void HandleEnter(int slotIndex)
        {
            if (!_saveSlots.IsOccupied(slotIndex))
            {
                return;
            }

            EnterShell(slotIndex, CampaignMode.Mode2, isNewSave: false);
        }

        /// <summary>
        /// Demo D-045 bypass: create/enter no longer call this. Kept for deferred Mode1 entry.
        /// </summary>
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

                EnterShell(slotIndex, CampaignMode.Mode1, isCreate);
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

                    EnterShell(slotIndex, mode, isCreate);
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

            if (_titleMenuView != null && _titleMenuView.IsVisible)
            {
                _titleMenuView.Show(_saveSlots.HasAnyOccupied());
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

            RefreshCameraFogMetaBlocking();
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
            RefreshCameraFogMetaBlocking();
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
            RefreshCameraFogMetaBlocking();
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

            if (IsShopStageOpen())
            {
                return;
            }

            if (_shopStageRootView != null)
            {
                return;
            }

            if (_shopPrefabCatalog == null || _shopPrefabCatalog.StageRoot == null)
            {
                Debug.LogWarning("[MetaShell] ShopStageRoot prefab missing — runtime full-screen fallback.");
                var fallback = new GameObject("ShopStageRoot(Overlay)");
                fallback.transform.SetParent(_inSaveShellView.transform, false);
                _shopStageRootView = fallback.AddComponent<ShopStageRootView>();
                _shopStageRootView.BuildFullscreenHierarchy();
            }
            else
            {
                var go = Instantiate(_shopPrefabCatalog.StageRoot, _inSaveShellView.transform);
                go.name = "ShopStageRoot(Overlay)";
                go.SetActive(true);
                _shopStageRootView = go.GetComponent<ShopStageRootView>();
                if (_shopStageRootView == null)
                {
                    _shopStageRootView = go.AddComponent<ShopStageRootView>();
                }
            }

            HideSiblingInSaveOverlays();
            _inSaveShellView.HideEquipmentWarehousePanel();
            _inSaveShellView.HideMagicBookSlotsPanel();

            _shopStageRootView.Bind(
                _shopProgress,
                _protagonistEquipment,
                _specialEquipSlots,
                _warehouse,
                _configs,
                _shopOfferRefresh,
                _shopPurchase,
                _shopSell,
                _confirmDialog,
                _toastView);

            _shopStageRootView.Closed += HandleShopOverlayClosed;
            _shopStageRootView.Open();
            RefreshCameraFogMetaBlocking();
        }

        private void HandleShopOverlayClosed()
        {
            if (_shopStageRootView != null)
            {
                _shopStageRootView.Closed -= HandleShopOverlayClosed;
            }

            _shopStageRootView = null;
            RefreshCameraFogMetaBlocking();
        }

        private void HandleShopStageComplete()
        {
            AdvanceStageFromGameplay();
        }

        private bool IsShopStageOpen()
        {
            return _shopModule != null && _shopModule.IsOpen;
        }

        private void DestroyShopOverlay()
        {
            if (_shopStageRootView == null)
            {
                return;
            }

            _shopStageRootView.Closed -= HandleShopOverlayClosed;
            Destroy(_shopStageRootView.gameObject);
            _shopStageRootView = null;
            RefreshCameraFogMetaBlocking();
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
            if (levelIds.Count == 0 && _toastView != null)
            {
                _toastView.Show("当前模式无可用关卡");
            }

            if (_inSaveShellView != null)
            {
                _inSaveShellView.ShowLevelSelectPanel(levelIds);
                RefreshCameraFogMetaBlocking();
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

                RefreshCameraFogMetaBlocking();
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

            RefreshCameraFogMetaBlocking();
        }

        private void HandleMetaOverlayClosed()
        {
            RefreshCameraFogMetaBlocking();
        }

        private void EnsureCameraFogService()
        {
            _cameraFog = GetComponent<CameraFogService>();
            if (_cameraFog == null)
            {
                _cameraFog = gameObject.AddComponent<CameraFogService>();
            }

            _cameraFog.Configure(_digPrefabCatalog);
        }

        private void RefreshCameraFogMetaBlocking()
        {
            if (_cameraFog == null)
            {
                EnsureCameraFogService();
            }

            var blocking = false;
            if (_inSaveShellView != null && _inSaveShellView.IsAnyMetaOverlayBlockingFog)
            {
                blocking = true;
            }

            if (_shopStageRootView != null)
            {
                blocking = true;
            }

            if (_techTreeCanvasView != null && _techTreeCanvasView.IsOpen)
            {
                blocking = true;
            }

            if (_titleSettingsPanelView != null && _titleSettingsPanelView.IsOpen)
            {
                blocking = true;
            }

            _cameraFog?.SetMetaOverlayBlocking(blocking);
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
        private string _pendingGmGrantEquipId;

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
                RefreshCameraFogMetaBlocking();
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
            _pendingGmGrantEquipId = null;
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
                RefreshCameraFogMetaBlocking();
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
                var levels = CollectEquipLevels(id);
                if (levels.Count == 0)
                {
                    if (_toastView != null)
                    {
                        _toastView.Show($"装备 {id} 无可用等级");
                    }

                    return;
                }

                _pendingGmGrantEquipId = id;
                if (_inSaveShellView != null)
                {
                    _inSaveShellView.ShowGmGrantLevelPicker("选择等级", levels);
                    RefreshCameraFogMetaBlocking();
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

        private void HandleGmGrantLevelPicked(int level)
        {
            if (_gmGrantKind != GmGrantKind.Equip || string.IsNullOrEmpty(_pendingGmGrantEquipId))
            {
                return;
            }

            if (_protagonistEquipment == null)
            {
                return;
            }

            var id = _pendingGmGrantEquipId;
            if (!_protagonistEquipment.DebugGrantAtLevel(id, level, out var error))
            {
                if (_toastView != null)
                {
                    _toastView.Show(string.IsNullOrEmpty(error) ? "发放装备失败" : error);
                }

                return;
            }

            if (_toastView != null)
            {
                _toastView.Show($"已发放装备 {id} Lv.{level}");
            }
        }

        private List<int> CollectEquipLevels(string equipId)
        {
            var levels = new List<int>();
            if (string.IsNullOrEmpty(equipId))
            {
                return levels;
            }

            foreach (var row in _configs.ProtagonistEquipmentRows)
            {
                if (row == null || string.IsNullOrEmpty(row.EquipId))
                {
                    continue;
                }

                if (!string.Equals(row.EquipId.Trim(), equipId, System.StringComparison.Ordinal))
                {
                    continue;
                }

                if (row.EquipLevel < 1)
                {
                    continue;
                }

                if (!levels.Contains(row.EquipLevel))
                {
                    levels.Add(row.EquipLevel);
                }
            }

            levels.Sort();
            return levels;
        }

        private void HandleLockedDifficultyClicked()
        {
            if (_toastView != null)
            {
                _toastView.Show("还未制作");
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

            DestroyShopOverlay();

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
                _inSaveShellView.HideDifficultySelectHost();
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

            // Clear sticky Meta overlay fog-hide (e.g. Tools closed via X without refresh).
            RefreshCameraFogMetaBlocking();
        }

        private void HandleLevelEnded(string message)
        {
            _bgm.Stop();
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

        private void PlayTitleBgm()
        {
            if (!_configs.IsLoaded && !_configs.TryLoadAll(CampaignMode.Mode1))
            {
                Debug.LogWarning($"[MetaShell] Title BGM skipped — config load failed: {_configs.LastError}");
                return;
            }

            _bgm.Play(BgmContext.Title);
        }

        private void EnsureBgmAudioSource()
        {
            if (_bgmAudioSource != null)
            {
                return;
            }

            var go = new GameObject("BgmAudioSource");
            go.transform.SetParent(transform, false);
            _bgmAudioSource = go.AddComponent<AudioSource>();
            _bgmAudioSource.playOnAwake = false;
            _bgmAudioSource.loop = true;
            _bgmAudioSource.spatialBlend = 0f;
        }
    }
}
