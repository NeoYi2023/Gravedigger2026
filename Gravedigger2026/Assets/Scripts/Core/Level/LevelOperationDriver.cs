using System;
using System.Collections.Generic;
using Gravedigger2026.Core.Config;
using UnityEngine;

namespace Gravedigger2026.Core.Level
{
    /// <summary>
    /// Drives LevelOperationConfig ascending stages (SPEC_03 §3.9 / D-010). Approach A.
    /// </summary>
    public sealed class LevelOperationDriver
    {
        public const string DemoSampleLevelId = "Level_01";

        private readonly ConfigCsvRepository _configs;
        private readonly GameplayStateService _gameplayState;
        private readonly Dictionary<GameplayState, IStageModule> _modules =
            new Dictionary<GameplayState, IStageModule>();

        private List<LevelOperationConfigRow> _stages = new List<LevelOperationConfigRow>();
        private int _stageIndex = -1;
        private LevelStageContext _currentContext;

        public event Action<LevelStageContext> StageChanged;
        public event Action<string> LevelEnded;

        public LevelOperationDriver(ConfigCsvRepository configs, GameplayStateService gameplayState)
        {
            _configs = configs ?? throw new ArgumentNullException(nameof(configs));
            _gameplayState = gameplayState ?? throw new ArgumentNullException(nameof(gameplayState));
        }

        public bool IsRunning => _stageIndex >= 0 && _stageIndex < _stages.Count;
        public string ActiveLevelId { get; private set; }
        public LevelStageContext CurrentContext => _currentContext;

        public void RegisterModule(IStageModule module)
        {
            if (module == null)
            {
                throw new ArgumentNullException(nameof(module));
            }

            _modules[module.HandledState] = module;
        }

        /// <summary>
        /// Registers placeholders for stages not yet wired by MetaShell.
        /// Dig / UM / Defend are overwritten by MetaShell when catalogs are bound (D-020 / D-030 / D-040).
        /// </summary>
        public void RegisterDefaultPlaceholders()
        {
            RegisterModule(new UpgradeManufacturePlaceholderStageModule());
            RegisterModule(new DefendPlaceholderStageModule());
        }

        public bool EnsureConfigsLoaded()
        {
            if (_configs.IsLoaded)
            {
                return true;
            }

            return _configs.TryLoadAll();
        }

        public bool TryEnterLevel(string levelId, out string error)
        {
            error = null;
            if (!EnsureConfigsLoaded())
            {
                error = _configs.LastError ?? "Config load failed.";
                return false;
            }

            StopCurrentLevelInternal(notifyEnded: false);

            var stages = _configs.GetStagesForLevel(levelId);
            if (stages.Count == 0)
            {
                error = $"No LevelOperation rows for LevelId '{levelId}'.";
                return false;
            }

            ActiveLevelId = levelId;
            _stages = stages;
            _stageIndex = 0;
            if (!TryEnterCurrentStage(out error))
            {
                StopCurrentLevelInternal(notifyEnded: false);
                return false;
            }

            Debug.Log($"[LevelOperationDriver] EnterLevel {levelId} ({_stages.Count} stages).");
            return true;
        }

        /// <summary>
        /// Demo placeholder: treat current stage as finished and advance (real end conditions in later slices).
        /// </summary>
        public bool TryAdvanceStage(out string message)
        {
            if (!IsRunning || _currentContext == null)
            {
                message = "No active Level stage.";
                return false;
            }

            ExitCurrentModule();

            _stageIndex++;
            if (_stageIndex >= _stages.Count)
            {
                var levelId = ActiveLevelId;
                StopCurrentLevelInternal(notifyEnded: false);
                message = $"VictorySettlement（占位）— 关卡 {levelId} 完成";
                Debug.Log($"[LevelOperationDriver] {message}");
                LevelEnded?.Invoke(message);
                StageChanged?.Invoke(null);
                return true;
            }

            if (!TryEnterCurrentStage(out var error))
            {
                message = error;
                StopCurrentLevelInternal(notifyEnded: false);
                StageChanged?.Invoke(null);
                return false;
            }

            message =
                $"进入阶段 {_currentContext.StageNumber} / {_stages.Count} — {_currentContext.GameplayType}";
            return true;
        }

