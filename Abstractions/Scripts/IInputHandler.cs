using System;
using UnityEngine;

namespace Tools.RecyclableScroller.Abstractions.Scripts
{
    /// <summary>
    /// Abstracts input and scroll events from framework.
    /// Implementations bridge ScrollRect events (uGUI) or EventCallback (UIToolkit).
    /// </summary>
    public interface IInputHandler
    {
        /// <summary>
        /// Fired when scroll position changes.
        /// Parameter is velocity magnitude (may be 0 if framework doesn't expose velocity).
        /// </summary>
        event Action<float> OnScroll;

        /// <summary>
        /// Fired when user begins dragging the scroll container.
        /// </summary>
        event Action OnDragBegin;

        /// <summary>
        /// Fired when user ends dragging (finger lifted, mouse released).
        /// </summary>
        event Action OnDragEnd;

        /// <summary>
        /// Set scroll velocity for momentum scrolling, ScrollToItem animations, etc.
        /// </summary>
        void SetScrollVelocity(Vector2 velocity);

        /// <summary>
        /// Stop scrolling immediately (zero velocity).
        /// </summary>
        void ForceStop();
    }
}

