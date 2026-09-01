using System;
using System.Collections.Generic;
using Gravedigger2026.Core;
using Gravedigger2026.Core.Audio;
using Gravedigger2026.Core.Combat;
using Gravedigger2026.Core.Config;
using Gravedigger2026.Core.Defend;
using Gravedigger2026.Core.Dig;
using Gravedigger2026.Core.Level;
using Gravedigger2026.Core.Pathing;
using Gravedigger2026.Core.PushMap;
using Gravedigger2026.Core.Rewards;
using Gravedigger2026.Core.UpgradeManufacture;
using Gravedigger2026.Core.AutoManufacture;
using Gravedigger2026.Core.ProtagonistEquipment;
using Gravedigger2026.Gameplay.Defend;
using Gravedigger2026.Gameplay.Dig;
using Gravedigger2026.Gameplay.Formation;
using Gravedigger2026.UI;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

namespace Gravedigger2026.Gameplay.PushMap
{
    /// <summary>
    /// PushMap stage presentation bridge (Approach A / PM-03–PM-09 + MassCombatPathing MP-04).
    /// Prepare reuses FormationEditorRoot; StartBattle initializes Shield/LOC. Objective chain,
    /// spawn/trap, AggroMode four-state, and PM-07 Boss clear.
    /// PM-12 (Approach B): StartBattle registers warriors (TryRegisterWarrior) and spawns
    /// register monsters (RegisterMonster) on PushMapSessionService; soldier→monster damage
    /// settles via scheme-D HitConfirm → MonsterDamageSettled (red popup/flash + provoke) /
    /// MonsterKilled(runtimeId, killerWarriorId, outgoingDamage). PM-13: monster→warrior TryApplyMonsterDamageToWarrior →
    /// WarriorDamageSettled (white popup/flash); CombatDead → PlayDie. DemoKill retired.
    /// D-071 CombatSkillIcon: SkillIconPopup / SkillPersistChanged → WarriorSkillIconHudView.
    /// VictorySettled → AddExperience → settlement UI (UI-017) → reward (UI-018) → LevelSelect;
    /// LevelFailure (Shield≤0 / loyal wipe) → settlement → LevelSelect; no Exp.
    /// CaptureLoot + DungeonUnlockIds on capture; LevelFailure does not credit Exp.
    /// PM-08: StartBattle NavMesh bake injects AirWall Not Walkable boxes (incl. 45°).
    /// MP-04: Bake AirWall → StaticBoxWalkableMask + FlowField Rebuild; advance via MassMoveScheduler
    /// (shared field + LocalDetour; no per-soldier SetDestination(Objective)).
    /// MP-05: engage/chase → AttackSlot claim + LocalDetour; slot refresh ≤50/frame; no per-frame CalculatePath.
    /// Combat camera: Runtime Ensure PushMapCamera (ortho combat pitch; Size from CombatConstantConfig).
    /// PM-09 Approach B: PushMapCameraFollowController follows CameraFollowPath max projection.
    /// PM-10: BodyRadius spawn spread + NavMeshAgent.radius for RVO.
    /// v0.66: Bake → deploy → FireStartBattleSpawns; advance does not pause on capture probe.
    /// v0.74.10: sticky target selection (view side) fixes the starvation deadlock;
    /// objective chain exhausted → FlowField redirects to BossPoint
    /// (ObjectiveArriveRadius = BossAdvanceArriveRadius).
    /// </summary>
    public sealed class PushMapStageController : MonoBehaviour
    {
        [SerializeField] private Transform _worldRoot;
        [SerializeField] private Camera _pushMapCamera;
        [SerializeField] private DefendHudView _hudView;

        private PushMapCameraFollowController _cameraFollow;
        private GameObject _resumeFollowButtonRoot;
        private FormationBondHudView _combatBondHud;

        private DefendPrefabCatalog _catalog;
        private FormationPrefabCatalog _formationCatalog;
        private ConfigCsvRepository _configs;
        private ProtagonistProgressService _progress;
        private WarriorPoolService _warriorPool;
        private BattleFormationService _formation;
        private WarehouseService _warehouse;
        private SpecialEquipSlotsService _specialEquipSlots;
        private ProtagonistEquipmentService _protagonistEquipment;
        private DungeonUnlockService _dungeonUnlocks;
        private RewardGrantService _rewardGrant;
        private Action _onVictoryAdvance;
        private Action<string> _onLevelFailure;
        private BgmService _bgm;
        private PushMapBattleSettlementView _settlementView;
        private PushMapRewardPopupView _rewardView;
        private string _pendingFailureReason;

        private PushMapSessionService _session;
        private CombatStatMulBuff _combatMagicBookBuff = CombatStatMulBuff.Identity;
        private GameObject _mapInstance;
        private FormationEditorController _formationEditor;
        private Vector2 _mapHalfExtents = new Vector2(5f, 2.5f);
        private Vector3 _mapCenter;
        private bool _running;
        private bool _driverOutcomeDispatched;

        private readonly List<ObjectivePoint> _objectives = new List<ObjectivePoint>();
        private readonly List<PushMapMonsterAgentView> _monsters = new List<PushMapMonsterAgentView>();
        private readonly List<PushMapAdvanceView> _advanceViews = new List<PushMapAdvanceView>();
        private readonly List<GameObject> _deployedViews = new List<GameObject>();
        private readonly Dictionary<string, SpawnPoint> _spawnPointsById = new Dictionary<string, SpawnPoint>(StringComparer.Ordinal);
        private readonly List<TrapZone> _trapZones = new List<TrapZone>();
        private BossPoint _bossPoint;
        private EngageZone _engageZone;
        private NavMeshDataInstance _navMeshInstance;
        private GameObject _battleProtagonistInstance;

        /// <summary>
        /// SPEC_03 §3.14 v0.74.10 Boss guidance: with the objective chain exhausted the
        /// shared field aims at the BossPoint and the Objective hold bubble tightens to
        /// this radius, so soldiers enter engage detect (≈0.38 on current configs) and
        /// convert to AttackSlot instead of holding a CaptureZone-sized ring short of it.
        /// </summary>

        private FlowFieldService _flowField;
        private StaticBoxWalkableMask _flowWalkableMask;
        private MassMoveScheduler _moveScheduler;
        private AttackSlotService _attackSlots;
        private readonly List<MassMoveSample> _moveSamples = new List<MassMoveSample>(64);
        private int _nextAdvanceMoveId;
        private int _slotGoalCursor;
        private int _lastSlotGoalRefreshCount;
        private bool _flowFieldReady;

        public void ConfigureCatalog(DefendPrefabCatalog catalog, FormationPrefabCatalog formationCatalog = null)
        {
            if (catalog != null)
            {
                _catalog = catalog;
            }

            if (formationCatalog != null)
            {
                _formationCatalog = formationCatalog;
            }
        }

        public void BindBgm(BgmService bgm)
        {
            _bgm = bgm;
        }

