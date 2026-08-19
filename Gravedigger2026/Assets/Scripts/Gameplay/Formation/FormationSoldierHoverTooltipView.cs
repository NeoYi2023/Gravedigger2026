using System.Collections.Generic;
using Gravedigger2026.Core.Config;
using UnityEngine;
using UnityEngine.UI;

namespace Gravedigger2026.Gameplay.Formation
{
    /// <summary>
    /// Mode2 soldier-bar hover tooltip (SPEC_03 UI-021 / D-065 / D-070).
    /// Does not block raycasts. Icon path = Resources/UI/Skills/{SkillId}.
    /// Each Icon has a 5×5 EffectStatus child (green/red from SkillConfig.EffectImplemented).
    /// </summary>
    public sealed class FormationSoldierHoverTooltipView : MonoBehaviour
    {
        public const string SkillIconResourcesFolder = "UI/Skills";
        private const float PanelWidth = 300f;
        private const float SkillIconSize = 44f;
        private const float EffectStatusSize = 5f;
        private const int MaxSkillSlots = 8;
        private static readonly Color EffectImplementedColor = new Color(0.12f, 0.78f, 0.22f, 1f);
        private static readonly Color EffectUnimplementedColor = new Color(0.86f, 0.16f, 0.16f, 1f);

        public struct SkillItem
        {
            public string DisplayName;
            public Sprite Icon;
            public bool EffectImplemented;
        }

        public sealed class Content
        {
            public string ClassName;
            public int ClassLevel;
            public string RaceDisplayName;
            public string BaseClassDisplay;
            public string PromoteClass;
            public int MaxHp;
            public float Strength;
            public float Agility;
            public float Intelligence;
            public StatKind PrimaryStat;
            public readonly List<SkillItem> Skills = new List<SkillItem>();
        }

        private static readonly Dictionary<string, Sprite> IconCache =
            new Dictionary<string, Sprite>(System.StringComparer.Ordinal);
        private static Sprite _solidWhiteSprite;

        [SerializeField] private Text _title;
        [SerializeField] private Text _levelBadge;
        [SerializeField] private Text _raceBadge;
        [SerializeField] private Text _baseClassBadge;
        [SerializeField] private Text _promoteBadge;
        [SerializeField] private Text _hpValue;
        [SerializeField] private Text _strValue;
        [SerializeField] private Text _agiValue;
        [SerializeField] private Text _intValue;
        [SerializeField] private Text _hpPrimary;
        [SerializeField] private Text _strPrimary;
        [SerializeField] private Text _agiPrimary;
        [SerializeField] private Text _intPrimary;
        [SerializeField] private RectTransform _skillsRow;

        private readonly List<Image> _skillIcons = new List<Image>();
        private readonly List<Image> _effectStatus = new List<Image>();
        private readonly List<Text> _skillNames = new List<Text>();
        private Canvas _canvas;
        private bool _built;

        public bool IsShowing => isActiveAndEnabled && gameObject.activeSelf;

        public static Sprite LoadSkillIcon(string skillId)
        {
            if (string.IsNullOrEmpty(skillId))
            {
                return null;
            }

            if (IconCache.TryGetValue(skillId, out var cached))
            {
                return cached;
            }

            var sprite = Resources.Load<Sprite>(SkillIconResourcesFolder + "/" + skillId);
            IconCache[skillId] = sprite;
            return sprite;
        }

        private void Awake()
        {
            EnsureHierarchy();
        }

        public void Hide()
        {
            if (gameObject.activeSelf)
            {
                gameObject.SetActive(false);
            }
        }

        public void Show(RectTransform slotRect, Content content)
        {
            EnsureHierarchy();
            Bind(content);
            gameObject.SetActive(true);
            Canvas.ForceUpdateCanvases();
            PositionAbove(slotRect);
        }

