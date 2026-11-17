using UnityEngine;

namespace UIScroller.Tests.PlayMode
{
    /// <summary>Remembers the last viewport position its slot was told, and how many times.</summary>
    public class ViewportRecorder : MonoBehaviour, IViewportPositionListener
    {
        public float Position = float.NaN;
        public int Calls;

        public void OnViewportPosition(float position)
        {
            Position = position;
            Calls++;
        }
    }
}
