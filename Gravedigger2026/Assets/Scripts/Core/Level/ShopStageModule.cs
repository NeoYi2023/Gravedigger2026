using System;
using Gravedigger2026.Core;
using Gravedigger2026.Core.AutoManufacture;
using Gravedigger2026.Core.Config;
using Gravedigger2026.Core.Dig;
using Gravedigger2026.Core.ProtagonistEquipment;
using Gravedigger2026.Core.Shop;
using Gravedigger2026.Gameplay.Shop;
using Gravedigger2026.UI;
using UnityEngine;

namespace Gravedigger2026.Core.Level
{
    /// <summary>
    /// Shop IStageModule (D-075): Instantiate full-screen ShopStageRoot; close advances the Level.
    /// </summary>
    public sealed class ShopStageModule : IStageModule
    {
        private readonly ShopPrefabCatalog _catalog;
        private readonly Transform _parent;
        private readonly ShopProgressService _progress;
        private readonly ProtagonistEquipmentService _protagonistEquipment;
        private readonly SpecialEquipSlotsService _magicBookSlots;
        private readonly WarehouseService _warehouse;
        private readonly ConfigCsvRepository _configs;
        private readonly ShopOfferRefreshService _refreshService;
        private readonly ShopPurchaseService _purchaseService;
        private readonly ShopSellService _sellService;
        private readonly ConfirmDialogView _confirmDialog;
        private readonly ToastView _toastView;
        private readonly Action _onComplete;
        private readonly Action<bool> _onPresentationActive;

        private GameObject _stageRootInstance;
        private ShopStageRootView _view;

        public ShopStageModule(
            ShopPrefabCatalog catalog,
            Transform parent,
            ShopProgressService progress,
            ProtagonistEquipmentService protagonistEquipment,
            SpecialEquipSlotsService magicBookSlots,
            WarehouseService warehouse,
            ConfigCsvRepository configs,
            ShopOfferRefreshService refreshService,
            ShopPurchaseService purchaseService,
            ShopSellService sellService,
            ConfirmDialogView confirmDialog,
            ToastView toastView,
            Action onComplete,
            Action<bool> onPresentationActive = null)
        {
            _catalog = catalog;
            _parent = parent;
            _progress = progress ?? throw new ArgumentNullException(nameof(progress));
            _protagonistEquipment = protagonistEquipment;
            _magicBookSlots = magicBookSlots;
            _warehouse = warehouse;
            _configs = configs ?? throw new ArgumentNullException(nameof(configs));
            _refreshService = refreshService;
            _purchaseService = purchaseService;
            _sellService = sellService;
            _confirmDialog = confirmDialog;
            _toastView = toastView;
            _onComplete = onComplete;
            _onPresentationActive = onPresentationActive;
        }

        public GameplayState HandledState => GameplayState.Shop;

        public bool IsOpen => _stageRootInstance != null;

        public void Enter(LevelStageContext context)
        {
            Exit(context);

            var prefab = _catalog != null ? _catalog.StageRoot : null;
            if (prefab == null)
            {
                Debug.LogWarning("[ShopStageModule] ShopStageRoot prefab missing — runtime full-screen fallback.");
                _stageRootInstance = new GameObject("ShopStageRoot(Clone)");
                if (_parent != null)
                {
                    _stageRootInstance.transform.SetParent(_parent, false);
                }

                _view = _stageRootInstance.AddComponent<ShopStageRootView>();
                _view.BuildFullscreenHierarchy();
            }
            else
            {
                _stageRootInstance = _parent != null
                    ? UnityEngine.Object.Instantiate(prefab, _parent)
                    : UnityEngine.Object.Instantiate(prefab);
                _stageRootInstance.name = "ShopStageRoot(Clone)";
                _stageRootInstance.SetActive(true);

                _view = _stageRootInstance.GetComponent<ShopStageRootView>();
                if (_view == null)
                {
                    _view = _stageRootInstance.AddComponent<ShopStageRootView>();
                }
            }

            _view.Bind(
                _progress,
                _protagonistEquipment,
                _magicBookSlots,
                _warehouse,
                _configs,
                _refreshService,
                _purchaseService,
                _sellService,
                _confirmDialog,
                _toastView);
            _view.Closed += HandleClosed;
            _onPresentationActive?.Invoke(true);
            _view.Open();

            Debug.Log(
                $"[Stage:Shop] Enter Level={context?.LevelId} Stage={context?.StageNumber} ConfigIdIgnored={context?.GameplayConfigId} (D-075)");
        }

        public void Exit(LevelStageContext context)
        {
            if (_view != null)
            {
                _view.Closed -= HandleClosed;
                _view = null;
            }

            if (_stageRootInstance != null)
            {
                UnityEngine.Object.Destroy(_stageRootInstance);
                _stageRootInstance = null;
            }

            _onPresentationActive?.Invoke(false);

            if (context != null)
            {
                Debug.Log($"[Stage:Shop] Exit Level={context.LevelId} Stage={context.StageNumber}");
            }
        }

        private void HandleClosed()
        {
            _view = null;
            _stageRootInstance = null;
            _onComplete?.Invoke();
        }
    }
}