        public void Begin(
            LevelStageContext context,
            ConfigCsvRepository configs,
            ProtagonistProgressService progress,
            WarriorPoolService warriorPool,
            BattleFormationService formation,
            WarehouseService warehouse = null,
            SpecialEquipSlotsService specialEquipSlots = null,
            ProtagonistEquipmentService protagonistEquipment = null,
            DungeonUnlockService dungeonUnlocks = null,
            Action onVictoryAdvance = null,
            Action<string> onLevelFailure = null)
        {
            EndInternal(destroyWorld: true);

            if (context?.PushMapConfig == null)
            {
                Debug.LogError("[PushMapStageController] Missing PushMapConfig.");
                return;
            }

            _configs = configs ?? throw new ArgumentNullException(nameof(configs));
            _progress = progress ?? throw new ArgumentNullException(nameof(progress));
            _warriorPool = warriorPool ?? throw new ArgumentNullException(nameof(warriorPool));
            _formation = formation ?? throw new ArgumentNullException(nameof(formation));
            _warehouse = warehouse;
            _specialEquipSlots = specialEquipSlots;
            _protagonistEquipment = protagonistEquipment;
            _dungeonUnlocks = dungeonUnlocks;
            _combatMagicBookBuff = CombatStatMulBuff.Identity;
            _rewardGrant = new RewardGrantService(_configs, _warehouse, _specialEquipSlots, _protagonistEquipment);
            _onVictoryAdvance = onVictoryAdvance;
            _onLevelFailure = onLevelFailure;
            _driverOutcomeDispatched = false;

            if (!_configs.IsLoaded)
            {
                _configs.TryLoadAll();
            }

            if (_catalog == null || !_catalog.TryGetMap(context.PushMapConfig.MapId, out var mapPrefab) || mapPrefab == null)
            {
                Debug.LogError(
                    $"[PushMapStageController] Map prefab missing for '{context.PushMapConfig.MapId}' (bind in DefendPrefabCatalog.Maps).");
                return;
            }

            EnsureWorldRoot();
            _mapInstance = Instantiate(mapPrefab, _worldRoot);
            _mapInstance.name = context.PushMapConfig.MapId;

            var bounds = _mapInstance.GetComponent<DigMapBounds>();
            _mapHalfExtents = bounds != null ? bounds.HalfExtents : new Vector2(5f, 2.5f);
            _mapCenter = bounds != null ? bounds.Center : _mapInstance.transform.position;

            CollectObjectives();
            CollectSpawnMarkers();
            _engageZone = _mapInstance.GetComponentInChildren<EngageZone>(true);

            EnsurePushMapCamera();
            ApplyPushMapCombatCameraPose();
            if (_pushMapCamera != null)
            {
                _pushMapCamera.gameObject.SetActive(false);
            }

            _session = new PushMapSessionService();
            _session.SetMonsterWorldXZProvider(TryGetMonsterWorldXZ);
            _session.PhaseChanged += HandlePhaseChanged;
            _session.LevelFailureRequested += HandleLevelFailureRequested;
            _session.VictorySettled += HandleVictorySettled;
            _session.ObjectiveCaptured += HandleObjectiveCaptured;
            _session.CurrentObjectiveChanged += HandleCurrentObjectiveChanged;
            _session.PushMapSpawnRequested += HandlePushMapSpawnRequested;
            _session.MonsterDamageSettled += HandleMonsterDamageSettled;
            _session.MonsterKilled += HandleMonsterKilled;
            _session.MonsterEnteredCombatDead += HandleMonsterEnteredCombatDead;
            _session.MonsterReviveStarted += HandleMonsterReviveStarted;
            _session.MonsterRevived += HandleMonsterRevived;
            _session.MonsterInvincibleChanged += HandleMonsterInvincibleChanged;
            _session.WarriorDamageSettled += HandleWarriorDamageSettled;
            _session.WarriorCombatDead += HandleWarriorCombatDead;
            _session.SkillIconPopup += HandleSkillIconPopup;
            _session.SkillPersistChanged += HandleSkillPersistChanged;
            _session.BeginPrepare(context.PushMapConfig);

            PushMapBattleResultUiFactory.Ensure(transform, out _settlementView, out _rewardView);
            _pendingFailureReason = null;

            if (_hudView != null)
            {
                _hudView.StartBattleRequested += HandleStartBattleRequested;
                _hudView.Show();
                _hudView.SetPrepareVisible(false);
                _hudView.SetCombatVisible(false);
                _hudView.SetPhaseText("推图战 — Prepare");
                _hudView.SetHint("布阵后点击「开战」；击杀 BOSS 通关入账经验；护盾归零/士兵全灭失败不入账");
            }

            OpenFormationEditor();
            _session.FirePreparePreviewSpawns(_configs);
            _running = true;
        }

        public void End()
        {
            EndInternal(destroyWorld: true);
        }

        private void EndInternal(bool destroyWorld)
        {
            _running = false;
            _bgm?.Stop();
            CameraFogService.Resolve()?.SetPushMapCombatActive(false);
            CloseFormationEditor();

            if (_session != null)
            {
                _session.PhaseChanged -= HandlePhaseChanged;
                _session.LevelFailureRequested -= HandleLevelFailureRequested;
                _session.VictorySettled -= HandleVictorySettled;
                _session.ObjectiveCaptured -= HandleObjectiveCaptured;
                _session.CurrentObjectiveChanged -= HandleCurrentObjectiveChanged;
                _session.PushMapSpawnRequested -= HandlePushMapSpawnRequested;
                _session.MonsterDamageSettled -= HandleMonsterDamageSettled;
                _session.MonsterKilled -= HandleMonsterKilled;
                _session.MonsterEnteredCombatDead -= HandleMonsterEnteredCombatDead;
                _session.MonsterReviveStarted -= HandleMonsterReviveStarted;
                _session.MonsterRevived -= HandleMonsterRevived;
                _session.MonsterInvincibleChanged -= HandleMonsterInvincibleChanged;
                _session.WarriorDamageSettled -= HandleWarriorDamageSettled;
                _session.WarriorCombatDead -= HandleWarriorCombatDead;
                _session.SkillIconPopup -= HandleSkillIconPopup;
                _session.SkillPersistChanged -= HandleSkillPersistChanged;
                _session.Stop();
                _session = null;
            }

            _driverOutcomeDispatched = false;
            _dungeonUnlocks = null;

            if (_hudView != null)
            {
                _hudView.StartBattleRequested -= HandleStartBattleRequested;
                _hudView.SetPrepareVisible(false);
                _hudView.SetCombatVisible(false);
            }

            DisableCameraFollow();
            if (destroyWorld && _resumeFollowButtonRoot != null)
            {
                var canvas = _resumeFollowButtonRoot.GetComponentInParent<Canvas>();
                if (canvas != null)
                {
                    Destroy(canvas.gameObject);
                }
                else
                {
                    Destroy(_resumeFollowButtonRoot);
                }

                _resumeFollowButtonRoot = null;
            }

            ClearDeployedViews();
            ClearSpawnedMonsters();
            ClearFlowFieldPathing();
            _objectives.Clear();
            _spawnPointsById.Clear();
            _trapZones.Clear();
            _bossPoint = null;
            _engageZone = null;
            ReleaseNavMesh();

            if (destroyWorld && _battleProtagonistInstance != null)
            {
                Destroy(_battleProtagonistInstance);
                _battleProtagonistInstance = null;
            }

            if (destroyWorld && _mapInstance != null)
            {
                Destroy(_mapInstance);
                _mapInstance = null;
            }

            if (_pushMapCamera != null)
            {
                _pushMapCamera.gameObject.SetActive(false);
            }
        }

        private void HandleStartBattleRequested()
        {
            if (_session == null || _formation == null || _progress == null || _configs == null)
            {
                return;
            }

            var deployed = _formation.Entries.Count;
            var degree = _formation.ComputeLossOfControlDegree(_progress.ControlPowerCap);
            if (!_session.TryStartBattle(deployed, _progress.ProtagonistMaxHP, degree, _configs, out var error))
            {
                _hudView?.SetHint(error ?? "不可开战");
                Debug.Log($"[PushMapStage] StartBattle rejected: {error}");
                return;
            }

            CloseFormationEditor();
            EnsurePushMapCamera();
            if (_pushMapCamera != null)
            {
                _pushMapCamera.gameObject.SetActive(true);
                ApplyPushMapCombatCameraPose();
            }

            ReleaseNavMesh();
            var airWallBoxes = CollectAirWallObstacles();
            _navMeshInstance = DefendNavMeshBaker.Bake(_mapCenter, _mapHalfExtents, airWallBoxes);
            ConfigureFlowFieldPathing(airWallBoxes);

            BeginObjectiveChain();
            _combatMagicBookBuff = _specialEquipSlots != null
                ? CombatMagicBookStatMul.Aggregate(_specialEquipSlots, _configs)
                : CombatStatMulBuff.Identity;
            if (_session != null && _session.CurrentObjectiveOrder <= 0)
            {
                // No objectives authored: CurrentObjectiveChanged never fires, so aim the
                // shared field at the BossPoint right away (v0.74.10 Boss guidance).
                RebuildFlowFieldTowardCurrentObjective();
            }

            DeployCombatUnits();
            EnsureCombatBondHud();
            RefreshCombatBondHud();
            _session.EmitStartBattleSkillIcons();
            ClearSpawnedMonsters();
            _session.FireStartBattleSpawns();
            ResolveStartBattleRebelRolls();
            _session.NotifyBossPointPresence(_bossPoint != null);
            FinishCombatIntroAndEnableGameplay();
            // Fog also latched in HandlePhaseChanged(Combat).
            var fog = CameraFogService.Resolve();
            if (fog == null)
            {
                Debug.LogWarning("[PushMapStage] CameraFogService missing — DigFogCanvas will stay inactive.");
            }
            else
            {
                fog.SetPushMapCombatActive(true);
            }

            if (_hudView != null)
            {
                _hudView.SetPrepareVisible(false);
                _hudView.SetCombatVisible(true);
                _hudView.SetCombatBondHudVisible(true);
                _hudView.SetPhaseText("推图战 — Combat");
                RefreshCombatHud();
                _hudView.SetHint(
                    $"忠诚兵推进/占领；近身普攻击杀 BOSS 通关（Exp={_session.Config?.StageExpReward ?? 0}）；" +
                    "护盾归零失败不入账；拖拽镜头可手动平移，点「恢复跟随」回默认");
            }

            Debug.Log(
                $"[PushMapStage] Combat entered (intro latch) — PendingBoss={_session.PendingBossCount} " +
                $"BossPoint={(_bossPoint != null ? "yes" : "no")}.");
        }