        private void Bind(Content content)
        {
            if (content == null)
            {
                return;
            }

            if (_title != null)
            {
                _title.text = content.ClassName ?? string.Empty;
            }

            SetBadge(_levelBadge, content.ClassLevel < 0 ? "0级" : $"{content.ClassLevel}级", true);
            SetBadge(_raceBadge, content.RaceDisplayName, !string.IsNullOrEmpty(content.RaceDisplayName));
            SetBadge(_baseClassBadge, content.BaseClassDisplay, !string.IsNullOrEmpty(content.BaseClassDisplay));
            SetBadge(_promoteBadge, content.PromoteClass, !string.IsNullOrEmpty(content.PromoteClass));

            if (_hpValue != null)
            {
                _hpValue.text = content.MaxHp.ToString();
            }

            if (_strValue != null)
            {
                _strValue.text = FormatStat(content.Strength);
            }

            if (_agiValue != null)
            {
                _agiValue.text = FormatStat(content.Agility);
            }

            if (_intValue != null)
            {
                _intValue.text = FormatStat(content.Intelligence);
            }

            SetPrimaryMarker(_hpPrimary, false);
            SetPrimaryMarker(_strPrimary, content.PrimaryStat == StatKind.Strength);
            SetPrimaryMarker(_agiPrimary, content.PrimaryStat == StatKind.Agility);
            SetPrimaryMarker(_intPrimary, content.PrimaryStat == StatKind.Intelligence);

            BindSkills(content.Skills);
        }

        private void BindSkills(List<SkillItem> skills)
        {
            EnsureSkillSlots();
            var count = skills != null ? skills.Count : 0;
            for (var i = 0; i < _skillIcons.Count; i++)
            {
                var show = i < count;
                var icon = _skillIcons[i];
                var name = _skillNames[i];
                if (icon != null)
                {
                    icon.transform.parent.gameObject.SetActive(show);
                }

                if (!show)
                {
                    continue;
                }

                var item = skills[i];
                if (icon != null)
                {
                    icon.sprite = item.Icon;
                    icon.enabled = true;
                    icon.color = item.Icon != null ? Color.white : new Color(0.85f, 0.85f, 0.88f, 1f);
                }

                if (i < _effectStatus.Count && _effectStatus[i] != null)
                {
                    _effectStatus[i].color = item.EffectImplemented
                        ? EffectImplementedColor
                        : EffectUnimplementedColor;
                }

                if (name != null)
                {
                    name.text = item.DisplayName ?? string.Empty;
                }
            }
        }

        private void PositionAbove(RectTransform slotRect)
        {
            if (slotRect == null)
            {
                return;
            }

            var canvasRt = GetCanvasRect();
            var self = transform as RectTransform;
            if (canvasRt == null || self == null)
            {
                return;
            }

            var cam = GetEventCamera();
            var corners = new Vector3[4];
            slotRect.GetWorldCorners(corners);
            var topCenter = (corners[1] + corners[2]) * 0.5f;
            var screen = RectTransformUtility.WorldToScreenPoint(cam, topCenter);
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRt, screen, cam, out var local))
            {
                return;
            }

            self.anchorMin = new Vector2(0.5f, 0.5f);
            self.anchorMax = new Vector2(0.5f, 0.5f);
            self.pivot = new Vector2(0.5f, 0f);

            var height = Mathf.Max(self.sizeDelta.y, self.rect.height);
            var width = Mathf.Max(self.sizeDelta.x, self.rect.width);
            var pos = local + new Vector2(0f, 10f);
            var canvasRect = canvasRt.rect;
            var halfW = width * 0.5f;
            pos.x = Mathf.Clamp(pos.x, canvasRect.xMin + halfW + 8f, canvasRect.xMax - halfW - 8f);
            if (pos.y + height > canvasRect.yMax - 8f)
            {
                pos.y = canvasRect.yMax - 8f - height;
            }

            if (pos.y < canvasRect.yMin + 8f)
            {
                pos.y = canvasRect.yMin + 8f;
            }

