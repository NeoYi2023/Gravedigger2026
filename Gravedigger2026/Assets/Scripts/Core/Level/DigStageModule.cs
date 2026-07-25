using System;
using Gravedigger2026.Core;
using Gravedigger2026.Core.Config;
using Gravedigger2026.Core.Dig;
using Gravedigger2026.Core.Level;
using Gravedigger2026.Gameplay.Dig;
using UnityEngine;

namespace Gravedigger2026.Core.Level
{
    /// <summary>
    /// Dig IStageModule (Approach A / D-020): Instantiate DigStageRoot + map by DigMapId.
    /// </summary>
    public sealed class DigStageModule : IStageModule
    {
        private readonly ConfigCsvRepository _configs;
        private readonly DigPrefabCatalog _catalog;
        private readonly Transform _parent;
        private readonly WarehouseService _warehouse;
        private readonly Action _onSummaryConfirmed;
        private readonly Action<bool> _onDigPresentationActive;

        private GameObject _stageRootInstance;
        private DigStageController _controller;

        public DigStageModule(
            ConfigCsvRepository configs,
            DigPrefabCatalog catalog,
            Transform parent,
            WarehouseService warehouse,
            Action onSummaryConfirmed,
            Action<bool> onDigPresentationActive = null)
        {
            _configs = configs ?? throw new ArgumentNullException(nameof(configs));
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            _parent = parent;
            _warehouse = warehouse ?? throw new ArgumentNullException(nameof(warehouse));
            _onSummaryConfirmed = onSummaryConfirmed;
            _onDigPresentationActive = onDigPresentationActive;
        }

        public GameplayState HandledState => GameplayState.Dig;

        public void Enter(LevelStageContext context)
        {
            Exit(context);

            if (context?.DigConfig == null)
            {
                Debug.LogError("[DigStageModule] Enter without DigConfig.");
                return;
            }

            var rootPrefab = _catalog.DigStageRootPrefab;
            if (rootPrefab == null)
            {
                Debug.LogError("[DigStageModule] DigStageRoot prefab missing on catalog.");
                return;
            }

            _stageRootInstance = _parent != null
                ? UnityEngine.Object.Instantiate(rootPrefab, _parent)
                : UnityEngine.Object.Instantiate(rootPrefab);
            _stageRootInstance.name = "DigStageRoot(Clone)";

            _controller = _stageRootInstance.GetComponent<DigStageController>();
            if (_controller == null)
            {
                _controller = _stageRootInstance.AddComponent<DigStageController>();
            }

            _controller.ConfigureCatalog(_catalog);

            var caps = DigProtagonistCapabilities.CreateDemoDefaults(_configs.GetAllGraveQualityIds());
            _onDigPresentationActive?.Invoke(true);
            _controller.Begin(context, _configs, _warehouse, caps, () =>
            {
                _onSummaryConfirmed?.Invoke();
            });

            Debug.Log(
                $"[Stage:Dig] Enter Level={context.LevelId} Stage={context.StageNumber} ConfigId={context.GameplayConfigId} MapId={context.ResolvedMapId} Prefab={context.ResolvedMapPrefabPath}");
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

            _onDigPresentationActive?.Invoke(false);

            if (context != null)
            {
                Debug.Log($"[Stage:Dig] Exit Level={context.LevelId} Stage={context.StageNumber}");
            }
        }
    }
}