        private void ResolveStartBattleRebelRolls()
        {
            if (_session.LockedLossOfControlDegree <= 0f || _session.LockedLossOfControlTierId <= 0)
            {
                Debug.Log("[PushMapSession] LossOfControl Degree≤0 — no rebel rolls.");
                return;
            }

            var entries = _formation.Entries;
            for (var i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                if (entry == null || string.IsNullOrEmpty(entry.WarriorId))
                {
                    continue;
                }

                if (!_warriorPool.TryGet(entry.WarriorId, out var warrior) || warrior == null)
                {
                    continue;
                }

                var raceBonus = 0f;
                var gemBonus = 0f;
                var skillBonus = SoldierSkillGrant.SumLossOfControlChanceBonus(warrior.SoldierSkills, _configs);
                if (_configs != null)
                {
                    if (!string.IsNullOrEmpty(warrior.RaceId)
                        && _configs.TryGetRace(warrior.RaceId, out var raceRow)
                        && raceRow != null)
                    {
                        raceBonus = raceRow.LossOfControlChanceBonus;
                    }

                    if (warrior.GemIds != null)
                    {
                        for (var g = 0; g < warrior.GemIds.Count; g++)
                        {
                            if (_configs.TryGetGem(warrior.GemIds[g], out var gemRow) && gemRow != null)
                            {
                                gemBonus += gemRow.LossOfControlChanceBonus;
                            }
                        }
                    }
                }

                var chance = LossOfControlMath.ComputeFinalLossChance(
                    _session.LockedTierChance, raceBonus, gemBonus, skillBonus);
                var roll = UnityEngine.Random.value;
                var rebel = roll < chance;
                if (rebel)
                {
                    var advance = FindAdvanceView(warrior.Id);
                    if (advance != null)
                    {
                        advance.SetRebel(true);
                    }

                    _session.SetWarriorRebel(warrior.Id, true);
                }
                Debug.Log(
                    $"[PushMapSession] RebelRoll {warrior.Id} chance={chance:0.###} roll={roll:0.###} " +
                    $"→ {(rebel ? "REBEL" : "loyal")} (Tier={_session.LockedTierChance:0.###} Race={raceBonus:0.###} " +
                    $"Gem={gemBonus:0.###} Skill={skillBonus:0.###})");
            }
        }

        private void Update()
        {
            if (!_running || _session == null || _session.Phase != PushMapPhase.Combat)
            {
                return;
            }

            if (_session.IsCombatIntroActive)
            {
                return;
            }

            var hasLoyalInZone = HasLoyalSoldierInCurrentZone();
            _session.TickCapture(hasLoyalInZone);
            _session.TickSkillCooldowns(Time.deltaTime);
            _session.TickCombatStatus(Time.deltaTime);
            TickMassCombatPathing();
            PollTrapEntry();
            PollPassiveProvocation();
        }

        /// <summary>
        /// PM-12: soldier HitConfirm settled damage on a monster — provoke it (PM-06 real-hit
        /// channel), flash it red, and pop a red `-N` (font 12) overhead DamagePopup.
        /// </summary>
        private void HandleMonsterDamageSettled(string runtimeId, float damage)
        {
            var monster = FindMonsterView(runtimeId);
            if (monster == null)
            {
                return;
            }

            monster.NotifyProvoked();
            var flash = monster.GetComponent<HitFlashView>();
            if (flash != null)
            {
                flash.Play(HitFlashView.MonsterFlashColor);
            }

            SpawnDamagePopup(monster.transform.position, damage, DamagePopupStyle.Monster);
        }

        /// <summary>
        /// PM-13: monster AttackPower settled on a warrior — white HitFlash + white `-N` (font 12).
        /// CombatDead presentation (PlayDie) is owned by PushMapAdvanceView.
        /// </summary>
        private void HandleWarriorDamageSettled(string warriorId, float damage)
        {
            var soldier = FindAdvanceView(warriorId);
            if (soldier == null)
            {
                return;
            }

            var flash = soldier.GetComponent<HitFlashView>();
            if (flash != null)
            {
                flash.Play(HitFlashView.SoldierFlashColor);
            }

            SpawnDamagePopup(soldier.transform.position, damage, DamagePopupStyle.Soldier);
        }

        private void HandleSkillIconPopup(string warriorId, string skillId)
        {
            EnsureSkillIconHud(FindAdvanceView(warriorId))?.PlayPopup(skillId);
        }

        private void HandleSkillPersistChanged(string warriorId, string skillId, bool on)
        {
            EnsureSkillIconHud(FindAdvanceView(warriorId))?.SetPersist(skillId, on);
        }

        /// <summary>D-071: CombatDead immediately clears that soldier's CombatSkillIcon HUD.</summary>
        private void HandleWarriorCombatDead(string warriorId)
        {
            FindAdvanceView(warriorId)?.GetComponent<WarriorSkillIconHudView>()?.ClearAll();
        }

        private void SpawnDamagePopup(Vector3 worldPos, float damage, DamagePopupStyle style)
        {
            var prefab = _catalog != null ? _catalog.DamagePopupPrefab : null;
            if (prefab == null)
            {
                Debug.LogWarning("[PushMapStage] DamagePopup prefab missing in DefendPrefabCatalog — popup skipped.");
                return;
            }

            DamagePopupView.Spawn(prefab, _worldRoot, worldPos, damage, style);
        }

        /// <summary>PM-12: monster RemainingHp≤0 — final death; Boss clear when applicable.</summary>
        private void HandleMonsterKilled(string runtimeId, string killerWarriorId, float outgoingDamage, string deathTag)
        {
            ApplyMonsterDeathPresentation(runtimeId, killerWarriorId, outgoingDamage, notifyBoss: true, deathTag);
        }

        /// <summary>MonsterCombatDead — may revive; no kill count / Boss.</summary>
        private void HandleMonsterEnteredCombatDead(
            string runtimeId,
            string killerWarriorId,
            float outgoingDamage,
            string deathTag)
        {
            ApplyMonsterDeathPresentation(
                runtimeId,
                killerWarriorId,
                outgoingDamage,
                notifyBoss: false,
                deathTag,
                fakeDeathCorpse: true);
        }

        private void HandleMonsterReviveStarted(string runtimeId, float reviveAnimSeconds)
        {
            var monster = FindMonsterView(runtimeId);
            monster?.NotifyReviveStarted(reviveAnimSeconds);
        }

        private void HandleMonsterRevived(string runtimeId)
        {
            var monster = FindMonsterView(runtimeId);
            if (monster == null)
            {
                return;
            }

            float? postReviveAlertRadius = null;
            if (_session != null
                && _session.TryGetMonster(runtimeId, out var state)
                && state != null
                && state.PostReviveAlertRadiusApplied)
            {
                postReviveAlertRadius = state.RuntimeAlertRadius;
            }

            monster.NotifyRevived(postReviveAlertRadius);
            if (_session == null || !_session.IsMonsterInvincible(runtimeId))
            {
                monster.NotifyPostReviveInvincibleEnded();
            }
        }

        private void HandleMonsterInvincibleChanged(string runtimeId, string skillId, bool on)
        {
            if (on)
            {
                return;
            }

            FindMonsterView(runtimeId)?.NotifyPostReviveInvincibleEnded();
        }

        private void ApplyMonsterDeathPresentation(
            string runtimeId,
            string killerWarriorId,
            float outgoingDamage,
            bool notifyBoss,
            string deathTag = "",
            bool fakeDeathCorpse = false)
        {
            var monster = FindMonsterView(runtimeId);
            if (monster == null)
            {
                return;
            }

            var isBoss = notifyBoss && monster.IsBoss;
            Vector3? killerPos = null;
            if (!string.IsNullOrEmpty(killerWarriorId))
            {
                var killer = FindAdvanceView(killerWarriorId);
                if (killer != null)
                {
                    killerPos = killer.transform.position;
                }
            }

            var maxHp = 0f;
            if (_session != null && _session.TryGetMonster(runtimeId, out var state) && state != null)
            {
                maxHp = state.MaxHp;
            }

            var isCorpseSmashKill = string.Equals(deathTag, "CorpseSmash", StringComparison.Ordinal);
            var distance = isCorpseSmashKill
                ? 0f
                : MonsterDeathPresentation.ComputeKnockbackDistance(maxHp, outgoingDamage);
            monster.NotifyKilled(killerPos, distance, killerWarriorId, outgoingDamage, fakeDeathCorpse);
            if (isBoss)
            {
                _session?.TryNotifyBossKilled();
            }
        }

        private PushMapMonsterAgentView FindMonsterView(string runtimeId)
        {
            if (string.IsNullOrEmpty(runtimeId))
            {
                return null;
            }

            for (var i = 0; i < _monsters.Count; i++)
            {
                var m = _monsters[i];
                if (m != null && string.Equals(m.RuntimeTargetId, runtimeId, StringComparison.Ordinal))
                {
                    return m;
                }
            }

            return null;
        }

        /// <summary>D-083: living monster targets for in-flight / landing corpse smash sweep.</summary>
        private void EnumerateLivingMonstersForCorpseSmash(Action<string, Vector2, float> visit)
        {
            if (visit == null || _session == null)
            {
                return;
            }

            for (var i = 0; i < _monsters.Count; i++)
            {
                var monster = _monsters[i];
                if (monster == null)
                {
                    continue;
                }

                var runtimeId = monster.RuntimeTargetId;
                if (string.IsNullOrEmpty(runtimeId) || !_session.IsMonsterTargetable(runtimeId))
                {
                    continue;
                }

                var p = monster.transform.position;
                visit(runtimeId, new Vector2(p.x, p.z), monster.BodyRadius);
            }
        }

