using System;
using UnityEngine;
using UnityEngine.UI;

namespace Gravedigger2026.UI
{
    public sealed class ToolsPanelView : MonoBehaviour
    {
        [SerializeField] private GameObject _root;
        [SerializeField] private Button _settingsButton;
        [SerializeField] private Button _levelButton;
        [SerializeField] private Button _grantProtagonistEquipmentButton;
        [SerializeField] private Button _grantMagicBookButton;
        [SerializeField] private Button _closeButton;

        public event Action SettingsClicked;
        public event Action LevelClicked;
        public event Action GrantProtagonistEquipmentClicked;
        public event Action GrantMagicBookClicked;

        private void Awake()
        {
            // Prefab starts inactive. Do NOT SetActive(false) here: when _root is this
            // GameObject, Awake only runs on the first Show(), and hiding again cancels that click.
            if (_settingsButton != null)
            {
                _settingsButton.onClick.AddListener(() => SettingsClicked?.Invoke());
            }

            if (_levelButton != null)
            {
                _levelButton.onClick.AddListener(() => LevelClicked?.Invoke());
            }

            EnsureGrantButtons();

            if (_grantProtagonistEquipmentButton != null)
            {
                _grantProtagonistEquipmentButton.onClick.AddListener(
                    () => GrantProtagonistEquipmentClicked?.Invoke());
            }

            if (_grantMagicBookButton != null)
            {
                _grantMagicBookButton.onClick.AddListener(() => GrantMagicBookClicked?.Invoke());
            }

            if (_closeButton != null)
            {
                _closeButton.onClick.AddListener(Hide);
            }
        }

        public bool IsOpen => _root != null && _root.activeSelf;

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

        public void Toggle()
        {
            if (IsOpen)
            {
                Hide();
            }
            else
            {
                Show();
            }
        }

        private void EnsureGrantButtons()
        {
            if (_levelButton == null)
            {
                return;
            }

            if (_grantProtagonistEquipmentButton == null)
            {
                _grantProtagonistEquipmentButton = CloneToolButton(
                    "GrantProtagonistEquipmentButton",
                    "增加主角装备",
                    new Color(0.30f, 0.48f, 0.42f, 1f),
                    new Vector2(0f, -164f));
            }

            if (_grantMagicBookButton == null)
            {
                _grantMagicBookButton = CloneToolButton(
                    "GrantMagicBookButton",
                    "增加魔法书",
                    new Color(0.38f, 0.36f, 0.52f, 1f),
                    new Vector2(0f, -216f));
            }

            var rootRt = _root != null ? _root.GetComponent<RectTransform>() : transform as RectTransform;
            if (rootRt != null && rootRt.sizeDelta.y < 360f)
            {
                rootRt.sizeDelta = new Vector2(rootRt.sizeDelta.x, 380f);
            }

            RelayoutToolButton(_settingsButton, new Vector2(0f, -60f), new Vector2(200f, 44f));
            RelayoutToolButton(_levelButton, new Vector2(0f, -112f), new Vector2(200f, 44f));
            RelayoutToolButton(_grantProtagonistEquipmentButton, new Vector2(0f, -164f), new Vector2(200f, 44f));
            RelayoutToolButton(_grantMagicBookButton, new Vector2(0f, -216f), new Vector2(200f, 44f));
            RelayoutToolButton(_closeButton, new Vector2(0f, -272f), new Vector2(200f, 40f));
        }

        private static void RelayoutToolButton(Button button, Vector2 anchoredPos, Vector2 size)
        {
            if (button == null)
            {
                return;
            }

            var rt = button.GetComponent<RectTransform>();
            if (rt == null)
            {
                return;
            }

            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;
        }

        private Button CloneToolButton(string name, string label, Color color, Vector2 anchoredPos)
        {
            var clone = Instantiate(_levelButton.gameObject, _levelButton.transform.parent);
            clone.name = name;
            var rt = clone.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchorMin = new Vector2(0.5f, 1f);
                rt.anchorMax = new Vector2(0.5f, 1f);
                rt.pivot = new Vector2(0.5f, 1f);
                rt.anchoredPosition = anchoredPos;
                rt.sizeDelta = new Vector2(200f, 44f);
            }

            var image = clone.GetComponent<Image>();
            if (image != null)
            {
                image.color = color;
            }

            var text = clone.GetComponentInChildren<Text>(true);
            if (text != null)
            {
                text.text = label;
            }

            var button = clone.GetComponent<Button>();
            if (button != null)
            {
                button.onClick.RemoveAllListeners();
            }

            return button;
        }
    }
}
