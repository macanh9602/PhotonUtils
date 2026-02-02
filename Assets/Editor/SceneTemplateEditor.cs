using UnityEditor;
using UnityEngine;
using System;
using System.Collections.Generic;
[Serializable]
public class SceneTemplateNode
{
    public string Name;
    public bool IsHeader; // Dùng để tạo dòng "==== NAME ===="
    public bool IsManager; // Có thể gắn thêm Icon hoặc tag nếu muốn
    public List<SceneTemplateNode> Children = new();

    public SceneTemplateNode(string name, bool isHeader = false)
    {
        Name = name;
        IsHeader = isHeader;
    }
}
public class SceneTemplateEditor : EditorWindow
{
    private SceneTemplate _template;
    private Vector2 _scroll;

    [MenuItem("Tools/Scene Hierarchy Organizer")]
    public static void Open() => GetWindow<SceneTemplateEditor>("Scene Organizer");

    private void OnGUI()
    {
        _template = (SceneTemplate)EditorGUILayout.ObjectField("Template", _template, typeof(SceneTemplate), false);

        if (_template == null)
        {
            EditorGUILayout.HelpBox("Chọn SceneTemplate để bắt đầu.", MessageType.Info);
            if (GUILayout.Button("Tạo Template Mẫu")) CreateDefaultTemplate();
            return;
        }

        _scroll = EditorGUILayout.BeginScrollView(_scroll);
        // Hiển thị danh sách chỉnh sửa tương tự như FolderEditor của bạn
        DrawNodeList(_template.RootObjects);
        EditorGUILayout.EndScrollView();

        if (GUILayout.Button("Apply To Current Scene", GUILayout.Height(40)))
        {
            ApplyToScene();
        }
    }

    void DrawNodeList(System.Collections.Generic.List<SceneTemplateNode> nodes, int indent = 0)
    {
        for (int i = 0; i < nodes.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(indent * 20);

            nodes[i].Name = EditorGUILayout.TextField(nodes[i].Name);
            nodes[i].IsHeader = EditorGUILayout.ToggleLeft("Header", nodes[i].IsHeader, GUILayout.Width(60));

            if (GUILayout.Button("+", GUILayout.Width(25))) nodes[i].Children.Add(new SceneTemplateNode("New Object"));
            if (GUILayout.Button("x", GUILayout.Width(25))) { nodes.RemoveAt(i); break; }

            EditorGUILayout.EndHorizontal();
            DrawNodeList(nodes[i].Children, indent + 1);
        }
    }

    void ApplyToScene()
    {
        foreach (var node in _template.RootObjects)
        {
            CreateObject(node, null);
        }
    }

    void CreateObject(SceneTemplateNode node, Transform parent)
    {
        string finalName = node.IsHeader ? $"=========== [{node.Name.ToUpper()}] =============" : node.Name;
        GameObject go = new GameObject(finalName);
        go.transform.SetParent(parent);

        // Nếu là Header, ta thường disable để tránh xử lý thừa, hoặc tag là EditorOnly
        if (node.IsHeader) go.tag = "EditorOnly";

        foreach (var child in node.Children)
        {
            CreateObject(child, go.transform);
        }

        Undo.RegisterCreatedObjectUndo(go, "Create Scene Template");
    }

    void CreateDefaultTemplate()
    {
        // Logic tạo file .asset mặc định tương tự như FolderEditor của bạn
        // Bao gồm: === MANAGERS ===, === SYSTEMS ===, === ENVIRONMENT ===...
    }
}