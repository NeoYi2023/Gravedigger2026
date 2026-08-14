using System;
using UnityEngine;
using UnityEngine.UI;

namespace Gravedigger2026.Gameplay.PushMap
{
    /// <summary>UI-017: PushMap battle settlement — victory/defeat, elapsed, kills, Continue.</summary>
    public sealed class PushMapBattleSettlementView : MonoBehaviour
    {
        [SerializeField] private GameObject _root;
        [SerializeField] private Text _resultText;
        [SerializeField] private Text _elapsedText;
        [SerializeField] private Text _killsText;
        [SerializeField] private Button _continueButton;

        private Action _onContinue;

        public void Bind(GameObject root, Text resultText, Text elapsedText, Text killsText, Button continueButton)
        {
            _root = root;
            _resultText = resultText;
            _elapsedText = elapsedText;
            _killsText = killsText;
            _continueButton = continueButton;
            if (_continueButton != null)
            {
                _continueButton.onClick.RemoveListener(HandleContinue);
                _continueButton.onClick.AddListener(HandleContinue);
            }
        }

        private void Awake()
        {
            if (_continueButton != null)
            {
                _continueButton.onClick.RemoveListener(HandleContinue);
                _continueButton.onClick.AddListener(HandleContinue);
            }
        }

        public void Show(bool isVictory, float elapsedSeconds, int monstersKilled, Action onContinue)
        {
            _onContinue = onContinue;
            if (_resultText != null)
            {
                _resultText.text = isVictory ? "胜利" : "失败";
            }

            if (_elapsedText != null)
            {
                _elapsedText.text = "战斗耗时：" + FormatElapsed(elapsedSeconds);
            }

            if (_killsText != null)
            {
                _killsText.text = "击杀怪物总数：" + monstersKilled;
            }

            if (_root != null)
            {
                _root.SetActive(true);
            }
        }

        public void Hide()
        {
            if (_root != null)
            {
                _root.SetActive(false);
            }

            _onContinue = null;
        }

        private void HandleContinue()
        {
            var cb = _onContinue;
            Hide();
            cb?.Invoke();
        }

        public static string FormatElapsed(float seconds)
        {
            var total = Mathf.Max(0, Mathf.FloorToInt(seconds));
            var m = total / 60;
            var s = total % 60;
            return $"{m:00}:{s:00}";
        }
    }
}