        public void StopCurrentLevel()
        {
            StopCurrentLevelInternal(notifyEnded: true);
            StageChanged?.Invoke(null);
        }

        /// <summary>
        /// LevelFailure abort: exit current stage, end Level without VictorySettlement (SPEC_03 §3.9 / D-043).
        /// </summary>
        public void AbortLevelAsFailure(string reason)
        {
            if (!IsRunning && string.IsNullOrEmpty(ActiveLevelId))
            {
                return;
            }

            var levelId = ActiveLevelId;
            ExitCurrentModule();
            ActiveLevelId = null;
            _stages = new List<LevelOperationConfigRow>();
            _stageIndex = -1;
            _currentContext = null;
            var message = string.IsNullOrEmpty(reason)
                ? $"LevelFailure — 关卡 {levelId} 中止"
                : $"LevelFailure — {reason}";
            Debug.LogWarning($"[LevelOperationDriver] {message}");
            LevelEnded?.Invoke(message);
            StageChanged?.Invoke(null);
        }

        /// <summary>
        /// ModeSelect Mode2 handoff: exit Defend module, rewrite current context to PushMap, enter PushMapStageModule (D-044).
        /// </summary>
        public bool TryHandoffModeSelectToPushMap(string gameplayConfigId, out string error)
        {
            error = null;
            if (!IsRunning || _currentContext == null)
            {
                error = "No active Level stage.";
                return false;
            }

            if (_currentContext.GameplayType != GameplayState.Defend)
            {
                error =
                    $"ModeSelect PushMap handoff requires Defend stage (current={_currentContext.GameplayType}).";
                return false;
            }

            if (!_configs.TryGetPushMap(gameplayConfigId, out var pushMap))
            {
                error = $"PushMapGameplayConfig '{gameplayConfigId}' not found.";
                return false;
            }

            if (!MapPrefabPaths.TryResolveAssetPath(pushMap.MapId, out var pushMapPath, out var pushMapErr))
            {
                error = pushMapErr ?? $"Map resolve failed for '{pushMap.MapId}'.";
                return false;
            }

            ExitCurrentModule();

            _currentContext.GameplayType = GameplayState.PushMap;
            _currentContext.GameplayConfigId = pushMap.GameplayConfigId;
            _currentContext.DefendConfig = null;
            _currentContext.PushMapConfig = pushMap;
            _currentContext.ResolvedMapId = pushMap.MapId;
            _currentContext.ResolvedMapPrefabPath = pushMapPath;
            _currentContext.MapResolveNote =
                $"ModeSelect PushMap Config={pushMap.GameplayConfigId} MapId={pushMap.MapId} → {pushMapPath} (Handoff→PushMapStageModule).";

            _gameplayState.SetState(GameplayState.PushMap);

            if (_modules.TryGetValue(GameplayState.PushMap, out var module))
            {
                module.Enter(_currentContext);
            }
            else
            {
                error = "No IStageModule for PushMap.";
                Debug.LogError($"[LevelOperationDriver] {error}");
                return false;
            }

            StageChanged?.Invoke(_currentContext);
            Debug.Log(
                $"[LevelOperationDriver] ModeSelect handoff → PushMap Config={pushMap.GameplayConfigId} Map={pushMap.MapId} Level={_currentContext.LevelId} Stage={_currentContext.StageNumber}");
            return true;
        }

        private bool TryEnterCurrentStage(out string error)
        {
            error = null;
            var row = _stages[_stageIndex];
            if (!TryBuildContext(row, out var context, out error))
            {
                return false;
            }

            _currentContext = context;
            _gameplayState.SetState(context.GameplayType);

            if (_modules.TryGetValue(context.GameplayType, out var module))
            {
                module.Enter(context);
            }
            else
            {
                Debug.LogWarning($"[LevelOperationDriver] No IStageModule for {context.GameplayType}.");
            }

            StageChanged?.Invoke(context);
            Debug.Log(
                $"[LevelOperationDriver] Stage LevelId={context.LevelId} StageNumber={context.StageNumber} GameplayType={context.GameplayType} Map={context.ResolvedMapId ?? "-"} Note={context.MapResolveNote}");
            return true;
        }

