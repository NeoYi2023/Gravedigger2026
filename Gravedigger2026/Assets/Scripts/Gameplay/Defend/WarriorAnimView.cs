using System.Collections.Generic;
using Gravedigger2026.Core.Config;
using UnityEngine;

namespace Gravedigger2026.Gameplay.Defend
{
    /// <summary>
    /// Character presentation for Creator bake Animator: move / Attack1 / Die / DirIndex (SPEC_04 §15.5).
    /// Used by soldiers and monsters (Defend / PushMap).
    /// IdleBT/RunBT BlendTrees blend on float Direction; DirIndex drives discrete Attack transitions.
    /// After asset normalize, Direction thresholds match DirIndex order — always write the same value.
    /// SetMoving(true) may interrupt attack into RunBT (soldiers) or walk/run gait (monster pools, v0.83.35)
    /// when move-target XZ distance &gt; 0.4 (v0.75.35).
    /// SetFacing applies 8-dir hysteresis + min dwell (v0.75.21).
    /// Move facing uses MassMove LastDesired; PlayAttack freezes DirIndex until attack ends (v0.83.31).
    /// Optional FacingYawFlip applies (dirIndex+4)%8 before write.
    /// Monsters may inject NormalAttackAnims/WalkAnims/RunAnims pools (v0.83.34); soldiers leave unset.
    /// Death: latch last non-null Die sprite + tiered corpse presentation (RGB/alpha) + CorpseSortingOrder=100 + disable Animator.
    /// </summary>
    public sealed class WarriorAnimView : MonoBehaviour
    {
        private const string DirIndexParam = "DirIndex";
        private const string DirectionParam = "Direction";
        private const string IdleStateName = "IdleBT";
        private const string RunStateName = "RunBT";
        private const string WalkStateName = "WalkBT";
        private const string IsRunParam = "IsRun";
        private const string IsWalkParam = "IsWalk";
        private const string DefaultAttackBase = "Attack1";
        private const string DieClipPrefix = "Die";
        private const string Die2ClipPrefix = "Die2";
        private const int SpriteSortingOrder = 200;
        /// <summary>SPEC_04 §15.2/§15.5: below living combat band 200 (soldiers/monsters/protagonist).</summary>
        private const int CorpseSortingOrder = 100;

        /// <summary>
        /// Creator Die clips end with a null sprite key (visual despawn). Latch the last
        /// non-null frame instead of normalizedTime≈1 / Play(...,1).
        /// </summary>
        private const float DieLatchNormalizedTime = 0.92f;

        /// <summary>SPEC_04 §15.5: RGB multiply on corpse sprites. ← CombatConstantConfig.</summary>
        public static float CorpseDarkenMul => CombatRuntimeTuning.DeathCorpseDarkenMul;

        /// <summary>PushMap fake-death corpse RGB multiply. ← CombatConstantConfig.</summary>
        public static float FakeDeathCorpseDarkenMul => CombatRuntimeTuning.DeathFakeDeathCorpseDarkenMul;

        /// <summary>Defend corpses: alpha after RGB darken. ← CombatConstantConfig.</summary>
        public static float DefendCorpseAlphaMul => CombatRuntimeTuning.DeathDefendCorpseAlphaMul;

        /// <summary>Fallback if Die clip never enters after PlayDie.</summary>
        private const float DieLatchFallbackSeconds = 2f;

        /// <summary>SPEC_04 §15.5: keep DirIndex until raw angle leaves sector center by more than 22.5°+this.</summary>
        public const float FacingHysteresisDegrees = 12f;

        /// <summary>SPEC_04 §15.5: minimum seconds between two DirIndex switches.</summary>
        public const float FacingSwitchMinDwellSeconds = 0.12f;

        /// <summary>
        /// SPEC_04 §15.5 v0.75.35: force Attack→Run only when planar distance to move target
        /// exceeds this (near-target nudges must not chop Attack1).
        /// </summary>
        public const float AttackInterruptMinMoveTargetDistance = 0.4f;

        /// <summary>SPEC_04 §15.5: grace so Attack1 clip can enter after Play/Trigger.</summary>
        private const float AttackFacingLockGraceSeconds = 0.08f;

        // DirIndex (0E 1W 2S 3N 4NE 5NW 6SE 7SW) → quantization sector of DirIndexFromXZ.
        private static readonly int[] DirIndexToSector = { 2, 6, 4, 0, 1, 7, 3, 5 };

        private static readonly string[] LocomotionBools =
        {
            "IsRun", "IsWalk", "IsStrafeLeft", "IsStrafeRight", "IsRunBackwards",
            "IsCrouching", "IsMounted", "UseIdle2", "UseIdle3", "UseIdle4"
        };

        [SerializeField] private Animator _animator;
        [Tooltip("View-layer normal-attack Trigger (SPEC_04 §15.5). Rules must not hardcode this.")]
        [SerializeField] private string _attackTriggerParam = "Attack1";
        [Tooltip("View-layer death Trigger (SPEC_04 §15.5).")]
        [SerializeField] private string _dieTriggerParam = "Die";
        [Tooltip("View-layer revive reverse-play Trigger (SPEC_04 §15.5 D-074); optional.")]
        [SerializeField] private string _die2TriggerParam = "Die2";
        [Tooltip("View-layer taunt Trigger (SPEC_04 §15.5 UI-016 card).")]
        [SerializeField] private string _tauntTriggerParam = "Taunt";
        [SerializeField] private int _defaultDirIndex = 2;

