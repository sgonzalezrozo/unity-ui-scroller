using UnityEditor;
using UnityEngine;

namespace UIScroller.Editor
{
    /// <summary>
    /// Links to the UI Scroller documentation. After the package is imported, shows a welcome dialog once per project.
    /// </summary>
    [InitializeOnLoad]
    internal static class UIScrollerDocumentation
    {
        internal const string Url = "https://github.com/sgonzalezrozo/unity-ui-scroller#readme";

        private const string ShownKeyPrefix = "com.sgonzalezrozo.ui-scroller.WelcomeShown:";

        static UIScrollerDocumentation()
        {
            if (Application.isBatchMode || EditorPrefs.GetBool(ShownKey, false))
                return;

            EditorApplication.update += ShowWelcomeWhenIdle;
        }

        // One key per project, so the dialog shows again in another project.
        private static string ShownKey => ShownKeyPrefix + Application.dataPath;

        private static void ShowWelcomeWhenIdle()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            EditorApplication.update -= ShowWelcomeWhenIdle;
            if (EditorPrefs.GetBool(ShownKey, false))
                return;

            EditorPrefs.SetBool(ShownKey, true);
            bool open = EditorUtility.DisplayDialog(
                "UI Scroller is installed",
                "There is nothing to set up.\n\nThe documentation shows how to build your first list in four steps: your data and slot, a list class, the scene and starting it.",
                "Open Documentation",
                "Close");

            if (open)
                OpenDocumentation();
        }

        internal static void OpenDocumentation() => Application.OpenURL(Url);
    }
}