        private bool TryBuildContext(LevelOperationConfigRow row, out LevelStageContext context, out string error)
        {
            context = new LevelStageContext
            {
                LevelId = row.LevelId,
                StageNumber = row.StageNumber,
                GameplayType = row.GameplayType,
                GameplayConfigId = row.GameplayConfigId
            };
            error = null;

            switch (row.GameplayType)
            {
                case GameplayState.UpgradeManufacture:
                    context.GameplayConfigIgnored = true;
                    context.MapResolveNote = "UM: GameplayConfigId ignored (SPEC_03 §3.9 / SPEC_04 §9.1).";
                    return true;

                case GameplayState.Dig:
                    if (!_configs.TryGetDig(row.GameplayConfigId, out var dig))
                    {
                        error = $"DigGameplayConfig '{row.GameplayConfigId}' not found.";
                        return false;
                    }

                    context.DigConfig = dig;
                    context.ResolvedMapId = dig.DigMapId;
                    if (!MapPrefabPaths.TryResolveAssetPath(dig.DigMapId, out var digPath, out var digErr))
                    {
                        error = digErr;
                        return false;
                    }

                    context.ResolvedMapPrefabPath = digPath;
                    context.MapResolveNote = $"DigMapId={dig.DigMapId} → {digPath} (Instantiate by DigStageModule).";
                    return true;

                case GameplayState.Defend:
                    // GameplayConfigId = Recommended only (ModeSelect picks actual config — D-044).
                    if (!string.IsNullOrEmpty(row.GameplayConfigId)
                        && _configs.TryGetDefend(row.GameplayConfigId, out var recommended))
                    {
                        context.DefendConfig = recommended;
                        context.ResolvedMapId = recommended.BattleMapId;
                        if (MapPrefabPaths.TryResolveAssetPath(
                                recommended.BattleMapId, out var battlePath, out _))
                        {
                            context.ResolvedMapPrefabPath = battlePath;
                        }

                        context.MapResolveNote =
                            $"ModeSelect pending; Recommended={row.GameplayConfigId} Map={recommended.BattleMapId}.";
                    }
                    else
                    {
                        context.MapResolveNote =
                            $"ModeSelect pending; Recommended '{row.GameplayConfigId}' missing or empty.";
                    }

                    return true;

                case GameplayState.PushMap:
                    // GameplayConfigId → PushMapGameplayConfig PK (direct lookup; ModeSelect Mode2 uses TryHandoffModeSelectToPushMap).
                    if (!_configs.TryGetPushMap(row.GameplayConfigId, out var pushMap))
                    {
                        error = $"PushMapGameplayConfig '{row.GameplayConfigId}' not found.";
                        return false;
                    }

                    context.PushMapConfig = pushMap;
                    context.ResolvedMapId = pushMap.MapId;
                    if (!MapPrefabPaths.TryResolveAssetPath(pushMap.MapId, out var pushMapPath, out var pushMapErr))
                    {
                        error = pushMapErr;
                        return false;
                    }

                    context.ResolvedMapPrefabPath = pushMapPath;
                    context.MapResolveNote =
                        $"PushMap MapId={pushMap.MapId} → {pushMapPath} (Instantiate by PushMapStageModule).";
                    return true;

                default:
                    error = $"Unsupported GameplayType '{row.GameplayType}'.";
                    return false;
            }
        }

        private void ExitCurrentModule()
        {
            if (_currentContext == null)
            {
                return;
            }

            if (_modules.TryGetValue(_currentContext.GameplayType, out var module))
            {
                module.Exit(_currentContext);
            }
        }

        private void StopCurrentLevelInternal(bool notifyEnded)
        {
            if (IsRunning && _currentContext != null)
            {
                ExitCurrentModule();
            }

            var hadLevel = !string.IsNullOrEmpty(ActiveLevelId);
            ActiveLevelId = null;
            _stages = new List<LevelOperationConfigRow>();
            _stageIndex = -1;
            _currentContext = null;

            if (notifyEnded && hadLevel)
            {
                LevelEnded?.Invoke("关卡已停止");
            }
        }
    }
}