        private int _dirIndexHash;
        private int _directionHash;
        private int _attackTriggerHash;
        private int _dieTriggerHash;
        private int _die2TriggerHash;
        private int _tauntTriggerHash;
        private int _isRunHash;
        private int _isWalkHash;
        private bool _hasDirIndex;
        private bool _hasDirection;
        private bool _hasAttackTrigger;
        private bool _hasDieTrigger;
        private bool _hasDie2Trigger;
        private bool _hasTauntTrigger;
        private bool _hasIsRun;
        private bool _hasIsWalk;
        private bool _facingYawFlip;
        private int _facingDirIndex = -1;
        private float _facingSwitchTimer;
        private bool _facingLockedForAttack;
        private float _attackFacingLockArmedAt;
        private bool _dead;
        private bool _dieLatched;
        private bool _enteredDieClip;
        private bool _moving;
        /// <summary>Monster gait: true=run anim, false=walk (SPEC_04 §15.5 v0.83.35).</summary>
        private bool _usingRun;
        /// <summary>True after a force Attack→move this move bout; cleared when near or stop.</summary>
        private bool _forceInterruptedWhileMoving;
        private float _dieStartedAt;
        private float _lastGoodDieNormalizedTime;
        private int _dieStateFullPathHash;
        private bool _reviving;
        private float _reviveStartedAt;
        private float _reviveDuration;
        private float _reviveStartNormalizedTime;
        private bool _corpseDarkened;
        private float _corpseDarkenMul = CombatConstantKeys.Safety.DeathCorpseDarkenMul;
        private float _corpseAlphaMul = 1f;
        private Dictionary<SpriteRenderer, Color> _spriteOriginals;
        private Dictionary<SpriteRenderer, Sprite> _lastNonNullDieSprites;

        /// <summary>When true, use MonsterConfig anim pools (SPEC_04 §15.5 v0.83.34).</summary>
        private bool _useMonsterAnimPools;
        private string[] _attackAnimPool;
        private string[] _walkAnimPool;
        private string[] _runAnimPool;
        private string _activeWalkState = WalkStateName;
        private string _activeRunState = RunStateName;
        private string _activeAttackBase = DefaultAttackBase;
        private int _activeAttackTriggerHash;
        private bool _hasActiveAttackTrigger;

        private void Awake()
        {
            EnsureAnimator();
            CacheParamHashes();
            ApplySortingOrder();
            CacheSpriteOriginals();
            ResetFacingStabilizerState();
        }

        private void Update()
        {
            TickAttackFacingLock();

            if (_reviving)
            {
                TickReviveAnim();
                return;
            }

            if (!_dead || _dieLatched || _animator == null)
            {
                return;
            }

            TickDieLatch();
        }

        public bool IsDieLatched => _dieLatched;

        public bool IsReviveAnimating => _reviving;

        /// <summary>
        /// SPEC_04 §15.5: FacingYawFlip 1 → (dirIndex+4)%8 before writing Animator params.
        /// </summary>
        public void SetFacingYawFlip(bool flip)
        {
            _facingYawFlip = flip;
        }

        /// <summary>
        /// SPEC_04 §15.5 v0.83.34: inject MonsterConfig anim pools (soldiers must not call).
        /// Empty pipes fall back to Attack1 / WalkBT / RunBT.
        /// </summary>
        public void ConfigureMonsterAnimPools(string normalAttackPipe, string walkPipe, string runPipe)
        {
            _useMonsterAnimPools = true;
            _attackAnimPool = ParseAnimPool(normalAttackPipe, DefaultAttackBase);
            _walkAnimPool = ParseAnimPool(walkPipe, WalkStateName);
            _runAnimPool = ParseAnimPool(runPipe, RunStateName);
            ResampleLocomotionAnims();
        }

        /// <summary>
        /// SPEC_04 §15.5: pick walk/run once from pools (Bind + post-revive). No-op without monster pools.
        /// </summary>
        public void ResampleLocomotionAnims()
        {
            if (!_useMonsterAnimPools)
            {
                return;
            }

            _activeWalkState = PickFromPool(_walkAnimPool, WalkStateName);
            _activeRunState = PickFromPool(_runAnimPool, RunStateName);
        }

        public void ResetToIdle()
        {
            EnsureAnimator();
            CacheParamHashes();
            ApplySortingOrder();
            CacheSpriteOriginals();
            _dead = false;
            _dieLatched = false;
            _enteredDieClip = false;
            _moving = false;
            _usingRun = false;
            _forceInterruptedWhileMoving = false;
            _dieStartedAt = 0f;
            _lastGoodDieNormalizedTime = 0f;
            _dieStateFullPathHash = 0;
            _lastNonNullDieSprites = null;
            _reviving = false;
            ResetFacingStabilizerState();
            ClearAttackFacingLock();
            if (_animator == null)
            {
                return;
            }

            _animator.enabled = true;
            _animator.speed = 1f;
            RestoreSpriteColors();
            _corpseDarkened = false;
            _corpseDarkenMul = CombatRuntimeTuning.DeathCorpseDarkenMul;
            _corpseAlphaMul = 1f;
            ClearLocomotionBools();
            _facingDirIndex = _defaultDirIndex;
            SetDirIndexValue(_defaultDirIndex);
            _animator.Play(IdleStateName, 0, 0f);
            _animator.Update(0f);
        }

        /// <summary>Revive reverse-play finished — restore locomotion; true-death darken kept until invincible ends.</summary>
        public void FinishReviveToIdle()
        {
            EnsureAnimator();
            CacheParamHashes();
            ApplySortingOrder();
            _dead = false;
            _dieLatched = false;
            _enteredDieClip = false;
            _moving = false;
            _usingRun = false;
            _forceInterruptedWhileMoving = false;
            _dieStartedAt = 0f;
            _lastGoodDieNormalizedTime = 0f;
            _dieStateFullPathHash = 0;
            _lastNonNullDieSprites = null;
            _reviving = false;
            ClearAttackFacingLock();
            if (_animator == null)
            {
                return;
            }

            _animator.enabled = true;
            _animator.speed = 1f;
            ResetDieTriggers();
            if (_hasAttackTrigger)
            {
                _animator.ResetTrigger(_attackTriggerHash);
            }

            ClearLocomotionBools();
            // Keep revive ForceSetFacing DirIndex (do not reset to default S).
            var dir = _facingDirIndex >= 0 ? _facingDirIndex : _defaultDirIndex;
            SetDirIndexValue(dir);
            _animator.Play(IdleStateName, 0, 0f);
            _animator.Update(0f);
        }

