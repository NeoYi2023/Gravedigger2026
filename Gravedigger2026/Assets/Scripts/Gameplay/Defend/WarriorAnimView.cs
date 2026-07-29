using UnityEngine;

namespace Gravedigger2026.Gameplay.Defend
{
    /// <summary>
    /// Defend soldier presentation. Drives Creator bake Animator: move / Attack1 / Die / DirIndex (SPEC_04 §15.5).
    /// </summary>
    public sealed class WarriorAnimView : MonoBehaviour
    {
        private const string DirIndexParam = "DirIndex";
        private const string IdleStateName = "IdleBT";
        private const string IsRunParam = "IsRun";
        private const int SpriteSortingOrder = 200;

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
        private int _attackTriggerHash;
        private int _dieTriggerHash;
        private int _isRunHash;
        private bool _hasDirIndex;
        private bool _hasAttackTrigger;
        private bool _hasDieTrigger;
        private bool _hasIsRun;
        private bool _dead;
        private bool _moving;

        private void Awake()
        {
            EnsureAnimator();
            CacheParamHashes();
            ApplySortingOrder();
        }

        public void ResetToIdle()
        {
            EnsureAnimator();
            CacheParamHashes();
            ApplySortingOrder();
            _dead = false;
            _moving = false;
            if (_animator == null)
            {
                return;
            }

            ClearLocomotionBools();
            SetDirIndexValue(_defaultDirIndex);
            _animator.Play(IdleStateName, 0, 0f);
            _animator.Update(0f);
        }

        public void SetFacing(Vector3 worldDirXZ)
        {
            if (_dead || _animator == null || !_hasDirIndex)
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
            _moving = false;
            if (_animator == null)
            {
                return;
            }

            ClearLocomotionBools();
            if (_hasDieTrigger)
            {
                _animator.ResetTrigger(_dieTriggerHash);
                _animator.SetTrigger(_dieTriggerHash);
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
            var renderers = GetComponentsInChildren<SpriteRenderer>(true);
            for (var i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null)
                {
                    renderers[i].sortingOrder = SpriteSortingOrder;
                }
            }
        }

        private void CacheParamHashes()
        {
            _hasDirIndex = false;
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
            _attackTriggerHash = Animator.StringToHash(attackName);
            _dieTriggerHash = Animator.StringToHash(dieName);
            _isRunHash = Animator.StringToHash(IsRunParam);

            foreach (var p in _animator.parameters)
            {
                if (p.nameHash == _dirIndexHash)
                {
                    _hasDirIndex = true;
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
            if (!_hasDirIndex)
            {
                return;
            }

            _animator.SetInteger(_dirIndexHash, dirIndex);
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
