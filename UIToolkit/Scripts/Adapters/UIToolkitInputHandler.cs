using System;
using RecyclableScroller.Abstractions;
using UnityEngine;
using UnityEngine.UIElements;

namespace RecyclableScroller.UIToolkit.Adapters
{
    /// <summary>
    /// Adapts UIToolkit ScrollView events to IInputHandler.
    /// Note: UIToolkit ScrollView doesn't expose velocity directly,
    /// so velocity parameter in OnScroll will be 0.
    /// </summary>
    public class UIToolkitInputHandler : IInputHandler
    {
        private readonly ScrollView scrollView;

        public event Action<float> OnScroll;
        public event Action OnDragBegin;
        public event Action OnDragEnd;

        public UIToolkitInputHandler(ScrollView scrollView)
        {
            this.scrollView = scrollView;

            if (scrollView != null)
            {
                scrollView.RegisterCallback<ChangeEvent<Vector2>>(evt =>
                {
                    OnScroll?.Invoke(0f);
                });

                scrollView.RegisterCallback<MouseDownEvent>(evt =>
                {
                    OnDragBegin?.Invoke();
                }, TrickleDown.TrickleDown);

                scrollView.RegisterCallback<MouseUpEvent>(evt =>
                {
                    OnDragEnd?.Invoke();
                }, TrickleDown.TrickleDown);
            }
        }

        public void SetScrollVelocity(Vector2 velocity)
        {
            // UIToolkit ScrollView doesn't support velocity directly
            // Smooth scrolling would require custom animation
            // For now, this is a no-op
        }

        public void ForceStop()
        {
            // UIToolkit doesn't have explicit velocity control
            // Scrolling naturally stops when user releases
        }

        public void Cleanup()
        {
            // Callbacks will be cleaned up when element is destroyed
        }
    }
}