        /// <summary>
        /// Idempotent: clear death/revive gates; re-sync DirIndex when logical facing is known (D-074).
        /// </summary>
        public void EnsureLocomotionReady()
        {
            _dead = false;
            _reviving = false;
            _dieLatched = false;
            _facingSwitchTimer = 0f;
            ClearAttackFacingLock();

            if (_animator == null)
            {
                return;
            }

            EnsureAnimator();
            CacheParamHashes();
            _animator.enabled = true;
            _animator.speed = 1f;
            ResetDieTriggers();
            if (_facingDirIndex >= 0)
            {
                SetDirIndexValue(_facingDirIndex);
                FlushAnimatorParams();
            }
        }

        /// <summary>
        /// Write DirIndex immediately (no hysteresis / dwell). Allowed while dead/reviving
        /// so Die2/Die reverse-play and post-revive Attack1_* pick the retarget facing.
        /// Living units ignore this while AttackFacingLock is active (SPEC_04 §15.5 v0.83.31).
        /// Always flushes Animator even when the logical index is unchanged (resync).
        /// </summary>
        public void ForceSetFacing(Vector3 worldDirXZ)
        {
            if (_animator == null || (!_hasDirIndex && !_hasDirection))
            {
                return;
            }

            if (_facingLockedForAttack && !_dead && !_reviving)
            {
                return;
            }

            worldDirXZ.y = 0f;
            if (worldDirXZ.sqrMagnitude < 0.0001f)
            {
                return;
            }

            var next = DirIndexFromXZ(worldDirXZ);
            _facingDirIndex = next;
            _facingSwitchTimer = 0f;
            SetDirIndexValue(next);
            FlushAnimatorParams();
        }

        /// <summary>Clear post-death corpse presentation (RGB/alpha); restores cached sprite colors.</summary>
        public void ClearCorpseDarken()
        {
            RestoreSpriteColors();
            _corpseDarkened = false;
        }

        /// <summary>Reverse latched die clip over durationSeconds, then FinishReviveToIdle.</summary>
        public void PlayReviveFromDeath(float durationSeconds)
        {
            if (!_dieLatched || durationSeconds <= 0f)
            {
                ResetToIdle();
                return;
            }

            _reviving = true;
            _reviveDuration = durationSeconds;
            _reviveStartedAt = Time.time;

            if (_animator == null)
            {
                _reviving = false;
                ResetToIdle();
                return;
            }

            EnsureAnimator();
            CacheParamHashes();
            _animator.enabled = true;
            _animator.speed = 0f;
            if (!_corpseDarkened)
            {
                ApplyCorpsePresentation();
                _corpseDarkened = true;
            }

            if (!TryBeginReviveDieClip(out _reviveStartNormalizedTime))
            {
                _reviving = false;
                ResetToIdle();
                return;
            }

            _animator.Play(_dieStateFullPathHash, 0, _reviveStartNormalizedTime);
            _animator.Update(0f);
        }

        private static readonly string[] DirIndexToDieSuffix = { "E", "W", "S", "N", "NE", "NW", "SE", "SW" };

        private string ResolveDirClipSuffix()
        {
            var dir = _facingDirIndex >= 0 ? _facingDirIndex : _defaultDirIndex;
            dir = ApplyFacingYawFlip(dir, _facingYawFlip);
            return DirIndexToDieSuffix[dir % 8];
        }

        private string ResolveReviveDie2StateName()
        {
            return "Die2_" + ResolveDirClipSuffix();
        }

        private string ResolveAttackStateName(string attackBase)
        {
            var bas = string.IsNullOrEmpty(attackBase) ? DefaultAttackBase : attackBase;
            return bas + "_" + ResolveDirClipSuffix();
        }

        private string PickAttackBase()
        {
            if (!_useMonsterAnimPools)
            {
                return DefaultAttackBase;
            }

            return PickFromPool(_attackAnimPool, DefaultAttackBase);
        }

        private static string[] ParseAnimPool(string pipeSeparated, string fallback)
        {
            if (string.IsNullOrWhiteSpace(pipeSeparated))
            {
                return new[] { fallback };
            }

            var parts = pipeSeparated.Split('|');
            var list = new List<string>(parts.Length);
            for (var i = 0; i < parts.Length; i++)
            {
                var t = parts[i] != null ? parts[i].Trim() : string.Empty;
                if (t.Length > 0)
                {
                    list.Add(t);
                }
            }

            return list.Count > 0 ? list.ToArray() : new[] { fallback };
        }

        private static string PickFromPool(string[] pool, string fallback)
        {
            if (pool == null || pool.Length == 0)
            {
                return fallback;
            }

            if (pool.Length == 1)
            {
                return pool[0];
            }

            return pool[UnityEngine.Random.Range(0, pool.Length)];
        }

        private void CacheActiveAttackTrigger(string attackBase)
        {
            _hasActiveAttackTrigger = false;
            _activeAttackTriggerHash = 0;
            if (_animator == null || string.IsNullOrEmpty(attackBase))
            {
                return;
            }

            var hash = Animator.StringToHash(attackBase);
            foreach (var p in _animator.parameters)
            {
                if (p.nameHash == hash && p.type == AnimatorControllerParameterType.Trigger)
                {
                    _activeAttackTriggerHash = hash;
                    _hasActiveAttackTrigger = true;
                    return;
                }
            }

            // Soldiers / default: fall back to serialized Attack1 trigger if base matches.
            if (attackBase == DefaultAttackBase && _hasAttackTrigger)
            {
                _activeAttackTriggerHash = _attackTriggerHash;
                _hasActiveAttackTrigger = true;
            }
        }