        // PM-06 Demo contract: a loyal soldier's first entry into a passive monster's
        // AttackRange stands in for "soldier attacks first" → NotifyProvoked.
        // Prefer real HitConfirm (HandleMonsterDamageSettled → NotifyProvoked); this is fallback.
        private void PollPassiveProvocation()
        {
            if (_monsters.Count == 0 || _advanceViews.Count == 0)
            {
                return;
            }

            for (var m = 0; m < _monsters.Count; m++)
            {
                var monster = _monsters[m];
                if (monster == null || !monster.IsAlive || !monster.IsPassive || monster.IsBoss)
                {
                    continue;
                }

                var range = monster.AttackRange;
                if (range <= 0f)
                {
                    continue;
                }

                for (var i = 0; i < _advanceViews.Count; i++)
                {
                    var soldier = _advanceViews[i];
                    if (soldier == null || soldier.IsRebel || !soldier.IsCombatActive)
                    {
                        continue;
                    }

                    if (CombatReach.IsInAttackRange(
                            Vector3.Distance(monster.transform.position, soldier.transform.position),
                            range,
                            monster.BodyRadius,
                            soldier.AgentRadius))
                    {
                        monster.NotifyProvoked();
                        break;
                    }
                }
            }
        }

        private void CollectObjectives()
        {
            _objectives.Clear();
            if (_mapInstance == null)
            {
                return;
            }

            var found = _mapInstance.GetComponentsInChildren<ObjectivePoint>(true);
            if (found != null)
            {
                _objectives.AddRange(found);
            }
            _objectives.Sort((a, b) => a.ObjectiveOrder.CompareTo(b.ObjectiveOrder));
        }

        /// <summary>
        /// PM-08: map AirWall → Not Walkable Box sources for StartBattle bake (incl. Y 45°).
        /// </summary>
        private List<DefendNavMeshBaker.NavMeshBoxObstacle> CollectAirWallObstacles()
        {
            var boxes = new List<DefendNavMeshBaker.NavMeshBoxObstacle>();
            if (_mapInstance == null)
            {
                return boxes;
            }

            var walls = _mapInstance.GetComponentsInChildren<AirWall>(true);
            if (walls == null || walls.Length == 0)
            {
                return boxes;
            }

            for (var i = 0; i < walls.Length; i++)
            {
                var wall = walls[i];
                if (wall == null)
                {
                    continue;
                }

                boxes.Add(new DefendNavMeshBaker.NavMeshBoxObstacle(
                    wall.transform.position,
                    wall.FullSize,
                    wall.transform.rotation));
            }

            Debug.Log($"[PushMapStage] AirWall bake obstacles={boxes.Count}.");
            return boxes;
        }

        private void CollectSpawnMarkers()
        {
            _spawnPointsById.Clear();
            _trapZones.Clear();
            _bossPoint = null;
            if (_mapInstance == null)
            {
                return;
            }

            var spawnPoints = _mapInstance.GetComponentsInChildren<SpawnPoint>(true);
            for (var i = 0; i < spawnPoints.Length; i++)
            {
                var sp = spawnPoints[i];
                if (sp == null)
                {
                    continue;
                }

                var id = sp.SpawnPointId;
                if (string.IsNullOrEmpty(id))
                {
                    continue;
                }

                _spawnPointsById[id] = sp;
            }

            var traps = _mapInstance.GetComponentsInChildren<TrapZone>(true);
            for (var i = 0; i < traps.Length; i++)
            {
                if (traps[i] != null)
                {
                    _trapZones.Add(traps[i]);
                }
            }

            _bossPoint = _mapInstance.GetComponentInChildren<BossPoint>(true);
            Debug.Log(
                $"[PushMapStage] Markers collected — SpawnPoints={_spawnPointsById.Count} " +
                $"Traps={_trapZones.Count} BossPoint={(_bossPoint != null ? "yes" : "no")}");
        }

        private void BeginObjectiveChain()
        {
            if (_session == null)
            {
                return;
            }

            var orders = new List<int>(_objectives.Count);
            for (var i = 0; i < _objectives.Count; i++)
            {
                orders.Add(_objectives[i].ObjectiveOrder);
            }
            _session.TryBeginObjectiveChain(orders);
        }

        private ObjectivePoint ResolveCurrentObjective()
        {
            if (_session == null || _session.CurrentObjectiveOrder <= 0)
            {
                return null;
            }

            for (var i = 0; i < _objectives.Count; i++)
            {
                if (_objectives[i].ObjectiveOrder == _session.CurrentObjectiveOrder)
                {
                    return _objectives[i];
                }
            }
            return null;
        }

        private CaptureZone ResolveCurrentCaptureZone()
        {
            var objective = ResolveCurrentObjective();
            return objective != null ? objective.CaptureZone : null;
        }

        /// <summary>
        /// v0.74.10 Boss guidance (SPEC_03 §3.14): chain exhausted (all objectives captured
        /// or none authored) while Combat runs and a BossPoint exists → shared advance goal
        /// is the BossPoint instead of a CaptureZone.
        /// </summary>
        private bool IsBossGuidanceActive =>
            _session != null &&
            _session.Phase == PushMapPhase.Combat &&
            _session.CurrentObjectiveOrder <= 0 &&
            _bossPoint != null;

