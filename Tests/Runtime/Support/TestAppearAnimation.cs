using UnityEngine;

namespace UIScroller.Tests.PlayMode
{
    /// <summary>A test component that records the appear-animation flag the list sets.</summary>
    public class TestAppearAnimation : MonoBehaviour, IAppearAnimation
    {
        public bool ShouldPlay { get; set; }
    }
}
