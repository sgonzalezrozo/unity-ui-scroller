using UnityEngine;
using UnityEngine.EventSystems;

namespace UIScroller
{
    /// <summary>
    /// Nested scrolling: a drag or wheel mostly across the list's axis goes to the nearest parent that handles it, so a
    /// horizontal carousel inside a vertical list scrolls the page. A drag is handed over once, at its start: the list
    /// points the event's pointerDrag at the parent, and the input module sends the rest of the gesture there.
    /// </summary>
    public abstract partial class ScrollerBase
    {
        public override void OnInitializePotentialDrag(PointerEventData eventData)
        {
            base.OnInitializePotentialDrag(eventData);

            // A touch on the inner list also stops the outer list's fling, as a touch on the outer list would.
            if (!settings.passDragsToParent)
                return;

            GameObject parent = ParentHandler<IInitializePotentialDragHandler>();
            if (parent != null)
                ExecuteEvents.Execute(parent, eventData, ExecuteEvents.initializePotentialDrag);
        }

        public override void OnScroll(PointerEventData data)
        {
            if (settings.passDragsToParent && IsAcrossAxis(data.scrollDelta))
            {
                GameObject parent = ParentHandler<IScrollHandler>();
                if (parent != null)
                {
                    ExecuteEvents.Execute(parent, data, ExecuteEvents.scrollHandler);
                    return;
                }
            }

            base.OnScroll(data);
            engine?.OnWheelScrolled();
        }

        /// <summary>True when the drag went to a parent: the list doesn't start a drag of its own.</summary>
        private bool HandDragToParent(PointerEventData eventData)
        {
            if (!settings.passDragsToParent || !IsAcrossAxis(eventData.position - eventData.pressPosition))
                return false;

            GameObject parent = ParentHandler<IBeginDragHandler>();
            if (parent == null)
                return false;

            eventData.pointerDrag = parent;
            ExecuteEvents.Execute(parent, eventData, ExecuteEvents.beginDragHandler);
            return true;
        }

        /// <summary>More movement across the list's axis than along it. An exactly diagonal movement stays with the list.</summary>
        private bool IsAcrossAxis(Vector2 movement)
            => settings.IsVertical ? Mathf.Abs(movement.x) > Mathf.Abs(movement.y) : Mathf.Abs(movement.y) > Mathf.Abs(movement.x);

        /// <summary>The nearest object above this list that handles <typeparamref name="THandler"/>, looked up each time (lists move, slots are pooled).</summary>
        private GameObject ParentHandler<THandler>() where THandler : IEventSystemHandler
        {
            Transform parent = transform.parent;
            return parent != null ? ExecuteEvents.GetEventHandler<THandler>(parent.gameObject) : null;
        }
    }
}
