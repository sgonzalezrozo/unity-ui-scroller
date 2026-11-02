using System.Collections;

namespace Tools.RecyclableScroller.Abstractions.Scripts
{
    /// <summary>
    /// Abstracts async scheduling (coroutines for uGUI, callbacks for UIToolkit).
    /// Used for deferred size calculation, animations, and frame-budget work.
    /// </summary>
    public interface IAsyncScheduler
    {
        /// <summary>
        /// Start a coroutine (uGUI) or equivalent async work (UIToolkit).
        /// Returns a handle to the scheduled work.
        /// </summary>
        object StartCoroutine(IEnumerator routine);

        /// <summary>
        /// Stop a scheduled coroutine/async work.
        /// Safe to call with null or already-completed handles.
        /// </summary>
        void StopCoroutine(object coroutineHandle);
    }
}