        private void ResetActiveAttackTrigger()
        {
            if (_animator == null)
            {
                return;
            }

            if (_hasActiveAttackTrigger)
            {
                _animator.ResetTrigger(_activeAttackTriggerHash);
            }
            else if (_hasAttackTrigger)
            {
                _animator.ResetTrigger(_attackTriggerHash);
            }
        }

        private bool TryEnterReviveDie2State(out float startNormalizedTime)
        {
            startNormalizedTime = DieLatchNormalizedTime;
            ClearLocomotionBools();
            if (_facingDirIndex >= 0)
            {
                SetDirIndexValue(_facingDirIndex);
            }
            else
            {
                SetDirIndexValue(_defaultDirIndex);
            }

            if (_hasDie2Trigger)
            {
                _animator.ResetTrigger(_die2TriggerHash);
                _animator.SetTrigger(_die2TriggerHash);
                for (var i = 0; i < 3; i++)
                {
                    _animator.Update(0f);
                }

                if (TryGetCurrentReviveDieClip(out _))
                {
                    return true;
                }
            }

            var stateName = ResolveReviveDie2StateName();
            _animator.Play(stateName, 0, startNormalizedTime);
            _animator.Update(0f);
            return TryGetCurrentReviveDieClip(out _);
        }

        private bool TryBeginReviveDieClip(out float startNormalizedTime)
        {
            startNormalizedTime = _lastGoodDieNormalizedTime > 0.01f
                ? _lastGoodDieNormalizedTime
                : DieLatchNormalizedTime;

            if (_animator != null && TryEnterReviveDie2State(out var die2Start))
            {
                var info = _animator.GetCurrentAnimatorStateInfo(0);
                _dieStateFullPathHash = info.fullPathHash;
                _lastNonNullDieSprites = null;
                startNormalizedTime = die2Start;
                _animator.Play(_dieStateFullPathHash, 0, startNormalizedTime);
                _animator.Update(0f);
                RememberNonNullDieSprites();
                return true;
            }

            return _dieStateFullPathHash != 0;
        }

        private void TickReviveAnim()
        {
            if (!_reviving)
            {
                return;
            }

            if (_animator == null || _dieStateFullPathHash == 0)
            {
                _reviving = false;
                ResetToIdle();
                return;
            }

            var t = Mathf.Clamp01((Time.time - _reviveStartedAt) / _reviveDuration);
            var normalized = Mathf.Lerp(_reviveStartNormalizedTime, 0f, t);
            _animator.Play(_dieStateFullPathHash, 0, normalized);
            _animator.Update(0f);
            if (t >= 1f)
            {
                _reviving = false;
                FinishReviveToIdle();
            }
        }

        /// <summary>~70° or sharper: skip min dwell (revive retarget / U-turn).</summary>
        private const float FacingDwellBypassDot = 0.34f;

        /// <summary>
        /// SPEC_04 §15.5: write DirIndex with hysteresis + min dwell so sector-boundary steer/aim
        /// jitter does not flicker 8-dir clips (e.g. N↔NE within 0.3s).
        /// </summary>
        public void SetFacing(Vector3 worldDirXZ)
        {
            if (_dead || _reviving || _facingLockedForAttack ||
                _animator == null || (!_hasDirIndex && !_hasDirection))
            {
                return;
            }

            worldDirXZ.y = 0f;
            if (worldDirXZ.sqrMagnitude < 0.0001f)
            {
                return;
            }

            _facingSwitchTimer += Time.deltaTime;
            var next = StabilizeDirIndex(_facingDirIndex, worldDirXZ, FacingHysteresisDegrees);
            if (next == _facingDirIndex)
            {
                return;
            }

            var bypassDwell = false;
            if (_facingDirIndex >= 0)
            {
                var n = worldDirXZ.normalized;
                var cur = DirIndexToUnitXZ(_facingDirIndex);
                bypassDwell = cur.x * n.x + cur.z * n.z < FacingDwellBypassDot;
            }

            if (!bypassDwell && _facingDirIndex >= 0 && _facingSwitchTimer < FacingSwitchMinDwellSeconds)
            {
                return;
            }

            _facingDirIndex = next;
            _facingSwitchTimer = 0f;
            SetDirIndexValue(next);
            FlushAnimatorParams();
        }

        /// <param name="moveTargetDistanceXZ">
        /// Planar distance to move target. Force Attack→Run only when &gt;
        /// <see cref="AttackInterruptMinMoveTargetDistance"/>. Default +∞ (treat as far).
        /// </param>
        /// <param name="useRun">Monster pools only: true=run gait; false=walk. Ignored for soldiers.</param>
        public void SetMoving(bool moving, float moveTargetDistanceXZ = float.PositiveInfinity, bool useRun = false)
        {
            if (_dead || _reviving || _animator == null)
            {
                return;
            }

            if (moving)
            {
                var forceInterrupt = moveTargetDistanceXZ > AttackInterruptMinMoveTargetDistance;
                if (_moving)
                {
                    if (_useMonsterAnimPools && useRun != _usingRun)
                    {
                        _usingRun = useRun;
                        if (forceInterrupt)
                        {
                            ForceEnterMoveFromAttack();
                            _forceInterruptedWhileMoving = true;
                        }
                        else
                        {
                            var stateName = ResolveActiveMoveStateName();
                            _animator.CrossFade(stateName, 0.1f, 0, 0f);
                            ApplyMovingLocomotionBoolsOnly();
                        }

                        return;
                    }

                    // Already moving: if distance just crossed the gate, still force-interrupt once.
                    if (forceInterrupt)
                    {
                        if (!_forceInterruptedWhileMoving)
                        {
                            ForceEnterMoveFromAttack();
                            _forceInterruptedWhileMoving = true;
                        }
                    }
                    else
                    {
                        _forceInterruptedWhileMoving = false;
                    }

                    return;
                }

                _moving = true;
                _usingRun = _useMonsterAnimPools && useRun;
                if (forceInterrupt)
                {
                    // SPEC_04 §15.5: Creator Attack_* only exits via ExitTime;
                    // locomotion Bool alone cannot cut mid-attack — force move state when far enough.
                    ForceEnterMoveFromAttack();
                    _forceInterruptedWhileMoving = true;
                }
                else
                {
                    // Near target: write move Bool without CrossFade (does not chop Attack).
                    _forceInterruptedWhileMoving = false;
                    ApplyMovingLocomotionBoolsOnly();
                }

                return;
            }

            if (!_moving)
            {
                return;
            }

            _moving = false;
            _usingRun = false;
            _forceInterruptedWhileMoving = false;
            ClearLocomotionBools();
        }

