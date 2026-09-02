using System;
using Gravedigger2026.Core;
using Gravedigger2026.Core.AutoManufacture;
using Gravedigger2026.Core.Config;
using Gravedigger2026.Core.Dig;
using Gravedigger2026.Core.ProtagonistEquipment;
using Gravedigger2026.Core.UpgradeManufacture;
using Gravedigger2026.Gameplay.Defend;
using Gravedigger2026.Gameplay.Formation;
using Gravedigger2026.Gameplay.SearchExtract;
using UnityEngine;

namespace Gravedigger2026.Core.Level
{
    /// <summary>
    /// SearchExtract IStageModule (SE-03 Approach A). GameplayType=SearchExtract enters
    /// SearchExtractPhase=Prepare; StartBattle (≥1) → Combat. SE-08 wires RewardGrant deps +
    /// Leave → onVictoryAdvance (TryAdvanceStage). SE-09 wires loyal wipe → onLevelFailure
    /// (AbortLevel + LevelSelect). Does not modify PushMapSessionService.
    /// </summary>
    public sealed class SearchExtractStageModule : IStageModule
    {
        private readonly ConfigCsvRepository _configs;
        private readonly DefendPrefabCatalog _catalog;
        private readonly FormationPrefabCatalog _formationCatalog;
        private readonly Transform _parent;
        private readonly ProtagonistProgressService _progress;
        private readonly WarriorPoolService _warriorPool;
        private readonly BattleFormationService _formation;
        private readonly WarehouseService _warehouse;
        private readonly SpecialEquipSlotsService _specialEquipSlots;
        private readonly ProtagonistEquipmentService _protagonistEquipment;
        private readonly Action _onVictoryAdvance;
        private readonly Action<string> _onLevelFailure;
        private readonly Action<bool> _onPresentationActive;

        private LevelStageContext _context;
        private GameObject _stageRootInstance;
        private SearchExtractStageController _controller;

        public SearchExtractStageModule(
            ConfigCsvRepository configs,
            DefendPrefabCatalog catalog,
            FormationPrefabCatalog formationCatalog,
            Transform parent,
            ProtagonistProgressService progress,
            WarriorPoolService warriorPool,
            BattleFormationService formation,
            WarehouseService warehouse = null,
            SpecialEquipSlotsService specialEquipSlots = null,
            ProtagonistEquipmentService protagonistEquipment = null,
            Action onVictoryAdvance = null,
            Action<string> onLevelFailure = null,
            Action<bool> onPresentationActive = null)
        {
            _configs = configs ?? throw new ArgumentNullException(nameof(configs));
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            _formationCatalog = formationCatalog;
            _parent = parent;
            _progress = progress ?? throw new ArgumentNullException(nameof(progress));
            _warriorPool = warriorPool ?? throw new ArgumentNullException(nameof(warriorPool));
            _formation = formation ?? throw new ArgumentNullException(nameof(formation));
            _warehouse = warehouse;
            _specialEquipSlots = specialEquipSlots;
            _protagonistEquipment = protagonistEquipment;
            _onVictoryAdvance = onVictoryAdvance;
            _onLevelFailure = onLevelFailure;
            _onPresentationActive = onPresentationActive;
        }

        public GameplayState HandledState => GameplayState.SearchExtract;

        public void Enter(LevelStageContext context)
        {
            Exit(context);
            _context = context;

            if (!_configs.IsLoaded)
            {
                _configs.TryLoadAll();
            }

            _progress.EnsureLoaded(_configs);
            _onPresentationActive?.Invoke(true);
            BeginPrepareStage();
        }

        public void Exit(LevelStageContext context)
        {
            TearDownStageRoot();
            _onPresentationActive?.Invoke(false);
            _context = null;

            if (context != null)
            {
                Debug.Log($"[Stage:SearchExtract] Exit Level={context.LevelId} Stage={context.StageNumber} Option={context.GameplayOptionId}");
            }
        }

        private void BeginPrepareStage()
        {
            if (_context?.SearchExtractConfig == null)
            {
                Debug.LogError("[SearchExtractStageModule] BeginPrepare without SearchExtractConfig.");
                return;
            }

            _stageRootInstance = new GameObject("SearchExtractStageRoot(Runtime)");
            if (_parent != null)
            {
                _stageRootInstance.transform.SetParent(_parent, false);
            }

            _controller = _stageRootInstance.AddComponent<SearchExtractStageController>();
            _controller.ConfigureCatalog(_catalog, _formationCatalog);
            _controller.Begin(
                _context,
                _configs,
                _progress,
                _warriorPool,
                _formation,
                _warehouse,
                _specialEquipSlots,
                _protagonistEquipment,
                _onVictoryAdvance,
                _onLevelFailure);

            Debug.Log(
                $"[Stage:SearchExtract] Prepare Level={_context.LevelId} Stage={_context.StageNumber} " +
                $"Option={_context.GameplayOptionId} ConfigId={_context.GameplayConfigId} " +
                $"MapId={_context.ResolvedMapId} N={_context.GatherPointCount} Formation={_formation.Entries.Count}");
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
