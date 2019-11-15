using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public abstract class XdCustomEditor : Editor
{
    private static readonly object[] EMPTY_ARRAY = new object[0];

    #region Editor Fields

    /// <summary>
    /// Type object for the internally used (decorated) editor.
    /// </summary>
    private System.Type XdEditorType;

    /// <summary>
    /// Type object for the object that is edited by this editor.
    /// </summary>
    private System.Type editedObjectType;

    private Editor editorInstance;

    #endregion

    private static Dictionary<string, MethodInfo> XdCustomMethods = new Dictionary<string, MethodInfo>();

    private static Assembly editorAssembly = Assembly.GetAssembly(typeof(Editor));

    protected Editor EditorInstance
    {
        get
        {
            if (editorInstance == null && targets != null && targets.Length > 0)
            {
                editorInstance = Editor.CreateEditor(targets, XdEditorType);
            }

            if (editorInstance == null)
            {
                Debug.LogError("Could not create editor !");
            }

            return editorInstance;
        }
    }

    public XdCustomEditor(string editorTypeName)
    {
        this.XdEditorType = editorAssembly.GetTypes().Where(t => t.Name == editorTypeName).FirstOrDefault();

        Init();
        
        var originalEditedType = GetCustomEditorType(XdEditorType);

        if (originalEditedType != editedObjectType)
        {
            throw new System.ArgumentException(
                string.Format("Type {0} does not match the editor {1} type {2}",
                          editedObjectType, editorTypeName, originalEditedType));
        }
    }

    private System.Type GetCustomEditorType(System.Type type)
    {
        var flags = BindingFlags.NonPublic | BindingFlags.Instance;

        var attributes = type.GetCustomAttributes(typeof(CustomEditor), true) as CustomEditor[];
        var field = attributes.Select(editor => editor.GetType().GetField("m_InspectedType", flags)).First();

        return field.GetValue(attributes[0]) as System.Type;
    }

    private void Init()
    {
        var flags = BindingFlags.NonPublic | BindingFlags.Instance;

        var attributes = this.GetType().GetCustomAttributes(typeof(CustomEditor), true) as CustomEditor[];
        var field = attributes.Select(editor => editor.GetType().GetField("m_InspectedType", flags)).First();

        editedObjectType = field.GetValue(attributes[0]) as System.Type;
    }

    void OnDisable()
    {
        if (editorInstance != null)
        {
            DestroyImmediate(editorInstance);
        }
    }
    
    protected void CallInspectorMethod(string methodName)
    {
        MethodInfo method = null;
        
        if (!XdCustomMethods.ContainsKey(methodName))
        {
            var flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public;

            method = XdEditorType.GetMethod(methodName, flags);

            if (method != null)
            {
                XdCustomMethods[methodName] = method;
            }
            else
            {
                Debug.LogError(string.Format("Could not find method {0}", method));
            }
        }
        else
        {
            method = XdCustomMethods[methodName];
        }

        if (method != null)
        {
            method.Invoke(EditorInstance, EMPTY_ARRAY);
        }
    }

    public void OnSceneGUI()
    {
        CallInspectorMethod("OnSceneGUI");
    }

    protected override void OnHeaderGUI()
    {
        CallInspectorMethod("OnHeaderGUI");
    }

    public override void OnInspectorGUI()
    {
        EditorInstance.OnInspectorGUI();
    }

    public override void DrawPreview(Rect previewArea)
    {
        EditorInstance.DrawPreview(previewArea);
    }

    public override string GetInfoString()
    {
        return EditorInstance.GetInfoString();
    }

    public override GUIContent GetPreviewTitle()
    {
        return EditorInstance.GetPreviewTitle();
    }

    public override bool HasPreviewGUI()
    {
        return EditorInstance.HasPreviewGUI();
    }

    public override void OnInteractivePreviewGUI(Rect r, GUIStyle background)
    {
        EditorInstance.OnInteractivePreviewGUI(r, background);
    }

    public override void OnPreviewGUI(Rect r, GUIStyle background)
    {
        EditorInstance.OnPreviewGUI(r, background);
    }

    public override void OnPreviewSettings()
    {
        EditorInstance.OnPreviewSettings();
    }

    public override void ReloadPreviewInstances()
    {
        EditorInstance.ReloadPreviewInstances();
    }

    public override Texture2D RenderStaticPreview(string assetPath, Object[] subAssets, int width, int height)
    {
        return EditorInstance.RenderStaticPreview(assetPath, subAssets, width, height);
    }

    public override bool RequiresConstantRepaint()
    {
        return EditorInstance.RequiresConstantRepaint();
    }

    public override bool UseDefaultMargins()
    {
        return EditorInstance.UseDefaultMargins();
    }
}