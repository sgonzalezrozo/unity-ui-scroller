using System;
using System.Collections;
using System.Collections.Generic;
using RecyclableScroller.Abstractions;
using UnityEngine;

namespace RecyclableScroller.UIToolkit.Adapters
{
    /// <summary>
    /// Async scheduler for UIToolkit using scheduled callbacks.
    /// Since UIToolkit elements don't have StartCoroutine like MonoBehaviour,
    /// we implement a simple coroutine-like system using EditorApplication.update.
    /// </summary>
    public class UIToolkitAsyncScheduler : IAsyncScheduler
    {
        private class CoroutineHandle
        {
            public IEnumerator routine;
            public int id;
        }

        private static int nextCoroutineId = 0;
        private readonly Dictionary<int, CoroutineHandle> activeCoroutines = new();
        private bool isScheduled;

        public object StartCoroutine(IEnumerator routine)
        {
            if (routine == null)
                return null;

            CoroutineHandle handle = new()
            {
                routine = routine,
                id = nextCoroutineId++
            };

            activeCoroutines[handle.id] = handle;

            if (!isScheduled)
            {
                isScheduled = true;
                #if UNITY_EDITOR
                UnityEditor.EditorApplication.update += ProcessCoroutines;
                #else
                // At runtime, we'd need a MonoBehaviour to call ProcessCoroutines
                // For now, this is editor-only. In production, inject a MonoBehaviour.
                #endif
            }

            return handle.id;
        }

        public void StopCoroutine(object coroutineHandle)
        {
            if (coroutineHandle is int id)
            {
                activeCoroutines.Remove(id);

                if (activeCoroutines.Count == 0)
                {
                    isScheduled = false;
                    #if UNITY_EDITOR
                    UnityEditor.EditorApplication.update -= ProcessCoroutines;
                    #endif
                }
            }
        }

        private void ProcessCoroutines()
        {
            List<int> completed = new();

            foreach (var kvp in activeCoroutines)
            {
                CoroutineHandle handle = kvp.Value;

                try
                {
                    if (!handle.routine.MoveNext())
                    {
                        // Coroutine finished
                        completed.Add(kvp.Key);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[UIToolkitAsyncScheduler] Coroutine error: {ex}");
                    completed.Add(kvp.Key);
                }
            }

            foreach (int id in completed)
                activeCoroutines.Remove(id);

            if (activeCoroutines.Count == 0)
            {
                isScheduled = false;
                #if UNITY_EDITOR
                UnityEditor.EditorApplication.update -= ProcessCoroutines;
                #endif
            }
        }
    }
}

