using System.Numerics;
using System.Xml;
using Raylib_cs;

namespace Game.Client.Data.UI.Widgets.Input;

abstract class InputUIWidget : UIWidget
{
    public const string NODE_NAME = "input";
    public const string INPUT_UI_FONT = "resources/gfx/fonts/pixellari.ttf";

    public TextUIWidget TextWidget { get; }
    public float TextScale { get; }

    public InputUIWidget(Vector2 pos, Vector2 size, Vector2 textPos, float textScale, string text, MouseCursor cursor) : base(pos, size, true, cursor)
    {
        TextScale = textScale;
        TextWidget = new TextUIWidget(textPos, text, INPUT_UI_FONT, (int)Math.Floor(Size.Y * textScale), true, Color.White, Color.Black);
        AddChildWidget(TextWidget, true);
    }

    public InputUIWidget(XmlNode inputWidgetNode, Vector2 textPos, UIWidget? parentWidget, MouseCursor cursor) : base(inputWidgetNode, true, parentWidget, cursor)
    {
        TextScale = Utils.TryParseXmlAttrib(inputWidgetNode.Attributes?["textscale"], 0.6f);
        string text = Utils.TryParseXmlAttrib(inputWidgetNode.Attributes?["text"], string.Empty);
        TextWidget = new TextUIWidget(textPos, text, INPUT_UI_FONT, (int)Math.Floor(Size.Y * TextScale), true, Color.White, Color.Black);
        AddChildWidget(TextWidget, true);
    }
}
