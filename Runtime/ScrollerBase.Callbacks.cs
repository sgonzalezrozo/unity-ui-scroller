using UnityEngine;
using UnityEngine.EventSystems;
#if UISCROLLER_LOCALIZATION
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
#endif

namespace UIScroller
{
    /// <summary>Unity callbacks: drags, the per-frame update, enable and disable, the language listener and resizes.</summary>
    public abstract partial class ScrollerBase
    {
        public override void OnBeginDrag(PointerEventData eventData)
        {
            if (HandDragToParent(eventData))
                return;

            base.OnBeginDrag(eventData);
            RememberLeftDrag(eventData);
            engine?.OnDragBegin();
        }

        public override void OnDrag(PointerEventData eventData)
        {
            RememberLeftDrag(eventData);
            base.OnDrag(eventData);
        }

        public override void OnEndDrag(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left)
                isLeftDragging = false;
            base.OnEndDrag(eventData);
            engine?.OnDragEnd();
        }

        private void RememberLeftDrag(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
                return;

            dragRestart ??= new PointerEventData(EventSystem.current);
            dragRestart.button = PointerEventData.InputButton.Left;
            dragRestart.position = eventData.position;
            dragRestart.pointerPressRaycast = eventData.pointerPressRaycast;
            isLeftDragging = true;
        }

        /// <summary>Applies the sizes measured slots reported during this frame's layout pass, then moves scroll animations on.</summary>
        protected override void LateUpdate()
        {
            base.LateUpdate();
            if (engine == null)
                return;

            engine.RunDeferredWork();
            engine.ApplyPendingResizes();
            engine.Tick(frameDeltaTime?.Invoke() ?? Time.unscaledDeltaTime);
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            engine?.OnEnabled();
            ListenToLocale();
            RunPendingStart();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            engine?.OnDisabled();
        }

        /// <summary>
        /// With Unity Localization installed, a language change invalidates every size. Play mode only. The list subscribes
        /// once for its lifetime, not on every enable: adding to an event combines delegates, which allocates. A change
        /// while the list is disabled is applied when it's enabled again.
        /// </summary>
        private void ListenToLocale()
        {
#if UISCROLLER_LOCALIZATION
            if (localeChangedWhileDisabled)
            {
                localeChangedWhileDisabled = false;
                InvalidateAllSizes();
            }

            if (isListeningToLocale || !Application.isPlaying || !LocalizationSettings.HasSettings)
                return;

            localeChangedHandler = OnLocaleChanged;
            LocalizationSettings.SelectedLocaleChanged += localeChangedHandler;
            isListeningToLocale = true;
#endif
        }

        private void StopListeningToLocale()
        {
#if UISCROLLER_LOCALIZATION
            if (!isListeningToLocale)
                return;

            LocalizationSettings.SelectedLocaleChanged -= localeChangedHandler;
            isListeningToLocale = false;
#endif
        }

#if UISCROLLER_LOCALIZATION
        private void OnLocaleChanged(Locale locale)
        {
            if (isActiveAndEnabled)
                InvalidateAllSizes();
            else
                localeChangedWhileDisabled = true;
        }
#endif

        protected override void OnDestroy()
        {
            StopListeningToLocale();
            engine?.Dispose();
            base.OnDestroy();
        }

        protected override void OnRectTransformDimensionsChange()
        {
            base.OnRectTransformDimensionsChange();
            engine?.OnViewportResized();
        }
    }
}
