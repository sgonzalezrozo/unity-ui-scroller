using System.Collections.Generic;
using ToolsCore;
using RecyclableScroller.Core.Interfaces;
using UnityEngine;

namespace RecyclableScroller.Core.Pooling
{
    public class CellPool
    {
        private readonly GameObject cellPrefab;
        private readonly GameObject headerPrefab;
        private readonly RectTransform contentTransform;
        private readonly Queue<RectTransform> availableCells;
        private readonly Queue<RectTransform> availableHeaders;
        private readonly List<RectTransform> activeCells;
        private readonly IPrefabInstantiator instantiator;
        private readonly Transform poolContainer;

        private readonly int initialPoolSize;
        private readonly int initialHeaderPoolSize;
        private bool isPrewarmed;

        public int AvailableCount => availableCells.Count + (availableHeaders?.Count ?? 0);
        public int ActiveCount => activeCells.Count;
        public int TotalCount => activeCells.Count + AvailableCount;
        public bool IsPrewarmed => isPrewarmed;

        public CellPool(GameObject prefab, RectTransform content, int poolSize, IPrefabInstantiator instantiator, GameObject headerPrefab = null, int headerPoolSize = 0)
        {
            cellPrefab = prefab;
            this.headerPrefab = headerPrefab;
            contentTransform = content;
            this.instantiator = instantiator ?? ToolInjectionServices.PrefabInstantiator;
            availableCells = new();
            if (headerPrefab != null)
                availableHeaders = new();
            activeCells = new();

            initialPoolSize = poolSize;
            initialHeaderPoolSize = headerPoolSize;

            GameObject poolGO = new("RecyclableScrollPoolContainer");
            UnityEngine.Object.DontDestroyOnLoad(poolGO);
            poolContainer = poolGO.transform;
        }

        public void PrewarmPoolDeferred()
        {
            if (isPrewarmed)
                return;

            PrewarmPool(initialPoolSize, false);

            if (initialHeaderPoolSize > 0 && headerPrefab != null)
                PrewarmPool(initialHeaderPoolSize, true);

            isPrewarmed = true;
        }

        private void PrewarmPool(int poolSize, bool isHeader)
        {
            for (int i = 0; i < poolSize; i++)
            {
                RectTransform cell = CreateCell(isHeader);

                if (cell == null)
                    continue;

                cell.SetParent(poolContainer, worldPositionStays: false);
                cell.gameObject.SetActive(false);
                if (isHeader)
                    availableHeaders.Enqueue(cell);
                else
                    availableCells.Enqueue(cell);
            }
        }

        private RectTransform CreateCell(bool isHeader)
        {
            GameObject prefab = isHeader ? headerPrefab : cellPrefab;

            if (prefab == null)
                return null;

            if (instantiator == null)
                return null;

            GameObject instance = instantiator.Instantiate(prefab, contentTransform);

            if (instance == null)
                return null;

            RectTransform rectTransform = instance.GetComponent<RectTransform>();

            if (rectTransform != null)
                return rectTransform;

            UnityEngine.Object.Destroy(instance);
            return null;
        }

        public RectTransform GetCell(bool requestHeader = false)
        {
            CleanDestroyedCells();

            RectTransform cell;
            Queue<RectTransform> targetQueue = requestHeader ? availableHeaders : availableCells;

            if (targetQueue != null && targetQueue.Count > 0)
                cell = targetQueue.Dequeue();
            else
                cell = CreateCell(requestHeader);

            if (cell == null || cell.gameObject == null)
                return null;

            cell.SetParent(contentTransform, worldPositionStays: false);
            activeCells.Add(cell);

            return cell;
        }

        public void ReturnCell(RectTransform cell)
        {
            if (cell == null)
                return;

            if (!activeCells.Remove(cell))
                return;

            if (cell.gameObject == null)
                return;

            cell.gameObject.SetActive(false);
            cell.SetParent(poolContainer, worldPositionStays: false);
            availableCells.Enqueue(cell);
        }

        public void ReturnAllCells()
        {
            int count = activeCells.Count;
            for (int i = count - 1; i >= 0; i--)
                ReturnCell(activeCells[i]);
        }

        private void CleanDestroyedCells()
        {
            int removedCount = 0;
            Queue<RectTransform> queueToClean = availableCells;

            while (queueToClean.Count > 0)
            {
                RectTransform cell = queueToClean.Peek();
                if (cell == null || cell.gameObject == null)
                {
                    queueToClean.Dequeue();
                    removedCount++;
                }
                else
                    break;
            }

            if (availableHeaders == null)
                return;

            removedCount = 0;
            queueToClean = availableHeaders;

            while (queueToClean.Count > 0)
            {
                RectTransform cell = queueToClean.Peek();
                if (cell == null || cell.gameObject == null)
                {
                    queueToClean.Dequeue();
                    removedCount++;
                }
                else
                    break;
            }
        }

        public void Cleanup()
            => ReturnAllCells();

        public void MarkAllPoolItemsAsNonAnimating()
        {
            int markedCount = 0;

            foreach (RectTransform cell in availableCells)
            {
                MarkCellAsNonAnimating(cell);
                markedCount++;
            }

            if (availableHeaders != null)
            {
                foreach (RectTransform cell in availableHeaders)
                {
                    MarkCellAsNonAnimating(cell);
                    markedCount++;
                }
            }

            for (int i = 0; i < activeCells.Count; i++)
            {
                MarkCellAsNonAnimating(activeCells[i]);
                markedCount++;
            }
        }

        private void MarkCellAsNonAnimating(RectTransform cell)
        {
            if (cell == null || cell.gameObject == null)
                return;

            IAnimationMarker animationMarker = cell.GetComponent<IAnimationMarker>();
            if (animationMarker != null)
                animationMarker.ShouldAnimate = false;
        }

        public void DestroyAllCells()
        {
            ReturnAllCells();

            while (availableCells.Count > 0)
            {
                RectTransform cell = availableCells.Dequeue();

                if (cell != null && cell.gameObject != null)
                    UnityEngine.Object.DestroyImmediate(cell.gameObject);
            }

            if (availableHeaders != null)
            {
                while (availableHeaders.Count > 0)
                {
                    RectTransform cell = availableHeaders.Dequeue();

                    if (cell != null && cell.gameObject != null)
                        UnityEngine.Object.DestroyImmediate(cell.gameObject);
                }
            }

            activeCells.Clear();

            if (poolContainer != null)
                UnityEngine.Object.DestroyImmediate(poolContainer.gameObject);
        }
    }
}

