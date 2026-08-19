using System.Collections.Generic;
using System.Text;
using Gravedigger2026.Core.Combat;
using Gravedigger2026.Core.Config;
using Gravedigger2026.Core.UpgradeManufacture;
using UnityEngine;
using UnityEngine.UI;

namespace Gravedigger2026.Gameplay.Formation
{
    /// <summary>
    /// Modal listing all formation bonds and activation progress (SPEC_03 §3.17).
    /// </summary>
    public sealed class FormationBondDetailView : MonoBehaviour
    {
        [SerializeField] private GameObject _root;
        [SerializeField] private Text _titleText;
        [SerializeField] private Text _bodyText;
        [SerializeField] private Button _closeButton;

        public void Configure(GameObject root, Text titleText, Text bodyText, Button closeButton)
        {
            _root = root;
            _titleText = titleText;
            _bodyText = bodyText;
            _closeButton = closeButton;
            // PushMap/Runtime: Awake() 可能先于 Configure() 运行，导致关闭监听丢失；
            // 因此关闭绑定必须在 Configure() 中也补齐。
            if (_closeButton != null)
            {
                _closeButton.onClick.RemoveListener(Hide);
                _closeButton.onClick.AddListener(Hide);
            }
            // Important: Awake() may have already run before Configure() (runtime AddComponent path).
            // Ensure the modal is deterministically hidden by default after binding references.
            Hide();
        }

        private void Awake()
        {
            if (_closeButton != null)
            {
                _closeButton.onClick.RemoveListener(Hide);
                _closeButton.onClick.AddListener(Hide);
            }

            Hide();
        }

        private void OnDestroy()
        {
            if (_closeButton != null)
            {
                _closeButton.onClick.RemoveListener(Hide);
            }
        }

        public void ShowLive(
            BattleFormationService formation,
            WarriorPoolService pool,
            ConfigCsvRepository configs)
        {
            if (configs == null || !configs.IsLoaded)
            {
                ShowText("阵容羁绊", "配置未加载。");
                return;
            }

            var evaluated = FormationBondEvaluator.Evaluate(formation, pool, configs);
            ShowEvaluated(evaluated, configs, combatSnapshot: false);
        }

        public void ShowEvaluated(
            IReadOnlyList<ActiveFormationBond> bonds,
            ConfigCsvRepository configs,
            bool combatSnapshot = false)
        {
            var title = combatSnapshot ? "阵容羁绊（战斗）" : "阵容羁绊";
            if (bonds == null || bonds.Count == 0)
            {
                ShowText(title, "暂无阵容羁绊配置。");
                return;
            }

            ShowText(title, BuildBody(bonds, configs, liveProgress: !combatSnapshot));
        }

        public void ShowSnapshot(IReadOnlyList<ActiveFormationBond> activeOnly, ConfigCsvRepository configs)
        {
            ShowEvaluated(activeOnly, configs, combatSnapshot: true);
        }

        public void Hide()
        {
            if (_root != null)
            {
                _root.SetActive(false);
            }
        }

        private void ShowText(string title, string body)
        {
            if (_titleText != null)
            {
                _titleText.text = title;
            }

            if (_bodyText != null)
            {
                _bodyText.text = body;
            }

            if (_root != null)
            {
                _root.SetActive(true);
            }
        }

        private static string BuildBody(
            IReadOnlyList<ActiveFormationBond> bonds,
            ConfigCsvRepository configs,
            bool liveProgress)
        {
            if (bonds == null || bonds.Count == 0)
            {
                return "暂无阵容羁绊配置。";
            }

            var sb = new StringBuilder(512);
            string lastBondId = null;
            for (var i = 0; i < bonds.Count; i++)
            {
                var bond = bonds[i];
                var row = bond.Row;
                if (row == null)
                {
                    continue;
                }

                if (!string.Equals(lastBondId, row.BondId, System.StringComparison.Ordinal))
                {
                    if (lastBondId != null)
                    {
                        sb.AppendLine();
                    }

                    lastBondId = row.BondId;
                }

                var status = bond.State switch
                {
                    FormationBondDisplayState.Active => "【已激活】",
                    FormationBondDisplayState.Superseded => "【已被高等级替代】",
                    _ => "【未激活】"
                };

                var name = string.IsNullOrEmpty(row.DisplayName) ? row.BondId : row.DisplayName;
                sb.Append(status).Append(' ').Append(name);
                if (row.BondLevel > 0)
                {
                    sb.Append(" Lv").Append(row.BondLevel);
                }

                sb.AppendLine();
                if (!string.IsNullOrEmpty(row.Description))
                {
                    sb.AppendLine(row.Description);
                }

                if (liveProgress || bond.State != FormationBondDisplayState.Inactive)
                {
                    sb.Append("进度：")
                        .Append(FormationBondEvaluator.FormatProgressLabel(bond))
                        .AppendLine();
                }

                if (!string.IsNullOrEmpty(row.BondBuff)
                    && configs.TryGetSkillEffect(row.BondBuff, out var effect)
                    && effect != null
                    && !string.IsNullOrEmpty(effect.Notes))
                {
                    sb.Append("效果：").Append(effect.Notes).AppendLine();
                }

                sb.AppendLine();
            }

            return sb.ToString().TrimEnd();
        }
    }
}
