using System;
using Gravedigger2026.Core.Config;
using UnityEngine;
using UnityEngine.UI;

namespace Gravedigger2026.UI
{
    /// <summary>One Sandbox cell: gameplay name and saved remaining count (SPEC_03 §3.20).</summary>
    public sealed class SandboxCellView : MonoBehaviour
    {
        private string _nodeId;
        private Action<string> _onClick;

        public static SandboxCellView Create(
            Transform parent,
            SandboxNodeConfigRow row,
            int remainingEnterCount,
            bool cleared,
            Action<string> onClick)
        {
            var go = new GameObject(
                "Cell_" + row.NodeId,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Button),
                typeof(LayoutElement),
                typeof(SandboxCellView));
            go.transform.SetParent(parent, false);

            var image = go.GetComponent<Image>();
            image.color = new Color(0.22f, 0.26f, 0.32f, 1f);

            var layout = go.GetComponent<LayoutElement>();
            layout.minWidth = 220f;
            layout.preferredWidth = 220f;
            layout.minHeight = 280f;
            layout.preferredHeight = 280f;
            layout.flexibleWidth = 0f;

            var nameGo = CreateLabel(go.transform, "Name", row.DisplayName, 28, new Vector2(0.5f, 0.62f));
            var countGo = CreateLabel(
                go.transform,
                "Count",
                cleared ? "已通关" : "可进入 " + remainingEnterCount + " 次",
                22,
                new Vector2(0.5f, 0.32f));
            nameGo.color = Color.white;
            countGo.color = new Color(0.85f, 0.9f, 0.75f, 1f);

            var view = go.GetComponent<SandboxCellView>();
            view._nodeId = row.NodeId;
            view._onClick = onClick;
            var button = go.GetComponent<Button>();
            button.interactable = !cleared;
            button.onClick.AddListener(view.HandleClick);
            return view;
        }

        private void HandleClick()
        {
            _onClick?.Invoke(_nodeId);
        }

        private static Text CreateLabel(Transform parent, string name, string value, int fontSize, Vector2 anchor)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(200f, 80f);
            var text = go.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.fontSize = fontSize;
            text.alignment = TextAnchor.MiddleCenter;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.color = Color.white;
            text.text = value ?? string.Empty;
            text.raycastTarget = false;
            return text;
        }
    }
}
