using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace UIScroller
{
    public class ScrollRectBinding
    {
        public ScrollRect ScrollRect { get; private set; }
        public RectTransform Content { get; private set; }
        public RectTransform Viewport { get; private set; }
        public IAxisLayout Direction { get; private set; }

        public bool TryBind(MonoBehaviour host, bool isVertical, bool reverse, GameObject slotPrefab)
        {
            ScrollRect = host.GetComponent<ScrollRect>();

            if (ScrollRect == null)
            {
                Debug.LogWarning("[UIScroller] ScrollRect component not found");
                return false;
            }

            Content = ScrollRect.content;
            Viewport = ResolveViewport(ScrollRect, Content);

            if (Content == null)
            {
                Debug.LogError($"[UIScroller] The ScrollRect on {host.gameObject.name} has no Content assigned.");
                return false;
            }

            List<Component> forbidden = ContentLayoutRules.FindForbidden(Content, Viewport);
            if (forbidden.Count > 0)
            {
                Debug.LogError($"[UIScroller] {host.gameObject.name}: {ContentLayoutRules.Describe(forbidden)}", host);
                return false;
            }

            if (slotPrefab == null)
            {
                Debug.LogError($"[UIScroller] No Slot Prefab is assigned on {host.gameObject.name}.");
                return false;
            }

            // Not when the list itself is the viewport (no Viewport assigned): a Canvas there would cover the whole list.
            if (Viewport != host.transform)
                ViewportCanvas.Ensure(Viewport);

            ScrollRect.horizontal = !isVertical;
            ScrollRect.vertical = isVertical;

            Direction = AxisLayouts.CreateDirection(isVertical, reverse);
            Direction.ConfigureContentTransform(Content);

            return true;
        }

        private static RectTransform ResolveViewport(ScrollRect scrollRect, RectTransform content)
        {
            if (scrollRect.viewport != null)
                return scrollRect.viewport;

            if (content != null)
                return content.parent as RectTransform;

            return scrollRect.GetComponent<RectTransform>();
        }
    }
}

