using UnityEngine;
using UnityEngine.UI;

namespace Gravedigger2026.Gameplay.AutoManufacture
{
    /// <summary>
    /// One AutoManufacture presentation soldier card (UI-016 Step1/2).
    /// </summary>
    public sealed class AutoMfgSoldierCardView : MonoBehaviour
    {
        public const float CardWidth = 150f;
        public const float CardHeight = 200f;

        [SerializeField] private Image _background;
        [SerializeField] private Text _questionMark;
        [SerializeField] private Text _className;
        [SerializeField] private Text _classLevel;
        [SerializeField] private Image _idleThumbnail;
        [SerializeField] private RawImage _livePreview;
        [SerializeField] private Text _amplifyLabel;

        private string _warriorId;
        private RectTransform _rect;

        public string WarriorId => _warriorId;
        public RectTransform RectTransform => _rect != null ? _rect : (_rect = transform as RectTransform);

        public void RuntimeWire(
            Image background,
            Text questionMark,
            Text className,
            Text classLevel,
            Image idleThumbnail,
            Text amplifyLabel,
            RawImage livePreview = null)
        {
            _background = background;
            _questionMark = questionMark;
            _className = className;
            _classLevel = classLevel;
            _idleThumbnail = idleThumbnail;
            _amplifyLabel = amplifyLabel;
            _livePreview = livePreview;
            _rect = transform as RectTransform;
            EnsureLivePreview();
        }

        public void BindMystery(string warriorId, string className, int classLevel)
        {
            _warriorId = warriorId ?? string.Empty;
            RefreshClass(className, classLevel);

            if (_questionMark != null)
            {
                _questionMark.text = "?";
                _questionMark.gameObject.SetActive(true);
            }

            if (_idleThumbnail != null)
            {
                _idleThumbnail.enabled = false;
                _idleThumbnail.sprite = null;
            }

            HideLivePreview();
            SetAmplifyVisible(false);
        }

        /// <summary>Update class name / Lv after ForceClass (etc.) during Step2 book pulse.</summary>
        public void RefreshClass(string className, int classLevel)
        {
            if (_className != null)
            {
                _className.text = className ?? string.Empty;
                _className.gameObject.SetActive(true);
            }

            if (_classLevel != null)
            {
                var level = classLevel < 0 ? 0 : classLevel;
                _classLevel.text = "Lv." + level;
                _classLevel.gameObject.SetActive(true);
            }
        }

        public void RevealIdle(Sprite idleSprite)
        {
            HideQuestion();
            HideLivePreview();

            if (_idleThumbnail != null)
            {
                _idleThumbnail.sprite = idleSprite;
                _idleThumbnail.enabled = true;
                _idleThumbnail.preserveAspect = true;
                _idleThumbnail.color = idleSprite != null
                    ? Color.white
                    : new Color(1f, 1f, 1f, 0.2f);
            }
        }

        public void ShowLivePreview(Texture texture)
        {
            HideQuestion();
            EnsureLivePreview();
            if (_livePreview == null)
            {
                return;
            }

            if (_idleThumbnail != null)
            {
                _idleThumbnail.enabled = false;
            }

            _livePreview.texture = texture;
            _livePreview.enabled = texture != null;
            _livePreview.color = Color.white;
            _livePreview.gameObject.SetActive(true);
        }

        public void SetAmplifyVisible(bool visible)
        {
            if (_amplifyLabel != null)
            {
                _amplifyLabel.gameObject.SetActive(visible);
                if (visible)
                {
                    _amplifyLabel.text = "加强";
                }
            }
        }

        private void HideQuestion()
        {
            if (_questionMark != null)
            {
                _questionMark.gameObject.SetActive(false);
            }
        }

        private void HideLivePreview()
        {
            if (_livePreview != null)
            {
                _livePreview.enabled = false;
                _livePreview.texture = null;
            }
        }

        private void EnsureLivePreview()
        {
            if (_livePreview != null)
            {
                return;
            }

            var host = _idleThumbnail != null ? _idleThumbnail.transform : transform;
            var existing = host.Find("LivePreview");
            if (existing != null)
            {
                _livePreview = existing.GetComponent<RawImage>();
                if (_livePreview != null)
                {
                    return;
                }
            }

            var go = new GameObject("LivePreview", typeof(RectTransform), typeof(RawImage));
            go.transform.SetParent(host, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            _livePreview = go.GetComponent<RawImage>();
            _livePreview.raycastTarget = false;
            _livePreview.enabled = false;
            var fitter = go.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            fitter.aspectRatio = 1f;
        }
    }
}
