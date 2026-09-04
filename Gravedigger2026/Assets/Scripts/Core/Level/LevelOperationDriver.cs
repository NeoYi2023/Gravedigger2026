using System;
using System.Collections.Generic;
using Gravedigger2026.Core.Config;
using Gravedigger2026.Core.Rewards;
using UnityEngine;

namespace Gravedigger2026.Core.Level
{
    /// <summary>
    /// Drives Level Operation + SubLevel route graph (SPEC_03 §3.9 / D-086 / D-088). Approach A.
    /// Enter → hydrate Cleared → RouteSelect → pick option → module → clear → persist/unlock → RouteSelect or victory.
    /// </summary>
    public sealed class LevelOperationDriver
    {
        public const string DemoSampleLevelId = "Level_01";

        private readonly ConfigCsvRepository _configs;
        private readonly GameplayStateService _gameplayState;
        private readonly Dictionary<GameplayState, IStageModule> _modules =
            new Dictionary<GameplayState, IStageModule>();

        private RewardGrantService _rewardGrant;
        private LevelRouteProgressService _routeProgress;
        private List<LevelOperationConfigRow> _stages = new List<LevelOperationConfigRow>();
        private readonly HashSet<string> _clearedOptions = new HashSet<string>(StringComparer.Ordinal);
        private readonly HashSet<string> _unlockedOptions = new HashSet<string>(StringComparer.Ordinal);
        private readonly Dictionary<string, int> _optionStageById =
            new Dictionary<string, int>(StringComparer.Ordinal);
        private string _activeOptionId;
        private LevelStageContext _currentContext;
        private bool _routeSelectVisible;
        private string _justClearedOptionId;

        public event Action<LevelStageContext> StageChanged;
        public event Action<string> LevelEnded;
        public event Action<LevelRouteSnapshot> RouteChanged;

        public LevelOperationDriver(ConfigCsvRepository configs, GameplayStateService gameplayState)
        {
            _configs = configs ?? throw new ArgumentNullException(nameof(configs));
            _gameplayState = gameplayState ?? throw new ArgumentNullException(nameof(gameplayState));
        }

        public void BindRewardGrant(RewardGrantService rewardGrant)
        {
            _rewardGrant = rewardGrant;
        }

        public void BindRouteProgress(LevelRouteProgressService routeProgress)
        {
            _routeProgress = routeProgress;
        }

        /// <summary>True while a Level is open (route or running option).</summary>
        public bool IsRunning => !string.IsNullOrEmpty(ActiveLevelId);
        public bool IsOptionRunning => IsRunning && _currentContext != null && !string.IsNullOrEmpty(_activeOptionId);
        public bool IsRouteSelectVisible => IsRunning && _routeSelectVisible;
        public string ActiveLevelId { get; private set; }
        public string ActiveGameplayOptionId => _activeOptionId;
        public LevelStageContext CurrentContext => _currentContext;

        public void RegisterModule(IStageModule module)
        {
            if (module == null)
            {
                throw new ArgumentNullException(nameof(module));
            }

            _modules[module.HandledState] = module;
        }

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
            return TryEnterLevel(levelId, out error, bypassUnlockGate: false);
        }

