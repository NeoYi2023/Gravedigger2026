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
    /// AutoManufacture IStageModule (SPEC_03 §3.15 / D-050–D-055 + Step2 per-slot MagicBook).
    /// Craft without books → presentation pulse apply → Deploy → advance.
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
        private readonly List<string> _flushedIds = new List<string>();
        private List<FormationClassZoneSnapshot> _zones;
        private string _mapId;
        private int _soldierCursor;
        private int _slotCursor;
        private bool _booksFullyApplied;
        private bool _deployed;

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
            _flushedIds.Clear();
            _soldierCursor = 0;
            _slotCursor = 0;
            _booksFullyApplied = false;
            _deployed = false;
            _zones = null;
            _mapId = null;

            Debug.Log(
                $"[Stage:AutoManufacture] Enter Level={context?.LevelId} Stage={context?.StageNumber} " +
                $"ConfigIdIgnored={context?.GameplayConfigId} (Step2 per-slot MagicBook + deferred deploy)");

            _formation.Clear();

            var crafted = _autoManufacture.RunBatch(out var stopReason, _flushedIds);
            _mapId = FormationMapResolver.ResolveUmBattleMapId(
                _configs,
                context != null ? context.LevelId : null,
                context != null ? context.StageNumber : 0);

            _zones = _catalog != null
                ? FormationClassZoneCollector.CollectFromCatalog(_catalog, _mapId)
                : new List<FormationClassZoneSnapshot>();
            if (_catalog == null)
            {
                Debug.LogWarning(
                    "[Stage:AutoManufacture] DefendPrefabCatalog missing — deploy will be skipped.");
            }

            _batchRecord?.Replace(_flushedIds);
            Debug.Log(
                $"[Stage:AutoManufacture] Batch crafted={crafted} flushed={_flushedIds.Count} " +
                $"map={_mapId} stop={stopReason} record={_batchRecord?.WarriorIds.Count ?? 0}");

            if (crafted == 0)
            {
                _booksFullyApplied = true;
                _deployed = true;
                _onNoSoldiersCraftable?.Invoke();
                _onComplete?.Invoke();
                return;
            }

            if (!StartPresentation())
            {
                FinishBooksAndDeploy(reason: "presentation-start-failed");
                _onComplete?.Invoke();
            }
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

            if (_flushedIds.Count > 0 && !_booksFullyApplied)
            {
                FinishBooksAndDeploy(reason: "stage-exit");
            }
            else if (_flushedIds.Count > 0 && !_deployed)
            {
                DeployFlushed(reason: "stage-exit-deploy-only");
            }

            _flushedIds.Clear();
            _zones = null;
            _soldierCursor = 0;
            _slotCursor = 0;
            _booksFullyApplied = false;
            _deployed = false;

            if (context != null)
            {
                Debug.Log(
                    $"[Stage:AutoManufacture] Exit Level={context.LevelId} Stage={context.StageNumber} " +
                    $"TempLeft={_autoManufacture.TempWarehouse.Count}");
            }
        }

        private bool StartPresentation()
        {
            var parent = _parent;
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
                try
                {
                    controller = AutoManufacturePresentationController.Build(parent);
                    _presentationRoot = controller.gameObject;
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[Stage:AutoManufacture] Presentation Build failed: {ex.Message}");
                    return false;
                }
            }
            else
            {
                Debug.Log("[Stage:AutoManufacture] Using catalog Presentation Prefab.");
            }

            _presentation = controller;
            _presentation.Begin(
                _flushedIds,
                _specialEquipSlots,
                _configs,
                _warriorPool,
                _catalog,
                HandlePresentationComplete,
                HandleBookPulsePeak);

            Debug.Log($"[Stage:AutoManufacture] Presentation started soldiers={_flushedIds.Count}");
            return true;
        }

        private void HandleBookPulsePeak(string warriorId, int slotIndex)
        {
            if (string.IsNullOrEmpty(warriorId)
                || !_warriorPool.TryGet(warriorId, out var warrior)
                || warrior == null)
            {
                return;
            }

            _autoManufacture.ApplyBookAtSlotAndRefinalize(warrior, slotIndex);

            var soldierIndex = _flushedIds.IndexOf(warriorId);
            if (soldierIndex >= 0)
            {
                _soldierCursor = soldierIndex;
                _slotCursor = slotIndex + 1;
                if (_slotCursor >= SpecialEquipSlotsService.SlotCount)
                {
                    _soldierCursor = soldierIndex + 1;
                    _slotCursor = 0;
                }
            }

            if (_presentation != null)
            {
                _presentation.RefreshFocusedCardClass(warriorId);
            }
        }

        private void HandlePresentationComplete()
        {
            FinishBooksAndDeploy(reason: "presentation-complete");
            _presentationFlags.ArmAutoOpenFormation();
            Debug.Log("[Stage:AutoManufacture] Presentation complete → advance + arm AutoOpenFormation");
            _onComplete?.Invoke();
        }

        private void FinishBooksAndDeploy(string reason)
        {
            if (!_booksFullyApplied)
            {
                for (var i = 0; i < _flushedIds.Count; i++)
                {
                    var id = _flushedIds[i];
                    if (!_warriorPool.TryGet(id, out var warrior) || warrior == null)
                    {
                        continue;
                    }

                    var fromSlot = 0;
                    if (i < _soldierCursor)
                    {
                        continue;
                    }

                    if (i == _soldierCursor)
                    {
                        fromSlot = _slotCursor;
                    }

                    if (fromSlot < SpecialEquipSlotsService.SlotCount)
                    {
                        _autoManufacture.ApplyRemainingBooksAndRefinalize(warrior, fromSlot);
                    }
                }

                _booksFullyApplied = true;
                _soldierCursor = _flushedIds.Count;
                _slotCursor = 0;
                Debug.Log($"[Stage:AutoManufacture] MagicBooks applied reason={reason}");
            }

            DeployFlushed(reason);
        }

        private void DeployFlushed(string reason)
        {
            if (_deployed)
            {
                return;
            }

            var zones = _zones ?? new List<FormationClassZoneSnapshot>();
            var deployed = _deploy.DeployBatch(_flushedIds, zones);
            _deployed = true;
            Debug.Log(
                $"[Stage:AutoManufacture] Deployed={deployed}/{_flushedIds.Count} map={_mapId} reason={reason}");
        }
    }
}
