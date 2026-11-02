using System;
using System.Collections;
using UnityEngine;

namespace RecyclableScroller.Runtime
{
    public class RebuildCoroutineScheduler
    {

        private static readonly WaitForEndOfFrame waitForEndOfFrameCache = new();

        private readonly MonoBehaviour host;
        private readonly Func<bool> isInitialized;
        private readonly ViewportMetrics viewportMetrics;
        private readonly ScrollStateManager scrollStateManager;
        private readonly ScrollNavigator navigator;
        private readonly GridCountResolver gridResolver;
        private readonly RebuildCoordinator rebuildCoordinator;
        private readonly Action recalculateContentSize;
        private readonly Action rebuildVisibleCells;
        private readonly Action<float> setViewportSize;
        private readonly Action resetContentAnchoredPosition;
        private readonly Action fireOnInitialized;
        private readonly Func<bool> scrollToInitOnEnable;

        private Coroutine deferredGridCountUpdateRoutine;
        private Coroutine deferredPoolWarmupRoutine;

        public RebuildCoroutineScheduler(
            MonoBehaviour host,
            Func<bool> isInitialized,
            ViewportMetrics viewportMetrics,
            ScrollStateManager scrollStateManager,
            ScrollNavigator navigator,
            GridCountResolver gridResolver,
            RebuildCoordinator rebuildCoordinator,
            Action recalculateContentSize,
            Action rebuildVisibleCells,
            Action<float> setViewportSize,
            Action resetContentAnchoredPosition,
            Action fireOnInitialized,
            Func<bool> scrollToInitOnEnable)
        {
            this.host = host;
            this.isInitialized = isInitialized;
            this.viewportMetrics = viewportMetrics;
            this.scrollStateManager = scrollStateManager;
            this.navigator = navigator;
            this.gridResolver = gridResolver;
            this.rebuildCoordinator = rebuildCoordinator;
            this.recalculateContentSize = recalculateContentSize;
            this.rebuildVisibleCells = rebuildVisibleCells;
            this.setViewportSize = setViewportSize;
            this.resetContentAnchoredPosition = resetContentAnchoredPosition;
            this.fireOnInitialized = fireOnInitialized;
            this.scrollToInitOnEnable = scrollToInitOnEnable;
        }

        public void ScheduleInitialBuild()
            => host.StartCoroutine(DeferredInitialBuild());

        public void ScheduleReEnableRebuild()
            => host.StartCoroutine(RebuildAfterReEnable());

        public void ScheduleDeferredGridUpdate()
        {
            if (deferredGridCountUpdateRoutine != null)
                host.StopCoroutine(deferredGridCountUpdateRoutine);

            deferredGridCountUpdateRoutine = host.StartCoroutine(DeferredGridCountUpdate());
        }

        public void ScheduleDeferredPoolWarmup(CellPool cellPool)
        {
            if (cellPool == null || cellPool.IsPrewarmed)
                return;

            if (deferredPoolWarmupRoutine != null)
                host.StopCoroutine(deferredPoolWarmupRoutine);

            deferredPoolWarmupRoutine = host.StartCoroutine(DeferredPoolWarmup(cellPool));
        }

        public void CancelPendingGridUpdate()
        {
            if (deferredGridCountUpdateRoutine == null)
                return;

            host.StopCoroutine(deferredGridCountUpdateRoutine);
            deferredGridCountUpdateRoutine = null;
        }

        private IEnumerator DeferredInitialBuild()
        {
            yield return waitForEndOfFrameCache;

            if (!isInitialized())
                yield break;

            Canvas.ForceUpdateCanvases();

            float runtimeViewportSize = viewportMetrics.GetViewportSize();
            setViewportSize(runtimeViewportSize);

            recalculateContentSize();
            resetContentAnchoredPosition();

            rebuildVisibleCells();
            fireOnInitialized();
        }

        private IEnumerator RebuildAfterReEnable()
        {
            yield return null;

            if (!isInitialized())
                yield break;

            scrollStateManager.ResetState();
            rebuildVisibleCells();

            if (scrollToInitOnEnable())
                navigator.ScrollToStart();
        }

        private IEnumerator DeferredGridCountUpdate()
        {
            yield return null;

            if (!isInitialized() || !gridResolver.IsDynamic)
            {
                deferredGridCountUpdateRoutine = null;
                yield break;
            }

            int newGridCount = gridResolver.Calculate(viewportMetrics.GetCrossAxisViewportSize());
            if (newGridCount == gridResolver.Current)
            {
                deferredGridCountUpdateRoutine = null;
                yield break;
            }

            rebuildCoordinator.RebuildLayout();
            deferredGridCountUpdateRoutine = null;
        }

        private IEnumerator DeferredPoolWarmup(CellPool cellPool)
        {
            yield return waitForEndOfFrameCache;

            if (!isInitialized() || cellPool == null || cellPool.IsPrewarmed)
            {
                deferredPoolWarmupRoutine = null;
                yield break;
            }

            cellPool.PrewarmPoolDeferred();
            deferredPoolWarmupRoutine = null;
        }
    }
}
