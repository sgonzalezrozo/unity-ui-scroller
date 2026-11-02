using Tools.RecyclableScroller.Core.Scripts.Interfaces;
using UnityEngine;
using UnityEngine.UI;

namespace Tools.RecyclableScroller.Core.Scripts.DataSources
{
    public static class DataSourceGuard<T> where T : class
    {
        public static bool ValidateForInitialize(GameObject gameObject, IRecyclableDataSource<T> source)
        {
            if (gameObject == null)
            {
                Debug.LogWarning("[RecyclableScrollRect] GameObject is null or destroyed");
                return false;
            }

            if (source == null)
            {
                Debug.LogError("[RecyclableScrollRect] Data source is null");
                return false;
            }

            if (gameObject.GetComponent<ScrollRect>() == null)
            {
                Debug.LogError("[RecyclableScrollRect] ScrollRect component not found on " + gameObject.name);
                return false;
            }

            if (!gameObject.activeInHierarchy)
            {
                Debug.LogWarning("[RecyclableScrollRect] Cannot initialize inactive GameObject: " + gameObject.name);
                return false;
            }

            return true;
        }

        public static bool ValidateForSetDataSource(GameObject gameObject, IRecyclableDataSource<T> newSource, bool isInitialized)
        {
            if (gameObject == null)
            {
                Debug.LogWarning("[RecyclableScrollRect] GameObject is null or destroyed");
                return false;
            }

            if (newSource == null)
            {
                Debug.LogError("[RecyclableScrollRect] Cannot set null data source");
                return false;
            }

            if (!isInitialized)
            {
                Debug.LogWarning("[RecyclableScrollRect] Scroller not initialized, call Initialize() first");
                return false;
            }

            return true;
        }
    }
}

