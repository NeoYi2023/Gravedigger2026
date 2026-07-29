using System;
using UnityEngine;

namespace Gravedigger2026.Gameplay.Dig
{
    /// <summary>
    /// Dig protagonist presentation. Drives Art Animator: Idle vs looping Dig (mapped Trigger).
    /// </summary>
    public sealed class DigDiggerView : MonoBehaviour
    {
        private const string DirIndexParam = "DirIndex";
        private const string IdleStateName = "IdleBT";

        private static readonly string[] LocomotionBools =
        {
            "IsRun", "IsWalk", "IsStrafeLeft", "IsStrafeRight", "IsRunBackwards",
            "IsCrouching", "IsMounted", "UseIdle2", "UseIdle3", "UseIdle4"
        };

        [SerializeField] private Animator _animator;
        [Tooltip("View-layer Dig→export Trigger mapping (SPEC_04 §15.5). Rules must not hardcode this.")]
        [SerializeField] private string _digTriggerParam = "Special1";
        [SerializeField] private int _fixedDirIndex = 2;
        [SerializeField] private float _retriggerNormalizedTime = 0.92f;

        private bool _digging;
        private int _dirIndexHash;
        private int _digTriggerHash;
        private bool _hasDirIndex;
        private bool _hasDigTrigger;
        private string _digTriggerName = "Special1";

        private void Awake()
        {
            if (_animator == null)
            {
                _animator = GetComponentInChildren<Animator>(true);
            }

            CacheParamHashes();
            ApplyFixedFacing();
            SetDigging(false);
        }

        private void Update()
        {
            if (!_digging || _animator == null || !_hasDigTrigger)
            {
                return;
            }

            if (!IsPlayingDigClip())
            {
                _animator.SetTrigger(_digTriggerHash);
                return;
            }

            var info = _animator.GetCurrentAnimatorStateInfo(0);
            if (!info.loop && info.normalizedTime >= _retriggerNormalizedTime)
            {
                _animator.Play(info.fullPathHash, 0, 0f);
            }
        }

        public void SetDigging(bool digging)
        {
            _digging = digging;
            if (_animator == null)
            {
                return;
            }

            ClearLocomotionBools();
            ApplyFixedFacing();

            if (digging)
            {
                if (_hasDigTrigger)
                {
                    _animator.ResetTrigger(_digTriggerHash);
                    _animator.SetTrigger(_digTriggerHash);
                }

                return;
            }

            if (_hasDigTrigger)
            {
                _animator.ResetTrigger(_digTriggerHash);
            }

            _animator.Play(IdleStateName, 0, 0f);
        }

        private bool IsPlayingDigClip()
        {
            var count = _animator.GetCurrentAnimatorClipInfoCount(0);
            if (count <= 0)
            {
                return false;
            }

            var clips = _animator.GetCurrentAnimatorClipInfo(0);
            for (var i = 0; i < clips.Length; i++)
            {
                var clip = clips[i].clip;
                if (clip != null
                    && clip.name.StartsWith(_digTriggerName, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private void CacheParamHashes()
        {
            _hasDirIndex = false;
            _hasDigTrigger = false;
            _digTriggerName = string.IsNullOrEmpty(_digTriggerParam) ? "Special1" : _digTriggerParam;

            if (_animator == null || _animator.runtimeAnimatorController == null)
            {
                return;
            }

            _dirIndexHash = Animator.StringToHash(DirIndexParam);
            _digTriggerHash = Animator.StringToHash(_digTriggerName);

            foreach (var p in _animator.parameters)
            {
                if (p.nameHash == _dirIndexHash)
                {
                    _hasDirIndex = true;
                }

                if (p.nameHash == _digTriggerHash)
                {
                    _hasDigTrigger = true;
                }
            }
        }

        private void ApplyFixedFacing()
        {
            if (_animator == null || !_hasDirIndex)
            {
                return;
            }

            _animator.SetInteger(_dirIndexHash, _fixedDirIndex);
        }

        private void ClearLocomotionBools()
        {
            if (_animator == null)
            {
                return;
            }

            for (var i = 0; i < LocomotionBools.Length; i++)
            {
                var name = LocomotionBools[i];
                foreach (var p in _animator.parameters)
                {
                    if (p.type == AnimatorControllerParameterType.Bool && p.name == name)
                    {
                        _animator.SetBool(p.nameHash, false);
                        break;
                    }
                }
            }
        }
    }
}
