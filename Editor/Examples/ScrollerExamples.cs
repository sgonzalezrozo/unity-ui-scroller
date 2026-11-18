using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace UIScroller.Editor
{
    /// <summary>
    /// Assets > UI Scroller > (example): asks for a name and creates a working example in a new folder inside the
    /// selected one: its scripts (item, slot, scroller, screen, and a page loader or header where needed), the slot
    /// and screen prefabs, and a bento layout JSON for the bento examples. Scripts are written first; the prefabs are
    /// built once Unity has compiled them, right after the domain reload.
    /// <code>
    /// Name/
    /// ├── NameItem.cs  NameSlot.cs  NameScroller.cs  NameScreen.cs
    /// ├── NameSlot.prefab    the slot template
    /// └── Name.prefab        Canvas + screen script + list: drop it in a scene and press Play
    /// </code>
    /// </summary>
    [InitializeOnLoad]
    internal static class ScrollerExamples
    {
        private const string MENU = "Assets/UI Scroller/";
        private const int FIRST_PRIORITY = 22;
        private const string NAME_TOKEN = "#NAME#";
        private const string EVENT_SYSTEM_TOKEN = "#EVENTSYSTEM#";
        private const string PENDING_KEY = "UIScroller.Examples.Pending";
        private const string TEMPLATES_FOLDER = "Templates~";

        [Serializable]
        private class PendingBuild
        {
            public string recipe;
            public string name;
            public string folder;
        }

        [Serializable]
        private class PendingList
        {
            public List<PendingBuild> builds = new();
        }

        static ScrollerExamples()
            => EditorApplication.delayCall += BuildPending;

        // Unity menus need one method per item. Priorities keep them in this order, right after Screen System's.
        [MenuItem(MENU + "Basic List", false, FIRST_PRIORITY)] private static void List() => Prompt("List");
        [MenuItem(MENU + "Grid", false, FIRST_PRIORITY + 1)] private static void Grid() => Prompt("Grid");
        [MenuItem(MENU + "Dynamic Size", false, FIRST_PRIORITY + 2)] private static void DynamicSize() => Prompt("DynamicSize");
        [MenuItem(MENU + "Dynamic Size on Click", false, FIRST_PRIORITY + 3)] private static void ExpandOnClick() => Prompt("ExpandOnClick");
        [MenuItem(MENU + "Expandable Row", false, FIRST_PRIORITY + 4)] private static void Expandable() => Prompt("Expandable");
        [MenuItem(MENU + "Bento", false, FIRST_PRIORITY + 5)] private static void Bento() => Prompt("Bento");
        [MenuItem(MENU + "Bento with Paging", false, FIRST_PRIORITY + 6)] private static void BentoPaging() => Prompt("BentoPaging");
        [MenuItem(MENU + "Grid Carousel", false, FIRST_PRIORITY + 7)] private static void GridCarousel() => Prompt("GridCarousel");
        [MenuItem(MENU + "Looping Carousel", false, FIRST_PRIORITY + 8)] private static void LoopCarousel() => Prompt("LoopCarousel");
        [MenuItem(MENU + "Sections", false, FIRST_PRIORITY + 9)] private static void Sections() => Prompt("Sections");
        [MenuItem(MENU + "Nested Shelves", false, FIRST_PRIORITY + 10)] private static void Nested() => Prompt("Nested");

        private static void Prompt(string recipeId)
        {
            ExampleRecipe recipe = Find(recipeId);
            string folder = SelectedFolder();
            ExampleNamePrompt.Show(
                $"New {recipe.Label}",
                "Name",
                recipe.DefaultName,
                $"{recipe.Summary}\nCreates {folder}/<Name>/ with its scripts and prefabs.",
                name => Create(recipe, folder, name));
        }

        /// <summary>Writes the example's scripts into <c>folder/name</c> and queues its prefabs for after the compile.</summary>
        internal static bool Create(ExampleRecipe recipe, string folder, string name)
        {
            name = ToIdentifier(name);
            string directory = $"{folder}/{name}";
            if (AssetDatabase.IsValidFolder(directory) || Directory.Exists(directory))
            {
                EditorUtility.DisplayDialog("UI Scroller", $"'{directory}' already exists. Pick another name.", "OK");
                return false;
            }

            string templates = Path.Combine(TemplatesRoot(), recipe.Id);
            if (!Directory.Exists(templates))
            {
                Debug.LogError($"[UI Scroller] The templates for {recipe.Label} are missing at {templates}.");
                return false;
            }

            List<(string path, string content)> files = new();
            string eventSystem = File.ReadAllText(Path.Combine(TemplatesRoot(), "Shared", "EventSystem.txt"));
            foreach (string template in Directory.GetFiles(templates, "*.txt"))
            {
                // "Slot.cs.txt" becomes "NameSlot.cs", "Layout.json.txt" becomes "NameLayout.json".
                string fileName = name + Path.GetFileNameWithoutExtension(template);
                string className = Path.GetFileNameWithoutExtension(fileName);
                if (fileName.EndsWith(".cs", StringComparison.Ordinal) && TypeExists(className))
                {
                    EditorUtility.DisplayDialog("UI Scroller", $"A class named {className} already exists. Pick another name.", "OK");
                    return false;
                }

                string content = File.ReadAllText(template).Replace(EVENT_SYSTEM_TOKEN, eventSystem).Replace(NAME_TOKEN, name);
                files.Add(($"{directory}/{fileName}", content));
            }

            Directory.CreateDirectory(directory);
            foreach ((string path, string content) in files)
                File.WriteAllText(path, content.Replace("\r\n", "\n"));

            Queue(new PendingBuild { recipe = recipe.Id, name = name, folder = directory });
            AssetDatabase.Refresh();
            Debug.Log($"[UI Scroller] {recipe.Label} scripts written to {directory}. Its prefabs are built after Unity compiles them.");

            // Nothing to compile (the scripts already compiled under another name, say): build now.
            if (!EditorApplication.isCompiling)
                EditorApplication.delayCall += BuildPending;
            return true;
        }

        private static void BuildPending()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += BuildPending;
                return;
            }

            PendingList pending = LoadPending();
            if (pending.builds.Count == 0)
                return;

            List<PendingBuild> waiting = new();
            foreach (PendingBuild build in pending.builds)
            {
                ExampleRecipe recipe = Find(build.recipe);
                if (recipe == null)
                    continue;

                if (ExampleBuild.FindScript(build.name + "Screen") == null)
                {
                    // Not compiled yet, or doesn't compile: keep it for the next reload.
                    if (EditorUtility.scriptCompilationFailed)
                        Debug.LogWarning($"[UI Scroller] The {build.name} example's prefabs will be built once the project compiles. Fix the compile errors in the Console.");
                    waiting.Add(build);
                    continue;
                }

                Build(recipe, build);
            }

            pending.builds = waiting;
            SavePending(pending);
        }

        private static void Build(ExampleRecipe recipe, PendingBuild build)
        {
            UnityEngine.SceneManagement.Scene preview = EditorSceneManager.NewPreviewScene();
            ExampleUIKit.TargetScene = preview;
            try
            {
                recipe.Build(new ExampleBuild(build.name, build.folder));
                AssetDatabase.SaveAssets();
                Debug.Log($"[UI Scroller] {recipe.Label} example ready: drop {build.folder}/{build.name}.prefab in a scene (or under a Canvas) and press Play.");
            }
            catch (Exception exception)
            {
                Debug.LogError($"[UI Scroller] Couldn't build the {build.name} example's prefabs: {exception}");
            }
            finally
            {
                ExampleUIKit.TargetScene = default;
                EditorSceneManager.ClosePreviewScene(preview);
            }
        }

        // ---------- Pending builds survive the domain reload in SessionState ----------

        private static void Queue(PendingBuild build)
        {
            PendingList pending = LoadPending();
            pending.builds.Add(build);
            SavePending(pending);
        }

        private static PendingList LoadPending()
        {
            string json = SessionState.GetString(PENDING_KEY, string.Empty);
            return string.IsNullOrEmpty(json) ? new PendingList() : JsonUtility.FromJson<PendingList>(json) ?? new PendingList();
        }

        private static void SavePending(PendingList pending)
        {
            if (pending.builds.Count == 0)
                SessionState.EraseString(PENDING_KEY);
            else
                SessionState.SetString(PENDING_KEY, JsonUtility.ToJson(pending));
        }

        // ---------- Helpers ----------

        private static ExampleRecipe Find(string id)
            => Array.Find(ScrollerExampleRecipes.All, recipe => recipe.Id == id);

        /// <summary>The Templates~ folder next to this script, in the installed package or a local copy of it.</summary>
        private static string TemplatesRoot()
        {
            UnityEditor.PackageManager.PackageInfo package = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(ScrollerExamples).Assembly);
            if (package != null)
                return Path.Combine(package.resolvedPath, "Editor", "Examples", TEMPLATES_FOLDER);

            return Path.Combine(Path.GetDirectoryName(ThisFile()) ?? string.Empty, TEMPLATES_FOLDER);
        }

        private static string ThisFile([CallerFilePath] string path = "")
            => path;

        private static bool TypeExists(string className)
        {
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assembly.GetType(className, false) != null)
                    return true;
            }

            return false;
        }

        /// <summary>A valid C# class name: letters, digits and underscores, not starting with a digit.</summary>
        internal static string ToIdentifier(string name)
        {
            System.Text.StringBuilder builder = new();
            foreach (char c in name ?? string.Empty)
            {
                if (char.IsLetterOrDigit(c) || c == '_')
                    builder.Append(c);
            }

            if (builder.Length == 0)
                return "Example";

            if (char.IsDigit(builder[0]))
                builder.Insert(0, '_');

            builder[0] = char.ToUpperInvariant(builder[0]);
            return builder.ToString();
        }

        /// <summary>The selected folder, the folder of the selected asset, or the folder open in the Project window.</summary>
        private static string SelectedFolder()
        {
            string path = Selection.activeObject != null ? AssetDatabase.GetAssetPath(Selection.activeObject) : null;
            if (!string.IsNullOrEmpty(path))
                return AssetDatabase.IsValidFolder(path) ? path : Path.GetDirectoryName(path)?.Replace('\\', '/');

            // ProjectWindowUtil.TryGetActiveFolderPath is internal.
            MethodInfo method = typeof(ProjectWindowUtil).GetMethod("TryGetActiveFolderPath", BindingFlags.Static | BindingFlags.NonPublic);
            if (method != null)
            {
                object[] args = { null };
                if ((bool)method.Invoke(null, args) && args[0] is string active && AssetDatabase.IsValidFolder(active))
                    return active;
            }

            return "Assets";
        }
    }
}
