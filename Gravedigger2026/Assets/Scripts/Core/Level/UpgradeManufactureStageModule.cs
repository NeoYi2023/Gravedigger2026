using System;
using Gravedigger2026.Core;
using Gravedigger2026.Core.Config;
using Gravedigger2026.Core.UpgradeManufacture;
using Gravedigger2026.Gameplay.UpgradeManufacture;
using UnityEngine;

namespace Gravedigger2026.Core.Level
{
    /// <summary>
    /// UM IStageModule (Approach A / D-030 + D-031 + D-032): Instantiate UpgradeManufactureStageRoot.
    /// </summary>
    public sealed class UpgradeManufactureStageModule : IStageModule
    {
        private readonly ConfigCsvRepository _configs;
        private readonly UpgradeManufacturePrefabCatalog _catalog;
        private readonly Transform _parent;
        private readonly ProtagonistProgressService _progress;
        private readonly ManufactureService _manufacture;
        private readonly WarriorPoolService _warriorPool;
        private readonly BattleFormationService _formation;
        private readonly Action _onComplete;
        private readonly Action<bool> _onUmPresentationActive;

        private GameObject _stageRootInstance;
        private UpgradeManufactureStageController _controller;

        public UpgradeManufactureStageModule(
            ConfigCsvRepository configs,
            UpgradeManufacturePrefabCatalog catalog,
            Transform parent,
            ProtagonistProgressService progress,
            ManufactureService manufacture,
            WarriorPoolService warriorPool,
            BattleFormationService formation,
            Action onComplete,
            Action<bool> onUmPresentationActive = null)
        {
            _configs = configs ?? throw new ArgumentNullException(nameof(configs));
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            _parent = parent;
            _progress = progress ?? throw new ArgumentNullException(nameof(progress));
            _manufacture = manufacture ?? throw new ArgumentNullException(nameof(manufacture));
            _warriorPool = warriorPool ?? throw new ArgumentNullException(nameof(warriorPool));
            _formation = formation ?? throw new ArgumentNullException(nameof(formation));
            _onComplete = onComplete;
            _onUmPresentationActive = onUmPresentationActive;
        }

        public GameplayState HandledState => GameplayState.UpgradeManufacture;

        public void Enter(LevelStageContext context)
        {
            Exit(context);

            if (!_configs.IsLoaded)
            {
                _configs.TryLoadAll();
            }

            _progress.EnsureLoaded(_configs);
            _manufacture.ClearAllSlots();

            var rootPrefab = _catalog.StageRootPrefab;
            if (rootPrefab == null)
            {
                Debug.LogError("[UpgradeManufactureStageModule] StageRoot prefab missing on catalog.");
                return;
            }

            _stageRootInstance = _parent != null
                ? UnityEngine.Object.Instantiate(rootPrefab, _parent)
                : UnityEngine.Object.Instantiate(rootPrefab);
            _stageRootInstance.name = "UpgradeManufactureStageRoot(Clone)";

            _controller = _stageRootInstance.GetComponent<UpgradeManufactureStageController>();
            if (_controller == null)
            {
                _controller = _stageRootInstance.AddComponent<UpgradeManufactureStageController>();
            }

            _controller.ConfigureCatalog(_catalog);
            _onUmPresentationActive?.Invoke(true);
            _controller.Begin(_progress, _manufacture, _warriorPool, _formation, () => _onComplete?.Invoke());

            Debug.Log(
                $"[Stage:UM] Enter Level={context?.LevelId} Stage={context?.StageNumber} ConfigIdIgnored={context?.GameplayConfigId} Formation={_formation.Entries.Count} (D-030/D-031/D-032)");
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

            _onUmPresentationActive?.Invoke(false);

            if (context != null)
            {
                Debug.Log(
                    $"[Stage:UM] Exit Level={context.LevelId} Stage={context.StageNumber} FormationKept={_formation.Entries.Count}");
            }
        }
    }
}
