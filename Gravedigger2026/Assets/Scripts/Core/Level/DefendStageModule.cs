using System;
using Gravedigger2026.Core;
using Gravedigger2026.Core.Config;
using Gravedigger2026.Core.Defend;
using Gravedigger2026.Core.Dig;
using Gravedigger2026.Core.UpgradeManufacture;
using Gravedigger2026.Gameplay.Defend;
using Gravedigger2026.Gameplay.Formation;
using UnityEngine;

namespace Gravedigger2026.Core.Level
{
    /// <summary>
    /// Defend IStageModule (D-040–D-044): ModeSelect gate then StageRoot + map by selected BattleMapId.
    /// </summary>
    public sealed class DefendStageModule : IStageModule
    {
        private readonly ConfigCsvRepository _configs;
        private readonly DefendPrefabCatalog _catalog;
        private readonly FormationPrefabCatalog _formationCatalog;
        private readonly Transform _parent;
        private readonly ProtagonistProgressService _progress;
        private readonly WarriorPoolService _warriorPool;
        private readonly BattleFormationService _formation;
        private readonly WarehouseService _warehouse;
        private readonly Action _onVictoryAdvance;
        private readonly Action<string> _onLevelFailure;
        private readonly Action<bool> _onDefendPresentationActive;

        private LevelStageContext _context;
        private GameObject _modeSelectInstance;
        private BattleModeSelectView _modeSelectView;
        private GameObject _stageRootInstance;
        private DefendStageController _controller;

        public DefendStageModule(
            ConfigCsvRepository configs,
            DefendPrefabCatalog catalog,
            FormationPrefabCatalog formationCatalog,
            Transform parent,
            ProtagonistProgressService progress,
            WarriorPoolService warriorPool,
            BattleFormationService formation,
            WarehouseService warehouse,
            Action onVictoryAdvance,
            Action<string> onLevelFailure,
            Action<bool> onDefendPresentationActive = null)
        {
            _configs = configs ?? throw new ArgumentNullException(nameof(configs));
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            _formationCatalog = formationCatalog;
            _parent = parent;
            _progress = progress ?? throw new ArgumentNullException(nameof(progress));
            _warriorPool = warriorPool ?? throw new ArgumentNullException(nameof(warriorPool));
            _formation = formation ?? throw new ArgumentNullException(nameof(formation));
            _warehouse = warehouse;
            _onVictoryAdvance = onVictoryAdvance;
            _onLevelFailure = onLevelFailure;
            _onDefendPresentationActive = onDefendPresentationActive;
        }

        public GameplayState HandledState => GameplayState.Defend;

        public void Enter(LevelStageContext context)
        {
            Exit(context);
            _context = context;

            if (!_configs.IsLoaded)
            {
                _configs.TryLoadAll();
            }

            _progress.EnsureLoaded(_configs);
            _onDefendPresentationActive?.Invoke(true);
            ShowModeSelect();
            Debug.Log(
                $"[Stage:Defend] ModeSelect Level={context?.LevelId} Stage={context?.StageNumber} Recommended={context?.GameplayConfigId}");
        }

        public void Exit(LevelStageContext context)
        {
            TearDownModeSelect();
            TearDownStageRoot();
            _onDefendPresentationActive?.Invoke(false);
            _context = null;

            if (context != null)
            {
                Debug.Log($"[Stage:Defend] Exit Level={context.LevelId} Stage={context.StageNumber}");
            }
        }

