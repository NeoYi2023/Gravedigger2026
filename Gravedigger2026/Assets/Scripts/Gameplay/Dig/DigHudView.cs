using System;
using UnityEngine;
using UnityEngine.UI;

namespace Gravedigger2026.Gameplay.Dig
{
    public sealed class DigHudView : MonoBehaviour
    {
        [SerializeField] private GameObject _root;
        [SerializeField] private Text _timerText;
        [SerializeField] private Text _warehouseText;

        public void Show()
        {
            if (_root != null)
            {
                _root.SetActive(true);
                var image = _root.GetComponent<UnityEngine.UI.Image>();
                if (image != null)
                {
                    image.raycastTarget = false;
                }
            }
        }

        public void Hide()
        {
            if (_root != null)
            {
                _root.SetActive(false);
            }
        }

        public void SetTimer(float remaining, float total)
        {
            if (_timerText == null)
            {
                return;
            }

            _timerText.text = $"Dig 剩余 {Mathf.CeilToInt(remaining)} / {Mathf.CeilToInt(total)} 秒";
        }

        public void SetWarehouse(string summary)
        {
            if (_warehouseText != null)
            {
                _warehouseText.text = summary ?? string.Empty;
            }
        }
    }
}
