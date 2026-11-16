using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace UIScroller
{
    /// <summary>
    /// The list sets Content's size itself, and changes it at runtime (items measured, expanded or collapsed), so no
    /// layout component may drive Content: no ContentSizeFitter, LayoutGroup, LayoutElement or AspectRatioFitter on
    /// it, and no LayoutGroup on the Viewport (it would size Content as its child).
    /// </summary>
    internal static class ContentLayoutRules
    {
        /// <summary>The layout components that would fight the list over Content's size. Empty when the setup is valid.</summary>
        public static List<Component> FindForbidden(RectTransform content, RectTransform viewport)
        {
            List<Component> found = new();
            if (content != null)
            {
                foreach (Component component in content.GetComponents<Component>())
                {
                    // Graphics (an Image on Content) only report a preferred size; nothing reads it without a controller.
                    if (component is ILayoutController || component is LayoutElement)
                        found.Add(component);
                }
            }

            if (viewport != null && viewport != content)
            {
                foreach (LayoutGroup group in viewport.GetComponents<LayoutGroup>())
                    found.Add(group);
            }

            return found;
        }

        /// <summary>A sentence naming the components and where they are, for an error or a help box.</summary>
        public static string Describe(List<Component> forbidden)
        {
            StringBuilder names = new();
            for (int i = 0; i < forbidden.Count; i++)
            {
                if (i > 0)
                    names.Append(", ");
                names.Append(forbidden[i].GetType().Name).Append(" on ").Append(forbidden[i].gameObject.name);
            }

            return $"Content can't have layout components: the list sets Content's size itself and changes it at runtime. Remove {names}.";
        }
    }
}