        private void ForceEnterMoveFromAttack()
        {
            ClearAttackFacingLock();
            ResetActiveAttackTrigger();

            var stateName = ResolveActiveMoveStateName();
            _animator.CrossFade(stateName, 0f, 0, 0f);
            ApplyMovingLocomotionBoolsOnly();
        }

        private void ApplyMovingLocomotionBoolsOnly()
        {
            if (_useMonsterAnimPools)
            {
                if (_usingRun)
                {
                    ClearLocomotionBoolsExcept(IsRunParam);
                    if (_hasIsRun)
                    {
                        _animator.SetBool(_isRunHash, true);
                    }
                }
                else
                {
                    ClearLocomotionBoolsExcept(IsWalkParam);
                    if (_hasIsWalk)
                    {
                        _animator.SetBool(_isWalkHash, true);
                    }
                }

                return;
            }

            ClearLocomotionBoolsExceptRun();
            if (_hasIsRun)
            {
                _animator.SetBool(_isRunHash, true);
            }
        }

        private string ResolveActiveMoveStateName()
        {
            if (_useMonsterAnimPools)
            {
                return _usingRun ? _activeRunState : _activeWalkState;
            }

            return RunStateName;
        }

        public void PlayAttack()
        {
            if (_dead || _reviving || _animator == null)
            {
                return;
            }

            if (!_useMonsterAnimPools && !_hasAttackTrigger)
            {
                return;
            }

            _moving = false;
            _usingRun = false;
            _forceInterruptedWhileMoving = false;
            ClearLocomotionBools();
            if (_facingDirIndex >= 0)
            {
                SetDirIndexValue(_facingDirIndex);
            }

            var attackBase = PickAttackBase();
            _activeAttackBase = attackBase;
            CacheActiveAttackTrigger(attackBase);
            ResetActiveAttackTrigger();

            _animator.Play(ResolveAttackStateName(attackBase), 0, 0f);
            FlushAnimatorParams();
            ArmAttackFacingLock();
            if (!IsPlayingAttackClip(attackBase) && _hasActiveAttackTrigger)
            {
                _animator.SetTrigger(_activeAttackTriggerHash);
                FlushAnimatorParams();
            }
        }

        /// <summary>UI-016 card reveal: one-shot Creator Taunt (SPEC_04 §15.5).</summary>
        public void PlayTaunt()
        {
            if (_dead || _animator == null || !_hasTauntTrigger)
            {
                return;
            }

            _moving = false;
            _usingRun = false;
            _forceInterruptedWhileMoving = false;
            ClearLocomotionBools();
            _animator.ResetTrigger(_tauntTriggerHash);
            _animator.SetTrigger(_tauntTriggerHash);
        }

        public bool HasTauntTrigger => _hasTauntTrigger;

        public bool IsPlayingTaunt()
        {
            if (_animator == null)
            {
                return false;
            }

            return ClipNameStartsWith(_animator.GetCurrentAnimatorClipInfo(0), "Taunt")
                   || ClipNameStartsWith(_animator.GetNextAnimatorClipInfo(0), "Taunt");
        }

        /// <summary>
        /// SPEC_04 §15.5: play Die (default) or Die2 when preferDie2 and Controller has Die2 Trigger.
        /// Tiered corpse presentation via corpseDarkenMul / corpseAlphaMul (see SPEC §15.5).
        /// </summary>
        public void PlayDie(
            bool preferDie2 = false,
            float? corpseDarkenMul = null,
            float? corpseAlphaMul = null)
        {
            if (_dead)
            {
                return;
            }

            _dead = true;
            _corpseDarkenMul = Mathf.Clamp01(corpseDarkenMul ?? CombatRuntimeTuning.DeathCorpseDarkenMul);
            _corpseAlphaMul = Mathf.Clamp01(corpseAlphaMul ?? 1f);
            _dieLatched = false;
            _enteredDieClip = false;
            _moving = false;
            _usingRun = false;
            _forceInterruptedWhileMoving = false;
            ClearAttackFacingLock();
            _dieStartedAt = Time.time;
            _lastGoodDieNormalizedTime = 0f;
            _dieStateFullPathHash = 0;
            _lastNonNullDieSprites = null;
            if (_animator == null)
            {
                LatchDeathPresentation();
                return;
            }

            ClearLocomotionBools();
            if (_hasAttackTrigger)
            {
                _animator.ResetTrigger(_attackTriggerHash);
            }

            ResetDieTriggers();
            var useDie2 = preferDie2 && _hasDie2Trigger;
            if (useDie2)
            {
                _animator.SetTrigger(_die2TriggerHash);
            }
            else if (_hasDieTrigger)
            {
                _animator.SetTrigger(_dieTriggerHash);
            }
            else
            {
                LatchDeathPresentation();
            }
        }

