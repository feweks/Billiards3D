using System.Numerics;
using System.Xml;
using Game.Client.Data.UI.Containers;
using Game.Client.Data.UI.Widgets;
using Game.Client.Managers;
using Raylib_cs;

namespace Game.Client.Data.UI;

abstract class InputUIWidget : UIWidget
{
    public const string INPUT_WIDGET_TYPE_BUTTON = "button";

    public MouseCursor HoveredCursorType { get; }
    public string Text { get; set; }

    public InputUIWidget(Vector2 pos, Vector2 size, string name, string text, MouseCursor cursorType) : base(pos, size, name)
    {
        Text = TranslationManager.Get(text);
        HoveredCursorType = cursorType;
    }

    public InputUIWidget(XmlNode inputWidgetNode, UIContainer parentContainer, MouseCursor cursorType) : base(inputWidgetNode, parentContainer)
    {
        Text = TranslationManager.Get(Utils.TryParseXmlAttrib(inputWidgetNode.Attributes?["text"], string.Empty));
        HoveredCursorType = cursorType;
    }
}
