using UnityEditor;
using UnityEngine;

namespace UIScroller.Editor
{
    /// <summary>Links to the UI Scroller documentation: the README on GitHub.</summary>
    internal static class UIScrollerDocumentation
    {
        internal const string Url = "https://github.com/sgonzalezrozo/unity-ui-scroller#readme";

        [MenuItem("Tools/UI Scroller/Documentation", priority = 100)]
        private static void OpenDocumentation() => Application.OpenURL(Url);
    }
}
