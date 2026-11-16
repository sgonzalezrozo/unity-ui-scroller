using UnityEngine;
using UnityEngine.UI;

namespace UIScroller
{
    public static class SourceGuard<T> where T : class
    {
        public static bool ValidateForInitialize(GameObject gameObject, IItemSource<T> source)
        {
            if (gameObject == null)
            {
                Debug.LogWarning("[UIScroller] GameObject is null or destroyed");
                return false;
            }

            if (source == null)
            {
                Debug.LogError("[UIScroller] Data source is null");
                return false;
            }

            if (gameObject.GetComponent<ScrollRect>() == null)
            {
                Debug.LogError("[UIScroller] ScrollRect component not found on " + gameObject.name);
                return false;
            }

            if (!gameObject.activeInHierarchy)
            {
                Debug.LogWarning("[UIScroller] Cannot initialize inactive GameObject: " + gameObject.name);
                return false;
            }

            return true;
        }

        public static bool ValidateForSetDataSource(GameObject gameObject, IItemSource<T> newSource, bool isInitialized)
        {
            if (gameObject == null)
            {
                Debug.LogWarning("[UIScroller] GameObject is null or destroyed");
                return false;
            }

            if (newSource == null)
            {
                Debug.LogError("[UIScroller] Cannot set null data source");
                return false;
            }

            if (!isInitialized)
            {
                Debug.LogWarning("[UIScroller] Scroller not initialized, call Initialize() first");
                return false;
            }

            return true;
        }
    }
}

