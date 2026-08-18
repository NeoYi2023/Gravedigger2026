using System.Collections.Generic;
using Gravedigger2026.Core.Config;
using Gravedigger2026.Core.ProtagonistEquipment;
using UnityEngine;
using UnityEngine.UI;

namespace Gravedigger2026.UI
{
    /// <summary>
    /// UI-022 / D-067: InSaveShell read-only protagonist equipment warehouse (OwnedEquips).
    /// </summary>
    public sealed class EquipmentWarehousePanelView : MonoBehaviour
    {
        private const string EmptyWarehouseHint = "尚未拥有装备";
        private const float RowHeight = 96f;

        [SerializeField] private GameObject _root;
        [SerializeField] private Text _titleText;
        [SerializeField] private Button _closeButton;
        [SerializeField] private Transform _listContent;
        [SerializeField] private GameObject _rowTemplate;
        [SerializeField] private Text _emptyHintText;

        private readonly List<GameObject> _spawnedRows = new List<GameObject>();
        private ProtagonistEquipmentService _equipment;
        private ConfigCsvRepository _configs;
        private bool _changedSubscribed;

        public event System.Action Closed;

        private void Awake()
        {
            // Prefab starts inactive. Do NOT SetActive(false) on self/_root here — see ToolsPanelView.
            if (_closeButton != null)
            {
                _closeButton.onClick.AddListener(HandleCloseClicked);
            }

            EnsureRuntimeUi();
            if (_rowTemplate != null)
            {
                _rowTemplate.SetActive(false);
            }
        }

        private void OnEnable()
        {
            SubscribeChanged();
            RefreshList();
        }

        private void OnDisable()
        {
            UnsubscribeChanged();
        }

        private void OnDestroy()
        {
            UnsubscribeChanged();
        }

        public bool IsOpen => _root != null && _root.activeSelf;

        public void Bind(ProtagonistEquipmentService equipment, ConfigCsvRepository configs)
        {
            UnsubscribeChanged();
            _equipment = equipment;
            _configs = configs;
            if (isActiveAndEnabled)
            {
                SubscribeChanged();
                RefreshList();
            }
        }

        public void Show()
        {
            EnsureRuntimeUi();
            if (_root != null)
            {
                _root.transform.SetAsLastSibling();
                _root.SetActive(true);
            }

            RefreshList();
        }

        public void Hide()
        {
            if (_root != null)
            {
                _root.SetActive(false);
            }
        }

        /// <summary>
        /// Builds Scroll + row template + empty hint when Prefab has not been patched yet.
        /// </summary>
        public void EnsureRuntimeUi()
        {
            if (_root == null)
            {
                _root = gameObject;
            }

            if (_listContent != null && _rowTemplate != null && _emptyHintText != null)
            {
                return;
            }

            var box = transform.Find("Box");
            if (box == null)
            {
                return;
            }

            if (_listContent == null || _rowTemplate == null)
            {
                BuildScroll(box);
            }

            if (_emptyHintText == null)
            {
                _emptyHintText = CreateEmptyHint(box);
            }
        }

        private void HandleCloseClicked()
        {
            Hide();
            Closed?.Invoke();
        }

        private void SubscribeChanged()
        {
            if (_changedSubscribed || _equipment == null)
            {
                return;
            }

            _equipment.Changed += HandleEquipmentChanged;
            _changedSubscribed = true;
        }

        private void UnsubscribeChanged()
        {
            if (!_changedSubscribed || _equipment == null)
            {
                _changedSubscribed = false;
                return;
            }

            _equipment.Changed -= HandleEquipmentChanged;
            _changedSubscribed = false;
        }

        private void HandleEquipmentChanged()
        {
            RefreshList();
        }

