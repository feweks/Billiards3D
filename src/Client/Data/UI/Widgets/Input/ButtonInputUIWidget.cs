using System.Drawing;
using System.Numerics;
using System.Xml;
using Raylib_cs;

namespace Game.Client.Data.UI.Widgets.Input;

class ButtonInputUIWidget : InputUIWidget
{
    public const string INPUT_NODE_TYPE = "button";

    public DynamicBoxUIWidget BoxWidget { get; internal set; } = null!; // shut up the compiler

    public ButtonInputUIWidget(Vector2 pos, Vector2 size, float textScale, string text) : base(pos, size, size / 2, textScale, text, MouseCursor.PointingHand)
    {
        InitGraphics();
    }

    public ButtonInputUIWidget(XmlNode buttonWidgetNode, UIWidget? parentWidget) : base(buttonWidgetNode, Vector2.Zero, parentWidget, MouseCursor.PointingHand)
    {
        TextWidget.RelativePosition = Size / 2;
        InitGraphics();
    }

    private void InitGraphics()
    {
        BoxWidget = new DynamicBoxUIWidget(Size / 2, Size, true);
        AddChildWidget(BoxWidget, true);
    }

    public override void Update(float dt)
    {
        base.Update(dt);

        TextWidget.Tint = BoxWidget.Tint;
    }
}
