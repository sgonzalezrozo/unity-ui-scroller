using UnityEngine;

namespace SlotList
{
    /// <summary>
    /// An eased (ease-out cubic) move from a start distance to a target the caller gives each frame, so the target can
    /// follow items that get measured on the way.
    /// </summary>
    internal sealed class ScrollAnimation
    {
        private float from;
        private float duration;
        private float elapsed;

        public bool IsRunning { get; private set; }

        public void Start(float startDistance, float seconds)
        {
            from = startDistance;
            duration = Mathf.Max(0.0001f, seconds);
            elapsed = 0f;
            IsRunning = true;
        }

        /// <summary>Moves time on and returns the eased progress, 0 to 1. Stops running at 1.</summary>
        public float Advance(float deltaTime)
        {
            elapsed += Mathf.Max(0f, deltaTime);
            float t = Mathf.Clamp01(elapsed / duration);
            if (t >= 1f)
                IsRunning = false;

            float remaining = 1f - t;
            return 1f - remaining * remaining * remaining;
        }

        public float PositionAt(float progress, float target)
            => Mathf.LerpUnclamped(from, target, progress);

        /// <summary>The content jumped by <paramref name="delta"/> (a looping list re-centred): the start moves with it.</summary>
        public void Shift(float delta)
            => from += delta;

        public void Cancel()
            => IsRunning = false;
    }
}
