using UnityEngine;
using UnityEngine.UI;

namespace RecyclableScroller.Runtime
{
    public abstract class RecyclableScrollRectBase : ScrollRect
    {
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
                return;

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
            horizontal = false;
            vertical = true;
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

