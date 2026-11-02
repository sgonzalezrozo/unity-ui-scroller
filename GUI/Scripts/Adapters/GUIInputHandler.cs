using System;
using Tools.RecyclableScroller.Abstractions.Scripts;
using UnityEngine;
using UnityEngine.UI;

namespace Tools.RecyclableScroller.GUI.Scripts.Adapters
{
    /// <summary>
    /// Adapts uGUI ScrollRect input events to IInputHandler interface.
    /// Bridges ScrollRect.onValueChanged, velocity tracking, and pointer events.
    /// </summary>
    public class GUIInputHandler : IInputHandler
    {
        private readonly ScrollRect scrollRect;

        public event Action<float> OnScroll;
        public event Action OnDragBegin;
        public event Action OnDragEnd;

        public GUIInputHandler(ScrollRect scrollRect)
        {
            this.scrollRect = scrollRect;
        }

        /// <summary>
        /// Setup scroll position listeners.
        /// Called after initialization to hook up to ScrollRect events.
        /// </summary>
        public void SetupScrollListener()
        {
            if (scrollRect == null)
                return;

            scrollRect.onValueChanged.AddListener(pos =>
            {
                float velocityMagnitude = scrollRect.velocity.magnitude;
                OnScroll?.Invoke(velocityMagnitude);
            });
        }

        /// <summary>
        /// Called from RecyclableScrollRectGUI.OnBeginDrag()
        /// </summary>
        public void HandlePointerDown()
        {
            OnDragBegin?.Invoke();
        }

        /// <summary>
        /// Called from RecyclableScrollRectGUI.OnEndDrag()
        /// </summary>
        public void HandlePointerUp()
        {
            OnDragEnd?.Invoke();
        }

        public void SetScrollVelocity(Vector2 velocity)
        {
            if (scrollRect != null)
                scrollRect.velocity = velocity;
        }

        public void ForceStop()
        {
            if (scrollRect != null)
                scrollRect.velocity = Vector2.zero;
        }

        public void Cleanup()
        {
            // ScrollRect listener will be removed by ScrollRect.OnDestroy
        }
    }
}

