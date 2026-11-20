using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using static UIScroller.Editor.ListUIKit;

namespace UIScroller.Editor
{
    /// <summary>
    /// Assets > UI Scroller > New Scroll List: creates a list for a feature, ready for real data, split into Model, View and
    /// Controller. Only the choices that need different scripts are asked here (where the items come from and how big they
    /// are); everything else is a setting in the list's Inspector. For a feature "Store", in the selected folder:
    /// <code>
    /// StoreItemData.cs         the data of one item
    /// StoreItemSlotModel.cs    the Model of one slot (ISlot): the list sets Data on it as you scroll
    /// StoreItemSlotView.cs     renders one slot from its Slot Model
    /// StoreScroller.cs         the list component
    /// StoreListModel.cs        owns the items and is the list's source (IItemSource)
    /// StoreListView.cs         gives the list its source and refreshes it when the items change
    /// StoreItem.prefab         the slot template
    /// StoreList.prefab         the list, with its Model and View; place it under a Canvas
    /// </code>
    /// Expandable adds ToggleStoreItemController, on an Action child of the summary. Multisource adds StoreHeaderData,
    /// StoreHeaderSlotModel, StoreHeaderSlotView and StoreHeader.prefab; Paginated adds StorePageLoader; the bento option
    /// adds StoreLayout.json. Everything goes in the namespace shown in the window. Scripts are written first, the
    /// folder's assembly definition gets the references they need, and the prefabs are built once Unity has compiled them.
    /// </summary>
    [InitializeOnLoad]
    public class ListWizardWindow : EditorWindow
    {
        public enum SourceKind
        {
            Simple,
            Sections
        }

        public enum SizeKind
        {
            Static,
            Dynamic,
            Expandable
        }

        private const string NAME_TOKEN = "#NAME#";
        private const string PENDING_KEY = "UIScroller.ListWizard.Pending";
        private const float STATIC_SIZE = 120f;
        private const float HEADER_SIZE = 72f;

        private static readonly GUIContent[] SourceLabels =
        {
            new("Simple", "One list of items, given all at once (a ListSource)."),
            new("Multisource (sections)", "Several lists, each under a header (a SectionedSource). Adds header data, a header slot Model and View, and a header prefab.")
        };

        private static readonly GUIContent[] SizeLabels =
        {
            new("Static", "Every item has the same size, a constant in the slot Model."),
            new("Dynamic", "Each item is as big as its content, measured with layout."),
            new("Expandable", "Items open to a bigger size when their summary is tapped. Adds a click Controller.")
        };

        [Serializable]
        private class Options
        {
            public string name = "Store";
            public string folder = "Assets";
            public string namespaceName = string.Empty;
            public bool namespaceEdited;
            public SourceKind source;
            public SizeKind size;
            public bool paginated;
            public bool bento;
            public bool textMeshPro = true;
        }

        [Serializable]
        private class PendingList
        {
            public List<Options> builds = new();
        }

        private Options options = new();

        static ListWizardWindow()
            => EditorApplication.delayCall += BuildPending;

