using UnityEngine;
using UnityEngine.UI;

namespace UIScroller
{
    /// <summary>
    /// Makes the Viewport a nested Canvas (P18). Unity rebuilds the batches of the whole Canvas a moved graphic belongs
    /// to, so without it every scroll rebuilds the batches of everything else on the screen. The nested Canvas gets
    /// its own GraphicRaycaster, because the outer one doesn't see its graphics, and the outer Canvas's shader
    /// channels, which TextMeshPro needs.
    /// </summary>
    internal static class ViewportCanvas
    {
        public static void Ensure(RectTransform viewport)
        {
            if (viewport == null)
                return;

            GameObject go = viewport.gameObject;
            Canvas canvas = go.GetComponent<Canvas>();
            if (canvas == null)
                canvas = go.AddComponent<Canvas>();

            if (go.GetComponent<GraphicRaycaster>() == null)
                go.AddComponent<GraphicRaycaster>();

            Canvas outer = viewport.parent != null ? viewport.parent.GetComponentInParent<Canvas>() : null;
            if (outer != null)
                canvas.additionalShaderChannels |= outer.additionalShaderChannels;
        }
    }
}