        /// <summary>
        /// SPEC_04 §15.5 DirIndex: 0E 1W 2S 3N 4NE 5NW 6SE 7SW. +X=E, +Z=N.
        /// </summary>
        public static int DirIndexFromXZ(Vector3 worldDirXZ)
        {
            worldDirXZ.y = 0f;
            if (worldDirXZ.sqrMagnitude < 0.0001f)
            {
                return 2;
            }

            var n = worldDirXZ.normalized;
            // atan2(x,z): 0 = +Z (N), +90° = +X (E)
            var deg = Mathf.Atan2(n.x, n.z) * Mathf.Rad2Deg;
            if (deg < 0f)
            {
                deg += 360f;
            }

            // 8 sectors centered on cardinals/diagonals (45° each)
            var sector = Mathf.RoundToInt(deg / 45f) % 8;
            switch (sector)
            {
                case 0: return 3; // N
                case 1: return 4; // NE
                case 2: return 0; // E
                case 3: return 6; // SE
                case 4: return 2; // S
                case 5: return 7; // SW
                case 6: return 1; // W
                case 7: return 5; // NW
                default: return 2;
            }
        }

        /// <summary>
        /// Keeps the current DirIndex unless the raw direction passes the current sector
        /// boundary by more than <paramref name="hysteresisDeg"/> (sector half-width 22.5°).
        /// </summary>
        public static int StabilizeDirIndex(int currentDirIndex, Vector3 rawDirXZ, float hysteresisDeg)
        {
            var candidate = DirIndexFromXZ(rawDirXZ);
            if (currentDirIndex < 0 || currentDirIndex > 7 || candidate == currentDirIndex)
            {
                return candidate;
            }

            rawDirXZ.y = 0f;
            if (rawDirXZ.sqrMagnitude < 0.0001f)
            {
                return currentDirIndex;
            }

            var n = rawDirXZ.normalized;
            var deg = Mathf.Atan2(n.x, n.z) * Mathf.Rad2Deg;
            if (deg < 0f)
            {
                deg += 360f;
            }

            var currentCenterDeg = DirIndexToSector[currentDirIndex] * 45f;
            var delta = Mathf.Abs(Mathf.DeltaAngle(deg, currentCenterDeg));
            return delta > 22.5f + hysteresisDeg ? candidate : currentDirIndex;
        }

        /// <summary>Unit XZ vector at the sector center of <paramref name="dirIndex"/> (round-trips through DirIndexFromXZ).</summary>
        public static Vector3 DirIndexToUnitXZ(int dirIndex)
        {
            var rad = DirIndexToSector[dirIndex] * 45f * Mathf.Deg2Rad;
            return new Vector3(Mathf.Sin(rad), 0f, Mathf.Cos(rad));
        }

        /// <summary>Presentation facing as a planar unit vector (+X east, +Z north).</summary>
        public bool TryGetFacingUnitXZ(out Vector3 unitXZ)
        {
            var dir = _facingDirIndex >= 0 ? _facingDirIndex : _defaultDirIndex;
            unitXZ = DirIndexToUnitXZ(dir);
            return unitXZ.sqrMagnitude > 1e-8f;
        }

        /// <summary>
        /// Applies FacingYawFlip: 1 → (dirIndex+4)%8.
        /// </summary>
        public static int ApplyFacingYawFlip(int dirIndex, bool facingYawFlip)
        {
            var clamped = dirIndex < 0 ? 0 : dirIndex % 8;
            return facingYawFlip ? (clamped + 4) % 8 : clamped;
        }

        private void ResetFacingStabilizerState()
        {
            _facingDirIndex = -1;
            _facingSwitchTimer = 0f;
            ClearAttackFacingLock();
        }

        private void ArmAttackFacingLock()
        {
            _facingLockedForAttack = true;
            _attackFacingLockArmedAt = Time.time;
        }

        private void ClearAttackFacingLock()
        {
            _facingLockedForAttack = false;
            _attackFacingLockArmedAt = 0f;
        }

        private void TickAttackFacingLock()
        {
            if (!_facingLockedForAttack || _dead || _reviving)
            {
                return;
            }

            if (Time.time - _attackFacingLockArmedAt < AttackFacingLockGraceSeconds)
            {
                return;
            }

            if (!IsPlayingAttackClip(_activeAttackBase))
            {
                ClearAttackFacingLock();
            }
        }

        private void TickDieLatch()
        {
            if (TryGetCurrentDieClip(out _))
            {
                _enteredDieClip = true;
                var info = _animator.GetCurrentAnimatorStateInfo(0);
                _dieStateFullPathHash = info.fullPathHash;
                RememberNonNullDieSprites();

                // Creator Die ends with a null sprite key — latch as soon as it appears.
                if (HasRememberedSpriteGoneNull())
                {
                    LatchDeathPresentation();
                    return;
                }

                if (_lastNonNullDieSprites != null && _lastNonNullDieSprites.Count > 0)
                {
                    _lastGoodDieNormalizedTime = Mathf.Clamp(info.normalizedTime, 0f, DieLatchNormalizedTime);
                }

                // Near end (before null key) or ExitTime starting Die→Idle.
                if ((info.normalizedTime >= DieLatchNormalizedTime &&
                     _lastNonNullDieSprites != null &&
                     _lastNonNullDieSprites.Count > 0) ||
                    _animator.IsInTransition(0))
                {
                    LatchDeathPresentation();
                }

                return;
            }

            // Left Die before latch — snap to last good Die time (never 1.0: that is the null key).
            if (_enteredDieClip)
            {
                if (_dieStateFullPathHash != 0 && _animator != null)
                {
                    var t = _lastGoodDieNormalizedTime > 0.01f
                        ? _lastGoodDieNormalizedTime
                        : DieLatchNormalizedTime;
                    _animator.Play(_dieStateFullPathHash, 0, t);
                    _animator.Update(0f);
                    RememberNonNullDieSprites();
                }

                LatchDeathPresentation();
                return;
            }

            if (Time.time - _dieStartedAt >= DieLatchFallbackSeconds)
            {
                LatchDeathPresentation();
            }
        }

