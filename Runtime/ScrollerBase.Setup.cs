using UnityEngine;
using UnityEngine.UI;

namespace UIScroller
{
    /// <summary>The settings drive the ScrollRect, and in the editor the Scroll View structure is built when the component is added.</summary>
    public abstract partial class ScrollerBase
    {
        // ---------- Settings drive the ScrollRect ----------

        /// <summary>Sets the ScrollRect's scroll axis and Content's anchors from the settings' direction.</summary>
        internal void ApplySettingsToScrollRect()
        {
            horizontal = !settings.IsVertical;
            vertical = settings.IsVertical;

            if (content != null)
                AxisLayouts.CreateDirection(settings.IsVertical, settings.IsReversed).ConfigureContentTransform(content);
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            horizontal = !settings.IsVertical;
            vertical = settings.IsVertical;

            // Unity doesn't allow changing RectTransforms inside OnValidate, so Content is set up right after.
            UnityEditor.EditorApplication.delayCall += ApplySettingsAfterValidate;
        }

        private void ApplySettingsAfterValidate()
        {
            if (this != null && !Application.isPlaying)
                ApplySettingsToScrollRect();
        }
#endif

        // ---------- Editor: build the Scroll View structure when the component is added ----------

        #if UNITY_EDITOR
        private new void Reset()
        {
            SetupScrollRectStructure();
        }

        private void SetupScrollRectStructure()
        {
            RectTransform rectTransform = GetComponent<RectTransform>();
            if (rectTransform is null)
                return;

            ConfigureImage();
            CreateOrGetViewport();
            CreateOrGetContainer();
            ConfigureScrollRect();
        }

        private void ConfigureImage()
        {
            Image image = GetComponent<Image>();
            if (image is null)
                image = gameObject.AddComponent<Image>();

            image.raycastTarget = true;
            image.maskable = false;
            image.type = Image.Type.Simple;
            image.color = new Color(1f, 1f, 1f, 0f);
        }

        private void CreateOrGetViewport()
        {
            if (viewport != null)
            {
                ViewportCanvas.Ensure(viewport);
                return;
            }

            Transform viewportTransform = transform.Find("Viewport");
            RectTransform viewportRect;

            if (viewportTransform is null)
            {
                GameObject viewportGo = new GameObject("Viewport");
                viewportRect = viewportGo.AddComponent<RectTransform>();
                viewportRect.SetParent(transform, false);
                viewportTransform = viewportGo.transform;
            }
            else
            {
                viewportRect = viewportTransform.GetComponent<RectTransform>();
                if (viewportRect is null)
                    viewportRect = viewportTransform.gameObject.AddComponent<RectTransform>();
            }

            if (viewportTransform.GetComponent<RectMask2D>() is null)
                viewportTransform.gameObject.AddComponent<RectMask2D>();

            viewport = viewportRect;
            StretchRectTransformToParent(viewportRect);
            ViewportCanvas.Ensure(viewportRect);
        }

        private void CreateOrGetContainer()
        {
            if (content != null)
                return;

            RectTransform viewportRect = viewport;
            if (viewportRect is null)
                return;

            Transform containerTransform = viewportRect.Find("Content");
            RectTransform containerRect;

            if (containerTransform is null)
            {
                GameObject containerGo = new GameObject("Content");
                containerRect = containerGo.AddComponent<RectTransform>();
                containerRect.SetParent(viewportRect, false);
            }
            else
            {
                containerRect = containerTransform.GetComponent<RectTransform>();
                if (containerRect is null)
                    containerRect = containerTransform.gameObject.AddComponent<RectTransform>();
            }

            content = containerRect;
            StretchRectTransformToParent(containerRect);
        }

        private void ConfigureScrollRect()
        {
            // The axis comes from the settings' direction; scrollbars aren't used (touch and web).
            horizontal = !settings.IsVertical;
            vertical = settings.IsVertical;
            horizontalScrollbar = null;
            verticalScrollbar = null;
            horizontalScrollbarVisibility = ScrollbarVisibility.AutoHide;
            verticalScrollbarVisibility = ScrollbarVisibility.AutoHide;
        }

        private static void StretchRectTransformToParent(RectTransform rectTransform)
        {
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
        }
#endif
    }
}