        /// <param name="bypassUnlockGate">
        /// When true (Tools GM), skip Stage1 <c>UnlockLevelId</c> gate (SPEC_03 §3.9).
        /// </param>
        public bool TryEnterLevel(string levelId, out string error, bool bypassUnlockGate)
        {
            error = null;
            if (!EnsureConfigsLoaded())
            {
                error = _configs.LastError ?? "Config load failed.";
                return false;
            }

            if (!bypassUnlockGate && !IsLevelUnlocked(levelId))
            {
                error = $"Level '{levelId}' is locked.";
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
            _clearedOptions.Clear();
            _unlockedOptions.Clear();
            _optionStageById.Clear();
            _activeOptionId = null;
            _currentContext = null;
            _routeSelectVisible = true;

            for (var i = 0; i < _stages.Count; i++)
            {
                var stage = _stages[i];
                var ids = stage.GameplayOptionIds;
                if (ids == null)
                {
                    continue;
                }

                for (var j = 0; j < ids.Length; j++)
                {
                    var oid = ids[j];
                    if (string.IsNullOrEmpty(oid))
                    {
                        continue;
                    }

                    _optionStageById[oid] = stage.StageNumber;
                    if (i == 0)
                    {
                        _unlockedOptions.Add(oid);
                    }
                }
            }

            HydrateClearedAndDeriveUnlocked();

            if (_unlockedOptions.Count == 0 && _clearedOptions.Count == 0)
            {
                error = $"Level '{levelId}' Stage1 has no gameplay options.";
                StopCurrentLevelInternal(notifyEnded: false);
                return false;
            }

            StageChanged?.Invoke(null);
            PublishRoute();
            Debug.Log(
                $"[LevelOperationDriver] EnterLevel {levelId} ({_stages.Count} stages, {_unlockedOptions.Count} unlocked, {_clearedOptions.Count} cleared) → RouteSelect.");
            return true;
        }

        /// <summary>
        /// LevelId unlock from Stage1 <c>UnlockLevelId</c> + route Cleared (SPEC_03 §3.9).
        /// </summary>
        public bool IsLevelUnlocked(string levelId)
        {
            if (string.IsNullOrEmpty(levelId) || _configs == null || !_configs.IsLoaded)
            {
                return false;
            }

            var kind = _configs.GetLevelUnlockKind(levelId);
            if (kind == LevelUnlockKind.AlwaysUnlocked)
            {
                return true;
            }

            if (kind == LevelUnlockKind.NeverUnlockable)
            {
                return false;
            }

            var prereq = _configs.GetUnlockPrerequisiteOptionId(levelId);
            if (string.IsNullOrEmpty(prereq))
            {
                return false;
            }

            if (_routeProgress != null && _routeProgress.IsCleared(prereq))
            {
                return true;
            }

            // Same-session cleared set when already inside a level (prereq may be on another LevelId
            // and only live in progress service — prefer progress; also allow in-memory if same session).
            return _clearedOptions.Contains(prereq);
        }

        public bool TrySelectGameplayOption(string optionId, out string error)
        {
            error = null;
            if (!IsRunning)
            {
                error = "No active Level.";
                return false;
            }

            if (IsOptionRunning)
            {
                error = "An option is already running.";
                return false;
            }

            if (string.IsNullOrEmpty(optionId))
            {
                error = "Empty GameplayOptionId.";
                return false;
            }

            if (_clearedOptions.Contains(optionId))
            {
                error = $"Option '{optionId}' already cleared.";
                return false;
            }

            if (!_unlockedOptions.Contains(optionId))
            {
                error = $"Option '{optionId}' is locked.";
                return false;
            }

            if (!_configs.TryGetSubLevel(optionId, out var sub) || sub == null)
            {
                error = $"SubLevel '{optionId}' not found.";
                return false;
            }

            if (!_optionStageById.TryGetValue(optionId, out var stageNumber))
            {
                error = $"Option '{optionId}' is not mounted on this Level.";
                return false;
            }

            if (!TryBuildContext(sub, stageNumber, out var context, out error))
            {
                return false;
            }

            _routeSelectVisible = false;
            _activeOptionId = optionId;
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
            PublishRoute();
            Debug.Log(
                $"[LevelOperationDriver] Select Option={optionId} Stage={stageNumber} Type={context.GameplayType} Map={context.ResolvedMapId ?? "-"} Note={context.MapResolveNote}");
            return true;
        }

        /// <summary>
        /// Clear current option: grant Reward, unlock next, return to RouteSelect or victory.
        /// </summary>
        public bool TryAdvanceStage(out string message)
        {
            if (!IsOptionRunning)
            {
                message = "No active gameplay option.";
                return false;
            }

            var optionId = _activeOptionId;
            if (!_configs.TryGetSubLevel(optionId, out var sub) || sub == null)
            {
                message = $"SubLevel '{optionId}' missing on advance.";
                return false;
            }

            ExitCurrentModule();
            GrantOptionReward(sub);
            _clearedOptions.Add(optionId);
            PersistClearedOption(optionId);
            _activeOptionId = null;
            _currentContext = null;

            var unlockIds = ParsePipeIds(sub.UnlockNextOptionIds);
            if (unlockIds.Count == 0)
            {
                var levelId = ActiveLevelId;
                StopCurrentLevelInternal(notifyEnded: false);
                message = $"VictorySettlement（占位）— 关卡 {levelId} 完成";
                Debug.Log($"[LevelOperationDriver] {message}");
                LevelEnded?.Invoke(message);
                StageChanged?.Invoke(null);
                PublishRoute();
                return true;
            }

            for (var i = 0; i < unlockIds.Count; i++)
            {
                var nextId = unlockIds[i];
                if (!_optionStageById.ContainsKey(nextId))
                {
                    Debug.LogWarning(
                        $"[LevelOperationDriver] UnlockNext '{nextId}' not mounted on Level '{ActiveLevelId}'.");
                    continue;
                }

                _unlockedOptions.Add(nextId);
            }

            _routeSelectVisible = true;
            StageChanged?.Invoke(null);
            _justClearedOptionId = optionId;
            PublishRoute();
            _justClearedOptionId = null;
            message = $"选项 {optionId} 通关 → 路线选择（解锁 {unlockIds.Count}）";
            Debug.Log($"[LevelOperationDriver] {message}");
            return true;
        }

        public void StopCurrentLevel()
        {
            StopCurrentLevelInternal(notifyEnded: true);
            StageChanged?.Invoke(null);
            PublishRoute();
        }

        public void CompleteLevelAfterBattleSettlement()
        {
            if (!IsRunning && string.IsNullOrEmpty(ActiveLevelId))
            {
                return;
            }

            var levelId = ActiveLevelId;
            if (!string.IsNullOrEmpty(_activeOptionId)
                && _configs.TryGetSubLevel(_activeOptionId, out var sub)
                && sub != null)
            {
                GrantOptionReward(sub);
                _clearedOptions.Add(_activeOptionId);
                PersistClearedOption(_activeOptionId);
            }

            ExitCurrentModule();
            ClearLevelState();
            Debug.Log($"[LevelOperationDriver] PushMap settlement complete — Level {levelId} ended (no VictorySettlement toast).");
            StageChanged?.Invoke(null);
            PublishRoute();
        }

        public void AbortLevelAsFailure(string reason)
        {
            if (!IsRunning && string.IsNullOrEmpty(ActiveLevelId))
            {
                return;
            }

            var levelId = ActiveLevelId;
            ExitCurrentModule();
            ClearLevelState();
            var message = string.IsNullOrEmpty(reason)
                ? $"LevelFailure — 关卡 {levelId} 中止"
                : $"LevelFailure — {reason}";
            Debug.LogWarning($"[LevelOperationDriver] {message}");
            LevelEnded?.Invoke(message);
            StageChanged?.Invoke(null);
            PublishRoute();
        }

        public bool TryHandoffModeSelectToPushMap(string gameplayConfigId, out string error)
        {
            error = null;
            if (!IsOptionRunning || _currentContext == null)
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
            PublishRoute();
            Debug.Log(
                $"[LevelOperationDriver] ModeSelect handoff → PushMap Config={pushMap.GameplayConfigId} Map={pushMap.MapId} Level={_currentContext.LevelId} Stage={_currentContext.StageNumber}");
            return true;
        }

        public LevelRouteSnapshot BuildRouteSnapshot()
        {
            var snap = new LevelRouteSnapshot
            {
                LevelId = ActiveLevelId,
                LevelName = ResolveLevelName(ActiveLevelId),
                Visible = IsRouteSelectVisible,
                RouteMapAssetId = ResolveRouteMapAssetId(ActiveLevelId),
                JustClearedOptionId = _justClearedOptionId
            };

            if (string.IsNullOrEmpty(ActiveLevelId) || _stages.Count == 0)
            {
                return snap;
            }

            var stageSnaps = new List<LevelRouteStageSnapshot>(_stages.Count);
            for (var i = 0; i < _stages.Count; i++)
            {
                var stage = _stages[i];
                var optionList = new List<LevelRouteOptionSnapshot>();
                var ids = stage.GameplayOptionIds;
                if (ids != null)
                {
                    for (var j = 0; j < ids.Length; j++)
                    {
                        var oid = ids[j];
                        if (string.IsNullOrEmpty(oid) || !_configs.TryGetSubLevel(oid, out var sub) || sub == null)
                        {
                            continue;
                        }

                        var ui = LevelRouteOptionUiState.Locked;
                        if (_clearedOptions.Contains(oid))
                        {
                            ui = LevelRouteOptionUiState.Cleared;
                        }
                        else if (string.Equals(_activeOptionId, oid, StringComparison.Ordinal))
                        {
                            ui = LevelRouteOptionUiState.Running;
                        }
                        else if (_unlockedOptions.Contains(oid))
                        {
                            ui = LevelRouteOptionUiState.Selectable;
                        }

                        optionList.Add(new LevelRouteOptionSnapshot
                        {
                            GameplayOptionId = oid,
                            StageNumber = stage.StageNumber,
                            Title = sub.Title,
                            Description = sub.Description,
                            IconAssetId = sub.IconAssetId,
                            IconAssetId2 = sub.IconAssetId2,
                            TipMessages = sub.TipMessages,
                            Reward = sub.Reward,
                            UnlockNextOptionIds = sub.UnlockNextOptionIds,
                            GameplayType = sub.GameplayType,
                            UiState = ui
                        });
                    }
                }

                stageSnaps.Add(new LevelRouteStageSnapshot
                {
                    StageNumber = stage.StageNumber,
                    Options = optionList.ToArray()
                });
            }

            snap.Stages = stageSnaps.ToArray();
            return snap;
        }

        private string ResolveLevelName(string levelId)
        {
            if (string.IsNullOrEmpty(levelId) || _stages == null || _stages.Count == 0)
            {
                return string.Empty;
            }

            string first = null;
            for (var i = 0; i < _stages.Count; i++)
            {
                var name = _stages[i] != null ? _stages[i].LevelName : null;
                if (string.IsNullOrEmpty(name))
                {
                    continue;
                }

                if (first == null)
                {
                    first = name;
                    continue;
                }

                if (!string.Equals(first, name, StringComparison.Ordinal))
                {
                    Debug.LogWarning(
                        $"[LevelOperationDriver] Level '{levelId}' has conflicting LevelName '{first}' vs '{name}'; using first.");
                    break;
                }
            }

            return first ?? string.Empty;
        }

        private string ResolveRouteMapAssetId(string levelId)
        {
            if (string.IsNullOrEmpty(levelId) || _stages == null || _stages.Count == 0)
            {
                return string.Empty;
            }

            string first = null;
            for (var i = 0; i < _stages.Count; i++)
            {
                var id = _stages[i] != null ? _stages[i].RouteMapAssetId : null;
                if (string.IsNullOrEmpty(id))
                {
                    continue;
                }

                if (first == null)
                {
                    first = id;
                    continue;
                }

                if (!string.Equals(first, id, StringComparison.Ordinal))
                {
                    Debug.LogWarning(
                        $"[LevelOperationDriver] Level '{levelId}' has conflicting RouteMapAssetId '{first}' vs '{id}'; using first.");
                    break;
                }
            }

            return first ?? string.Empty;
        }

        private void GrantOptionReward(SubLevelConfigRow sub)
        {
            if (_rewardGrant == null || string.IsNullOrEmpty(sub.Reward))
            {
                return;
            }

            var entries = LootDropParser.ParseIdSemicolonCount(
                sub.Reward,
                msg => Debug.LogWarning($"[LevelOperationDriver] {msg}"));
            _rewardGrant.GrantEntries(
                entries,
                msg => Debug.Log($"[LevelOperationDriver] Reward: {msg}"),
                msg => Debug.LogWarning($"[LevelOperationDriver] {msg}"));
        }

        private bool TryBuildContext(
            SubLevelConfigRow sub,
            int stageNumber,
            out LevelStageContext context,
            out string error)
        {
            context = new LevelStageContext
            {
                LevelId = ActiveLevelId,
                StageNumber = stageNumber,
                GameplayOptionId = sub.GameplayOptionId,
                GameplayType = sub.GameplayType,
                GameplayConfigId = sub.GameplayConfigId
            };
            error = null;

            switch (sub.GameplayType)
            {
                case GameplayState.UpgradeManufacture:
                    context.GameplayConfigIgnored = true;
                    context.MapResolveNote = "UM: GameplayConfigId ignored (SPEC_03 §3.9 / SPEC_04 §9.1).";
                    return true;

                case GameplayState.AutoManufacture:
                    context.GameplayConfigIgnored = true;
                    context.MapResolveNote =
                        "AutoManufacture: GameplayConfigId ignored (SPEC_03 §3.9 / §3.15 / SPEC_04 §9.1).";
                    return true;

                case GameplayState.Shop:
                    context.GameplayConfigIgnored = true;
                    context.MapResolveNote =
                        "Shop: GameplayConfigId ignored (SPEC_03 §3.5 / §3.9 / D-075 / SPEC_04 §9.1).";
                    return true;

                case GameplayState.Dig:
                    if (!_configs.TryGetDig(sub.GameplayConfigId, out var dig))
                    {
                        error = $"DigGameplayConfig '{sub.GameplayConfigId}' not found.";
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
                    if (!string.IsNullOrEmpty(sub.GameplayConfigId)
                        && _configs.TryGetDefend(sub.GameplayConfigId, out var recommended))
                    {
                        context.DefendConfig = recommended;
                        context.ResolvedMapId = recommended.BattleMapId;
                        if (MapPrefabPaths.TryResolveAssetPath(
                                recommended.BattleMapId, out var battlePath, out _))
                        {
                            context.ResolvedMapPrefabPath = battlePath;
                        }

                        context.MapResolveNote =
                            $"ModeSelect pending; Recommended={sub.GameplayConfigId} Map={recommended.BattleMapId}.";
                    }
                    else
                    {
                        context.MapResolveNote =
                            $"ModeSelect pending; Recommended '{sub.GameplayConfigId}' missing or empty.";
                    }

                    return true;

                case GameplayState.PushMap:
                    if (!_configs.TryGetPushMap(sub.GameplayConfigId, out var pushMap))
                    {
                        error = $"PushMapGameplayConfig '{sub.GameplayConfigId}' not found.";
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

                case GameplayState.SearchExtract:
                    if (!_configs.TryGetSearchExtract(sub.GameplayConfigId, out var searchExtract))
                    {
                        error = $"SearchExtractGameplayConfig '{sub.GameplayConfigId}' not found.";
                        return false;
                    }

                    context.SearchExtractConfig = searchExtract;
                    context.GatherPointCount = sub.GatherPointCount;
                    context.GatherPointRewards = sub.GatherPointRewards ?? string.Empty;
                    context.ResolvedMapId = searchExtract.MapId;
                    if (!MapPrefabPaths.TryResolveAssetPath(
                            searchExtract.MapId, out var searchExtractPath, out var searchExtractErr))
                    {
                        error = searchExtractErr;
                        return false;
                    }

                    context.ResolvedMapPrefabPath = searchExtractPath;
                    context.MapResolveNote =
                        $"SearchExtract MapId={searchExtract.MapId} → {searchExtractPath} (Instantiate by SearchExtractStageModule).";
                    return true;

                default:
                    error = $"Unsupported GameplayType '{sub.GameplayType}'.";
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
            if (IsOptionRunning)
            {
                ExitCurrentModule();
            }

            var hadLevel = !string.IsNullOrEmpty(ActiveLevelId);
            ClearLevelState();

            if (notifyEnded && hadLevel)
            {
                LevelEnded?.Invoke("关卡已停止");
            }
        }

        private void ClearLevelState()
        {
            ActiveLevelId = null;
            _stages = new List<LevelOperationConfigRow>();
            _clearedOptions.Clear();
            _unlockedOptions.Clear();
            _optionStageById.Clear();
            _activeOptionId = null;
            _currentContext = null;
            _routeSelectVisible = false;
            _justClearedOptionId = null;
        }

        private void PublishRoute()
        {
            RouteChanged?.Invoke(BuildRouteSnapshot());
        }

        private void PersistClearedOption(string optionId)
        {
            if (_routeProgress == null || string.IsNullOrEmpty(optionId))
            {
                return;
            }

            _routeProgress.MarkCleared(optionId);
        }

        /// <summary>
        /// After Stage1 unlock seed: hydrate Cleared from save and derive further Unlocked.
        /// </summary>
        private void HydrateClearedAndDeriveUnlocked()
        {
            if (_routeProgress == null)
            {
                return;
            }

            foreach (var clearedId in _routeProgress.ClearedOptionIds)
            {
                if (string.IsNullOrEmpty(clearedId) || !_optionStageById.ContainsKey(clearedId))
                {
                    continue;
                }

                _clearedOptions.Add(clearedId);
            }

            foreach (var clearedId in _clearedOptions)
            {
                if (!_configs.TryGetSubLevel(clearedId, out var sub) || sub == null)
                {
                    continue;
                }

                var unlockIds = ParsePipeIds(sub.UnlockNextOptionIds);
                for (var i = 0; i < unlockIds.Count; i++)
                {
                    var nextId = unlockIds[i];
                    if (!_optionStageById.ContainsKey(nextId))
                    {
                        continue;
                    }

                    _unlockedOptions.Add(nextId);
                }
            }
        }

        private static List<string> ParsePipeIds(string encoded)
        {
            var result = new List<string>();
            if (string.IsNullOrWhiteSpace(encoded))
            {
                return result;
            }

            var parts = encoded.Split('|');
            for (var i = 0; i < parts.Length; i++)
            {
                var p = parts[i] != null ? parts[i].Trim() : string.Empty;
                if (p.Length > 0)
                {
                    result.Add(p);
                }
            }

            return result;
        }
    }
}
