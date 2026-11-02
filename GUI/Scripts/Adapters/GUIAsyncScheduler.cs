using System.Collections;
using Tools.RecyclableScroller.Abstractions.Scripts;
using UnityEngine;

namespace Tools.RecyclableScroller.GUI.Scripts.Adapters
{
    /// <summary>
    /// Adapts MonoBehaviour coroutines to IAsyncScheduler.
    /// Used for deferred size calculation and async operations.
    /// </summary>
    public class GUIAsyncScheduler : IAsyncScheduler
    {
        private readonly MonoBehaviour monoBehaviour;

        public GUIAsyncScheduler(MonoBehaviour host)
        {
            monoBehaviour = host;
        }

        public object StartCoroutine(IEnumerator routine)
        {
            if (monoBehaviour == null)
                return null;

            return monoBehaviour.StartCoroutine(routine);
        }

        public void StopCoroutine(object coroutineHandle)
        {
            if (monoBehaviour == null || coroutineHandle == null)
                return;

            if (coroutineHandle is Coroutine co)
                monoBehaviour.StopCoroutine(co);
        }
    }
}

