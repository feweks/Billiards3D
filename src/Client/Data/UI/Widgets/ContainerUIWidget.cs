using System.Numerics;
using System.Xml;
using Raylib_cs;

namespace Game.Client.Data.UI.Widgets;

class ContainerUIWidget : UIWidget
{
    public const string NODE_NAME = "container";
    public const string CONTAINER_UI_TYPE_NONE = "none";
    public const string CONTAINER_UI_TYPE_DEBUG = "debug";
    public const string CONTAINER_UI_TYPE_BOX = "box";

    public string Type { get; }

    private DynamicBoxUIWidget? box;

    public ContainerUIWidget(Vector2 position, Vector2 size, string type) : base(position, size, false)
    {
        Type = type;
        InitGraphics();
    }

    public ContainerUIWidget(XmlNode containerWidgetNode, UIWidget? parentWidget) : base(containerWidgetNode, false, parentWidget, MouseCursor.Default)
    {
        Type = Utils.TryParseXmlAttrib(containerWidgetNode.Attributes?["type"], CONTAINER_UI_TYPE_NONE);
        InitGraphics();
    }

    private void InitGraphics()
    {
        switch (Type)
        {
            case CONTAINER_UI_TYPE_BOX:
                box = new DynamicBoxUIWidget(Size / 2, Size, false);
                AddChildWidget(box);
                break;
        }
    }

    public override void Update(float dt)
    {
        base.Update(dt);
    }

    public override void Draw()
    {
        base.Draw();

        if (Type == CONTAINER_UI_TYPE_DEBUG)
            Raylib.DrawRectangleLinesEx(GetInteractableArea(), 4f, Color.DarkBlue);
    }
}
