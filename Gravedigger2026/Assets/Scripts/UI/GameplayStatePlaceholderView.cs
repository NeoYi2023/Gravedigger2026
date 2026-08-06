using Gravedigger2026.Core;
using Gravedigger2026.Core.Level;
using UnityEngine;
using UnityEngine.UI;

namespace Gravedigger2026.UI
{
    public sealed class GameplayStatePlaceholderView : MonoBehaviour
    {
        [SerializeField] private GameObject _digPanel;
        [SerializeField] private GameObject _upgradeManufacturePanel;
        [SerializeField] private GameObject _defendPanel;
        [SerializeField] private Text _stateLabel;
        [SerializeField] private Text _stageInfoLabel;

        private bool _modePanelsSuppressed;
        private GameplayState _lastState = GameplayState.Dig;

        public void ShowState(GameplayState state)
        {
            _lastState = state;
            ApplyModePanels();

            if (_stateLabel != null)
            {
                _stateLabel.text = StateDisplayName(state);
            }
        }

        /// <summary>
        /// When Dig vertical presentation is active, hide large placeholder panels so the Dig camera is visible.
        /// </summary>
        public void SetModePanelsSuppressed(bool suppressed)
        {
            _modePanelsSuppressed = suppressed;
            ApplyModePanels();
        }

        public void ShowStageInfo(LevelStageContext context)
        {
            if (_stageInfoLabel == null)
            {
                return;
            }

            if (context == null)
            {
                _stageInfoLabel.text = "关卡：未运行";
                return;
            }

            var mapPart = string.IsNullOrEmpty(context.ResolvedMapId)
                ? (context.GameplayConfigIgnored ? "MapId=（UM 忽略）" : "MapId=-")
                : $"MapId={context.ResolvedMapId}";

            _stageInfoLabel.text =
                $"LevelId={context.LevelId}  StageNumber={context.StageNumber}  GameplayType={context.GameplayType}  {mapPart}";
        }

        private void ApplyModePanels()
        {
            if (_modePanelsSuppressed)
            {
                SetActiveSafe(_digPanel, false);
                SetActiveSafe(_upgradeManufacturePanel, false);
                SetActiveSafe(_defendPanel, false);
                return;
            }

            SetActiveSafe(_digPanel, _lastState == GameplayState.Dig);
            SetActiveSafe(_upgradeManufacturePanel, _lastState == GameplayState.UpgradeManufacture);
            SetActiveSafe(_defendPanel, _lastState == GameplayState.Defend);
        }

        private static void SetActiveSafe(GameObject go, bool active)
        {
            if (go != null)
            {
                go.SetActive(active);
            }
        }

        private static string StateDisplayName(GameplayState state)
        {
            switch (state)
            {
                case GameplayState.Dig:
                    return "当前玩法：挖坟（Dig）";
                case GameplayState.UpgradeManufacture:
                    return "当前玩法：升级与制造（UpgradeManufacture）";
                case GameplayState.Defend:
                    return "当前玩法：防守（Defend）";
                case GameplayState.PushMap:
                    return "当前玩法：推图战（PushMap）";
                default:
                    return "当前玩法：未知";
            }
        }
    }
}
