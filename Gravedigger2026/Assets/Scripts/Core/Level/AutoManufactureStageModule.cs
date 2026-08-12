using System;
using System.Collections.Generic;
using Gravedigger2026.Core;
using Gravedigger2026.Core.AutoManufacture;
using Gravedigger2026.Core.Config;
using Gravedigger2026.Core.UpgradeManufacture;
using Gravedigger2026.Gameplay.AutoManufacture;
using Gravedigger2026.Gameplay.Defend;
using Gravedigger2026.Gameplay.Formation;
using UnityEngine;

namespace Gravedigger2026.Core.Level
{
    /// <summary>
    /// AutoManufacture IStageModule (SPEC_03 §3.15 / D-050–D-055 AM-03/05/06 + UI-016).
    /// Rules sync batch → presentation Step1–2 (if crafted&gt;0) → advance.
    /// </summary>
    public sealed class AutoManufactureStageModule : IStageModule
    {
        private readonly AutoManufactureService _autoManufacture;
        private readonly BattleFormationService _formation;
        private readonly AutoFormationDeployService _deploy;
        private readonly AutoManufactureBatchRecordService _batchRecord;
        private readonly SpecialEquipSlotsService _specialEquipSlots;
        private readonly AutoManufacturePresentationFlags _presentationFlags;
        private readonly AutoManufacturePrefabCatalog _presentationCatalog;
        private readonly ConfigCsvRepository _configs;
        private readonly DefendPrefabCatalog _catalog;
        private readonly WarriorPoolService _warriorPool;
        private readonly Transform _parent;
        private readonly Action _onComplete;
        private readonly Action _onNoSoldiersCraftable;

        private GameObject _presentationRoot;
        private AutoManufacturePresentationController _presentation;

        public AutoManufactureStageModule(
            AutoManufactureService autoManufacture,
            BattleFormationService formation,
            AutoFormationDeployService deploy,
            ConfigCsvRepository configs,
            DefendPrefabCatalog catalog,
            WarriorPoolService warriorPool,
            Transform parent,
            SpecialEquipSlotsService specialEquipSlots,
            AutoManufacturePresentationFlags presentationFlags,
            Action onComplete,
            Action onNoSoldiersCraftable = null,
            AutoManufactureBatchRecordService batchRecord = null,
            AutoManufacturePrefabCatalog presentationCatalog = null)
        {
            _autoManufacture = autoManufacture ?? throw new ArgumentNullException(nameof(autoManufacture));
            _formation = formation ?? throw new ArgumentNullException(nameof(formation));
            _deploy = deploy ?? throw new ArgumentNullException(nameof(deploy));
            _configs = configs ?? throw new ArgumentNullException(nameof(configs));
            _catalog = catalog;
            _warriorPool = warriorPool ?? throw new ArgumentNullException(nameof(warriorPool));
            _parent = parent;
            _specialEquipSlots = specialEquipSlots;
            _presentationFlags = presentationFlags ?? throw new ArgumentNullException(nameof(presentationFlags));
            _onComplete = onComplete;
            _onNoSoldiersCraftable = onNoSoldiersCraftable;
            _batchRecord = batchRecord;
            _presentationCatalog = presentationCatalog;
        }

        public GameplayState HandledState => GameplayState.AutoManufacture;

        public void Enter(LevelStageContext context)
        {
            Exit(context);
            _presentationFlags.Clear();

            Debug.Log(
                $"[Stage:AutoManufacture] Enter Level={context?.LevelId} Stage={context?.StageNumber} " +
                $"ConfigIdIgnored={context?.GameplayConfigId} (AM-06 clear+deploy + UI-016)");

            _formation.Clear();

            var flushedIds = new List<string>();
            var crafted = _autoManufacture.RunBatch(out var stopReason, flushedIds);
            var mapId = FormationMapResolver.ResolveUmBattleMapId(
                _configs,
                context != null ? context.LevelId : null,
                context != null ? context.StageNumber : 0);

            var zones = _catalog != null
                ? FormationClassZoneCollector.CollectFromCatalog(_catalog, mapId)
                : new List<FormationClassZoneSnapshot>();
            if (_catalog == null)
            {
                Debug.LogWarning(
                    "[Stage:AutoManufacture] DefendPrefabCatalog missing — deploy skipped (formation cleared).");
            }

            var deployed = _deploy.DeployBatch(flushedIds, zones);
            _batchRecord?.Replace(flushedIds);
            Debug.Log(
                $"[Stage:AutoManufacture] Batch crafted={crafted} flushed={flushedIds.Count} " +
                $"deployed={deployed} map={mapId} stop={stopReason} record={_batchRecord?.WarriorIds.Count ?? 0}");

            if (crafted == 0)
            {
                _onNoSoldiersCraftable?.Invoke();
                _onComplete?.Invoke();
                return;
            }

            StartPresentation(flushedIds);
        }

        public void Exit(LevelStageContext context)
        {
            if (_presentation != null)
            {
                _presentation.End();
                _presentation = null;
            }

            if (_presentationRoot != null)
            {
                UnityEngine.Object.Destroy(_presentationRoot);
                _presentationRoot = null;
            }

            if (context != null)
            {
                Debug.Log(
                    $"[Stage:AutoManufacture] Exit Level={context.LevelId} Stage={context.StageNumber} " +
                    $"TempLeft={_autoManufacture.TempWarehouse.Count}");
            }
        }

        private void StartPresentation(List<string> flushedIds)
        {
            var parent = _parent;
            // Prefer catalog Prefab when wired; otherwise runtime Build (Demo-safe).
            var prefab = _presentationCatalog != null ? _presentationCatalog.PresentationRoot : null;
            AutoManufacturePresentationController controller = null;
            if (prefab != null)
            {
                _presentationRoot = parent != null
                    ? UnityEngine.Object.Instantiate(prefab, parent)
                    : UnityEngine.Object.Instantiate(prefab);
                _presentationRoot.name = "AutoManufacturePresentationRoot(Clone)";
                controller = _presentationRoot.GetComponent<AutoManufacturePresentationController>();
            }

            if (controller == null || !controller.IsWired)
            {
                if (_presentationRoot != null)
                {
                    UnityEngine.Object.Destroy(_presentationRoot);
                    _presentationRoot = null;
                }

                Debug.LogWarning(
                    "[Stage:AutoManufacture] Presentation Prefab missing/unwired — fallback to runtime Build. " +
                    "Assign AutoManufacturePrefabCatalog on MetaShellRoot to use Prefab edits.");
                controller = AutoManufacturePresentationController.Build(parent);
                _presentationRoot = controller.gameObject;
            }
            else
            {
                Debug.Log("[Stage:AutoManufacture] Using catalog Presentation Prefab.");
            }

            _presentation = controller;
            _presentation.Begin(
                flushedIds,
                _specialEquipSlots,
                _configs,
                _warriorPool,
                _catalog,
                HandlePresentationComplete);

            Debug.Log($"[Stage:AutoManufacture] Presentation started soldiers={flushedIds.Count}");
        }

        private void HandlePresentationComplete()
        {
            _presentationFlags.ArmAutoOpenFormation();
            Debug.Log("[Stage:AutoManufacture] Presentation complete → advance + arm AutoOpenFormation");
            _onComplete?.Invoke();
        }
    }
}