        private void LatchDeathPresentation()
        {
            if (_dieLatched)
            {
                return;
            }

            _dieLatched = true;
            if (_animator != null)
            {
                _animator.speed = 0f;
            }

            // Re-apply last corpse pose: trailing null key / Play(…,1) would otherwise clear sprites.
            RestoreLastNonNullDieSprites();
            ApplyCorpsePresentation();
            _corpseDarkened = true;

            ApplySortingOrder(CorpseSortingOrder);

            // Stop Animator writes so the null key cannot clear sprites on a later evaluate.
            if (_animator != null)
            {
                _animator.enabled = false;
            }
        }

        private void RememberNonNullDieSprites()
        {
            CacheSpriteOriginals();
            if (_spriteOriginals == null)
            {
                return;
            }

            foreach (var pair in _spriteOriginals)
            {
                var renderer = pair.Key;
                if (renderer == null || renderer.sprite == null)
                {
                    continue;
                }

                _lastNonNullDieSprites ??= new Dictionary<SpriteRenderer, Sprite>();
                _lastNonNullDieSprites[renderer] = renderer.sprite;
            }
        }

        private bool HasRememberedSpriteGoneNull()
        {
            if (_lastNonNullDieSprites == null || _lastNonNullDieSprites.Count == 0)
            {
                return false;
            }

            foreach (var pair in _lastNonNullDieSprites)
            {
                if (pair.Key != null && pair.Key.sprite == null)
                {
                    return true;
                }
            }

            return false;
        }

        private void RestoreLastNonNullDieSprites()
        {
            if (_lastNonNullDieSprites == null)
            {
                return;
            }

            foreach (var pair in _lastNonNullDieSprites)
            {
                if (pair.Key != null && pair.Value != null)
                {
                    pair.Key.sprite = pair.Value;
                }
            }
        }

        private bool IsPlayingAttackClip(string attackBase)
        {
            if (_animator == null)
            {
                return false;
            }

            var prefix = string.IsNullOrEmpty(attackBase) ? DefaultAttackBase : attackBase;
            return ClipNameStartsWith(_animator.GetCurrentAnimatorClipInfo(0), prefix)
                   || ClipNameStartsWith(_animator.GetNextAnimatorClipInfo(0), prefix);
        }

        private static bool IsDieClipName(string clipName)
        {
            if (string.IsNullOrEmpty(clipName))
            {
                return false;
            }

            // Death latch accepts Die_* and Die2_* (SPEC_04 §15.5 knockback clip pick).
            if (clipName.StartsWith(Die2ClipPrefix, System.StringComparison.Ordinal))
            {
                return clipName.Length == Die2ClipPrefix.Length ||
                       clipName[Die2ClipPrefix.Length] == '_';
            }

            return clipName.StartsWith(DieClipPrefix, System.StringComparison.Ordinal) &&
                   (clipName.Length == DieClipPrefix.Length || clipName[DieClipPrefix.Length] == '_');
        }

        private static bool IsReviveDieClipName(string clipName)
        {
            if (string.IsNullOrEmpty(clipName))
            {
                return false;
            }

            if (clipName.StartsWith(Die2ClipPrefix, System.StringComparison.Ordinal))
            {
                return clipName.Length == Die2ClipPrefix.Length ||
                       clipName[Die2ClipPrefix.Length] == '_';
            }

            return clipName.StartsWith(DieClipPrefix, System.StringComparison.Ordinal) &&
                   clipName.Length > DieClipPrefix.Length &&
                   clipName[DieClipPrefix.Length] == '_';
        }

        private bool TryGetCurrentReviveDieClip(out AnimationClip clip)
        {
            clip = null;
            if (_animator == null)
            {
                return false;
            }

            var infos = _animator.GetCurrentAnimatorClipInfo(0);
            if (infos == null || infos.Length == 0 || infos[0].clip == null)
            {
                return false;
            }

            clip = infos[0].clip;
            return IsReviveDieClipName(clip.name);
        }

        private bool TryGetCurrentDieClip(out AnimationClip clip)
        {
            clip = null;
            if (_animator == null)
            {
                return false;
            }

            var infos = _animator.GetCurrentAnimatorClipInfo(0);
            if (infos == null || infos.Length == 0 || infos[0].clip == null)
            {
                return false;
            }

            clip = infos[0].clip;
            return IsDieClipName(clip.name);
        }

        private void ApplyCorpsePresentation()
        {
            CacheSpriteOriginals();
            if (_spriteOriginals == null)
            {
                return;
            }

            foreach (var pair in _spriteOriginals)
            {
                var sprite = pair.Key;
                if (sprite == null)
                {
                    continue;
                }

                var c = pair.Value;
                sprite.color = new Color(
                    c.r * _corpseDarkenMul,
                    c.g * _corpseDarkenMul,
                    c.b * _corpseDarkenMul,
                    c.a * _corpseAlphaMul);
            }
        }

        private void RestoreSpriteColors()
        {
            if (_spriteOriginals == null)
            {
                return;
            }

            foreach (var pair in _spriteOriginals)
            {
                if (pair.Key != null)
                {
                    pair.Key.color = pair.Value;
                }
            }
        }

        private void CacheSpriteOriginals()
        {
            if (_spriteOriginals != null)
            {
                return;
            }

            _spriteOriginals = new Dictionary<SpriteRenderer, Color>();
            var sprites = GetComponentsInChildren<SpriteRenderer>(true);
            for (var i = 0; i < sprites.Length; i++)
            {
                var sprite = sprites[i];
                if (sprite == null || IsAllyFootCircleRenderer(sprite))
                {
                    continue;
                }

                _spriteOriginals[sprite] = sprite.color;
            }
        }

