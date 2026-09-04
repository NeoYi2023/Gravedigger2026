using System;
using Gravedigger2026.Core.Defend;
using UnityEngine;
using UnityEngine.UI;

namespace Gravedigger2026.Gameplay.PushMap
{
    /// <summary>
    /// UI-017: shared PushMap / SearchExtract battle settlement —
    /// victory (time/kills/casualties/class rows + Continue) or defeat (casualties + Title/Restart).
    /// </summary>
    public sealed class PushMapBattleSettlementView : MonoBehaviour
    {
        [SerializeField] private GameObject _root;
        [SerializeField] private Text _resultText;
        [SerializeField] private Text _elapsedText;
        [SerializeField] private Text _killsText;
        [SerializeField] private Text _casualtyTotalText;
        [SerializeField] private GameObject _classRowRoot;
        [SerializeField] private Text _warriorCountText;
        [SerializeField] private Text _archerCountText;
        [SerializeField] private Text _mageCountText;
        [SerializeField] private Text _thiefCountText;
        [SerializeField] private Button _continueButton;
        [SerializeField] private Button _returnTitleButton;
        [SerializeField] private Button _restartButton;
        [SerializeField] private GameObject _victoryButtonsRoot;
        [SerializeField] private GameObject _defeatButtonsRoot;

        private Action _onContinue;
        private Action _onReturnTitle;
        private Action _onRestart;

        public void Bind(
            GameObject root,
            Text resultText,
            Text elapsedText,
            Text killsText,
            Text casualtyTotalText,
            GameObject classRowRoot,
            Text warriorCountText,
            Text archerCountText,
            Text mageCountText,
            Text thiefCountText,
            Button continueButton,
            Button returnTitleButton,
            Button restartButton,
            GameObject victoryButtonsRoot,
            GameObject defeatButtonsRoot)
        {
            _root = root;
            _resultText = resultText;
            _elapsedText = elapsedText;
            _killsText = killsText;
            _casualtyTotalText = casualtyTotalText;
            _classRowRoot = classRowRoot;
            _warriorCountText = warriorCountText;
            _archerCountText = archerCountText;
            _mageCountText = mageCountText;
            _thiefCountText = thiefCountText;
            _continueButton = continueButton;
            _returnTitleButton = returnTitleButton;
            _restartButton = restartButton;
            _victoryButtonsRoot = victoryButtonsRoot;
            _defeatButtonsRoot = defeatButtonsRoot;
            WireButtons();
        }

        private void Awake()
        {
            WireButtons();
        }

        private void WireButtons()
        {
            if (_continueButton != null)
            {
                _continueButton.onClick.RemoveListener(HandleContinue);
                _continueButton.onClick.AddListener(HandleContinue);
            }

            if (_returnTitleButton != null)
            {
                _returnTitleButton.onClick.RemoveListener(HandleReturnTitle);
                _returnTitleButton.onClick.AddListener(HandleReturnTitle);
            }

            if (_restartButton != null)
            {
                _restartButton.onClick.RemoveListener(HandleRestart);
                _restartButton.onClick.AddListener(HandleRestart);
            }
        }

        public void ShowVictory(
            float elapsedSeconds,
            int monstersKilled,
            bool showKills,
            bool showElapsed,
            BattleCasualtyStats casualties,
            Action onContinue)
        {
            _onContinue = onContinue;
            _onReturnTitle = null;
            _onRestart = null;

            if (_resultText != null)
            {
                _resultText.text = "胜利";
            }

            if (_elapsedText != null)
            {
                _elapsedText.gameObject.SetActive(showElapsed);
                if (showElapsed)
                {
                    _elapsedText.text = "战斗耗时：" + FormatElapsed(elapsedSeconds);
                }
            }

            if (_killsText != null)
            {
                _killsText.gameObject.SetActive(showKills);
                if (showKills)
                {
                    _killsText.text = "击杀怪物总数：" + monstersKilled;
                }
            }

            ApplyCasualtyTotal(casualties.Total);
            ApplyClassRows(casualties, visible: true);
            SetButtonMode(victory: true);

            if (_root != null)
            {
                _root.SetActive(true);
            }
        }

        public void ShowDefeat(BattleCasualtyStats casualties, Action onReturnTitle, Action onRestart)
        {
            _onContinue = null;
            _onReturnTitle = onReturnTitle;
            _onRestart = onRestart;

            if (_resultText != null)
            {
                _resultText.text = "失败";
            }

            if (_elapsedText != null)
            {
                _elapsedText.gameObject.SetActive(false);
            }

            if (_killsText != null)
            {
                _killsText.gameObject.SetActive(false);
            }

            ApplyCasualtyTotal(casualties.Total);
            ApplyClassRows(casualties, visible: false);
            SetButtonMode(victory: false);

            if (_root != null)
            {
                _root.SetActive(true);
            }
        }

        /// <summary>Legacy entry used by older callers; prefer ShowVictory / ShowDefeat.</summary>
        public void Show(bool isVictory, float elapsedSeconds, int monstersKilled, Action onContinue)
        {
            if (isVictory)
            {
                ShowVictory(elapsedSeconds, monstersKilled, showKills: true, showElapsed: true, default, onContinue);
            }
            else
            {
                ShowDefeat(default, onContinue, onContinue);
            }
        }

        public void Hide()
        {
            if (_root != null)
            {
                _root.SetActive(false);
            }

            _onContinue = null;
            _onReturnTitle = null;
            _onRestart = null;
        }

        private void ApplyCasualtyTotal(int total)
        {
            if (_casualtyTotalText != null)
            {
                _casualtyTotalText.gameObject.SetActive(true);
                _casualtyTotalText.text = "阵亡士兵总数：" + total;
            }
        }

        private void ApplyClassRows(BattleCasualtyStats casualties, bool visible)
        {
            if (_classRowRoot != null)
            {
                _classRowRoot.SetActive(visible);
            }

            if (!visible)
            {
                return;
            }

            SetCount(_warriorCountText, casualties.Warrior);
            SetCount(_archerCountText, casualties.Archer);
            SetCount(_mageCountText, casualties.Mage);
            SetCount(_thiefCountText, casualties.Thief);
        }

        private static void SetCount(Text text, int count)
        {
            if (text != null)
            {
                text.text = count.ToString();
            }
        }

        private void SetButtonMode(bool victory)
        {
            if (_victoryButtonsRoot != null)
            {
                _victoryButtonsRoot.SetActive(victory);
            }

            if (_defeatButtonsRoot != null)
            {
                _defeatButtonsRoot.SetActive(!victory);
            }

            if (_continueButton != null && _victoryButtonsRoot == null)
            {
                _continueButton.gameObject.SetActive(victory);
            }

            if (_returnTitleButton != null && _defeatButtonsRoot == null)
            {
                _returnTitleButton.gameObject.SetActive(!victory);
            }

            if (_restartButton != null && _defeatButtonsRoot == null)
            {
                _restartButton.gameObject.SetActive(!victory);
            }
        }

        private void HandleContinue()
        {
            var cb = _onContinue;
            Hide();
            cb?.Invoke();
        }

        private void HandleReturnTitle()
        {
            var cb = _onReturnTitle;
            Hide();
            cb?.Invoke();
        }

        private void HandleRestart()
        {
            var cb = _onRestart;
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
