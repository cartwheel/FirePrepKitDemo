using System.Collections.Generic;
using UnityEngine;

public class ExperienceImporterFileClasses : MonoBehaviour
{
    public class RootObject
    {
        public List<Children> children;
        public GradientResources resources;
        public string homeArtboard;
        public List<Interaction> interactions;
      //  public UxStyle style;
    }
    public class GradientResources
    {
        public List<UxGradient> gradients;
    }
    public class GradientChild
    {
        public List<UxGradient> gradient;
    }
    public class UxGradient
    {
        public string uid, type;
        public List<Stop> stops;
    }
    public class Stop
    {
        public float offset;
        public UxColor color;
    }
    public class Children
    {
        public string id, name, path, type;
        public UxDesignBounds Uxdesignbounds;
        public List<Children> children;
        public List<Images> components;
        //used in artboards 
        public ArtBoard artBoard;
        public UxTransform transform;
        public UxStyle style;
        public UxShape shape;
        public UxGroup group;
        public UxText text;
        public string visible;
    }
    public class UxDesignBounds
    {
        public float x, width, y, height;
    }
    public class ArtBoard
    {
        public string id, name, path;
        public UxDesignBounds Uxdesignbounds;
        public List<Children> children;
        //public UxStyle style;
    }
    public class Images
    {
        public string id, name, path, type;
    }
    public class Manifest
    {
        public List<ArtBoard> artBoards;
        public List<Images> images;
    }


    //used in artboards 
    public class UxTransform
    {
        public float a, b, c, d;
        public float tx, ty;
    }
    public class UxShape
    {
        public string type;
        public float x, y, width, height;
    }
    public class UxGroup
    {
        public List<Children> children;
    }
    // UxStyle with subclasses;
    public class UxStyle
    {
        public UxStroke stroke;
        public UxFill fill;
        public UxFont font;
        public UxTextAttributes textAttributes;
        public string opacity;
    }

    // UxFill with subclasses;
    public class UxFill
    {
        public string type;
        public UxPattern pattern;
        public UxColor color;
        public UxFillGradient gradient;
    }
    public class UxPattern
    {
        public float width, height;
        public UxMeta meta;
    }
    public class UxMeta
    {
        public UxUx ux;
    }
    public class UxUx
    {
        public string scaleBehavior;
        public string uid;
        public float offsetX, offsetY;
    }
    // UxStroke with subclasses
    public class UxStroke
    {
        public string type, align;
        public float width;
        public UxColor color;
    }
    public class UxColor
    {
        public string mode;
        public UxColorValue value;
        public string alpha;
    }
    public class UxColorValue {
        public float r, g, b;
    }
    public class UxFont
    {
        public float size;
        public string postscriptName, family, style;
    }
    public class UxTextAttributes
    {
        public float lineHeight;
        public string paragraphAlign;
    }
    public class UxText
    {
        public UxFrame frame;
        public string rawText;
    }
    public class UxFrame
    {
        public string type;
        public float width, height;
    }
    public class UxFillGradient
    {
        public float x1, y1, x2, y2;
        public string @ref;
    }
    public class Interaction
    {
        public string uid, triggerEvent, action;
        public Properties properties;
    }
    public class Properties
    {
        public string transition, easing, destination;
        public float duration;
        public bool preserveScrollPosition;
    }
}
