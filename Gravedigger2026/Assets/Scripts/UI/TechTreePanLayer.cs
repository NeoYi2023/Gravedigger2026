using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Gravedigger2026.UI
{
    /// <summary>
    /// Full-rect pan catcher behind TechTree nodes (empty LMB drag).
    /// Must be a top-level MonoBehaviour (own file) for Prefab serialization.
    /// </summary>
    public sealed class TechTreePanLayer : MonoBehaviour, IBeginDragHandler, IDragHandler
    {
        public event Action<Vector2> DragDelta;

        public void OnBeginDrag(PointerEventData eventData)
        {
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left)
            {
                DragDelta?.Invoke(eventData.delta);
            }
        }
    }
}
