using System.Numerics;
using System.Xml;
using Game.Client.Managers;
using Raylib_cs;

namespace Game.Client.Data.UI.Widgets;

class IconBoxUIWidget : UIWidget
{
    public const string NODE_NAME = "icon";
    private const int ICON_SIZE = 32;

    public int Icon { get; set; }
    public float ScaleFactor { get; set; }

    private DynamicBoxUIWidget boxWidget = null!;
    private Texture2D iconsTex;
    private List<Rectangle> iconsData = null!;

    public IconBoxUIWidget(Vector2 pos, float size, int icon, float scaleFactor) : base(pos, new Vector2(size), true, MouseCursor.PointingHand)
    {
        Icon = icon;
        ScaleFactor = scaleFactor;

        InitGraphics();
    }

    public IconBoxUIWidget(XmlNode iconBoxWidgetNode, UIWidget? parentWidget) : base(iconBoxWidgetNode, true, parentWidget, MouseCursor.PointingHand)
    {
        Icon = Utils.TryParseXmlAttrib(iconBoxWidgetNode.Attributes?["type"], 0);
        ScaleFactor = Utils.TryParseXmlAttrib(iconBoxWidgetNode.Attributes?["iconscale"], 0.45f);
        float size = Utils.TryParseXmlAttrib(iconBoxWidgetNode.Attributes?["size"], 100);
        Size.X = Size.Y = size;

        InitGraphics();
    }

    private void InitGraphics()
    {
        iconsTex = ResourcesManager.GetTexture("resources/gfx/ui/ui_icons.png");
        iconsData = new List<Rectangle>();

        if (iconsTex.Width >= ICON_SIZE && iconsTex.Height >= ICON_SIZE)
        {
            float xOffset = 0;
            float yOffset = 0;
            bool loadedAll = false;

            while (!loadedAll)
            {
                var src = new Rectangle(xOffset, yOffset, ICON_SIZE, ICON_SIZE);
                iconsData.Add(src);

                xOffset += ICON_SIZE;
                if (xOffset >= iconsTex.Width)
                {
                    xOffset = 0;
                    yOffset += ICON_SIZE;
                    if (yOffset >= iconsTex.Height)
                        loadedAll = true;
                }
            }
        }
        else
        {
            Raylib.TraceLog(TraceLogLevel.Warning, $"Failed to load ui icons: invalid texture dimensions");
        }

        boxWidget = new DynamicBoxUIWidget(Size / 2, Size, true);
        AddChildWidget(boxWidget, true);
    }

    public override void Draw()
    {
        base.Draw();

        if (Icon >= 0 && Icon < iconsData.Count)
        {
            var dest = new Rectangle(Position, Size * 0.66f);
            Raylib.DrawTexturePro(iconsTex, iconsData[Icon], dest, dest.Size / 2, Rotation, Tint);
        }
    }
}
