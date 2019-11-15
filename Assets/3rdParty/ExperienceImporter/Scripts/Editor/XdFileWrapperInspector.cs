using UnityEditor;
using UnityEngine;
using static XdFileReimport;

[CustomEditor(typeof(XdFileWrapper))]
public class XdFileWrapperInspector : Editor
{
    public override void OnInspectorGUI()
    {
        XdFileWrapper Target = (XdFileWrapper)target;
        Object targetObject = AssetDatabase.LoadAssetAtPath(Target.fileName, typeof(Object));
        if (Target.fileName != null)
        {
            if(targetObject != null) EditorGUILayout.InspectorTitlebar(false, targetObject);
            GUILayout.Label("AdobeXd file path: " + Target.fileName);
            
            if (Target.CurrentDate > Target.LastImportedDate && Target.imported)
            {
                EditorGUILayout.HelpBox("File has been updated since last import.", MessageType.Info);
              
            }
            EditorGUILayout.BeginHorizontal();
            if (Target.CurrentDate > Target.LastImportedDate && Target.imported)
            {
                if (GUILayout.Button("Update", GUILayout.Width(90)))
                {
                    if (EditorUtility.DisplayDialog("Reload imported file?",
                                  "Do you want to reload? All changes made in Unity will be lost.",
                                  "Reload", "Keep current version"))
                    {
                        Reimport(Target.fileName);
                    }
                }
            }
            else
            {
                if (Target.imported)
                {

                    if (GUILayout.Button("Reimport", GUILayout.Width(90)))
                    {
                        if (EditorUtility.DisplayDialog("Reload imported file?",
                                      "Do you want to reload? All changes made in Unity will be lost.",
                                      "Reload", "Keep current version"))
                        {
                            Reimport(Target.fileName);
                        }
                    }
                }
                else
                {
                    if (GUILayout.Button("Import", GUILayout.Width(80)))
                    {
                        Import(Target.fileName);

                    }
                }   
            }
           
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Open", GUILayout.Width(60)))
            {
                AssetDatabase.OpenAsset(targetObject);

            }
            EditorGUILayout.EndHorizontal();
        }
        
    }
}