        private void ShowModeSelect()
        {
            TearDownModeSelect();

            var prefab = _catalog != null ? _catalog.BattleModeSelectRootPrefab : null;
            if (prefab != null)
            {
                _modeSelectInstance = _parent != null
                    ? UnityEngine.Object.Instantiate(prefab, _parent)
                    : UnityEngine.Object.Instantiate(prefab);
                _modeSelectInstance.name = "BattleModeSelectRoot(Clone)";
                _modeSelectView = _modeSelectInstance.GetComponent<BattleModeSelectView>();
                if (_modeSelectView == null)
                {
                    _modeSelectView = _modeSelectInstance.AddComponent<BattleModeSelectView>();
                }
            }
            else
            {
                _modeSelectInstance = _parent != null
                    ? new GameObject("BattleModeSelectRoot(Runtime)")
                    : new GameObject("BattleModeSelectRoot(Runtime)");
                if (_parent != null)
                {
                    _modeSelectInstance.transform.SetParent(_parent, false);
                }

                _modeSelectView = _modeSelectInstance.AddComponent<BattleModeSelectView>();
            }

            _modeSelectView.ConfirmRequested += HandleModeSelectConfirm;
            _modeSelectView.Show(
                _configs.GetAllDefendRows(),
                _context != null ? _context.GameplayConfigId : null);
        }

        private void HandleModeSelectConfirm(BattleMode mode, string gameplayConfigId)
        {
            if (mode != BattleMode.Defend)
            {
                Debug.LogWarning("[Stage:Defend] PushMap confirm ignored (stub).");
                return;
            }

            if (!_configs.TryGetDefend(gameplayConfigId, out var defend))
            {
                Debug.LogError($"[Stage:Defend] Selected DefendGameplayConfig '{gameplayConfigId}' not found.");
                return;
            }

            if (_context == null)
            {
                Debug.LogError("[Stage:Defend] ModeSelect confirm without stage context.");
                return;
            }

            _context.DefendConfig = defend;
            _context.GameplayConfigId = defend.GameplayConfigId;
            _context.ResolvedMapId = defend.BattleMapId;
            if (MapPrefabPaths.TryResolveAssetPath(defend.BattleMapId, out var path, out var err))
            {
                _context.ResolvedMapPrefabPath = path;
                _context.MapResolveNote =
                    $"ModeSelect Defend Config={defend.GameplayConfigId} Map={defend.BattleMapId} → {path}";
            }
            else
            {
                _context.MapResolveNote = err ?? "Map resolve failed.";
            }

            TearDownModeSelect();
            BeginPrepareStage();
        }

        private void BeginPrepareStage()
        {
            if (_context?.DefendConfig == null)
            {
                Debug.LogError("[DefendStageModule] BeginPrepare without DefendConfig.");
                return;
            }

            var rootPrefab = _catalog.DefendStageRootPrefab;
            if (rootPrefab == null)
            {
                Debug.LogError("[DefendStageModule] DefendStageRoot prefab missing on catalog.");
                return;
            }

            _stageRootInstance = _parent != null
                ? UnityEngine.Object.Instantiate(rootPrefab, _parent)
                : UnityEngine.Object.Instantiate(rootPrefab);
            _stageRootInstance.name = "DefendStageRoot(Clone)";

            _controller = _stageRootInstance.GetComponent<DefendStageController>();
            if (_controller == null)
            {
                _controller = _stageRootInstance.AddComponent<DefendStageController>();
            }

            _controller.ConfigureCatalog(_catalog, _formationCatalog);
            _controller.Begin(
                _context,
                _configs,
                _progress,
                _warriorPool,
                _formation,
                _warehouse,
                _onVictoryAdvance,
                _onLevelFailure);

            Debug.Log(
                $"[Stage:Defend] Prepare Level={_context.LevelId} Stage={_context.StageNumber} ConfigId={_context.GameplayConfigId} MapId={_context.ResolvedMapId} Formation={_formation.Entries.Count}");
        }

        private void TearDownModeSelect()
        {
            if (_modeSelectView != null)
            {
                _modeSelectView.ConfirmRequested -= HandleModeSelectConfirm;
                _modeSelectView.Hide();
                _modeSelectView = null;
            }

            if (_modeSelectInstance != null)
            {
                UnityEngine.Object.Destroy(_modeSelectInstance);
                _modeSelectInstance = null;
            }
        }

        private void TearDownStageRoot()
        {
            if (_controller != null)
            {
                _controller.End();
                _controller = null;
            }

            if (_stageRootInstance != null)
            {
                UnityEngine.Object.Destroy(_stageRootInstance);
                _stageRootInstance = null;
            }
        }
    }
}
