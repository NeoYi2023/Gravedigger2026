using System;
using Gravedigger2026.Core;
using Gravedigger2026.Core.AutoManufacture;
using Gravedigger2026.Core.Config;
using Gravedigger2026.Core.Dig;
using Gravedigger2026.Core.PushMap;
using Gravedigger2026.Core.ProtagonistEquipment;
using Gravedigger2026.Core.UpgradeManufacture;
using Gravedigger2026.Gameplay.Defend;
using Gravedigger2026.Gameplay.Formation;
using Gravedigger2026.Gameplay.PushMap;
using UnityEngine;

namespace Gravedigger2026.Core.Level
{
    /// <summary>
    /// PushMap IStageModule (PM-03–PM-07): enters PushMapPhase=Prepare directly when
    /// GameplayType=PushMap; also entered via Defend ModeSelect Mode2 handoff (D-044).
    /// Instantiates Maps/{MapId} and reuses the shared FormationEditor;
    /// StartBattle (≥1) initializes Shield and locks LossOfControl (semantically §3.12).
    /// PM-07 wires Boss clear Exp + CaptureLoot/DungeonUnlock hooks via DungeonUnlockService.
    /// ModeSelect Mode2 handoff enters this module via TryHandoffModeSelectToPushMap (D-044).
    /// </summary>
    public sealed class PushMapStageModule : IStageModule
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
        private readonly DungeonUnlockService _dungeonUnlocks;
        private readonly Action _onVictoryAdvance;
        private readonly Action<string> _onLevelFailure;
        private readonly Action<bool> _onPushMapPresentationActive;

        private LevelStageContext _context;
        private GameObject _stageRootInstance;
        private PushMapStageController _controller;

        public PushMapStageModule(
            ConfigCsvRepository configs,
            DefendPrefabCatalog catalog,
            FormationPrefabCatalog formationCatalog,
            Transform parent,
            ProtagonistProgressService progress,
            WarriorPoolService warriorPool,
            BattleFormationService formation,
            WarehouseService warehouse,
            SpecialEquipSlotsService specialEquipSlots,
            ProtagonistEquipmentService protagonistEquipment,
            DungeonUnlockService dungeonUnlocks,
            Action onVictoryAdvance,
            Action<string> onLevelFailure,
            Action<bool> onPushMapPresentationActive = null)
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
            _dungeonUnlocks = dungeonUnlocks;
            _onVictoryAdvance = onVictoryAdvance;
            _onLevelFailure = onLevelFailure;
            _onPushMapPresentationActive = onPushMapPresentationActive;
        }

        public GameplayState HandledState => GameplayState.PushMap;

        public void Enter(LevelStageContext context)
        {
            Exit(context);
            _context = context;

            if (!_configs.IsLoaded)
            {
                _configs.TryLoadAll();
            }

            _progress.EnsureLoaded(_configs);
            _onPushMapPresentationActive?.Invoke(true);
            BeginPrepareStage();
        }

        public void Exit(LevelStageContext context)
        {
            TearDownStageRoot();
            _onPushMapPresentationActive?.Invoke(false);
            _context = null;

            if (context != null)
            {
                Debug.Log($"[Stage:PushMap] Exit Level={context.LevelId} Stage={context.StageNumber}");
            }
        }

        private void BeginPrepareStage()
        {
            if (_context?.PushMapConfig == null)
            {
                Debug.LogError("[PushMapStageModule] BeginPrepare without PushMapConfig.");
                return;
            }

            _stageRootInstance = _parent != null
                ? new GameObject("PushMapStageRoot(Runtime)")
                : new GameObject("PushMapStageRoot(Runtime)");
            if (_parent != null)
            {
                _stageRootInstance.transform.SetParent(_parent, false);
            }

            _controller = _stageRootInstance.AddComponent<PushMapStageController>();
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
                _dungeonUnlocks,
                _onVictoryAdvance,
                _onLevelFailure);

            Debug.Log(
                $"[Stage:PushMap] Prepare Level={_context.LevelId} Stage={_context.StageNumber} ConfigId={_context.GameplayConfigId} MapId={_context.ResolvedMapId} Formation={_formation.Entries.Count}");
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
