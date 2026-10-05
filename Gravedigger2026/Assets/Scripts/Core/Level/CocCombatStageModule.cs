using System;
using Gravedigger2026.Core.Coc;
using Gravedigger2026.Core.Config;
using Gravedigger2026.Core.Sandbox;
using Gravedigger2026.Core.UpgradeManufacture;
using Gravedigger2026.Gameplay.Coc;
using Gravedigger2026.Gameplay.Defend;
using UnityEngine;

namespace Gravedigger2026.Core.Level
{
    /// <summary>
    /// COC IStageModule (SPEC_03 §3.21 / D-099). Sandbox Enter only.
    /// Slice 03d reports <see cref="CocRoundResult"/> for clear / round-loss routing.
    /// </summary>
    public sealed class CocCombatStageModule : IStageModule
    {
        private readonly ConfigCsvRepository _configs;
        private readonly DefendPrefabCatalog _catalog;
        private readonly WarriorPoolService _warriorPool;
        private readonly SandboxProgressService _progress;
        private readonly Transform _parent;
        private readonly Action<CocRoundResult> _onRoundEnded;
        private readonly CocCombatSessionService _session = new CocCombatSessionService();

        private GameObject _stageRoot;
        private CocCombatStageController _controller;

        public CocCombatStageModule(
            ConfigCsvRepository configs,
            DefendPrefabCatalog catalog,
            WarriorPoolService warriorPool,
            SandboxProgressService progress,
            Transform parent,
            Action<CocRoundResult> onRoundEnded)
        {
            _configs = configs ?? throw new ArgumentNullException(nameof(configs));
            _catalog = catalog;
            _warriorPool = warriorPool;
            _progress = progress;
            _parent = parent;
            _onRoundEnded = onRoundEnded;
        }

        public GameplayState HandledState => GameplayState.CocCombat;

        public void Enter(LevelStageContext context)
        {
            Exit(context);

            if (context?.CocConfig == null)
            {
                Debug.LogError("[CocCombatStageModule] Enter without CocConfig.");
                return;
            }

            var spawns = _configs.GetCocSpawns(context.CocConfig.GameplayConfigId);
            _session.Begin(context.CocConfig, spawns);
            _stageRoot = new GameObject("CocCombatStageRoot");
            if (_parent != null)
            {
                _stageRoot.transform.SetParent(_parent, false);
            }

            _controller = _stageRoot.AddComponent<CocCombatStageController>();
            _controller.Begin(context, _catalog, _configs, _session, _warriorPool, _progress, HandleRoundEnded);
            Debug.Log(
                $"[Stage:CocCombat] Enter ConfigId={context.GameplayConfigId} MapId={context.ResolvedMapId}");
        }

        public void Exit(LevelStageContext context)
        {
            if (_controller != null)
            {
                _controller.End();
                _controller = null;
            }

            if (_stageRoot != null)
            {
                UnityEngine.Object.Destroy(_stageRoot);
                _stageRoot = null;
            }

            if (_session.IsActive)
            {
                _session.End();
            }

            if (context != null)
            {
                Debug.Log($"[Stage:CocCombat] Exit ConfigId={context.GameplayConfigId}");
            }
        }

        private void HandleRoundEnded(CocRoundResult result)
        {
            _onRoundEnded?.Invoke(result);
        }
    }
}
