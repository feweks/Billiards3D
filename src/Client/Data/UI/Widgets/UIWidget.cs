using Game.Client.Data.UI.Containers;
using Raylib_cs;
using System.Numerics;
using System.Xml;

namespace Game.Client.Data.UI.Widgets;

class UIWidget : UIElement
{
    public readonly Color NormalColor = Color.White;
    public readonly Color HoverColor = new Color(200, 200, 200, 255);
    public readonly Color PressColor = Color.Gray;
    public readonly string TextFontPath = "resources/gfx/fonts/pixellari.ttf";

    public Vector2 RelativePosition;
    public Vector2 AbsolutePosition { get => Position + RelativePosition; }
    public bool Clicked { get; internal set; }
    public bool Hovered { get; internal set; }

    public UIWidget(Vector2 pos, Vector2 size, string name) : base(pos, size, name) { }

    public UIWidget(XmlNode widgetNode, UIContainer parentContainer) : base(widgetNode, parentContainer.Size / 2f) { }
}