        private void RefreshList()
        {
            EnsureRuntimeUi();
            ClearRows();

            var owned = _equipment != null ? _equipment.OwnedEquips : null;
            var count = owned != null ? owned.Count : 0;
            if (_emptyHintText != null)
            {
                _emptyHintText.text = EmptyWarehouseHint;
                _emptyHintText.gameObject.SetActive(count == 0);
            }

            if (count == 0 || _listContent == null || _rowTemplate == null)
            {
                return;
            }

            _rowTemplate.SetActive(false);
            for (var i = 0; i < count; i++)
            {
                var piece = owned[i];
                if (piece == null || string.IsNullOrEmpty(piece.EquipId))
                {
                    continue;
                }

                var go = Instantiate(_rowTemplate, _listContent);
                go.name = "EquipRow_" + piece.EquipId;
                go.SetActive(true);
                BindRow(go, piece);
                _spawnedRows.Add(go);
            }
        }

        private void BindRow(GameObject row, OwnedEquip piece)
        {
            ProtagonistEquipmentConfigRow configRow = null;
            if (_configs != null)
            {
                _configs.TryGetProtagonistEquipment(piece.EquipId, piece.Level, out configRow);
            }

            var displayName = configRow != null && !string.IsNullOrEmpty(configRow.DisplayName)
                ? configRow.DisplayName
                : piece.EquipId;
            var description = configRow != null ? configRow.Description : string.Empty;

            var title = row.transform.Find("Title")?.GetComponent<Text>();
            if (title != null)
            {
                title.text = displayName + " Lv." + piece.Level;
            }

            var desc = row.transform.Find("Description")?.GetComponent<Text>();
            if (desc != null)
            {
                desc.text = description ?? string.Empty;
            }

            var icon = row.transform.Find("Icon")?.GetComponent<Image>();
            if (icon != null)
            {
                Sprite sprite = null;
                if (configRow != null && !string.IsNullOrEmpty(configRow.IconAssetId))
                {
                    sprite = Resources.Load<Sprite>(configRow.IconAssetId);
                }

                icon.sprite = sprite;
                icon.enabled = sprite != null;
            }

            var button = row.GetComponent<Button>();
            if (button != null)
            {
                button.interactable = false;
                button.onClick.RemoveAllListeners();
            }
        }

        private void ClearRows()
        {
            for (var i = 0; i < _spawnedRows.Count; i++)
            {
                if (_spawnedRows[i] != null)
                {
                    Destroy(_spawnedRows[i]);
                }
            }

            _spawnedRows.Clear();
        }

        private void BuildScroll(Transform box)
        {
            var existingScroll = box.Find("EquipScroll");
            Transform content = null;
            GameObject rowTemplate = null;
            if (existingScroll != null)
            {
                content = existingScroll.Find("Viewport/Content");
                if (content != null)
                {
                    var existingRow = content.Find("EquipRowTemplate");
                    if (existingRow != null)
                    {
                        rowTemplate = existingRow.gameObject;
                    }
                }
            }

            if (content == null)
            {
                var scrollGo = new GameObject("EquipScroll", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
                scrollGo.transform.SetParent(box, false);
                var scrollRt = scrollGo.GetComponent<RectTransform>();
                Place(scrollRt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                    new Vector2(0f, 10f), new Vector2(460f, 400f));
                scrollGo.GetComponent<Image>().color = new Color(0.10f, 0.11f, 0.14f, 1f);

                var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
                viewport.transform.SetParent(scrollGo.transform, false);
                StretchFull(viewport.GetComponent<RectTransform>());
                viewport.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.02f);
                viewport.GetComponent<Mask>().showMaskGraphic = false;

                var contentGo = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup),
                    typeof(ContentSizeFitter));
                contentGo.transform.SetParent(viewport.transform, false);
                var contentRt = contentGo.GetComponent<RectTransform>();
                contentRt.anchorMin = new Vector2(0f, 1f);
                contentRt.anchorMax = new Vector2(1f, 1f);
                contentRt.pivot = new Vector2(0.5f, 1f);
                contentRt.anchoredPosition = Vector2.zero;
                contentRt.sizeDelta = new Vector2(0f, 0f);
                var vlg = contentGo.GetComponent<VerticalLayoutGroup>();
                vlg.padding = new RectOffset(12, 12, 12, 12);
                vlg.spacing = 10f;
                vlg.childAlignment = TextAnchor.UpperCenter;
                vlg.childControlWidth = true;
                vlg.childControlHeight = false;
                vlg.childForceExpandWidth = true;
                vlg.childForceExpandHeight = false;
                var fitter = contentGo.GetComponent<ContentSizeFitter>();
                fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
                fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