        private bool HasLoyalSoldierInCurrentZone()
        {
            var zone = ResolveCurrentCaptureZone();
            if (zone == null)
            {
                return false;
            }

            for (var i = 0; i < _advanceViews.Count; i++)
            {
                var soldier = _advanceViews[i];
                if (soldier == null || soldier.IsRebel || !soldier.IsCombatActive)
                {
                    continue;
                }

                if (zone.ContainsXZ(soldier.transform.position))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>PM-05: first loyal-soldier entry into a TrapZone notifies rules (once per point).</summary>
        private void PollTrapEntry()
        {
            if (_trapZones.Count == 0 || _advanceViews.Count == 0)
            {
                return;
            }

            for (var t = 0; t < _trapZones.Count; t++)
            {
                var trap = _trapZones[t];
                if (trap == null)
                {
                    continue;
                }

                for (var i = 0; i < _advanceViews.Count; i++)
                {
                    var soldier = _advanceViews[i];
                    if (soldier == null || soldier.IsRebel || !soldier.IsCombatActive)
                    {
                        continue;
                    }

                    if (trap.ContainsXZ(soldier.transform.position))
                    {
                        _session.TryNotifyTrapEnter(trap.TrapZoneId);
                        break; // rules de-dupes per point; no need to check more soldiers this frame
                    }
                }
            }
        }

        private void HandlePushMapSpawnRequested(PushMapSpawnRequest request)
        {
            if (request == null || _configs == null || _catalog == null || _worldRoot == null)
            {
                return;
            }

            if (!_configs.TryGetMonster(request.MonsterId, out var monsterRow) || monsterRow == null)
            {
                Debug.LogWarning($"[PushMapStage] MonsterConfig missing: {request.MonsterId} — skip spawn.");
                return;
            }

            var basePos = ResolveSpawnPosition(request);
            var protagonistTf = _battleProtagonistInstance != null ? _battleProtagonistInstance.transform : null;
            var bodyRadius = Mathf.Max(0.05f, monsterRow.BodyRadius);

            var occupied = new List<PushMapSpawnSpread.Footprint>(_monsters.Count);
            for (var m = 0; m < _monsters.Count; m++)
            {
                var existing = _monsters[m];
                if (existing == null || !existing.IsAlive)
                {
                    continue;
                }

                occupied.Add(new PushMapSpawnSpread.Footprint(
                    existing.transform.position,
                    existing.BodyRadius));
            }

            var spreadPositions = new List<Vector3>(request.SpawnCount);
            PushMapSpawnSpread.ComputePositions(
                basePos,
                request.SpawnCount,
                bodyRadius,
                occupied,
                spreadPositions);

            for (var i = 0; i < request.SpawnCount; i++)
            {
                var pos = i < spreadPositions.Count ? spreadPositions[i] : basePos;

                var pickedModelId = monsterRow.PickSpawnModelId();
                if (string.IsNullOrEmpty(pickedModelId))
                {
                    Debug.LogWarning(
                        $"[PushMapStage] Monster ModelId pool empty for {monsterRow.MonsterId} — skip spawn.");
                    continue;
                }

                _catalog.TryGetMonsterModel(pickedModelId, out var modelPrefab);
                if (modelPrefab == null)
                {
                    Debug.LogWarning(
                        $"[PushMapStage] Monster prefab missing: Assets/Prefabs/Defend/Monsters/{pickedModelId}.prefab — runtime temp cube.");
                }

                GameObject go;
                if (modelPrefab != null)
                {
                    go = Instantiate(modelPrefab, _worldRoot);
                }
                else
                {
                    go = CreateTempMonsterVisual(pickedModelId);
                    go.transform.SetParent(_worldRoot, false);
                }

                go.name = $"Monster_{monsterRow.MonsterId}_{_monsters.Count}";
                go.transform.position = pos;

                var view = go.GetComponent<PushMapMonsterAgentView>();
                if (view == null)
                {
                    view = go.AddComponent<PushMapMonsterAgentView>();
                }

                var runtimeId = go.name;
                view.Bind(
                    monsterRow,
                    protagonistTf,
                    () => _advanceViews,
                    tag => _session?.ApplyShieldHit(tag),
                    1f,
                    _attackSlots,
                    _moveScheduler,
                    ++_nextAdvanceMoveId,
                    (monsterRuntimeId, warriorId, attackPower) =>
                        _session != null &&
                        _session.TryApplyMonsterDamageToWarrior(monsterRuntimeId, warriorId, attackPower),
                    () => _session != null && _session.IsMonsterStunned(runtimeId),
                    () => _session != null ? _session.GetMonsterSlowMoveMul(runtimeId) : 1f,
                    () => _session != null ? _session.GetMonsterSlowAttackMul(runtimeId) : 1f);
                view.ApplySpawnInitialFacing(PushMapSpawnFacing.ResolveDirIndex(request.InitialFacing));
                if (request.IsBoss)
                {
                    view.MarkAsBoss(true);
                }

                view.SetCorpseSmashBridge(
                    (corpseRuntimeId, killerId, killerOutgoing, targetRuntimeId) =>
                        _session != null &&
                        _session.TryApplyCorpseSmashDamage(
                            corpseRuntimeId,
                            killerId,
                            killerOutgoing,
                            targetRuntimeId),
                    EnumerateLivingMonstersForCorpseSmash);

                view.SetReviveCallbacks(
                    () => _session?.TryNotifyMonsterDeathPresentationComplete(runtimeId),
                    () => _session?.TryNotifyMonsterReviveAnimComplete(runtimeId));

                var isPreparePreview = request.Trigger == PushMapSpawnTrigger.PreparePreview;
                if (!isPreparePreview)
                {
                    // PM-12: register monster HP on the session (kill = RemainingHp≤0 via HitConfirm).
                    _session?.RegisterMonster(view.RuntimeTargetId, monsterRow.MonsterId, monsterRow.MaxHP);
                }
                else
                {
                    // Prepare has no NavMesh bake yet — keep visual Idle only.
                    var agent = go.GetComponent<NavMeshAgent>();
                    if (agent != null)
                    {
                        agent.enabled = false;
                    }
                }

                if (go.GetComponent<HitFlashView>() == null)
                {
                    go.AddComponent<HitFlashView>();
                }

                _monsters.Add(view);
                view.SetCombatGameplayEnabled(
                    !isPreparePreview
                    && (_session == null || _session.IsCombatGameplayActive));
            }

            Debug.Log(
                $"[PushMapStage] Spawned {request.SpawnCount}x {request.MonsterId} at '{request.SpawnPointId}' " +
                $"({request.Trigger}; Boss={request.IsBoss}).");
        }

        private Vector3 ResolveSpawnPosition(PushMapSpawnRequest request)
        {
            if (request.IsBoss && _bossPoint != null)
            {
                return _bossPoint.transform.position;
            }

            if (!string.IsNullOrEmpty(request.SpawnPointId)
                && _spawnPointsById.TryGetValue(request.SpawnPointId, out var sp)
                && sp != null)
            {
                return sp.transform.position;
            }

            Debug.LogWarning(
                $"[PushMapStage] Spawn position unresolved for point '{request.SpawnPointId}' " +
                $"(Boss={request.IsBoss}) — fallback map edge.");
            return _mapCenter + new Vector3(_mapHalfExtents.x * 0.9f, 0f, 0f);
        }

        private static GameObject CreateTempMonsterVisual(string modelId)
        {
            var root = new GameObject(string.IsNullOrEmpty(modelId) ? "MonsterTemp" : modelId);
            var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.name = "Body";
            body.transform.SetParent(root.transform, false);
            body.transform.localScale = new Vector3(0.9f, 1.1f, 0.9f);
            body.transform.localPosition = new Vector3(0f, 0.55f, 0f);
            UnityEngine.Object.Destroy(body.GetComponent<Collider>());
            var renderer = body.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.material.color = new Color(0.75f, 0.25f, 0.25f);
            }

            return root;
        }

        private void ClearSpawnedMonsters()
        {
            for (var i = 0; i < _monsters.Count; i++)
            {
                if (_monsters[i] != null)
                {
                    Destroy(_monsters[i].gameObject);
                }
            }
            _monsters.Clear();
        }

        private void DeployCombatUnits()
        {
            ClearDeployedViews();

            var protagonistPrefab = _catalog != null ? _catalog.BattleProtagonistPrefab : null;
            if (protagonistPrefab == null)
            {
                Debug.LogError("[PushMapStage] BattleProtagonist prefab missing.");
            }
            else
            {
                _battleProtagonistInstance = Instantiate(protagonistPrefab, _worldRoot);
                _battleProtagonistInstance.name = "BattleProtagonist";
                _battleProtagonistInstance.transform.position = _mapCenter;
                _deployedViews.Add(_battleProtagonistInstance);
            }

            if (_formation == null || _warriorPool == null || _catalog == null)
            {
                return;
            }

            var entries = _formation.Entries;
            for (var i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                if (entry == null || string.IsNullOrEmpty(entry.WarriorId))
                {
                    continue;
                }

                if (!_warriorPool.TryGet(entry.WarriorId, out var warrior) || warrior == null)
                {
                    Debug.LogWarning($"[PushMapStage] Warrior '{entry.WarriorId}' missing from pool — skip deploy.");
                    continue;
                }

                if (!_catalog.TryGetWarriorAppearance(warrior.AppearanceId, out var appearancePrefab)
                    || appearancePrefab == null)
                {
                    Debug.LogWarning(
                        $"[PushMapStage] Appearance Prefab missing: Assets/Prefabs/Defend/Warriors/{warrior.AppearanceId}.prefab");
                    continue;
                }

                var go = Instantiate(appearancePrefab, _worldRoot);
                go.name = $"Warrior_{warrior.Id}";
                go.transform.position = new Vector3(
                    _mapCenter.x + entry.PositionX,
                    _mapCenter.y,
                    _mapCenter.z + entry.PositionZ);
                WarriorAllIn1StyleView.ApplyTo(go, _catalog != null ? _catalog.VisualStyleCatalog : null, warrior);

                ClassConfigRow classRow = null;
                if (_configs != null && !string.IsNullOrEmpty(warrior.ClassId))
                {
                    _configs.TryGetClass(warrior.ClassId, out classRow);
                }

                var attackRange = 1f;
                if (classRow != null && classRow.AttackRange > 0.05f)
                {
                    attackRange = classRow.AttackRange;
                }

                var modelScale = WarriorVisualModelScale.Resolve(warrior);
                attackRange *= modelScale;

                // PM-12: register combat stats (HP / NormalAttackPower / AttackSpeed / windup /
                // projectile) on the PushMap session before the view goes live.
                if (_session != null &&
                    !_session.TryRegisterWarrior(warrior, classRow, _combatMagicBookBuff, out _, out var regError))
                {
                    Debug.LogWarning($"[PushMapStage] RegisterWarrior failed: {regError}");
                    Destroy(go);
                    continue;
                }

                _deployedViews.Add(go);

                var advance = go.GetComponent<PushMapAdvanceView>();
                if (advance == null)
                {
                    advance = go.AddComponent<PushMapAdvanceView>();
                }

                var bodyRadius = BodyAppearanceConfigRow.DefaultBodyRadius;
                var pushCoefficient = BodyAppearanceConfigRow.DefaultPushCoefficient;
                var repulsionScale = BodyAppearanceConfigRow.DefaultRepulsionScale;
                var facingYawFlip = false;
                if (_configs != null &&
                    _configs.TryGetAppearance(warrior.AppearanceId, out var appearanceRow) &&
                    appearanceRow != null)
                {
                    bodyRadius = appearanceRow.BodyRadius;
                    pushCoefficient = appearanceRow.PushCoefficient;
                    repulsionScale = appearanceRow.RepulsionScale;
                    facingYawFlip = appearanceRow.FacingYawFlip == 1;
                }

                bodyRadius *= WarriorVisualModelScale.Resolve(warrior);

                var moveSpeed = 1.5f;
                if (_session != null &&
                    _session.TryGetWarrior(warrior.Id, out var combatState) &&
                    combatState != null)
                {
                    moveSpeed = Mathf.Max(0.1f, combatState.MoveSpeed);
                }

                var chaseMult = classRow != null
                    ? classRow.ChaseMoveSpeedMult
                    : ClassConfigRow.DefaultChaseMoveSpeedMult;

                _nextAdvanceMoveId++;
                advance.Bind(
                    _moveScheduler,
                    _nextAdvanceMoveId,
                    moveSpeed,
                    ProvidePushMapMonsters,
                    attackRange,
                    warrior.AttackMode,
                    warrior.Id,
                    _attackSlots,
                    _session,
                    _catalog != null ? _catalog.ProjectilePrefab : null,
                    _worldRoot,
                    _catalog,
                    bodyRadius,
                    facingYawFlip,
                    pushCoefficient,
                    repulsionScale,
                    chaseMult);
                if (go.GetComponent<HitFlashView>() == null)
                {
                    go.AddComponent<HitFlashView>();
                }

                EnsureSkillIconHud(advance);
                _advanceViews.Add(advance);
            }

            Debug.Log($"[PushMapStage] Deployed protagonist + {_advanceViews.Count} loyal-capable warriors.");
        }

        private IReadOnlyList<PushMapMonsterAgentView> ProvidePushMapMonsters()
        {
            return _monsters;
        }

        /// <summary>D-073 SE-03: world XZ for AOE skill effects (Session rules layer).</summary>
        private Vector2? TryGetMonsterWorldXZ(string runtimeId)
        {
            if (string.IsNullOrEmpty(runtimeId))
            {
                return null;
            }

            for (var i = 0; i < _monsters.Count; i++)
            {
                var monster = _monsters[i];
                if (monster == null || !string.Equals(monster.RuntimeTargetId, runtimeId, StringComparison.Ordinal))
                {
                    continue;
                }

                // Hit-center may resolve on a just-killed View before NotifyKilled despawn.
                var p = monster.transform.position;
                return new Vector2(p.x, p.z);
            }

            return null;
        }

        private WarriorSkillIconHudView EnsureSkillIconHud(PushMapAdvanceView soldier)
        {
            if (soldier == null)
            {
                return null;
            }

            var hud = soldier.GetComponent<WarriorSkillIconHudView>();
            if (hud == null)
            {
                hud = soldier.gameObject.AddComponent<WarriorSkillIconHudView>();
            }

            hud.Bind(_pushMapCamera, _catalog != null ? _catalog.SkillIconHudPrefab : null);
            return hud;
        }

        private PushMapAdvanceView FindAdvanceView(string warriorId)
        {
            if (string.IsNullOrEmpty(warriorId))
            {
                return null;
            }

            for (var i = 0; i < _advanceViews.Count; i++)
            {
                var view = _advanceViews[i];
                if (view != null &&
                    string.Equals(view.AttackerId, warriorId, StringComparison.Ordinal))
                {
                    return view;
                }
            }

            return null;
        }

        private void ClearDeployedViews()
        {
            for (var i = 0; i < _deployedViews.Count; i++)
            {
                if (_deployedViews[i] != null)
                {
                    Destroy(_deployedViews[i]);
                }
            }
            _deployedViews.Clear();
            _advanceViews.Clear();
            _moveScheduler?.Clear();
            _attackSlots?.Clear();
            _battleProtagonistInstance = null;
        }

        private void EnsurePathingServices()
        {
            _flowField ??= new FlowFieldService();
            _flowWalkableMask ??= new StaticBoxWalkableMask();
            _moveScheduler ??= new MassMoveScheduler();
            _attackSlots ??= new AttackSlotService();
        }

        private void ConfigureFlowFieldPathing(IReadOnlyList<DefendNavMeshBaker.NavMeshBoxObstacle> airWallBoxes)
        {
            EnsurePathingServices();
            _flowWalkableMask.Clear();
            _moveScheduler.Clear();
            _attackSlots.Clear();
            _nextAdvanceMoveId = 0;
            _slotGoalCursor = 0;
            _lastSlotGoalRefreshCount = 0;
            _flowFieldReady = false;

            if (airWallBoxes != null)
            {
                for (var i = 0; i < airWallBoxes.Count; i++)
                {
                    var box = airWallBoxes[i];
                    _flowWalkableMask.AddBox(
                        StaticBoxWalkableMask.BoxObstacle.FromFullSize(box.Center, box.Size, box.Rotation));
                }
            }

            _flowField.Configure(_mapCenter, _mapHalfExtents, FlowFieldService.DefaultCellSize);
            _moveScheduler.BindFlowField(_flowField);
            _flowFieldReady = true;
            Debug.Log(
                $"[PushMapStage] FlowField configured — cell={_flowField.CellSize} " +
                $"grid={_flowField.Cols}x{_flowField.Rows} airWallMaskBoxes={_flowWalkableMask.BoxCount}.");
        }

        private void ClearFlowFieldPathing()
        {
            _flowFieldReady = false;
            _moveScheduler?.Clear();
            _attackSlots?.Clear();
            _flowWalkableMask?.Clear();
            _moveSamples.Clear();
            _nextAdvanceMoveId = 0;
            _slotGoalCursor = 0;
            _lastSlotGoalRefreshCount = 0;
        }

        private void RebuildFlowFieldTowardCurrentObjective()
        {
            if (!_flowFieldReady || _flowField == null || _flowWalkableMask == null)
            {
                return;
            }

            var objective = ResolveCurrentObjective();
            Vector3 goalWorld;
            string goalLabel;
            if (objective != null)
            {
                goalWorld = objective.transform.position;
                goalLabel = $"Order={_session?.CurrentObjectiveOrder ?? 0}";
            }
            else if (IsBossGuidanceActive)
            {
                // Chain exhausted → guide the squad onto the BossPoint (v0.74.10).
                goalWorld = _bossPoint.transform.position;
                goalLabel = "BossPoint(chain exhausted)";
            }
            else
            {
                Debug.Log("[PushMapStage] FlowField Rebuild skipped — no CurrentObjective.");
                return;
            }

            var before = _flowField.RebuildCount;
            _flowField.Rebuild(goalWorld, _flowWalkableMask);
            Debug.Log(
                $"[PushMapStage] FlowField Rebuild shared field — {goalLabel} " +
                $"goal={goalWorld} RebuildCount={_flowField.RebuildCount} (was {before}) " +
                $"maskBoxes={_flowWalkableMask.BoxCount}.");
        }

        /// <summary>
        /// MP-05: budgeted AttackSlot claim/release + MassMoveScheduler steer (≤50 each, round-robin).
        /// </summary>
        private void TickMassCombatPathing()
        {
            if (_moveScheduler == null)
            {
                return;
            }

            var zone = ResolveCurrentCaptureZone();
            _moveScheduler.SetObjectiveArriveRadius(
                zone != null
                    ? zone.Radius
                    : IsBossGuidanceActive
                        ? CombatRuntimeTuning.BossAdvanceArriveRadius
                        : MassMoveScheduler.DefaultObjectiveArriveRadius);

            TickAttackSlotGoals();

            _moveSamples.Clear();
            for (var i = 0; i < _advanceViews.Count; i++)
            {
                var view = _advanceViews[i];
                if (view != null)
                {
                    _moveSamples.Add(view.BuildSample());
                }
            }

            for (var i = 0; i < _monsters.Count; i++)
            {
                var monster = _monsters[i];
                if (monster != null && monster.IsAlive && !monster.IsStationary && monster.MoveId != 0)
                {
                    _moveSamples.Add(monster.BuildSample());
                }
            }

            _moveScheduler.Tick(_moveSamples, Time.deltaTime);
        }

        private void TickAttackSlotGoals()
        {
            if (_attackSlots == null || _moveScheduler == null)
            {
                return;
            }

            // Build round-robin roster: loyal soldiers then chase-capable monsters.
            var rosterCount = _advanceViews.Count + _monsters.Count;
            if (rosterCount <= 0)
            {
                _lastSlotGoalRefreshCount = 0;
                return;
            }

            var budget = Mathf.Min(MassMoveScheduler.MaxRecalcPerFrame, rosterCount);
            var refreshed = 0;
            for (var n = 0; n < budget; n++)
            {
                if (_slotGoalCursor >= rosterCount)
                {
                    _slotGoalCursor = 0;
                }

                if (_slotGoalCursor < _advanceViews.Count)
                {
                    RefreshSoldierSlotGoal(_advanceViews[_slotGoalCursor]);
                }
                else
                {
                    var mi = _slotGoalCursor - _advanceViews.Count;
                    if (mi >= 0 && mi < _monsters.Count)
                    {
                        _monsters[mi]?.TryRefreshChaseGoal(_attackSlots, _moveScheduler);
                    }
                }

                _slotGoalCursor++;
                refreshed++;
            }

            _lastSlotGoalRefreshCount = refreshed;
        }

        private void RefreshSoldierSlotGoal(PushMapAdvanceView soldier)
        {
            if (soldier == null ||
                soldier.IsRebel ||
                !soldier.IsCombatActive ||
                _moveScheduler == null ||
                _attackSlots == null)
            {
                return;
            }

            if (!soldier.TryGetEngageMonster(out var monster) || monster == null)
            {
                _attackSlots.Release(soldier.AttackerId);
                _moveScheduler.SetPaused(soldier.MoveId, false);
                _moveScheduler.SetGoal(soldier.MoveId, GoalKind.Objective);
                return;
            }

            var targetBody = monster.BodyRadius;
            var distXZ = CombatReach.DistanceXZ(soldier.transform.position, monster.transform.position);
            var inMelee = CombatReach.IsInAttackRange(
                distXZ,
                soldier.AttackRange,
                soldier.AgentRadius,
                targetBody);

            // SC-03: melee chase → Surround gap claim (B+); ranged → Chase (full ring).
            var claimed = _attackSlots.TryClaim(
                soldier.AttackerId,
                monster.RuntimeTargetId,
                soldier.AttackRange,
                monster.transform.position,
                out var slotPos,
                soldier.AttackMode,
                soldier.transform.position,
                targetBody,
                soldier.AgentRadius,
                CombatMoveModePolicy.SurroundFor(GoalKind.AttackSlot, soldier.AttackMode));

            if (inMelee)
            {
                // Monster parity (SPEC_03 §3.12 v0.82.57): already in AttackRange → hold and
                // swing. Do not keep seeking a farther ring slot (inside-ring walk-away).
                var hold = claimed
                    ? CombatReach.ChaseDestinationXZ(
                        soldier.transform.position,
                        monster.transform.position,
                        slotPos,
                        soldier.AttackRange,
                        soldier.AgentRadius,
                        targetBody,
                        MassMoveScheduler.ArriveEpsilon)
                    : new Vector2(soldier.transform.position.x, soldier.transform.position.z);
                _moveScheduler.SetGoal(soldier.MoveId, GoalKind.AttackSlot, hold);
                _moveScheduler.SetPaused(soldier.MoveId, true);
                return;
            }

            if (!claimed)
            {
                // No free slot: keep Objective FlowField (do not hard-freeze). Overflow soldiers
                // continue advance / LocalDetour around the ring until a slot frees or the
                // monster dies to an ally's HitConfirm (PM-12).
                _moveScheduler.SetPaused(soldier.MoveId, false);
                _moveScheduler.SetGoal(soldier.MoveId, GoalKind.Objective);
                return;
            }

            var dest = CombatReach.ChaseDestinationXZ(
                soldier.transform.position,
                monster.transform.position,
                slotPos,
                soldier.AttackRange,
                soldier.AgentRadius,
                targetBody,
                MassMoveScheduler.ArriveEpsilon);
            _moveScheduler.SetPaused(soldier.MoveId, false);
            _moveScheduler.SetGoal(soldier.MoveId, GoalKind.AttackSlot, dest);
        }

        private void ReleaseNavMesh()
        {
            if (_navMeshInstance.valid)
            {
                NavMesh.RemoveNavMeshData(_navMeshInstance);
                _navMeshInstance = default;
            }
        }

        private void HandleObjectiveCaptured(int order)
        {
            CreditCaptureRewards(order);
            RefreshCombatHud();
        }

        private void CreditCaptureRewards(int order)
        {
            var loot = _session?.Config?.CaptureLoot;
            var unlockIds = _session?.Config?.DungeonUnlockIds;
            Debug.Log(
                $"[PushMapStage] Objective {order} captured — granting CaptureLoot='{loot}' Unlock='{unlockIds}' (no Exp).");

            if (_rewardGrant != null && !string.IsNullOrEmpty(loot))
            {
                var entries = LootDropParser.ParseIdSemicolonCount(
                    loot,
                    msg => Debug.LogWarning($"[PushMapStage] {msg}"));
                var granted = _rewardGrant.GrantEntries(
                    entries,
                    msg => Debug.Log($"[PushMapStage] {msg}"),
                    msg => Debug.LogWarning($"[PushMapStage] {msg}"));
                _session?.RecordCaptureLoot(granted);
            }

            _dungeonUnlocks?.UnlockEncoded(unlockIds);
        }

        private void HandleCurrentObjectiveChanged(int newOrder)
        {
            RebuildFlowFieldTowardCurrentObjective();
            RefreshCombatHud();
        }

        private void RefreshCombatHud()
        {
            if (_hudView == null || _session == null)
            {
                return;
            }

            _hudView.SetCombatStatus(
                $"Shield {_session.Shield}/{_session.ShieldCap} · Degree={_session.LockedLossOfControlDegree:0.##} " +
                $"Tier={_session.LockedLossOfControlTierId} · 当前目标={_session.CurrentObjectiveOrder} · BossPending={_session.PendingBossCount}");
        }

        private void HandleVictorySettled(long stageExp)
        {
            if (!_running && _driverOutcomeDispatched)
            {
                return;
            }

            _running = false;
            CameraFogService.Resolve()?.SetPushMapCombatActive(false);
            DisableCameraFollow();
            WriteDungeonUnlocksOnClear();

            var before = _progress != null ? _progress.LifetimeExperience : 0L;
            var levels = _progress != null ? _progress.AddExperience(stageExp) : 0;
            var after = _progress != null ? _progress.LifetimeExperience : 0L;
            if (_hudView != null)
            {
                _hudView.SetPhaseText("推图战 — Ended");
                _hudView.SetHint($"BOSS 通关：+{stageExp} Exp（{before}→{after}）升{levels}级 → 战斗结算");
            }

            Debug.Log(
                $"[PushMapStage] Victory Exp +{stageExp} Lifetime={after} Level={_progress?.Level} (+{levels})");

            ShowSettlementPanel(isVictory: true);
        }

        private void HandlePhaseChanged(PushMapPhase phase)
        {
            var fog = CameraFogService.Resolve();
            if (phase == PushMapPhase.Combat)
            {
                _bgm?.Play(BgmContext.Combat);
                fog?.SetPushMapCombatActive(true);
            }
            else
            {
                _bgm?.Stop();
                fog?.SetPushMapCombatActive(false);
            }
        }

        private void HandleLevelFailureRequested()
        {
            if (!_running && _driverOutcomeDispatched)
            {
                return;
            }

            _running = false;
            CameraFogService.Resolve()?.SetPushMapCombatActive(false);
            DisableCameraFollow();
            _pendingFailureReason = "PushMap LevelFailure";
            if (_hudView != null)
            {
                _hudView.SetPhaseText("推图战 — Ended");
                _hudView.SetHint("LevelFailure — 不入账本阶段经验 → 战斗结算");
            }

            Debug.LogWarning("[PushMapStage] LevelFailure — no stage Exp credited; show settlement.");
            ShowSettlementPanel(isVictory: false);
        }

        private void ShowSettlementPanel(bool isVictory)
        {
            if (_settlementView == null)
            {
                PushMapBattleResultUiFactory.Ensure(transform, out _settlementView, out _rewardView);
            }

            var elapsed = _session != null ? _session.CombatElapsedSeconds : 0f;
            var kills = _session != null ? _session.MonstersKilled : 0;
            if (_settlementView != null)
            {
                _settlementView.Show(isVictory, elapsed, kills, () => HandleSettlementContinue(isVictory));
            }
            else
            {
                HandleSettlementContinue(isVictory);
            }
        }

        private void HandleSettlementContinue(bool isVictory)
        {
            if (isVictory)
            {
                ShowRewardPopupThenComplete();
                return;
            }

            DispatchFailureToDriver();
        }

        private void ShowRewardPopupThenComplete()
        {
            if (_rewardView == null)
            {
                PushMapBattleResultUiFactory.Ensure(transform, out _settlementView, out _rewardView);
            }

            var body = BuildRewardBodyText();
            if (_rewardView != null)
            {
                _rewardView.Show(body, DispatchVictoryToDriver);
            }
            else
            {
                DispatchVictoryToDriver();
            }
        }

        private string BuildRewardBodyText()
        {
            var sb = new System.Text.StringBuilder();
            var exp = _session != null ? _session.StageExpCredited : 0L;
            sb.AppendLine($"经验 × {exp}");
            if (_session != null && _session.CaptureLootLedger != null && _session.CaptureLootLedger.Count > 0)
            {
                foreach (var pair in _session.CaptureLootLedger)
                {
                    if (string.Equals(pair.Key, LootDropParser.SpiritId, StringComparison.Ordinal))
                    {
                        sb.AppendLine($"精魂 × {pair.Value}");
                    }
                    else
                    {
                        var displayName = pair.Key;
                        if (_configs != null &&
                            _configs.TryGetItemCatalog(pair.Key, out var item) &&
                            item != null &&
                            !string.IsNullOrEmpty(item.DisplayName))
                        {
                            displayName = item.DisplayName;
                        }

                        sb.AppendLine($"{displayName} × {pair.Value}");
                    }
                }
            }
            else
            {
                sb.AppendLine("（无占领掉落）");
            }

            return sb.ToString().TrimEnd();
        }

        private void DispatchVictoryToDriver()
        {
            if (_driverOutcomeDispatched)
            {
                return;
            }

            _driverOutcomeDispatched = true;
            _onVictoryAdvance?.Invoke();
        }

        private void DispatchFailureToDriver()
        {
            if (_driverOutcomeDispatched)
            {
                return;
            }

            _driverOutcomeDispatched = true;
            _onLevelFailure?.Invoke(
                string.IsNullOrEmpty(_pendingFailureReason) ? "PushMap LevelFailure" : _pendingFailureReason);
        }

        private void WriteDungeonUnlocksOnClear()
        {
            var unlockIds = _session?.Config?.DungeonUnlockIds;
            if (string.IsNullOrEmpty(unlockIds))
            {
                return;
            }

            Debug.Log($"[PushMapStage] Boss-clear DungeonUnlockIds='{unlockIds}'");
            _dungeonUnlocks?.UnlockEncoded(unlockIds);
        }

        private void OpenFormationEditor()
        {
            CloseFormationEditor();
            var mode = _formation != null ? _formation.BoundCampaignMode : CampaignMode.Mode1;
            var rootPrefab = _formationCatalog != null ? _formationCatalog.ResolveEditorRoot(mode) : null;
            if (rootPrefab == null)
            {
                Debug.LogError("[PushMapStage] FormationEditorRoot missing — falling back to HUD StartBattle only.");
                if (_hudView != null)
                {
                    _hudView.SetPrepareVisible(true);
                }

                EnsurePushMapCamera();
                if (_pushMapCamera != null)
                {
                    _pushMapCamera.gameObject.SetActive(true);
                    var cam = _configs != null
                        ? _configs.GetCameraPresentationConstants()
                        : CameraPresentationConstants.SafetyDefaults;
                    cam.ApplyTopDownPose(_pushMapCamera, _mapCenter, cam.PushMapPrepareOrthoSize);
                    ApplyCombatCameraTransparencySort(_pushMapCamera);
                }

                return;
            }

            var instance = Instantiate(rootPrefab, transform);
            instance.name = mode == CampaignMode.Mode2
                ? "FormationEditorRoot_Mode2(Clone)"
                : "FormationEditorRoot(Clone)";
            _formationEditor = instance.GetComponent<FormationEditorController>();
            if (_formationEditor == null)
            {
                _formationEditor = instance.AddComponent<FormationEditorController>();
            }

            _formationEditor.StartBattleRequested += HandleStartBattleRequested;
            _formationEditor.Begin(
                FormationEditorMode.PushMapPrepare,
                _catalog,
                _warriorPool,
                _formation,
                _progress,
                _configs,
                null,
                _mapInstance);
        }

        private void CloseFormationEditor()
        {
            if (_formationEditor == null)
            {
                return;
            }

            _formationEditor.StartBattleRequested -= HandleStartBattleRequested;
            _formationEditor.End();
            Destroy(_formationEditor.gameObject);
            _formationEditor = null;
        }

        private void EnsureWorldRoot()
        {
            if (_worldRoot == null)
            {
                var root = new GameObject("PushMapWorldRoot");
                root.transform.SetParent(transform, false);
                _worldRoot = root.transform;
            }
        }

        /// <summary>
        /// Runtime StageRoot has no Prefab camera; create PushMapCamera matching DefendCamera contract.
        /// </summary>
        private void EnsurePushMapCamera()
        {
            if (_pushMapCamera != null)
            {
                EnsureCameraFollowComponent();
                return;
            }

            var camGo = new GameObject("PushMapCamera", typeof(Camera));
            camGo.transform.SetParent(transform, false);
            camGo.SetActive(false);
            _pushMapCamera = camGo.GetComponent<Camera>();
            _pushMapCamera.orthographic = true;
            _pushMapCamera.clearFlags = CameraClearFlags.SolidColor;
            _pushMapCamera.backgroundColor = new Color(0.12f, 0.14f, 0.16f, 1f);
            _pushMapCamera.depth = 5;
            var cam = _configs != null
                ? _configs.GetCameraPresentationConstants()
                : CameraPresentationConstants.SafetyDefaults;
            _pushMapCamera.nearClipPlane = cam.NearClip;
            _pushMapCamera.farClipPlane = cam.FarClip;
            EnsureCameraFollowComponent();
        }

        private void EnsureCameraFollowComponent()
        {
            if (_pushMapCamera == null)
            {
                return;
            }

            _cameraFollow = _pushMapCamera.GetComponent<PushMapCameraFollowController>();
            if (_cameraFollow == null)
            {
                _cameraFollow = _pushMapCamera.gameObject.AddComponent<PushMapCameraFollowController>();
            }
        }

        private void FinishCombatIntroAndEnableGameplay()
        {
            _session?.EndCombatIntro();
            SetCombatUnitsGameplayEnabled(true);
            ResumeAllMassMoves();
            EnableCameraFollowForCombat();
            RefreshCombatHud();
            Debug.Log("[PushMapStage] Combat gameplay active (no StartBattle intro).");
        }

        private void ResumeAllMassMoves()
        {
            if (_moveScheduler == null)
            {
                return;
            }

            for (var i = 0; i < _advanceViews.Count; i++)
            {
                var soldier = _advanceViews[i];
                if (soldier == null || soldier.MoveId == 0 || soldier.IsRebel || !soldier.IsCombatActive)
                {
                    continue;
                }

                _moveScheduler.SetPaused(soldier.MoveId, false);
            }

            for (var i = 0; i < _monsters.Count; i++)
            {
                var monster = _monsters[i];
                if (monster == null || monster.MoveId == 0 || !monster.IsAlive || monster.IsStationary)
                {
                    continue;
                }

                _moveScheduler.SetPaused(monster.MoveId, false);
            }
        }

        private void SetCombatUnitsGameplayEnabled(bool enabled)
        {
            for (var i = 0; i < _monsters.Count; i++)
            {
                _monsters[i]?.SetCombatGameplayEnabled(enabled);
            }
        }

        private void EnableCameraFollowForCombat()
        {
            EnsurePushMapCamera();
            EnsureResumeFollowButton();
            if (_cameraFollow == null)
            {
                return;
            }

            var cameraPath = _mapInstance != null
                ? _mapInstance.GetComponentInChildren<PushMapCameraPath>(true)
                : null;
            if (cameraPath != null && !cameraPath.HasBakedPath)
            {
                if (!cameraPath.TryBake(out var bakeError))
                {
                    Debug.LogWarning($"[PushMapStage] CameraFollowPath bake failed: {bakeError}");
                }
            }

            _cameraFollow.Bind(_pushMapCamera, _advanceViews, ResolveCurrentObjective, cameraPath);
            _cameraFollow.ApplyPresentationConstants(
                _configs != null
                    ? _configs.GetCameraPresentationConstants()
                    : CameraPresentationConstants.SafetyDefaults);
            _cameraFollow.EnableForCombat();
        }

        private void DisableCameraFollow()
        {
            if (_cameraFollow != null)
            {
                _cameraFollow.Disable();
            }

            if (_resumeFollowButtonRoot != null)
            {
                _resumeFollowButtonRoot.SetActive(false);
            }
        }

        private void RefreshCombatBondHud()
        {
            EnsureCombatBondHud();
            if (_combatBondHud == null || _formation == null || _warriorPool == null || _configs == null)
            {
                return;
            }

            var evaluated = FormationBondEvaluator.Evaluate(_formation, _warriorPool, _configs);
            _combatBondHud.BindServices(null, null, _configs);
            _combatBondHud.SetSnapshot(evaluated);
            _combatBondHud.gameObject.SetActive(true);
        }

        private void EnsureCombatBondHud()
        {
            if (_combatBondHud != null)
            {
                return;
            }

            if (_hudView != null && _hudView.CombatBondHud != null)
            {
                _combatBondHud = _hudView.CombatBondHud;
                return;
            }

            _combatBondHud = FormationBondHudRuntimeFactory.Create(transform, sortingOrder: 65);
        }

        private void EnsureResumeFollowButton()
        {
            if (_resumeFollowButtonRoot != null)
            {
                if (_cameraFollow != null)
                {
                    var existing = _resumeFollowButtonRoot.GetComponentInChildren<Button>(true);
                    _cameraFollow.BindResumeButton(_resumeFollowButtonRoot, existing);
                }

                return;
            }

            var canvasGo = new GameObject(
                "PushMapResumeFollowCanvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 80;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);

            var buttonGo = new GameObject("ResumeFollowButton", typeof(RectTransform), typeof(Image), typeof(Button));
            buttonGo.transform.SetParent(canvasGo.transform, false);
            var rt = buttonGo.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.1f);
            rt.anchorMax = new Vector2(0.5f, 0.1f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(220f, 56f);
            buttonGo.GetComponent<Image>().color = new Color(0.28f, 0.42f, 0.55f, 0.95f);

            var labelGo = new GameObject("Label", typeof(RectTransform), typeof(Text));
            labelGo.transform.SetParent(buttonGo.transform, false);
            var labelRt = labelGo.GetComponent<RectTransform>();
            labelRt.anchorMin = Vector2.zero;
            labelRt.anchorMax = Vector2.one;
            labelRt.offsetMin = Vector2.zero;
            labelRt.offsetMax = Vector2.zero;
            var label = labelGo.GetComponent<Text>();
            label.text = "恢复跟随";
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Color.white;
            label.fontSize = 22;
            label.font = Resources.GetBuiltinResource<Font>("Arial.ttf");

            _resumeFollowButtonRoot = buttonGo;
            buttonGo.SetActive(false);

            var button = buttonGo.GetComponent<Button>();
            if (_cameraFollow != null)
            {
                _cameraFollow.BindResumeButton(_resumeFollowButtonRoot, button);
            }
        }

        /// <summary>
        /// Oblique combat pose; Size from CombatConstantConfig PushMapCameraOrthoSize.
        /// </summary>
        private void ApplyPushMapCombatCameraPose()
        {
            if (_pushMapCamera == null)
            {
                return;
            }

            var cam = _configs != null
                ? _configs.GetCameraPresentationConstants()
                : CameraPresentationConstants.SafetyDefaults;
            cam.ApplyCombatCameraPose(_pushMapCamera, _mapCenter, cam.PushMapOrthoSize);
            ApplyCombatCameraTransparencySort(_pushMapCamera);
        }

        private static void ApplyCombatCameraTransparencySort(Camera camera)
        {
            if (camera == null)
            {
                return;
            }

            // SPEC_04 §15.2: same-order character sprites draw far-to-near along world
            // +Z — lower on screen (smaller Z) occludes higher.
            camera.transparencySortMode = TransparencySortMode.CustomAxis;
            camera.transparencySortAxis = Vector3.forward;
        }

    }
}
