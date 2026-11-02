using RecyclableScroller.Core.Interfaces;
using UnityEngine;
using UnityEngine.UI;

namespace RecyclableScroller.UGUI.Adapters
{
    /// <summary>
    /// Binds uGUI ScrollRect components to the scroller.
    /// Stub implementation - full version to be moved from Scripts folder.
    /// </summary>
    public class ScrollComponentBinder
    {
        public ScrollRect ScrollRect { get; private set; }
        public RectTransform Content { get; private set; }
        public RectTransform Viewport { get; private set; }
        public IScrollDirection Direction { get; private set; }

        public bool TryBind(MonoBehaviour host, bool isVertical, bool reverse, GameObject cellPrefab)
        {
            ScrollRect = host.GetComponent<ScrollRect>();

            if (ScrollRect == null)
            {
                Debug.LogWarning("[RecyclableScrollerIssue] ScrollRect component not found");
                return false;
            }

            Content = ScrollRect.content;
            Viewport = ResolveViewport(ScrollRect, Content);

            if (Content == null || cellPrefab == null)
                return false;

            ScrollRect.horizontal = !isVertical;
            ScrollRect.vertical = isVertical;

            // Direction stub - will be created from original Scripts
            Direction = null;

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

