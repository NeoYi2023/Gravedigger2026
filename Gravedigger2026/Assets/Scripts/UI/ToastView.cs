using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Gravedigger2026.UI
{
    /// <summary>
    /// Toast host GameObject stays active so coroutines can run; visibility via CanvasGroup
    /// (or a child <see cref="_root"/> when distinct from this object).
    /// </summary>
    public sealed class ToastView : MonoBehaviour
    {
        [SerializeField] private GameObject _root;
        [SerializeField] private Text _messageText;
        [SerializeField] private float _visibleSeconds = 1.6f;

        private Coroutine _routine;
        private CanvasGroup _canvasGroup;

        private void Awake()
        {
            EnsureCanvasGroup();
            SetVisible(false);
        }

        public void RuntimeConfigure(GameObject root, Text messageText, float visibleSeconds)
        {
            _root = root;
            _messageText = messageText;
            _visibleSeconds = visibleSeconds;
            EnsureCanvasGroup();
            SetVisible(false);
        }

        public void Show(string message)
        {
            Show(message, _visibleSeconds);
        }

        public void Show(string message, float visibleSeconds)
        {
            if (_messageText != null)
            {
                _messageText.text = message;
            }

            // Host must stay active for StartCoroutine (never SetActive(false) on this GO).
            if (!gameObject.activeSelf)
            {
                gameObject.SetActive(true);
            }

            SetVisible(true);

            if (_routine != null)
            {
                StopCoroutine(_routine);
            }

            var seconds = visibleSeconds > 0f ? visibleSeconds : _visibleSeconds;
            _routine = StartCoroutine(HideAfterDelay(seconds));
        }

        private IEnumerator HideAfterDelay(float visibleSeconds)
        {
            yield return new WaitForSecondsRealtime(visibleSeconds);
            SetVisible(false);
            _routine = null;
        }

        private void SetVisible(bool visible)
        {
            if (_root != null && _root != gameObject)
            {
                _root.SetActive(visible);
                return;
            }

            var cg = EnsureCanvasGroup();
            cg.alpha = visible ? 1f : 0f;
            cg.interactable = visible;
            cg.blocksRaycasts = visible;
        }

        private CanvasGroup EnsureCanvasGroup()
        {
            if (_canvasGroup == null)
            {
                _canvasGroup = GetComponent<CanvasGroup>();
                if (_canvasGroup == null)
                {
                    _canvasGroup = gameObject.AddComponent<CanvasGroup>();
                }
            }

            return _canvasGroup;
        }
    }
}
