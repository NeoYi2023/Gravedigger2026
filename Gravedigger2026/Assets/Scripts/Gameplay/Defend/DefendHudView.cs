using System;
using Gravedigger2026.Gameplay.Formation;
using UnityEngine;
using UnityEngine.UI;

namespace Gravedigger2026.Gameplay.Defend
{
    /// <summary>
    /// Defend Prepare + Combat HUD (UI-009 StartBattle; Shield + countdown).
    /// </summary>
    public sealed class DefendHudView : MonoBehaviour
    {
        [SerializeField] private GameObject _root;
        [SerializeField] private GameObject _preparePanel;
        [SerializeField] private GameObject _combatPanel;
        [SerializeField] private Text _phaseText;
        [SerializeField] private Text _combatStatusText;
        [SerializeField] private Button _startBattleButton;
        [SerializeField] private Text _hintText;
        [SerializeField] private FormationBondHudView _combatBondHud;

        public FormationBondHudView CombatBondHud => _combatBondHud;

        public event Action StartBattleRequested;

        private void OnEnable()
        {
            if (_startBattleButton != null)
            {
                _startBattleButton.onClick.AddListener(HandleStartBattle);
            }
        }

        private void OnDisable()
        {
            if (_startBattleButton != null)
            {
                _startBattleButton.onClick.RemoveListener(HandleStartBattle);
            }
        }

        public void Show()
        {
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
        }

        public void SetPrepareVisible(bool visible)
        {
            if (_preparePanel != null)
            {
                _preparePanel.SetActive(visible);
            }
        }

        public void SetCombatVisible(bool visible)
        {
            if (_combatPanel != null)
            {
                _combatPanel.SetActive(visible);
            }
        }

        public void SetPhaseText(string text)
        {
            if (_phaseText != null)
            {
                _phaseText.text = text ?? string.Empty;
            }
        }

        public void SetCombatStatus(string text)
        {
            if (_combatStatusText != null)
            {
                _combatStatusText.text = text ?? string.Empty;
            }
        }

        public void SetHint(string text)
        {
            if (_hintText != null)
            {
                _hintText.text = text ?? string.Empty;
            }
        }

        public void SetCombatBondHudVisible(bool visible)
        {
            if (_combatBondHud != null)
            {
                _combatBondHud.gameObject.SetActive(visible);
            }
        }

        public void SetStartBattleInteractable(bool interactable)
        {
            if (_startBattleButton != null)
            {
                _startBattleButton.interactable = interactable;
            }
        }

        private void HandleStartBattle()
        {
            StartBattleRequested?.Invoke();
        }
    }
}