            self.anchoredPosition = pos;
        }

        private RectTransform GetCanvasRect()
        {
            if (_canvas == null)
            {
                _canvas = GetComponentInParent<Canvas>();
            }

            return _canvas != null ? _canvas.transform as RectTransform : null;
        }

        private Camera GetEventCamera()
        {
            if (_canvas == null)
            {
                _canvas = GetComponentInParent<Canvas>();
            }

            if (_canvas == null || _canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                return null;
            }

            return _canvas.worldCamera;
        }

        private void EnsureHierarchy()
        {
            if (_built)
            {
                return;
            }

            _built = true;
            var rt = transform as RectTransform;
            if (rt != null)
            {
                rt.sizeDelta = new Vector2(PanelWidth, 236f);
            }

            var image = GetComponent<Image>();
            if (image == null)
            {
                image = gameObject.AddComponent<Image>();
            }

            image.color = Color.white;
            image.raycastTarget = false;

            var group = GetComponent<CanvasGroup>();
            if (group == null)
            {
                group = gameObject.AddComponent<CanvasGroup>();
            }

            group.blocksRaycasts = false;
            group.interactable = false;

            var font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            var body = Color.black;

            if (_title == null)
            {
                _title = CreateText(transform, "Title", "职业名", 22, TextAnchor.MiddleLeft, body, font);
                PlaceTop(_title.rectTransform, 8f, 28f);
            }

            var badgeRow = EnsureChild("BadgeRow");
            PlaceTop(badgeRow, 40f, 22f);
            var badgeHlg = badgeRow.GetComponent<HorizontalLayoutGroup>();
            if (badgeHlg == null)
            {
                badgeHlg = badgeRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            }

            badgeHlg.spacing = 6f;
            badgeHlg.childAlignment = TextAnchor.MiddleLeft;
            badgeHlg.childControlHeight = true;
            badgeHlg.childControlWidth = false;
            badgeHlg.childForceExpandWidth = false;
            badgeHlg.childForceExpandHeight = true;

            _levelBadge = EnsureBadge(badgeRow, "LevelBadge", new Color(0.98f, 0.82f, 0.18f, 1f), _levelBadge);
            _raceBadge = EnsureBadge(badgeRow, "RaceBadge", new Color(0.25f, 0.78f, 0.85f, 1f), _raceBadge);
            _baseClassBadge = EnsureBadge(badgeRow, "BaseClassBadge", new Color(0.86f, 0.28f, 0.28f, 1f), _baseClassBadge);
            _promoteBadge = EnsureBadge(badgeRow, "PromoteBadge", new Color(0.92f, 0.48f, 0.72f, 1f), _promoteBadge);

            var stats = EnsureChild("Stats");
            PlaceTop(stats, 68f, 88f);
            CreateStatRow(stats, 0, "血量", out _hpValue, out _hpPrimary, font, body);
            CreateStatRow(stats, 1, "力量", out _strValue, out _strPrimary, font, body);
            CreateStatRow(stats, 2, "敏捷", out _agiValue, out _agiPrimary, font, body);
            CreateStatRow(stats, 3, "智力", out _intValue, out _intPrimary, font, body);

            _skillsRow = EnsureChild("SkillsRow");
            PlaceBottom(_skillsRow, 8f, 64f);
            var skillsHlg = _skillsRow.GetComponent<HorizontalLayoutGroup>();
            if (skillsHlg == null)
            {
                skillsHlg = _skillsRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            }

            skillsHlg.spacing = 8f;
            skillsHlg.childAlignment = TextAnchor.UpperLeft;
            skillsHlg.childControlWidth = false;
            skillsHlg.childControlHeight = false;
            skillsHlg.childForceExpandWidth = false;
            skillsHlg.childForceExpandHeight = false;

            EnsureSkillSlots();
        }

        private void EnsureSkillSlots()
        {
            if (_skillsRow == null)
            {
                return;
            }

            var font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            while (_skillIcons.Count < MaxSkillSlots)
            {
                var index = _skillIcons.Count;
                var cell = new GameObject($"Skill_{index}", typeof(RectTransform));
                cell.transform.SetParent(_skillsRow, false);
                var cellRt = cell.GetComponent<RectTransform>();
                cellRt.sizeDelta = new Vector2(SkillIconSize + 4f, 62f);

                var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
                iconGo.transform.SetParent(cell.transform, false);
                var iconRt = iconGo.GetComponent<RectTransform>();
                iconRt.anchorMin = new Vector2(0.5f, 1f);
                iconRt.anchorMax = new Vector2(0.5f, 1f);
                iconRt.pivot = new Vector2(0.5f, 1f);
                iconRt.anchoredPosition = Vector2.zero;
                iconRt.sizeDelta = new Vector2(SkillIconSize, SkillIconSize);
                var icon = iconGo.GetComponent<Image>();
                icon.color = new Color(0.85f, 0.85f, 0.88f, 1f);
                icon.preserveAspect = true;
                icon.raycastTarget = false;
                var outline = iconGo.AddComponent<Outline>();
                outline.effectColor = new Color(0.35f, 0.35f, 0.38f, 1f);
                outline.effectDistance = new Vector2(1f, -1f);

                var statusGo = new GameObject("EffectStatus", typeof(RectTransform), typeof(Image), typeof(Outline));
                statusGo.transform.SetParent(iconGo.transform, false);
                var statusRt = statusGo.GetComponent<RectTransform>();
                statusRt.anchorMin = new Vector2(1f, 1f);
                statusRt.anchorMax = new Vector2(1f, 1f);
                statusRt.pivot = new Vector2(1f, 1f);
                statusRt.anchoredPosition = Vector2.zero;
                statusRt.sizeDelta = new Vector2(EffectStatusSize, EffectStatusSize);
                var status = statusGo.GetComponent<Image>();
                status.sprite = GetSolidWhiteSprite();
                status.type = Image.Type.Simple;
                status.color = EffectUnimplementedColor;
                status.raycastTarget = false;
                var statusOutline = statusGo.GetComponent<Outline>();
                statusOutline.effectColor = new Color(0.08f, 0.08f, 0.1f, 1f);
                statusOutline.effectDistance = new Vector2(1f, -1f);
                statusGo.transform.SetAsLastSibling();

                var name = CreateText(cell.transform, "Name", string.Empty, 10, TextAnchor.UpperCenter, Color.black, font);
                var nameRt = name.rectTransform;
                nameRt.anchorMin = new Vector2(0f, 0f);
                nameRt.anchorMax = new Vector2(1f, 0f);
                nameRt.pivot = new Vector2(0.5f, 0f);
                nameRt.anchoredPosition = Vector2.zero;
                nameRt.sizeDelta = new Vector2(0f, 16f);
                name.horizontalOverflow = HorizontalWrapMode.Overflow;
                name.verticalOverflow = VerticalWrapMode.Overflow;

                _skillIcons.Add(icon);
                _effectStatus.Add(status);
                _skillNames.Add(name);
                cell.SetActive(false);
            }
        }

        private RectTransform EnsureChild(string name)
        {
            var existing = transform.Find(name) as RectTransform;
            if (existing != null)
            {
                return existing;
            }

            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(transform, false);
            return go.GetComponent<RectTransform>();
        }

        private static Text EnsureBadge(RectTransform parent, string name, Color color, Text existing)
        {
            if (existing != null)
            {
                return existing;
            }

            var found = parent.Find(name);
            if (found != null)
            {
                var t = found.GetComponent<Text>();
                if (t != null)
                {
                    return t;
                }
            }

            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
            var le = go.GetComponent<LayoutElement>();
            le.minWidth = 36f;
            le.preferredHeight = 20f;
            le.minHeight = 20f;
            var brt = go.GetComponent<RectTransform>();
            brt.sizeDelta = new Vector2(56f, 20f);
            var fitter = go.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;
            var font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            var text = CreateText(go.transform, "Label", string.Empty, 12, TextAnchor.MiddleCenter, Color.black, font);
            Stretch(text.rectTransform);
            text.rectTransform.offsetMin = new Vector2(6f, 0f);
            text.rectTransform.offsetMax = new Vector2(-6f, 0f);
            return text;
        }

        private static void CreateStatRow(
            RectTransform parent,
            int index,
            string label,
            out Text value,
            out Text primary,
            Font font,
            Color body)
        {
            var rowName = "Stat_" + label;
            var existing = parent.Find(rowName) as RectTransform;
            RectTransform row;
            if (existing != null)
            {
                row = existing;
            }
            else
            {
                var go = new GameObject(rowName, typeof(RectTransform));
                go.transform.SetParent(parent, false);
                row = go.GetComponent<RectTransform>();
            }

            row.anchorMin = new Vector2(0f, 1f);
            row.anchorMax = new Vector2(1f, 1f);
            row.pivot = new Vector2(0f, 1f);
            row.anchoredPosition = new Vector2(0f, -index * 22f);
            row.sizeDelta = new Vector2(0f, 22f);

            var labelTf = row.Find("Label");
            if (labelTf == null)
            {
                var labelText = CreateText(row, "Label", label, 14, TextAnchor.MiddleLeft, body, font);
                var lrt = labelText.rectTransform;
                lrt.anchorMin = new Vector2(0f, 0f);
                lrt.anchorMax = new Vector2(0f, 1f);
                lrt.pivot = new Vector2(0f, 0.5f);
                lrt.anchoredPosition = Vector2.zero;
                lrt.sizeDelta = new Vector2(56f, 0f);
            }

            var valueTf = row.Find("Value");
            if (valueTf != null)
            {
                value = valueTf.GetComponent<Text>();
            }
            else
            {
                value = CreateText(row, "Value", "0", 14, TextAnchor.MiddleLeft, body, font);
                var vrt = value.rectTransform;
                vrt.anchorMin = new Vector2(0f, 0f);
                vrt.anchorMax = new Vector2(0f, 1f);
                vrt.pivot = new Vector2(0f, 0.5f);
                vrt.anchoredPosition = new Vector2(56f, 0f);
                vrt.sizeDelta = new Vector2(80f, 0f);
            }

            var primaryTf = row.Find("Primary");
            if (primaryTf != null)
            {
                primary = primaryTf.GetComponent<Text>();
            }
            else
            {
                primary = CreateText(row, "Primary", "(主属性)", 13, TextAnchor.MiddleLeft, new Color(0.75f, 0.12f, 0.12f, 1f), font);
                var prt = primary.rectTransform;
                prt.anchorMin = new Vector2(0f, 0f);
                prt.anchorMax = new Vector2(1f, 1f);
                prt.pivot = new Vector2(0f, 0.5f);
                prt.offsetMin = new Vector2(140f, 0f);
                prt.offsetMax = Vector2.zero;
            }

            primary.gameObject.SetActive(false);
        }

        private static Text CreateText(
            Transform parent,
            string name,
            string text,
            int size,
            TextAnchor anchor,
            Color color,
            Font font)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var t = go.GetComponent<Text>();
            t.text = text;
            t.fontSize = size;
            t.alignment = anchor;
            t.color = color;
            t.font = font;
            t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            return t;
        }

        private static Sprite GetSolidWhiteSprite()
        {
            if (_solidWhiteSprite != null)
            {
                return _solidWhiteSprite;
            }

            var tex = Texture2D.whiteTexture;
            _solidWhiteSprite = Sprite.Create(
                tex,
                new Rect(0f, 0f, tex.width, tex.height),
                new Vector2(0.5f, 0.5f),
                1f);
            return _solidWhiteSprite;
        }

        private static void PlaceTop(RectTransform rt, float yFromTop, float height)
        {
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -yFromTop);
            rt.sizeDelta = new Vector2(-24f, height);
        }

        private static void PlaceBottom(RectTransform rt, float yFromBottom, float height)
        {
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = new Vector2(0f, yFromBottom);
            rt.sizeDelta = new Vector2(-16f, height);
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static void SetBadge(Text badge, string text, bool visible)
        {
            if (badge == null)
            {
                return;
            }

            var root = badge.transform.parent != null ? badge.transform.parent.gameObject : badge.gameObject;
            root.SetActive(visible);
            if (visible)
            {
                badge.text = text ?? string.Empty;
            }
        }

        private static void SetPrimaryMarker(Text marker, bool visible)
        {
            if (marker != null)
            {
                marker.gameObject.SetActive(visible);
            }
        }

        private static string FormatStat(float value)
        {
            if (Mathf.Abs(value - Mathf.Round(value)) < 0.05f)
            {
                return Mathf.RoundToInt(value).ToString();
            }

            return value.ToString("0.##");
        }
    }
}
