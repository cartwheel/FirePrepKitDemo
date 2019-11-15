using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static ExperienceImporterFileClasses;
using ICSharpCode.SharpZipLib.Zip;

public class ExperienceImporter : AssetPostprocessor
{
    string assetsFolder, metaData, filename, fileWithExt, importFilePath;
    DirectoryInfo projectFolder;
    DateTime fileLastTimeStamp, fileCurrentTimestamp;
    Manifest parsedManifest;
    List<UxGradient> gradients;
    List<Interaction> interactions = new List<Interaction>();
    string homeArtboard;
    ExperienceImporterTransition experienceImporterTransition;
    GameObject masterParent;
    GameObject mainBoard;
    void OnPreprocessAsset()
    {
        metaData = assetImporter.userData;
        filename = Path.GetFileNameWithoutExtension(assetPath);
        fileWithExt = Path.GetFileName(assetPath);
        assetsFolder = Application.dataPath;
        projectFolder = Directory.GetParent(assetsFolder);
        importFilePath = assetImporter.assetPath;
        fileCurrentTimestamp = WithoutMS(File.GetLastWriteTime(importFilePath));
        if (!EditorApplication.isPlaying)
        {
            if (assetPath.Contains(".xd"))
            {
                if (!metaData.Contains("alreadyImported: true"))
                {
                    if (EditorUtility.DisplayDialog("Parse Adobe Xd file to Unity?",
                        "Do you want to convert " + fileWithExt + " to Unity scenes?",
                        "Convert", "Add file to project as is"))
                    {
                        CoreImporter();
                    }

                }
                else if (metaData.Contains("alreadyImported: true"))
                {
                    string dateOf = "dateOfModification: ";
                    int from = metaData.IndexOf(dateOf) + dateOf.Length;
                    if (metaData.LastIndexOf("~;") > 0) fileLastTimeStamp = WithoutMS(DateTime.Parse(metaData.Substring(from, metaData.LastIndexOf("~;") - from)));
                    if (metaData.Contains("reimport: true;"))
                    {
                        ReimportAsset();
                        Selection.activeObject = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
                    }
                    else if (fileCurrentTimestamp > fileLastTimeStamp && !metaData.Contains("reimport: true;"))
                    {
                        if (EditorUtility.DisplayDialog("Reload imported file?",
                           "File " + fileWithExt + " has changed. Do you want to reload? All changes made in Unity will be lost.",
                           "Reload", "Keep current version"))
                        {
                            ReimportAsset();
                            Selection.activeObject = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
                        }
                    }
                }
            }

        }
        else
            Debug.LogWarning("Can't import Xd files in playmode");
    }

    public void ReimportAsset()
    {
        if (Directory.Exists(projectFolder + "/~Temp-" + filename)) Directory.Delete(projectFolder + "/~Temp-" + filename, true);
        if (Directory.Exists("Assets/Resources/[AdobeXd]-" + filename)) Directory.Delete("Assets/Resources/[AdobeXd]-" + filename, true);
        CoreImporter();
    }

