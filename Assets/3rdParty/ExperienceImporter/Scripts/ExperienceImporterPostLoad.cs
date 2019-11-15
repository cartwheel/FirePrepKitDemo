using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

[ExecuteInEditMode]
public class ExperienceImporterPostLoad : MonoBehaviour
{
  //  [HideInInspector]
    public string ImagePath = "";
    [HideInInspector]
    public Image image;

    private void Awake()
    {
        image = gameObject.GetComponent<Image>();
    }

    // Update is called once per frame
    void Update()
    {
        if (ImagePath.Length > 0 && image.sprite == null)
        {
            TextureImporter importer = AssetImporter.GetAtPath(ImagePath) as TextureImporter;
            importer.textureType = TextureImporterType.Sprite;
            AssetDatabase.ImportAsset(importer.assetPath, ImportAssetOptions.ForceUpdate);
            string filename = Path.GetFileNameWithoutExtension(ImagePath);
            string directory = Path.GetDirectoryName(ImagePath.Replace("Assets/Resources/", ""));
            image.sprite = Resources.Load<Sprite>(Path.Combine(directory, filename));

           // image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(ImagePath);
        }
    }
}