                var scroll = scrollGo.GetComponent<ScrollRect>();
                scroll.viewport = viewport.GetComponent<RectTransform>();
                scroll.content = contentRt;
                scroll.horizontal = false;
                scroll.vertical = true;
                scroll.movementType = ScrollRect.MovementType.Clamped;
                content = contentGo.transform;
            }

            if (rowTemplate == null && content != null)
            {
                rowTemplate = CreateEquipRowTemplate(content);
            }

            _listContent = content;
            _rowTemplate = rowTemplate;
            if (_rowTemplate != null)
            {
                _rowTemplate.SetActive(false);
            }
        }

        private static GameObject CreateEquipRowTemplate(Transform content)
        {
            var go = new GameObject("EquipRowTemplate", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image),
                typeof(LayoutElement));
            go.transform.SetParent(content, false);
            go.GetComponent<Image>().color = new Color(0.28f, 0.38f, 0.52f, 1f);
            var le = go.GetComponent<LayoutElement>();
            le.minHeight = RowHeight;
            le.preferredHeight = RowHeight;

            var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            iconGo.transform.SetParent(go.transform, false);
            Place(iconGo.GetComponent<RectTransform>(), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(0f, 0.5f), new Vector2(12f, 0f), new Vector2(64f, 64f));
            var icon = iconGo.GetComponent<Image>();
            icon.color = Color.white;
            icon.preserveAspect = true;
            icon.raycastTarget = false;

            var title = CreateUiText(go.transform, "Title", "Name Lv.1", 20, TextAnchor.MiddleLeft);
            StretchFull(title.rectTransform);
            title.rectTransform.offsetMin = new Vector2(84f, 56f);
            title.rectTransform.offsetMax = new Vector2(-12f, -8f);
            title.horizontalOverflow = HorizontalWrapMode.Overflow;

            var desc = CreateUiText(go.transform, "Description", "Description", 16, TextAnchor.UpperLeft);
            StretchFull(desc.rectTransform);
            desc.rectTransform.offsetMin = new Vector2(84f, 8f);
            desc.rectTransform.offsetMax = new Vector2(-12f, -40f);
            desc.horizontalOverflow = HorizontalWrapMode.Wrap;
            desc.verticalOverflow = VerticalWrapMode.Overflow;

            go.SetActive(false);
            return go;
        }

        private static Text CreateEmptyHint(Transform box)
        {
            var existing = box.Find("EmptyHint")?.GetComponent<Text>();
            if (existing != null)
            {
                existing.text = EmptyWarehouseHint;
                existing.gameObject.SetActive(false);
                return existing;
            }

            var text = CreateUiText(box, "EmptyHint", EmptyWarehouseHint, 22, TextAnchor.MiddleCenter);
            Place(text.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, 10f), new Vector2(420f, 60f));
            text.gameObject.SetActive(false);
            return text;
        }

        private static Text CreateUiText(Transform parent, string name, string content, int fontSize, TextAnchor alignment)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            go.transform.SetParent(parent, false);
            var text = go.GetComponent<Text>();
            text.text = content;
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = Color.white;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            return text;
        }

        private static void StretchFull(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static void Place(
            RectTransform rt,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 pivot,
            Vector2 anchoredPos,
            Vector2 size)
        {
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;
        }
    }
}
