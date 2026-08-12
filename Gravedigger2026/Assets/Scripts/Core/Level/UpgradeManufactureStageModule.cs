using System;
using Gravedigger2026.Core;
using Gravedigger2026.Core.AutoManufacture;
using Gravedigger2026.Core.Config;
using Gravedigger2026.Core.UpgradeManufacture;
using Gravedigger2026.Gameplay.Defend;
using Gravedigger2026.Gameplay.Formation;
using Gravedigger2026.Gameplay.UpgradeManufacture;
using UnityEngine;

namespace Gravedigger2026.Core.Level
{
    /// <summary>
    /// UM IStageModule (D-030 + D-031 + D-032): Instantiate UpgradeManufactureStageRoot.
    /// </summary>
    public sealed class UpgradeManufactureStageModule : IStageModule
    {
        private readonly ConfigCsvRepository _configs;
        private readonly UpgradeManufacturePrefabCatalog _catalog;
        private readonly FormationPrefabCatalog _formationCatalog;
        private readonly DefendPrefabCatalog _defendCatalog;
        private readonly Transform _parent;
        private readonly ProtagonistProgressService _progress;
        private readonly ManufactureService _manufacture;
        private readonly WarriorPoolService _warriorPool;
        private readonly BattleFormationService _formation;
        private readonly AutoManufactureBatchRecordService _batchRecord;
        private readonly AutoManufacturePresentationFlags _presentationFlags;
        private readonly Action _onComplete;
        private readonly Action<bool> _onUmPresentationActive;

        private GameObject _stageRootInstance;
        private UpgradeManufactureStageController _controller;

        public UpgradeManufactureStageModule(
            ConfigCsvRepository configs,
            UpgradeManufacturePrefabCatalog catalog,
            FormationPrefabCatalog formationCatalog,
            DefendPrefabCatalog defendCatalog,
            Transform parent,
            ProtagonistProgressService progress,
            ManufactureService manufacture,
            WarriorPoolService warriorPool,
            BattleFormationService formation,
            Action onComplete,
            Action<bool> onUmPresentationActive = null,
            AutoManufactureBatchRecordService batchRecord = null,
            AutoManufacturePresentationFlags presentationFlags = null)
        {
            _configs = configs ?? throw new ArgumentNullException(nameof(configs));
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            _formationCatalog = formationCatalog;
            _defendCatalog = defendCatalog;
            _parent = parent;
            _progress = progress ?? throw new ArgumentNullException(nameof(progress));
            _manufacture = manufacture ?? throw new ArgumentNullException(nameof(manufacture));
            _warriorPool = warriorPool ?? throw new ArgumentNullException(nameof(warriorPool));
            _formation = formation ?? throw new ArgumentNullException(nameof(formation));
            _onComplete = onComplete;
            _onUmPresentationActive = onUmPresentationActive;
            _batchRecord = batchRecord;
            _presentationFlags = presentationFlags;
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

            var mode = _formation != null ? _formation.BoundCampaignMode : CampaignMode.Mode1;
            var rootPrefab = _catalog.ResolveStageRoot(mode);
            if (rootPrefab == null)
            {
                Debug.LogError("[UpgradeManufactureStageModule] StageRoot prefab missing on catalog.");
                return;
            }

            _stageRootInstance = _parent != null
                ? UnityEngine.Object.Instantiate(rootPrefab, _parent)
                : UnityEngine.Object.Instantiate(rootPrefab);
            _stageRootInstance.name = mode == CampaignMode.Mode2
                ? "UpgradeManufactureStageRoot_Mode2(Clone)"
                : "UpgradeManufactureStageRoot(Clone)";

            _controller = _stageRootInstance.GetComponent<UpgradeManufactureStageController>();
            if (_controller == null)
            {
                _controller = _stageRootInstance.AddComponent<UpgradeManufactureStageController>();
            }

            _controller.ConfigureCatalog(_catalog);
            _onUmPresentationActive?.Invoke(true);
            var autoOpenFormation = _presentationFlags != null && _presentationFlags.ConsumeAutoOpenFormation();
            _controller.Begin(
                _configs,
                _formationCatalog,
                _defendCatalog,
                _progress,
                _manufacture,
                _warriorPool,
                _formation,
                context,
                () => _onComplete?.Invoke(),
                _batchRecord,
                autoOpenFormation);

            Debug.Log(
                $"[Stage:UM] Enter Level={context?.LevelId} Stage={context?.StageNumber} ConfigIdIgnored={context?.GameplayConfigId} Formation={_formation.Entries.Count} AutoOpenFormation={autoOpenFormation} (D-030/D-031/D-032/D-055)");
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
