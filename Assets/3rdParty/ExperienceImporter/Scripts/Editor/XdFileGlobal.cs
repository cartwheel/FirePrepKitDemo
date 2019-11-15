using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using static ExperienceImporter;

[InitializeOnLoad]
public class XdFileGlobal
{
    private static XdFileWrapper wrapper = null;
    private static bool selectionChanged = false;
    static string dateOf = "dateOfModification: ";
    static int from;

    static XdFileGlobal()
    {
        Selection.selectionChanged += SelectionChanged;
        EditorApplication.update += Update;
    }

    private static void SelectionChanged()
    {
        selectionChanged = true;
    }

    private static void Update()
    {
        if (selectionChanged == false) return;

        selectionChanged = false;
        if (Selection.activeObject != wrapper)
        {
            string fn = AssetDatabase.GetAssetPath(Selection.activeInstanceID);
            string metaData = "";
            bool metaExists = File.Exists(fn + ".meta");
            if (metaExists) metaData = File.ReadAllText(fn + ".meta");
            from = metaData.IndexOf(dateOf) + dateOf.Length;
            if (fn.ToLower().EndsWith(".xd"))
            {
                if (wrapper == null)
                {
                    wrapper = ScriptableObject.CreateInstance<XdFileWrapper>();
                    wrapper.hideFlags = HideFlags.DontSave;
                }
                if (metaExists)
                {
                    DateTime fileLastTimeStamp = new DateTime();
                    if (metaData.LastIndexOf("~;") > 0) fileLastTimeStamp = ExperienceImporter.WithoutMS(DateTime.Parse(metaData.Substring(from, metaData.LastIndexOf("~;") - from)));
                    wrapper.LastImportedDate = fileLastTimeStamp;
                    wrapper.CurrentDate = WithoutMS(File.GetLastWriteTime(fn));
                    wrapper.imported = metaData.Contains("alreadyImported: true");
                }
                wrapper.fileName = fn;
                Selection.activeObject = wrapper;

                Editor[] ed = Resources.FindObjectsOfTypeAll<XdFileWrapperInspector>();
                if (ed.Length > 0) ed[0].Repaint();
            }
        }
    }
}
