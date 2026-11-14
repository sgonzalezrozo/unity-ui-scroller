using UnityEngine;

namespace SlotList.Tests.PlayMode
{
    /// <summary>Counts how often any slot changes parent. Moving a UI element to another parent rebuilds its graphics.</summary>
    public class ParentWatch : MonoBehaviour
    {
        public static int Changes;

        /// <summary>How often any slot was enabled: waking a slot runs every script's OnEnable and re-registers its graphics.</summary>
        public static int Enables;

        private void OnTransformParentChanged()
            => Changes++;

        private void OnEnable()
            => Enables++;
    }
}
