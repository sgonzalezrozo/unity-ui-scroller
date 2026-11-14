using UnityEngine;
using UnityEngine.UI;

namespace SlotList
{
    public static class SourceGuard<T> where T : class
    {
        public static bool ValidateForInitialize(GameObject gameObject, IItemSource<T> source)
        {
            if (gameObject == null)
            {
                Debug.LogWarning("[SlotList] GameObject is null or destroyed");
                return false;
            }

            if (source == null)
            {
                Debug.LogError("[SlotList] Data source is null");
                return false;
            }

            if (gameObject.GetComponent<ScrollRect>() == null)
            {
                Debug.LogError("[SlotList] ScrollRect component not found on " + gameObject.name);
                return false;
            }

            if (!gameObject.activeInHierarchy)
            {
                Debug.LogWarning("[SlotList] Cannot initialize inactive GameObject: " + gameObject.name);
                return false;
            }

            return true;
        }

        public static bool ValidateForSetDataSource(GameObject gameObject, IItemSource<T> newSource, bool isInitialized)
        {
            if (gameObject == null)
            {
                Debug.LogWarning("[SlotList] GameObject is null or destroyed");
                return false;
            }

            if (newSource == null)
            {
                Debug.LogError("[SlotList] Cannot set null data source");
                return false;
            }

            if (!isInitialized)
            {
                Debug.LogWarning("[SlotList] Scroller not initialized, call Initialize() first");
                return false;
            }

            return true;
        }
    }
}

