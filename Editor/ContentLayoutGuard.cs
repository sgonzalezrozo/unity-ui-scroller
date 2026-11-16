using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace UIScroller.Editor
{
    /// <summary>
    /// Removes a layout component (ContentSizeFitter, LayoutGroup, LayoutElement, AspectRatioFitter) the moment it's
    /// added in the editor to a list's Content, or a LayoutGroup added to its Viewport. The list sets Content's size
    /// itself and changes it at runtime, so these would fight it. Components added from code are caught by Initialize.
    /// </summary>
    [InitializeOnLoad]
    internal static class ContentLayoutGuard
    {
        static ContentLayoutGuard()
            => ObjectFactory.componentWasAdded += OnComponentAdded;

        internal static bool IsForbiddenOn(Component component, ScrollerBase list)
        {
            if (component == null || list == null)
                return false;

            Transform target = component.transform;
            if (target == list.content)
                return component is ILayoutController || component is LayoutElement;

            return target == list.viewport && component is LayoutGroup;
        }

        private static void OnComponentAdded(Component component)
        {
            ScrollerBase list = component != null ? component.GetComponentInParent<ScrollerBase>(true) : null;
            if (!IsForbiddenOn(component, list))
                return;

            string name = component.GetType().Name;
            string where = component.gameObject.name;
            Debug.LogWarning($"[UIScroller] Removed {name} from {where}: the list sets Content's size itself and changes it at runtime, so Content can't have layout components.", list);

            // Removing a component inside the callback that added it isn't safe; do it right after.
            EditorApplication.delayCall += () =>
            {
                if (component != null)
                    Undo.DestroyObjectImmediate(component);
            };
        }
    }
}
