using UnityEditor;
using UnityEngine;
using System.IO;

public class TemplateEditor : EditorWindow
{
    private FolderTemplate _template;
    private Vector2 _scroll;

    [MenuItem("Tools/Project Template Editor")]
    public static void Open()
    {
        GetWindow<TemplateEditor>("Project Template");
    }

    private void OnGUI()
    {
        DrawHeader();

        if (_template == null)
        {
            EditorGUILayout.HelpBox(
                "Select or create a FolderTemplate asset.",
                MessageType.Info
            );
            return;
        }

        _scroll = EditorGUILayout.BeginScrollView(_scroll);
        DrawFolderList(_template.RootFolders);
        EditorGUILayout.EndScrollView();

        GUILayout.Space(10);
        DrawFooter();
    }

    void DrawHeader()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

        _template = (FolderTemplate)EditorGUILayout.ObjectField(
            _template,
            typeof(FolderTemplate),
            false
        );

        if (GUILayout.Button("New", EditorStyles.toolbarButton))
        {
            CreateNewTemplate();
        }

        EditorGUILayout.EndHorizontal();
    }

    void DrawFolderList(System.Collections.Generic.List<FolderNode> folders, int indent = 0)
    {
        for (int i = 0; i < folders.Count; i++)
        {
            var folder = folders[i];

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(indent * 16);

            folder.Enabled = EditorGUILayout.Toggle(folder.Enabled, GUILayout.Width(20));
            folder.Name = EditorGUILayout.TextField(folder.Name);

            var oldBg = GUI.backgroundColor;

            GUI.backgroundColor = new Color(0.4f, 0.85f, 0.5f);
            if (GUILayout.Button("+", GUILayout.Width(22)))
                folder.Children.Add(new FolderNode("NewFolder"));

            GUI.backgroundColor = new Color(0.9f, 0.35f, 0.35f);
            if (GUILayout.Button("X", GUILayout.Width(22)))
                folders.RemoveAt(i);

            GUI.backgroundColor = oldBg;
            EditorGUILayout.EndHorizontal();

            if (folder.Children.Count > 0)
                DrawFolderList(folder.Children, indent + 1);
        }
    }

    void DrawFooter()
    {
        if (GUILayout.Button("Apply To Project", GUILayout.Height(30)))
        {
            ApplyFolders("Assets", _template.RootFolders);
            AssetDatabase.Refresh();
        }
    }

    void ApplyFolders(string rootPath, System.Collections.Generic.List<FolderNode> folders)
    {
        foreach (var folder in folders)
        {
            if (!folder.Enabled) continue;

            string path = Path.Combine(rootPath, folder.Name);

            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder(rootPath, folder.Name);
            }

            ApplyFolders(path, folder.Children);
        }
    }

    void CreateNewTemplate()
    {
        string path = EditorUtility.SaveFilePanelInProject(
            "Create Core Folder Template",
            "UnityUtilityCoreTemplate",
            "asset",
            "Save core project template"
        );

        if (string.IsNullOrEmpty(path))
            return;

        var template = ScriptableObject.CreateInstance<FolderTemplate>();

        template.RootFolders.Add(new FolderNode("_Core")
        {
            Children =
        {
            new FolderNode("Helpers"),
            new FolderNode("Utilities"),
            new FolderNode("Extensions"),
            new FolderNode("Math"),
            new FolderNode("Debug"),
            new FolderNode("Animation"),
            new FolderNode("UI"),
            new FolderNode("World"),
            new FolderNode("Patterns"),
        }
        });

        template.RootFolders.Add(new FolderNode("_Presets")
        {
            Children =
        {
            new FolderNode("Textures"),
            new FolderNode("Sprites"),
            new FolderNode("Models"),
            new FolderNode("Audio"),
            new FolderNode("Fonts"),
            new FolderNode("Notes"),
        }
        });

        template.RootFolders.Add(new FolderNode("_Frameworks")
        {
            Children =
        {
            new FolderNode("Addressables"),
            new FolderNode("Firebase"),
            new FolderNode("Backend"),
            new FolderNode("Ads"),
            new FolderNode("Analytics"),
            new FolderNode("SDKs"),
        }
        });

        template.RootFolders.Add(new FolderNode("_Prefabs")
        {
            Children =
        {
            new FolderNode("Camera"),
            new FolderNode("UI"),
            new FolderNode("Effects"),
            new FolderNode("Debug"),
            new FolderNode("Samples"),
        }
        });

        template.RootFolders.Add(new FolderNode("_Shaders")
        {
            Children =
        {
            new FolderNode("UI"),
            new FolderNode("World"),
            new FolderNode("Effects"),
            new FolderNode("Common"),
        }
        });

        template.RootFolders.Add(new FolderNode("_Samples"));
        template.RootFolders.Add(new FolderNode("_Docs"));
        template.RootFolders.Add(new FolderNode("Plugins"));

        AssetDatabase.CreateAsset(template, path);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Selection.activeObject = template;
        EditorGUIUtility.PingObject(template);

        _template = template;
    }



}
