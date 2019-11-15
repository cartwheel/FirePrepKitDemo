using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

public class XdFileReimport : MonoBehaviour
{
    public static void Reimport(string filepath)
    {
        if (filepath != null)
        {
            string metaData = File.ReadAllText(filepath + ".meta");
            metaData = Regex.Replace(metaData, " dateOfModification.*~;", " reimport: true;");
            StreamWriter writer = new StreamWriter(filepath + ".meta", false);
            writer.Write(metaData);
            writer.Close();
            AssetDatabase.Refresh();

        }
    }
    public static void Import (string filepath)
    {
        if (filepath != null)
            AssetDatabase.ImportAsset(filepath, ImportAssetOptions.Default);
    }
}