    void CoreImporter()
    {
        assetImporter.userData = "alreadyImported: true;" + " dateOfModification: " + fileCurrentTimestamp.ToString() + "~;";
        string unpackDirectory = projectFolder + "/~Temp-" + filename;
        if (Directory.Exists(unpackDirectory)) Directory.Delete(unpackDirectory, true);
        Directory.CreateDirectory(unpackDirectory);
        //   ZipFile.ExtractToDirectory(importFilePath, unpackDirectory);
        //var options = new ReadOptions();
        //using (ZipFile zip = ZipFile.Read(importFilePath, options))
        //{
        //    zip.ExtractAll(unpackDirectory);
        //}

        FastZip fastZip = new FastZip();
        string fileFilter = null;
        fastZip.ExtractZip(importFilePath, unpackDirectory, fileFilter);

        parsedManifest = ParseManifest("~Temp-" + filename + "/manifest");
        if (parsedManifest != null) {
        ImagesExtension(parsedManifest.images, filename);
        ReadGradients();
        ReadInteractions();
        ReadObjectsInArtBoard(parsedManifest, filename);
        CreateScene(parsedManifest, filename);
        AssetDatabase.Refresh();
        }
        else Debug.LogError("Couldn't import file");
        if (Directory.Exists(unpackDirectory)) Directory.Delete(unpackDirectory, true);
    }
    Manifest ParseManifest(string path)
    {
        Manifest manifest = new Manifest
        {
            artBoards = new List<ArtBoard>(),
            images = new List<Images>()
        };
        RootObject root = new RootObject();

        try
        {
            root = JsonConvert.DeserializeObject<RootObject>(File.ReadAllText(path).Replace("#", ""));
        }
        catch
        {
            Debug.LogError("Couldn't parse manifest file.");
        }
        if ( root.children != null) {
            foreach (Children x in root.children)
        {
            if (x.name == "artwork")
            {
                foreach (Children y in x.children)
                {
                    if (y.name != "pasteboard")
                    {
                        ArtBoard art = new ArtBoard
                        {
                            id = y.id,
                            name = y.name,
                            path = y.path,
                            Uxdesignbounds = y.Uxdesignbounds
                        };
                        manifest.artBoards.Add(art);
                    }
                }
            }
            if (x.name == "resources")
            {
                foreach (Images y in x.components)
                {
                    Images img = new Images
                    {
                        id = y.id,
                        name = y.name,
                        path = y.path,
                        type = y.type
                    };
                    manifest.images.Add(img);
                }
            }
        }
        return manifest;
        }
        else return null;
    }
    void ImagesExtension(List<Images> listOfImages, string filename)
    {
        string folderPath = "/~Temp-" + filename + "/resources/";
        string newpath = CreateFolder(filename + "/Images/");
        foreach (Images image in listOfImages)
        {
            string oldpath = projectFolder + folderPath + image.path;
            string ext = ParseExtension(image.type);
            System.IO.File.Move(oldpath, newpath + image.path + ext);
        }
    }
    string CreateFolder(string filenameWithSubfolder)
    {
        string newpath = assetsFolder + "/Resources/[AdobeXd]-" + filenameWithSubfolder;
        if (!Directory.Exists(newpath)) Directory.CreateDirectory(newpath);
        return newpath;
    }
    public static DateTime WithoutMS(DateTime inputDate)
    {
        return inputDate.AddTicks(-(inputDate.Ticks % 10000000));
    }
    void ReadObjectsInArtBoard(Manifest manifest, string filename)
    {
        string folderPath = projectFolder + "/~Temp-" + filename + "/artwork/";
        foreach (ArtBoard artBoard in manifest.artBoards)
        {
            RootObject artRoot = new RootObject();
            try
            {
                artRoot = JsonConvert.DeserializeObject<RootObject>(File.ReadAllText(folderPath + artBoard.path + "/graphics/graphicContent.agc"));
            }
            catch
            {
                Debug.LogWarning("Didn't found any artBoards?");
            }
            artBoard.children = artRoot.children;
        }
    }
    Color ParseRGBColor(UxColor color)
    {
        UxColorValue colorValue = color.value;
        if (color.alpha == null) return new Color(colorValue.r / 255, colorValue.g / 255, colorValue.b / 255);

        return new Color(colorValue.r / 255, colorValue.g / 255, colorValue.b / 255, float.Parse(color.alpha, CultureInfo.InvariantCulture));
    }
    string ParseExtension(string type)
    {
        switch (type)
        {
            case "image/png":
                return ".png";
            case "image/jpeg":
                return ".jpg";
        }
        return "";
    }
    void CreateScene(Manifest manifest, string filename)
    {
        var Scene = EditorSceneManager.OpenScene("Assets/3rdParty/ExperienceImporter/EmptyScene.unity", OpenSceneMode.Single);
        ExperienceImporterMaster[] previousVersion = UnityEngine.Object.FindObjectsOfType<ExperienceImporterMaster>(); ;
        if (previousVersion.Length > 0)
        {
            foreach (ExperienceImporterMaster prev in previousVersion) UnityEngine.GameObject.DestroyImmediate(prev.gameObject);
        }
        CreateFolder(filename + "/Scenes");
        CreateFolder(filename + "/Prefabs");
        CreateFolder(filename + "/Gradients");
        // var newScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        masterParent = new GameObject(filename);
        masterParent.AddComponent<ExperienceImporterMaster>();
        masterParent.AddComponent<RectTransform>();
        GameObject events = new GameObject("EventSystem");
        events.AddComponent<EventSystem>();
        events.AddComponent<StandaloneInputModule>();
        events.transform.SetParent(masterParent.transform);
        GameObject canv = new GameObject("Canvas");
        Canvas canvas = canv.AddComponent<Canvas>();
        CanvasScaler canvS = canv.AddComponent<CanvasScaler>();
        GameObject cam = new GameObject("Camera");
        Camera camera = cam.AddComponent<Camera>();
        GraphicRaycaster graphicRaycaster = canv.AddComponent<GraphicRaycaster>();
        canvas.transform.SetParent(masterParent.transform);
        canvas.worldCamera = camera;
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.planeDistance = 25;
        graphicRaycaster.blockingObjects = GraphicRaycaster.BlockingObjects.None;
        graphicRaycaster.ignoreReversedGraphics = true;
        cam.transform.parent = masterParent.transform;
        camera.orthographic = true;
        canvS.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvS.referenceResolution = new Vector2(manifest.artBoards[0].Uxdesignbounds.width, manifest.artBoards[0].Uxdesignbounds.height);
        canvS.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;

        mainBoard = new GameObject("Artboards");
        RectTransform mainRect = mainBoard.AddComponent<RectTransform>();
        mainBoard.transform.SetParent(canv.transform);
        Anchoring(mainRect);
        experienceImporterTransition = mainBoard.AddComponent<ExperienceImporterTransition>();
        experienceImporterTransition.Artboards = mainBoard;
        experienceImporterTransition.homeboard = homeArtboard;

        foreach (ArtBoard artBoard in manifest.artBoards)
        {
            GameObject art = new GameObject(artBoard.name);
            RectTransform rect = art.AddComponent<RectTransform>();
            ExperienceImporterArtboard experienceImporterArtboard = art.AddComponent<ExperienceImporterArtboard>();
            art.AddComponent<Mask>();
            art.AddComponent<Image>();

            string artBoardId = artBoard.path.Replace("artboard-", "");
            experienceImporterArtboard.elementId = artBoardId;
            art.transform.SetParent(mainBoard.transform);
            Anchoring(rect);
            rect.sizeDelta = new Vector2(artBoard.Uxdesignbounds.width, artBoard.Uxdesignbounds.height);
            rect.anchoredPosition = new Vector3(artBoard.Uxdesignbounds.x, -artBoard.Uxdesignbounds.y, 0);
            CreateChildren(artBoard.children[0].artBoard.children, mainBoard, art);
            int interactionId = interactions.FindIndex(i => i.uid == artBoardId);
            if (interactionId != -1)
            {
                ExperienceImporterInteractiveElement click = art.AddComponent<ExperienceImporterInteractiveElement>();
                click.targetElementId = interactions[interactionId].properties.destination;
                click.mainBoard = mainBoard;
            }
            if (artBoardId == homeArtboard) mainBoard.transform.localPosition -= art.transform.localPosition;
            if (artBoard.children[0].style != null)
            {
                if (artBoard.children[0].style.fill.type != "none")
                {
                    DoBackground(artBoard.children[0], art);
                }
            }
#if UNITY_2018_3_OR_NEWER
            PrefabUtility.SaveAsPrefabAssetAndConnect(art, "Assets/Resources/[AdobeXd]-" + filename + "/Prefabs/" + artBoard.name + ".prefab", InteractionMode.AutomatedAction);
#endif
        }
        EditorSceneManager.SaveScene(Scene, "Assets/Resources/[AdobeXd]-" + filename + "/Scenes/" + filename + ".unity", false);

    }
    void CreateChildren(List<Children> parent, GameObject relativeTo, GameObject hierarchyParent)
    {
        foreach (Children child in parent)
        {
            GameObject obj = new GameObject(child.name);
            obj.transform.SetParent(relativeTo.transform);
            RectTransform objRect = obj.AddComponent<RectTransform>();
            Anchoring(objRect);

            objRect.anchoredPosition = new Vector3(child.transform.tx, -child.transform.ty, 0);

            obj.transform.SetParent(hierarchyParent.transform);

            switch (child.type)
            {
                case "shape":
                    objRect.sizeDelta = new Vector2(child.shape.width, child.shape.height);
                    Image objImg = obj.AddComponent<Image>();
                    DoBackground(child, obj);
                    if (child.style.opacity != null)
                    {
                        objImg.color = new Vector4(objImg.color.r, objImg.color.g, objImg.color.b, objImg.color.a * float.Parse(child.style.opacity, CultureInfo.InvariantCulture));
                    }
                    break;
                case "text":
                    Text text = obj.AddComponent<Text>();
                    text.text = child.text.rawText.Replace("\u000b", "\n");
                    text.color = ParseRGBColor(child.style.fill.color);
                    text.fontSize = Mathf.RoundToInt(child.style.font.size);
                    text.lineSpacing = 1;
                    text.alignment = TextAnchor.LowerLeft;
                    if (child.style.textAttributes != null)
                    {
                        if (child.style.textAttributes.lineHeight != 0) text.lineSpacing = child.style.textAttributes.lineHeight / text.fontSize;
                    }
                    string[] lines = text.text.Split(new[] { "\n" }, StringSplitOptions.None);
                    List<string> lines2 = new List<string>();
                    foreach (string line in lines)
                    {
                        lines2.AddRange(line.Split(new[] { "\u000b" }, StringSplitOptions.None));
                    }
                    switch (child.text.frame.type)
                    {
                        case "area":
                            objRect.sizeDelta = new Vector2(child.text.frame.width, child.text.frame.height);
                            text.resizeTextForBestFit = true;
                            break;
                        case "positioned":
                            objRect.sizeDelta = new Vector2(0, text.fontSize + text.lineSpacing * text.fontSize * (lines2.Count - 1 + 0.33f));
                            text.horizontalOverflow = HorizontalWrapMode.Overflow;
                            objRect.anchoredPosition = new Vector3(objRect.anchoredPosition.x, (objRect.anchoredPosition.y + text.fontSize * 1.08f), 0);
                            break;
                    }
                    if (child.style.opacity != null)
                    {
                        text.color = new Vector4(text.color.r, text.color.g, text.color.b, text.color.a * float.Parse(child.style.opacity, CultureInfo.InvariantCulture));
                    }
                    if (child.style.font.style.Contains("bold") || child.style.font.style.Contains("Bold")) text.fontStyle = FontStyle.Bold;
                    else if (child.style.font.style.Contains("Italic") || child.style.font.style.Contains("italic")) text.fontStyle = FontStyle.Italic;
                    else if ((child.style.font.style.Contains("bold") || child.style.font.style.Contains("Bold")) && (child.style.font.style.Contains("Italic") || child.style.font.style.Contains("italic"))) text.fontStyle = FontStyle.BoldAndItalic;
                    else text.fontStyle = FontStyle.Normal;

                    if (child.style.textAttributes != null)
                    {
                        if (child.style.textAttributes.paragraphAlign != null)
                        {
                            switch (child.style.textAttributes.paragraphAlign)
                            {
                                case "right":
                                    text.alignment = TextAnchor.LowerRight;
                                    break;
                                case "center":
                                    text.alignment = TextAnchor.LowerCenter;
                                    break;
                                default:
                                    break;
                            }
                        }
                    }
                    if (child.style.stroke != null)
                    {
                        if (child.style.stroke.type != null)
                        {
                            Outline textOutline = obj.AddComponent<Outline>();
                            textOutline.effectColor = ParseRGBColor(child.style.stroke.color);
                            textOutline.effectDistance = new Vector2(child.style.stroke.width, child.style.stroke.width);
                        }
                    }
                    text.resizeTextForBestFit = true;
                    break;
                default:
                    break;
            }
            if (child.id != null && interactions.Count > 0)
            {
                int interactionId = interactions.FindIndex(i => i.uid == child.id);
                if (interactionId != -1)
                {
                    ExperienceImporterInteractiveElement click = obj.AddComponent<ExperienceImporterInteractiveElement>();
                    click.targetElementId = interactions[interactionId].properties.destination;
                    click.mainBoard = mainBoard;
                }
            }
            if (child.type != "group")
            {
                if (child.children != null) CreateChildren(child.children, relativeTo, obj);
            }
            else
            {
                CreateChildren(child.group.children, obj, obj);
            }
            if (child.visible == "false") obj.SetActive(false);
        }
    }
    void CreateStroke(GameObject objectToStrokeAround, float strokeWidth, Color strokeColor, string align)
    {
        RectTransform parentRect = objectToStrokeAround.GetComponent<RectTransform>();
        GameObject stroke = new GameObject("stroke");
        GameObject[] strokes = { new GameObject("Up"), new GameObject("Down"), new GameObject("Left"), new GameObject("Right") };
        RectTransform strokeParentRect = stroke.AddComponent<RectTransform>();
        stroke.transform.SetParent(objectToStrokeAround.transform);
        Anchoring(strokeParentRect);
        strokeParentRect.sizeDelta = parentRect.sizeDelta;
        foreach (GameObject strokeElem in strokes)
        {
            RectTransform strokeRect = strokeElem.AddComponent<RectTransform>();
            Image strokeImg = strokeElem.AddComponent<Image>();
            strokeImg.color = strokeColor;
            strokeElem.transform.SetParent(stroke.transform);
            if (strokeElem.name == "Up" || strokeElem.name == "Down") strokeRect.sizeDelta = new Vector2(parentRect.sizeDelta.x, strokeWidth);
            else strokeRect.sizeDelta = new Vector2(strokeWidth, parentRect.sizeDelta.y);
            Vector2 pivotAnchor = new Vector2();
            Vector2 orientation = new Vector2();
            switch (strokeElem.name)
            {
                case "Up":
                    pivotAnchor = new Vector2(0.5f, 1);
                    break;
                case "Down":
                    pivotAnchor = new Vector2(0.5f, 0);
                    break;
                case "Left":
                    pivotAnchor = new Vector2(0, 0.5f);
                    break;
                case "Right":
                    pivotAnchor = new Vector2(1, 0.5f);
                    break;
            }
            strokeRect.pivot = pivotAnchor;
            strokeRect.anchorMin = pivotAnchor;
            strokeRect.anchorMax = pivotAnchor;
            float multiplier = 0;
            if (pivotAnchor.x == 0.5f) orientation = new Vector2(0, 1);
            else orientation = new Vector2(1, 0);
            if (pivotAnchor.x == 0 || pivotAnchor.y == 0) orientation = -orientation;
            switch (align)
            {
                case "inside":
                    multiplier = 0;
                    break;
                case "outside":
                    multiplier = 1;
                    break;
                default:
                    multiplier = 0.5f;
                    break;
            }
            strokeRect.anchoredPosition = orientation * multiplier * strokeWidth;
            strokeRect.sizeDelta += 2 * new Vector2(Mathf.Abs(orientation.y), Mathf.Abs(orientation.x)) * multiplier * strokeWidth;
            strokeRect.localScale = Vector3.one;
        }
    }
    void ReadGradients()
    {
        string pattern = "\"[a-zA-Z0-9-]{36}\":{";
        Regex rgx = new Regex(pattern);
        string textFile = File.ReadAllText(projectFolder + "/~Temp-" + filename + "/resources/graphics/graphicContent.agc");

        int i = 0;
        foreach (Match match in rgx.Matches(textFile))
        {
            string uid = match.Value.Replace(":{", "");
            if (i == 0)
            {
                textFile = Regex.Replace(textFile, match.Value, "[\"uid\":" + uid + ",");
                textFile = Regex.Replace(textFile, "},\"clipPaths\".*", "]}}").Replace("{[", "[{");
            }
            else textFile = Regex.Replace(textFile, match.Value, "{\"uid\":" + uid + ",");
            i++;
        }
        try
        {
            gradients = JsonConvert.DeserializeObject<RootObject>(textFile).resources.gradients;
        }
        catch
        {
            Debug.Log("Didn't found any gradients");
        }
    }
    void ReadInteractions()
    {
        string pattern = "\"[a-zA-Z0-9-]{36}\":";
        Regex rgx = new Regex(pattern);
        string textFile = File.ReadAllText(projectFolder + "/~Temp-" + filename + "/interactions/interactions.json");

        int i = 0;
        foreach (Match match in rgx.Matches(textFile))
        {
            string uid = match.Value.Replace(":[{", "");
            textFile = Regex.Replace(textFile, match.Value, "{\"uid\":" + uid + ",");
            textFile = textFile.Replace(",[{", "").Replace("}],{", "},{").Replace("{{", "[{").Replace("}]}}", "}]}").Replace("\":\"triggerEvent", "\",\"triggerEvent");
            i++;
        }
        RootObject root = new RootObject();

        try
        {
            root = JsonConvert.DeserializeObject<RootObject>(textFile);
        }
        catch
        {
            Debug.Log("Didn't found any interactions");
        }
        if (root != null)
        {
            if (root.interactions != null)
                interactions = root.interactions;
            if (root.homeArtboard != null)
                homeArtboard = root.homeArtboard;

        }
    }
    Texture2D CreateGradientTexture(int width, int height, List<Stop> stops, Vector2 gradientVectorBegining, Vector2 gradientVectorEnd, string gradientUid)
    {
        if (stops == null || stops.Count == 0)
        {
            Debug.LogError("No colors assigned");
            return null;
        }

        int length = stops.Count;
        if (length > 8)
        {
            Debug.LogWarning("Too many colors! maximum is 8, assigned: " + length);
            length = 8;
        }
        var colorKeys = new GradientColorKey[length];
        var alphaKeys = new GradientAlphaKey[length];
        for (int i = 0; i < length; i++)
        {
            float step = stops[i].offset;
            colorKeys[i].color = ParseRGBColor(stops[i].color);
            colorKeys[i].time = step;
            alphaKeys[i].alpha = ParseRGBColor(stops[i].color).a;
            alphaKeys[i].time = step;
        }
        Gradient gradient = new Gradient();
        gradient.SetKeys(colorKeys, alphaKeys);

        Texture2D outputTexture = new Texture2D(width, height, TextureFormat.ARGB32, false, false)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Point
        };