        [MenuItem("Assets/UI Scroller/New Scroll List", false, 22)]
        private static void Open()
        {
            ListWizardWindow window = GetWindow<ListWizardWindow>(true, "New Scroll List", true);
            window.options = new Options { folder = SelectedFolder(), textMeshPro = TextMeshProType != null };
            window.options.namespaceName = AutoNamespace(window.options);
            window.minSize = new Vector2(440f, 330f);
            window.maxSize = new Vector2(700f, 600f);
            window.ShowUtility();
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(8);
            options.name = EditorGUILayout.TextField(new GUIContent("Feature Name", "Prefixes every script and prefab: Store makes StoreItemData, StoreScroller..."), options.name);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Folder", options.folder);
            if (GUILayout.Button("Select", GUILayout.Width(70)))
                PickFolder();
            EditorGUILayout.EndHorizontal();

            if (!options.namespaceEdited)
                options.namespaceName = AutoNamespace(options);
            EditorGUI.BeginChangeCheck();
            options.namespaceName = EditorGUILayout.TextField(new GUIContent("Namespace", "The namespace of every script. By default the folder's assembly root namespace (or name) and its first folder: Assets/Client/Shop makes Client.Shop."), options.namespaceName);
            if (EditorGUI.EndChangeCheck())
                options.namespaceEdited = true;

            EditorGUILayout.Space(6);
            options.source = (SourceKind)EditorGUILayout.Popup(new GUIContent("Source"), (int)options.source, SourceLabels);
            // A sectioned list doesn't ask for pages, so paging goes with a simple source only.
            using (new EditorGUI.DisabledScope(options.source != SourceKind.Simple))
                options.paginated = EditorGUILayout.Toggle(new GUIContent("Paginated", "Items arrive a page at a time as you scroll near the end (a PagedSource). Adds a page loader. Simple source only."), options.paginated && options.source == SourceKind.Simple);

            options.size = (SizeKind)EditorGUILayout.Popup(new GUIContent("Size Behaviour"), (int)options.size, SizeLabels);

            // Bento decides every tile's size from the layout, so it only goes with static items.
            using (new EditorGUI.DisabledScope(options.size != SizeKind.Static))
                options.bento = EditorGUILayout.Toggle(new GUIContent("Create Bento JSON", "Turns bento on and creates a layout JSON for the list. Bento tiles take their size from the layout, so it needs static items."), options.bento && options.size == SizeKind.Static);

            using (new EditorGUI.DisabledScope(TextMeshProType == null))
                options.textMeshPro = EditorGUILayout.Toggle(new GUIContent("Use TextMeshPro", TextMeshProType == null ? "TextMeshPro isn't installed: labels use Unity's Text." : "Labels use TextMeshPro instead of Unity's Text."), options.textMeshPro && TextMeshProType != null);

            string name = ToIdentifier(options.name);
            EditorGUILayout.Space(6);
            EditorGUILayout.HelpBox($"Creates in {options.folder}:\n{string.Join(", ", PlannedFiles(name))}\n\n" +
                                    "Direction, spacing, grid, loop and snap are settings on the list: change them in its Inspector.", MessageType.None);

            GUILayout.FlexibleSpace();
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Cancel", GUILayout.Width(90)))
                Close();

            using (new EditorGUI.DisabledScope(string.IsNullOrWhiteSpace(options.name)))
            {
                if (GUILayout.Button("Create", GUILayout.Width(90)) && Create(options))
                    Close();
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(8);
        }

        private void PickFolder()
        {
            string path = EditorUtility.OpenFolderPanel("Folder", options.folder, "");
            if (string.IsNullOrEmpty(path))
                return;

            if (!path.StartsWith(Application.dataPath, StringComparison.Ordinal))
            {
                EditorUtility.DisplayDialog("UI Scroller", "Pick a folder inside Assets.", "OK");
                return;
            }

            options.folder = "Assets" + path.Substring(Application.dataPath.Length);
            if (!options.namespaceEdited)
                options.namespaceName = AutoNamespace(options);
        }

        // ---------- Files ----------

        /// <summary>Template (in Templates~) to output file name, for these options.</summary>
        private static List<(string template, string file)> Files(Options o, string name)
        {
            List<(string, string)> files = new()
            {
                ("ItemData.cs.txt", $"{name}ItemData.cs"),
                ($"ItemSlotModel.{o.size}.cs.txt", $"{name}ItemSlotModel.cs"),
                ($"ItemSlotView.{o.size}.cs.txt", $"{name}ItemSlotView.cs"),
                ($"Scroller.{ScrollerTemplate(o)}.cs.txt", $"{name}Scroller.cs"),
                ($"ListModel.{SourceTemplate(o)}.cs.txt", $"{name}ListModel.cs"),
                ($"ListView.{SourceTemplate(o)}.cs.txt", $"{name}ListView.cs")
            };

            if (o.size == SizeKind.Expandable)
                files.Add(("ToggleItemController.cs.txt", $"Toggle{name}ItemController.cs"));

            if (o.source == SourceKind.Sections)
            {
                files.Add(("HeaderData.cs.txt", $"{name}HeaderData.cs"));
                files.Add(("HeaderSlotModel.cs.txt", $"{name}HeaderSlotModel.cs"));
                files.Add(("HeaderSlotView.cs.txt", $"{name}HeaderSlotView.cs"));
            }

            if (IsPaged(o))
                files.Add(("PageLoader.cs.txt", $"{name}PageLoader.cs"));

            if (o.bento && o.size == SizeKind.Static)
                files.Add(("Layout.json.txt", $"{name}Layout.json"));

            return files;
        }

        /// <summary>The Scroller template: a sectioned list or a plain one (paged lists are plain).</summary>
        private static string ScrollerTemplate(Options o)
            => o.source == SourceKind.Sections ? "Sections" : "Simple";

        private static bool IsPaged(Options o)
            => o.paginated && o.source == SourceKind.Simple;

        /// <summary>The ListModel and ListView templates: Simple, Sections or Paged.</summary>
        private static string SourceTemplate(Options o)
            => IsPaged(o) ? "Paged" : o.source.ToString();

        private List<string> PlannedFiles(string name)
        {
            List<string> planned = new();
            foreach ((_, string file) in Files(options, name))
                planned.Add(file);

            planned.Add($"{name}Item.prefab");
            if (options.source == SourceKind.Sections)
                planned.Add($"{name}Header.prefab");
            planned.Add($"{name}List.prefab");
            return planned;
        }

        /// <summary>Writes the scripts for <paramref name="o"/> and queues the prefabs for after the compile.</summary>
        private static bool Create(Options o)
        {
            string name = ToIdentifier(o.name);
            o.name = name;
            o.namespaceName = ToNamespace(string.IsNullOrWhiteSpace(o.namespaceName) ? AutoNamespace(o) : o.namespaceName);
            o.textMeshPro &= TextMeshProType != null;

            if (!AssetDatabase.IsValidFolder(o.folder))
            {
                EditorUtility.DisplayDialog("UI Scroller", $"'{o.folder}' isn't a folder.", "OK");
                return false;
            }

            List<(string path, string content)> output = new();
            foreach ((string template, string file) in Files(o, name))
            {
                string path = $"{o.folder}/{file}";
                string className = Path.GetFileNameWithoutExtension(file);
                if (File.Exists(path) || (file.EndsWith(".cs", StringComparison.Ordinal) && TypeExists(Qualified(o, className))))
                {
                    EditorUtility.DisplayDialog("UI Scroller", $"{(File.Exists(path) ? path : $"A class named {className}")} already exists. Pick another feature name.", "OK");
                    return false;
                }

                output.Add((path, Fill(File.ReadAllText(Path.Combine(TemplatesRoot(), template)), o, name)));
            }

            foreach (string prefab in new[] { "Item", "Header", "List" })
            {
                string path = $"{o.folder}/{name}{prefab}.prefab";
                if (File.Exists(path))
                {
                    EditorUtility.DisplayDialog("UI Scroller", $"{path} already exists. Pick another feature name.", "OK");
                    return false;
                }
            }

            foreach ((string path, string content) in output)
                File.WriteAllText(path, content.Replace("\r\n", "\n"));

            // Before the compile: the scripts under an .asmdef fail with CS0246 without these, and the prefabs are only
            // built after a successful compile.
            AssemblyReferences.Ensure(o.folder, RequiredAssemblies(o));

            PendingList pending = LoadPending();
            pending.builds.Add(o);
            SavePending(pending);

            AssetDatabase.Refresh();
            Debug.Log($"[UI Scroller] {name} scripts written to {o.folder}, in the namespace {o.namespaceName}. The prefabs are built after Unity compiles them.");
            if (!EditorApplication.isCompiling)
                EditorApplication.delayCall += BuildPending;
            return true;
        }

        /// <summary>The assemblies the generated scripts use, for the .asmdef that compiles their folder.</summary>
        private static string[] RequiredAssemblies(Options o)
        {
            return o.textMeshPro
                ? new[] { "UIScroller", "ToolsCore", "UnityEngine.UI", "Unity.TextMeshPro" }
                : new[] { "UIScroller", "ToolsCore", "UnityEngine.UI" };
        }

        private static string Fill(string template, Options o, string name)
        {
            bool hasDescription = o.size != SizeKind.Static;
            return template
                .Replace("#TMP_USING#", o.textMeshPro ? "using TMPro;\n" : string.Empty)
                .Replace("#UI_USING#", o.textMeshPro ? string.Empty : "using UnityEngine.UI;\n")
                .Replace("#TEXT_TYPE#", o.textMeshPro ? "TMP_Text" : "Text")
                .Replace("#DESCRIPTION_FIELD#", hasDescription ? "\n        public string description;" : string.Empty)
                .Replace("#PLACEHOLDER_FIELDS#", hasDescription
                    ? "title = $\"Item {i + 1}\", description = i % 2 == 0 ? \"A short description.\" : \"A longer description that wraps to more lines, so the items show different sizes.\""
                    : "title = $\"Item {i + 1}\"")
                .Replace("#NAMESPACE#", o.namespaceName)
                .Replace(NAME_TOKEN, name);
        }

        // ---------- Prefabs, after the compile ----------

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

            List<Options> waiting = new();
            foreach (Options build in pending.builds)
            {
                if (FindScript(build, build.name + "Scroller") == null || FindScript(build, build.name + "ItemSlotModel") == null)
                {
                    if (EditorUtility.scriptCompilationFailed)
                        Debug.LogWarning($"[UI Scroller] {build.name}'s prefabs will be built once the project compiles. Fix the compile errors in the Console.");
                    waiting.Add(build);
                    continue;
                }

                Build(build);
            }

            pending.builds = waiting;
            SavePending(pending);
        }

