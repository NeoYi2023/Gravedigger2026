using System.Collections.Generic;
using UnityEngine;

namespace Gravedigger2026.Gameplay.Defend
{
    /// <summary>
    /// Character presentation for Creator bake Animator: move / Attack1 / Die / DirIndex (SPEC_04 §15.5).
    /// Used by soldiers and monsters (Defend / PushMap).
    /// IdleBT/RunBT BlendTrees blend on float Direction; DirIndex drives discrete Attack transitions.
    /// After asset normalize, Direction thresholds match DirIndex order — always write the same value.
    /// Optional FacingYawFlip applies (dirIndex+4)%8 before write.
    /// Death: latch last non-null Die sprite + darken + CorpseSortingOrder=100 + disable Animator.
    /// </summary>
    public sealed class WarriorAnimView : MonoBehaviour
    {
        private const string DirIndexParam = "DirIndex";
        private const string DirectionParam = "Direction";
        private const string IdleStateName = "IdleBT";
        private const string IsRunParam = "IsRun";
        private const string DieClipPrefix = "Die";
        private const int SpriteSortingOrder = 200;
        /// <summary>SPEC_04 §15.2/§15.5: below living combat band 200 (soldiers/monsters/protagonist).</summary>
        private const int CorpseSortingOrder = 100;

        /// <summary>
        /// Creator Die clips end with a null sprite key (visual despawn). Latch the last
        /// non-null frame instead of normalizedTime≈1 / Play(...,1).
        /// </summary>
        private const float DieLatchNormalizedTime = 0.92f;

        /// <summary>SPEC_04 §15.5: RGB multiply on corpse sprites (α unchanged).</summary>
        private const float CorpseDarkenMul = 0.4f;

        /// <summary>Fallback if Die clip never enters after PlayDie.</summary>
        private const float DieLatchFallbackSeconds = 2f;

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
        [SerializeField] private int _defaultDirIndex = 2;

        private int _dirIndexHash;
        private int _directionHash;
        private int _attackTriggerHash;
        private int _dieTriggerHash;
        private int _isRunHash;
        private bool _hasDirIndex;
        private bool _hasDirection;
        private bool _hasAttackTrigger;
        private bool _hasDieTrigger;
        private bool _hasIsRun;
        private bool _facingYawFlip;
        private bool _dead;
        private bool _dieLatched;
        private bool _enteredDieClip;
        private bool _moving;
        private float _dieStartedAt;
        private float _lastGoodDieNormalizedTime;
        private int _dieStateFullPathHash;
        private Dictionary<SpriteRenderer, Color> _spriteOriginals;
        private Dictionary<SpriteRenderer, Sprite> _lastNonNullDieSprites;

        private void Awake()
        {
            EnsureAnimator();
            CacheParamHashes();
            ApplySortingOrder();
            CacheSpriteOriginals();
        }

        private void Update()
        {
            if (!_dead || _dieLatched || _animator == null)
            {
                return;
            }

            TickDieLatch();
        }

        /// <summary>
        /// SPEC_04 §15.5: FacingYawFlip 1 → (dirIndex+4)%8 before writing Animator params.
        /// </summary>
        public void SetFacingYawFlip(bool flip)
        {
            _facingYawFlip = flip;
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
            _dieStartedAt = 0f;
            _lastGoodDieNormalizedTime = 0f;
            _dieStateFullPathHash = 0;
            _lastNonNullDieSprites = null;
            if (_animator == null)
            {
                return;
            }

            _animator.enabled = true;
            _animator.speed = 1f;
            RestoreSpriteColors();
            ClearLocomotionBools();
            SetDirIndexValue(_defaultDirIndex);
            _animator.Play(IdleStateName, 0, 0f);
            _animator.Update(0f);
        }

        public void SetFacing(Vector3 worldDirXZ)
        {
            if (_dead || _animator == null || (!_hasDirIndex && !_hasDirection))
            {
                return;
            }

            worldDirXZ.y = 0f;
            if (worldDirXZ.sqrMagnitude < 0.0001f)
            {
                return;
            }

            SetDirIndexValue(DirIndexFromXZ(worldDirXZ));
        }

        public void SetMoving(bool moving)
        {
            if (_dead || _animator == null)
            {
                return;
            }

            if (_moving == moving)
            {
                return;
            }

            _moving = moving;
            if (moving)
            {
                ClearLocomotionBoolsExceptRun();
                if (_hasIsRun)
                {
                    _animator.SetBool(_isRunHash, true);
                }

                return;
            }

            ClearLocomotionBools();
        }

        public void PlayAttack()
        {
            if (_dead || _animator == null || !_hasAttackTrigger)
            {
                return;
            }

            _moving = false;
            ClearLocomotionBools();
            _animator.ResetTrigger(_attackTriggerHash);
            _animator.SetTrigger(_attackTriggerHash);
        }

        public void PlayDie()
        {
            if (_dead)
            {
                return;
            }

            _dead = true;
            _dieLatched = false;
            _enteredDieClip = false;
            _moving = false;
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
            if (_hasDieTrigger)
            {
                _animator.ResetTrigger(_dieTriggerHash);
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
        /// Applies FacingYawFlip: 1 → (dirIndex+4)%8.
        /// </summary>
        public static int ApplyFacingYawFlip(int dirIndex, bool facingYawFlip)
        {
            var clamped = dirIndex < 0 ? 0 : dirIndex % 8;
            return facingYawFlip ? (clamped + 4) % 8 : clamped;
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
            ApplyCorpseDarken();
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

        private static bool IsDieClipName(string clipName)
        {
            return !string.IsNullOrEmpty(clipName) &&
                   clipName.StartsWith(DieClipPrefix, System.StringComparison.Ordinal);
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

        private void ApplyCorpseDarken()
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
                    c.r * CorpseDarkenMul,
                    c.g * CorpseDarkenMul,
                    c.b * CorpseDarkenMul,
                    c.a);
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
                if (sprite != null)
                {
                    _spriteOriginals[sprite] = sprite.color;
                }
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
                if (renderers[i] != null)
                {
                    renderers[i].sortingOrder = order;
                }
            }
        }

        private void CacheParamHashes()
        {
            _hasDirIndex = false;
            _hasDirection = false;
            _hasAttackTrigger = false;
            _hasDieTrigger = false;
            _hasIsRun = false;

            if (_animator == null || _animator.runtimeAnimatorController == null)
            {
                return;
            }

            var attackName = string.IsNullOrEmpty(_attackTriggerParam) ? "Attack1" : _attackTriggerParam;
            var dieName = string.IsNullOrEmpty(_dieTriggerParam) ? "Die" : _dieTriggerParam;
            _dirIndexHash = Animator.StringToHash(DirIndexParam);
            _directionHash = Animator.StringToHash(DirectionParam);
            _attackTriggerHash = Animator.StringToHash(attackName);
            _dieTriggerHash = Animator.StringToHash(dieName);
            _isRunHash = Animator.StringToHash(IsRunParam);

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
                else if (p.nameHash == _isRunHash && p.type == AnimatorControllerParameterType.Bool)
                {
                    _hasIsRun = true;
                }
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
            if (_animator == null)
            {
                return;
            }

            for (var i = 0; i < LocomotionBools.Length; i++)
            {
                var name = LocomotionBools[i];
                if (name == IsRunParam)
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
    }
}