        Texture2D fillMap = new Texture2D(width, height, TextureFormat.ARGB32, false, false)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Point
        };


        gradientVectorBegining.x *= width;
        gradientVectorEnd.x *= width;
        gradientVectorBegining.y = height - gradientVectorBegining.y * height;
        gradientVectorEnd.y = height - gradientVectorEnd.y * height;


        float gradientVectorLength = Vector2.Distance(gradientVectorBegining, gradientVectorEnd);
        float angle = (Mathf.PI - Mathf.Atan((gradientVectorBegining.y - gradientVectorEnd.y) / (gradientVectorBegining.x - gradientVectorEnd.x)));
        Vector2 delta = new Vector2(Mathf.Cos(angle), -1 * Mathf.Sin(angle));
        for (int s = -(int)gradientVectorLength; s < gradientVectorLength; s++)
        {
            Vector2 point = gradientVectorBegining + s * delta;
            float distanceFromBegining = Vector2.Distance(gradientVectorBegining, point);
            Color color;
            if (distanceFromBegining > gradientVectorLength)
            {
                color = gradient.colorKeys[gradient.colorKeys.Length - 1].color;
            }
            else if (distanceFromBegining < 0)
            {
                color = gradient.colorKeys[0].color;
            }
            else
            {
                color = gradient.Evaluate(distanceFromBegining / gradientVectorLength);
            }
            for (int t = -2 * (int)gradientVectorLength; t < 2 * gradientVectorLength; t++)
            {
                float beta = Mathf.PI / 2 - angle;
                Vector2 newDetlta = new Vector2(Mathf.Cos(beta), Mathf.Sin(beta));
                Vector2 point2 = point + t * newDetlta;
                if ((point2.x < width && point2.x > 0) && (point2.y < height && point2.y > 0))
                {
                    outputTexture.SetPixel(Mathf.RoundToInt(point2.x), Mathf.RoundToInt(point2.y), color);
                    fillMap.SetPixel(Mathf.RoundToInt(point2.x), Mathf.RoundToInt(point2.y), Color.red);
                }
            }
        }
        FillEmptyPixels(width, height, outputTexture, fillMap);
        outputTexture.Apply(false);
        System.IO.File.WriteAllBytes("Assets/Resources/[AdobeXd]-" + filename + "/Gradients/" + gradientUid + ".png", outputTexture.EncodeToPNG());
        return outputTexture;
    }

    private static void FillEmptyPixels(int width, int height, Texture2D outputTexture, Texture2D fillMap)
    {
        int unsuccsessful = 0;
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (fillMap.GetPixel(x, y) != Color.red)
                {
                    List<float> red = new List<float>();
                    List<float> green = new List<float>();
                    List<float> blue = new List<float>();
                    List<float> alpha = new List<float>();
                    for (int x1 = x - 1; x1 < x + 2; x1++)
                    {
                        for (int y1 = y - 1; y1 < y + 2; y1++)
                        {
                            if (fillMap.GetPixel(x1, y1) == Color.red)
                            {
                                Color readColor = outputTexture.GetPixel(x1, y1);
                                red.Add(readColor.r);
                                green.Add(readColor.g);
                                blue.Add(readColor.b);
                                alpha.Add(readColor.a);
                            }
                        }
                    }
                    if (red.Count > 0 && green.Count > 0 && blue.Count > 0 && alpha.Count > 0)
                    {
                        Color averageColor = new Color(red.Average(), green.Average(), blue.Average(), alpha.Average());
                        outputTexture.SetPixel(x, y, averageColor);
                        fillMap.SetPixel(x, y, Color.red);
                    }
                    else unsuccsessful++;
                }
            }
        }
        if (unsuccsessful > 0) FillEmptyPixels(width, height, outputTexture, fillMap);
    }

    void Anchoring(RectTransform rect)
    {
        rect.localScale = Vector3.one;
        rect.anchoredPosition = Vector3.zero;
        rect.anchorMin = new Vector2(0, 1);
        rect.anchorMax = new Vector2(0, 1);
        rect.pivot = new Vector2(0, 1);
    }
    void DoBackground(Children child, GameObject obj)
    {
        RectTransform objRect = obj.GetComponent<RectTransform>();
        Image objImg = obj.GetComponent<Image>();
        if (child.style != null && child.style.fill != null)
        {
            switch (child.style.fill.type)
            {
                case "none":
                    objImg.color = Vector4.zero;
                    break;
                case "solid":
                    objImg.color = ParseRGBColor(child.style.fill.color);
                    break;
                case "pattern":
                    // mask
                    GameObject masking = new GameObject("mask");
                    RectTransform maskRect = masking.AddComponent<RectTransform>();
                    masking.AddComponent<Image>();
                    masking.AddComponent<Mask>();
                    masking.transform.SetParent(obj.transform);
                    Anchoring(maskRect);
                    maskRect.sizeDelta = objRect.sizeDelta;
                    // image
                    GameObject background = new GameObject("imageBackground");
                    RectTransform backRect = background.AddComponent<RectTransform>();
                    Image backgroundImage = background.AddComponent<Image>();
                    ExperienceImporterPostLoad postLoad = background.AddComponent<ExperienceImporterPostLoad>();
                    string imagePath = child.style.fill.pattern.meta.ux.uid;
                    background.transform.SetParent(masking.transform);
                    Anchoring(backRect);
                    backRect.anchoredPosition = Vector2.zero;
                    backgroundImage.preserveAspect = true;
                    int index = parsedManifest.images.FindIndex(i => i.path == imagePath);
                    postLoad.ImagePath = "Assets/Resources/[AdobeXd]-" + filename + "/Images/" + imagePath + ParseExtension(parsedManifest.images[index].type);
                    float imageAspect = child.style.fill.pattern.width / child.style.fill.pattern.height;
                    if (imageAspect > 1)
                    {
                        backRect.sizeDelta = new Vector2(objRect.sizeDelta.y * imageAspect, objRect.sizeDelta.y);
                        if (objRect.sizeDelta.x > backRect.sizeDelta.x)
                        {
                            float multiplicator = objRect.sizeDelta.x / backRect.sizeDelta.x;
                            backRect.sizeDelta *= multiplicator;
                        }
                    }
                    else
                    {
                        backRect.sizeDelta = new Vector2(objRect.sizeDelta.x, objRect.sizeDelta.x / imageAspect);
                        if (objRect.sizeDelta.y > backRect.sizeDelta.y)
                        {
                            float multiplicator = objRect.sizeDelta.y / backRect.sizeDelta.y;
                            backRect.sizeDelta *= multiplicator;
                        }
                    }
                    backRect.anchoredPosition = new Vector3(-(backRect.sizeDelta.x - objRect.sizeDelta.x) / 2, (backRect.sizeDelta.y - objRect.sizeDelta.y) / 2, 0);
                    //Debug.Log("Image: " + postLoad.ImagePath + "aspect: " + imageAspect + "sizeobject: " + objRect.sizeDelta + " sizeimage: " + backRect.sizeDelta);
                    if (child.style.fill.pattern.meta.ux.offsetX != 0) backRect.anchoredPosition = new Vector3(backRect.anchoredPosition.x + backRect.sizeDelta.x * child.style.fill.pattern.meta.ux.offsetX, backRect.anchoredPosition.y, 0);
                    if (child.style.fill.pattern.meta.ux.offsetY != 0) backRect.anchoredPosition = new Vector3(backRect.anchoredPosition.x, backRect.anchoredPosition.y - child.style.fill.pattern.meta.ux.offsetY * backRect.sizeDelta.y, 0);

                    break;
                case "gradient":
                    if (child.style.fill.gradient != null)
                    {
                        string gradientUid = child.style.fill.gradient.@ref;
                        int gradientIndex = gradients.FindIndex(i => i.uid == gradientUid);
                        if (gradients[gradientIndex].type == "linear")
                        {
                            ExperienceImporterPostLoad postLoadGradient = obj.AddComponent<ExperienceImporterPostLoad>();
                            CreateGradientTexture((int)(objRect.sizeDelta.x / 2), (int)(objRect.sizeDelta.y / 2), gradients[gradientIndex].stops, new Vector2(child.style.fill.gradient.x1, child.style.fill.gradient.y1), new Vector2(child.style.fill.gradient.x2, child.style.fill.gradient.y2), gradientUid);
                            postLoadGradient.ImagePath = "Assets/Resources/[AdobeXd]-" + filename + "/Gradients/" + gradientUid + ".png";

                        }
                        else if (gradients[gradientIndex].type == "radial")
                        {
                            Debug.LogWarning("Radial gradients aren't supported yet");
                            objImg.color = ParseRGBColor(gradients[gradientIndex].stops[0].color);
                        }
                    }
                    break;
                default:
                    objImg.color = Color.white;
                    break;
            }
            if (child.style.stroke != null)
            {
                if (child.style.stroke.width > 0)
                {
                    if (child.style.stroke.type != "none")
                    {
                        CreateStroke(obj, child.style.stroke.width, ParseRGBColor(child.style.stroke.color), child.style.stroke.align);
                    }
                }

            }
        }
    }
}