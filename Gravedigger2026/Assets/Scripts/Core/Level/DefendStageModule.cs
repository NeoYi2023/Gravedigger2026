using System;
using Gravedigger2026.Core;
using Gravedigger2026.Core.Config;
using Gravedigger2026.Core.Dig;
using Gravedigger2026.Core.UpgradeManufacture;
using Gravedigger2026.Gameplay.Defend;
using Gravedigger2026.Gameplay.Formation;
using UnityEngine;

namespace Gravedigger2026.Core.Level
{
    /// <summary>
    /// Defend IStageModule (Approach A / D-040–D-043): Instantiate DefendStageRoot + map by BattleMapId.
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

            if (context?.DefendConfig == null)
            {
                Debug.LogError("[DefendStageModule] Enter without DefendConfig.");
                return;
            }

            if (!_configs.IsLoaded)
            {
                _configs.TryLoadAll();
            }

            _progress.EnsureLoaded(_configs);

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
            _onDefendPresentationActive?.Invoke(true);
            _controller.Begin(
                context,
                _configs,
                _progress,
                _warriorPool,
                _formation,
                _warehouse,
                _onVictoryAdvance,
                _onLevelFailure);

            Debug.Log(
                $"[Stage:Defend] Enter Level={context.LevelId} Stage={context.StageNumber} ConfigId={context.GameplayConfigId} MapId={context.ResolvedMapId} Prefab={context.ResolvedMapPrefabPath} Formation={_formation.Entries.Count}");
        }

        public void Exit(LevelStageContext context)
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

            _onDefendPresentationActive?.Invoke(false);

            if (context != null)
            {
                Debug.Log($"[Stage:Defend] Exit Level={context.LevelId} Stage={context.StageNumber}");
            }
        }
    }
}