        private static void Build(Options o)
        {
            UnityEngine.SceneManagement.Scene preview = EditorSceneManager.NewPreviewScene();
            TargetScene = preview;
            UseTextMeshPro = o.textMeshPro;
            try
            {
                GameObject item = Save(BuildItem(o), $"{o.folder}/{o.name}Item.prefab");
                GameObject header = o.source == SourceKind.Sections ? Save(BuildHeader(o), $"{o.folder}/{o.name}Header.prefab") : null;
                GameObject list = Save(BuildList(o, item, header), $"{o.folder}/{o.name}List.prefab");
                AssetDatabase.SaveAssets();

                Selection.activeObject = list;
                EditorGUIUtility.PingObject(list);
                Debug.Log($"[UI Scroller] {o.name}List.prefab is ready: place it under a Canvas, and change its items through {o.name}ListModel.");
            }
            catch (Exception exception)
            {
                Debug.LogError($"[UI Scroller] Couldn't build {o.name}'s prefabs: {exception}");
            }
            finally
            {
                TargetScene = default;
                UseTextMeshPro = false;
                EditorSceneManager.ClosePreviewScene(preview);
            }
        }

        private static GameObject BuildItem(Options o)
        {
            // Raycast off: drags reach the list's own raycast target behind the slots, and visual-only graphics
            // shouldn't catch touches (only an Action does).
            GameObject root = NewRoot($"{o.name}Item");
            AddImage(root, ItemColor);
            root.AddComponent(FindScript(o, $"{o.name}ItemSlotModel"));
            Component view = root.AddComponent(FindScript(o, $"{o.name}ItemSlotView"));

            switch (o.size)
            {
                case SizeKind.Static:
                {
                    Graphic title = NewLabel("Title", root.transform, "Title", 32, TextColor);
                    Stretch(title.rectTransform, 24f, 0f, 24f, 0f);
                    Set(view, "titleText", title);
                    break;
                }
                case SizeKind.Dynamic:
                {
                    Vertical(root, 24, 8f);
                    Graphic title = NewLabel("Title", root.transform, "Title", 32, TextColor, bold: true, top: true, grows: true);
                    Graphic description = NewLabel("Description", root.transform, "Description", 26, MutedText, top: true, grows: true);
                    Set(view, "titleText", title);
                    Set(view, "descriptionText", description);
                    break;
                }
                case SizeKind.Expandable:
                    BuildExpandableItem(o, root, view);
                    break;
            }

            return root;
        }