        private void EnsureAnimator()
        {
            if (_animator == null)
            {
                _animator = GetComponentInChildren<Animator>(true);
            }

            if (_animator != null)
            {
                _animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            }
        }

        private void ApplySortingOrder()
        {
            ApplySortingOrder(SpriteSortingOrder);
        }

        private void ApplySortingOrder(int order)
        {
            var renderers = GetComponentsInChildren<SpriteRenderer>(true);
            for (var i = 0; i < renderers.Length; i++)
            {
                var renderer = renderers[i];
                if (renderer == null || IsAllyFootCircleRenderer(renderer))
                {
                    continue;
                }

                renderer.sortingOrder = order;
            }
        }

        /// <summary>
        /// AllyFootCircle keeps its own Order In Layer (SPEC_04 §9.7); do not overwrite to 200/100.
        /// </summary>
        private static bool IsAllyFootCircleRenderer(SpriteRenderer renderer)
        {
            if (renderer == null)
            {
                return false;
            }

            var n = renderer.gameObject.name;
            return n == "AllyFootCircle" ||
                   (n != null && n.StartsWith("SkillIcon", System.StringComparison.Ordinal));
        }

        private void CacheParamHashes()
        {
            _hasDirIndex = false;
            _hasDirection = false;
            _hasAttackTrigger = false;
            _hasDieTrigger = false;
            _hasDie2Trigger = false;
            _hasTauntTrigger = false;
            _hasIsRun = false;
            _hasIsWalk = false;

            if (_animator == null || _animator.runtimeAnimatorController == null)
            {
                return;
            }

            var attackName = string.IsNullOrEmpty(_attackTriggerParam) ? "Attack1" : _attackTriggerParam;
            var dieName = string.IsNullOrEmpty(_dieTriggerParam) ? "Die" : _dieTriggerParam;
            var die2Name = string.IsNullOrEmpty(_die2TriggerParam) ? "Die2" : _die2TriggerParam;
            var tauntName = string.IsNullOrEmpty(_tauntTriggerParam) ? "Taunt" : _tauntTriggerParam;
            _dirIndexHash = Animator.StringToHash(DirIndexParam);
            _directionHash = Animator.StringToHash(DirectionParam);
            _attackTriggerHash = Animator.StringToHash(attackName);
            _dieTriggerHash = Animator.StringToHash(dieName);
            _die2TriggerHash = Animator.StringToHash(die2Name);
            _tauntTriggerHash = Animator.StringToHash(tauntName);
            _isRunHash = Animator.StringToHash(IsRunParam);
            _isWalkHash = Animator.StringToHash(IsWalkParam);

            foreach (var p in _animator.parameters)
            {
                if (p.nameHash == _dirIndexHash && p.type == AnimatorControllerParameterType.Int)
                {
                    _hasDirIndex = true;
                }
                else if (p.nameHash == _directionHash && p.type == AnimatorControllerParameterType.Float)
                {
                    _hasDirection = true;
                }
                else if (p.nameHash == _attackTriggerHash)
                {
                    _hasAttackTrigger = true;
                }
                else if (p.nameHash == _dieTriggerHash)
                {
                    _hasDieTrigger = true;
                }
                else if (p.nameHash == _die2TriggerHash)
                {
                    _hasDie2Trigger = true;
                }
                else if (p.nameHash == _tauntTriggerHash)
                {
                    _hasTauntTrigger = true;
                }
                else if (p.nameHash == _isRunHash && p.type == AnimatorControllerParameterType.Bool)
                {
                    _hasIsRun = true;
                }
                else if (p.nameHash == _isWalkHash && p.type == AnimatorControllerParameterType.Bool)
                {
                    _hasIsWalk = true;
                }
            }
        }

        private void ResetDieTriggers()
        {
            if (_animator == null)
            {
                return;
            }

            if (_hasDieTrigger)
            {
                _animator.ResetTrigger(_dieTriggerHash);
            }

            if (_hasDie2Trigger)
            {
                _animator.ResetTrigger(_die2TriggerHash);
            }
        }

        private void SetDirIndexValue(int dirIndex)
        {
            var written = ApplyFacingYawFlip(dirIndex, _facingYawFlip);
            if (_hasDirIndex)
            {
                _animator.SetInteger(_dirIndexHash, written);
            }

            if (_hasDirection)
            {
                _animator.SetFloat(_directionHash, written);
            }
        }

        private void FlushAnimatorParams()
        {
            if (_animator != null && _animator.enabled)
            {
                _animator.Update(0f);
            }
        }

        private void ClearLocomotionBools()
        {
            if (_animator == null)
            {
                return;
            }

            for (var i = 0; i < LocomotionBools.Length; i++)
            {
                SetBoolIfPresent(LocomotionBools[i], false);
            }
        }

        private void ClearLocomotionBoolsExceptRun()
        {
            ClearLocomotionBoolsExcept(IsRunParam);
        }

        private void ClearLocomotionBoolsExcept(string keepParam)
        {
            if (_animator == null)
            {
                return;
            }

            for (var i = 0; i < LocomotionBools.Length; i++)
            {
                var name = LocomotionBools[i];
                if (name == keepParam)
                {
                    continue;
                }

                SetBoolIfPresent(name, false);
            }
        }

        private void SetBoolIfPresent(string name, bool value)
        {
            foreach (var p in _animator.parameters)
            {
                if (p.type == AnimatorControllerParameterType.Bool && p.name == name)
                {
                    _animator.SetBool(p.nameHash, value);
                    return;
                }
            }
        }

        private static bool ClipNameStartsWith(AnimatorClipInfo[] infos, string prefix)
        {
            if (infos == null || infos.Length == 0 || infos[0].clip == null || string.IsNullOrEmpty(prefix))
            {
                return false;
            }

            return infos[0].clip.name.StartsWith(prefix, System.StringComparison.Ordinal);
        }
    }
}