        /// <summary>
        /// The summary holds a UI Content with the title and, as its last child, an invisible Action with the click
        /// Controller: the Action covers the summary only, so controls added to the details keep their own taps.
        /// </summary>
        private static void BuildExpandableItem(Options o, GameObject root, Component view)
        {
            RectTransform summary = Place(NewRect("Summary", root.transform), new Vector2(0f, 1f), Vector2.one, new Vector2(0f, -STATIC_SIZE), Vector2.zero);
            RectTransform content = Stretch(NewRect("UI Content", summary));
            Graphic title = NewLabel("Title", content, "Title", 32, TextColor, bold: true);
            Stretch(title.rectTransform, 24f, 0f, 24f, 0f);
            AddAction(summary, FindScript(o, $"Toggle{o.name}ItemController"));

            RectTransform details = Place(NewRect("Details", root.transform), Vector2.zero, Vector2.one, new Vector2(24f, 24f), new Vector2(-24f, -STATIC_SIZE));
            Graphic description = NewLabel("Description", details, "Description", 26, MutedText, top: true);
            details.gameObject.SetActive(false);

            Set(view, "titleText", title);
            Set(view, "descriptionText", description);
            Set(view, "detailsRoot", details.gameObject);
        }

        /// <summary>An invisible, stretched raycast target named Action, with <paramref name="controller"/> on it.</summary>
        private static void AddAction(Transform parent, Type controller)
        {
            RectTransform action = Stretch(NewRect("Action", parent));
            Image target = AddImage(action.gameObject, Color.clear, true);
            target.canvasRenderer.cullTransparentMesh = true;
            action.gameObject.AddComponent(controller);
        }

        private static GameObject BuildHeader(Options o)
        {
            GameObject root = NewRoot($"{o.name}Header");
            AddImage(root, HeaderColor);
            root.AddComponent(FindScript(o, $"{o.name}HeaderSlotModel"));
            Component view = root.AddComponent(FindScript(o, $"{o.name}HeaderSlotView"));
            Graphic title = NewLabel("Title", root.transform, "Header", 30, TextColor, bold: true);
            Stretch(title.rectTransform, 24f, 0f, 24f, 0f);
            Set(view, "titleText", title);
            return root;
        }

        private static GameObject BuildList(Options o, GameObject item, GameObject header)
        {
            ScrollerBase list = NewList($"{o.name}List", FindScript(o, $"{o.name}Scroller"));
            list.SlotPrefab = item;
            ScrollerSettings settings = list.Settings;
            settings.lineSpacing = 8f;

            if (header != null)
            {
                settings.sectionSpacing = 16f;
                settings.headerContentSpacing = 8f;
                Set(list, "headerPrefab", header);
            }

            if (o.bento && o.size == SizeKind.Static)
            {
                settings.bento = true;
                settings.cellAspect = 1f;
                settings.slotSpacing = 8f;
                Set(list, "bentoLayoutJson", AssetDatabase.LoadAssetAtPath<TextAsset>($"{o.folder}/{o.name}Layout.json"));
            }

            list.ApplySettingsToScrollRect();

            list.gameObject.AddComponent(FindScript(o, $"{o.name}ListModel"));
            Component view = list.gameObject.AddComponent(FindScript(o, $"{o.name}ListView"));
            Set(view, "scroller", list);
            return list.gameObject;
        }

        private static GameObject Save(GameObject root, string path)
        {
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            DestroyImmediate(root);
            return prefab;
        }

        // ---------- Pending builds survive the domain reload in SessionState ----------

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

        private static Type FindScript(Options o, string className)
        {
            string fullName = Qualified(o, className);
            foreach (Type type in TypeCache.GetTypesDerivedFrom<MonoBehaviour>())
            {
                if (type.FullName == fullName)
                    return type;
            }

            return null;
        }

        private static string Qualified(Options o, string className)
            => string.IsNullOrEmpty(o.namespaceName) ? className : $"{o.namespaceName}.{className}";

        /// <summary>
        /// The folder's assembly root namespace (or the assembly name) plus the first folder under the assembly's folder,
        /// so a feature keeps one flat namespace: Assets/Client/Shop/StorePage under Client.asmdef makes Client.Shop.
        /// Without an assembly definition, the feature name.
        /// </summary>
        private static string AutoNamespace(Options o)
        {
            string asmdef = AssemblyReferences.FindOwningAsmdef(o.folder);
            if (asmdef == null || !asmdef.StartsWith("Assets/"))
                return ToIdentifier(o.name);

            string json = File.ReadAllText(asmdef);
            string root = AssemblyReferences.ReadRootNamespace(json);
            if (string.IsNullOrEmpty(root))
                root = AssemblyReferences.ReadAssemblyName(json);

            string asmdefFolder = Path.GetDirectoryName(asmdef)?.Replace('\\', '/') ?? "Assets";
            string folder = o.folder.TrimEnd('/');
            if (folder.Length <= asmdefFolder.Length || !folder.StartsWith(asmdefFolder + "/", StringComparison.Ordinal))
                return ToNamespace(root);

            string feature = folder.Substring(asmdefFolder.Length + 1).Split('/')[0];
            return ToNamespace($"{root}.{feature}");
        }

        /// <summary>Each dot-separated part as a valid identifier.</summary>
        internal static string ToNamespace(string value)
        {
            string[] parts = (value ?? string.Empty).Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < parts.Length; i++)
                parts[i] = ToIdentifier(parts[i]);
            return parts.Length == 0 ? "Feature" : string.Join(".", parts);
        }

        /// <summary>The Templates~ folder next to this script, in the installed package or a local copy of it.</summary>
        private static string TemplatesRoot()
        {
            UnityEditor.PackageManager.PackageInfo package = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(ListWizardWindow).Assembly);
            return package != null
                ? Path.Combine(package.resolvedPath, "Editor", "ListWizard", "Templates~")
                : Path.Combine(Path.GetDirectoryName(ThisFile()) ?? string.Empty, "Templates~");
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

        /// <summary>A valid C# class name: letters, digits and underscores, starting with an upper-case letter.</summary>
        internal static string ToIdentifier(string name)
        {
            System.Text.StringBuilder builder = new();
            foreach (char c in name ?? string.Empty)
            {
                if (char.IsLetterOrDigit(c) || c == '_')
                    builder.Append(c);
            }

            if (builder.Length == 0)
                return "Feature";

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
