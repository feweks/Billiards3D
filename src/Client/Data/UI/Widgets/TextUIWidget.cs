using System.Globalization;
using System.Numerics;
using System.Xml;
using Game.Client.Managers;
using Raylib_cs;

namespace Game.Client.Data.UI.Widgets;

class TextUIWidget : UIWidget
{
    public const string NODE_NAME = "text";
    private const string FONTS_PATH = "resources/gfx/fonts/{0}.ttf";
    private const string DEFAULT_FONT = "pixellari";
    private const int DEFAULT_FONT_SIZE = 18;

    public string Text { get => _text; set { _text = value; UpdateSize(); } }
    public int FontSize { get; }

    public Color TextColor;
    public Color OutlineColor;

    private string _text = null!; // shut up the compiler
    private Font fnt;
    private string fntPath;

    public TextUIWidget(Vector2 pos, string text, string font, int fontSize, bool interactable, Color textCol, Color outlineCol) : base(pos, Vector2.Zero, interactable, MouseCursor.PointingHand)
    {
        fntPath = font;
        FontSize = fontSize;
        TextColor = textCol;
        OutlineColor = outlineCol;
        InitGraphics();
        string translation = TranslationManager.Get(text);
        Text = translation != text ? translation : text;
    }

    public TextUIWidget(XmlNode textNodeWidget, UIWidget? parentWidget) : base(textNodeWidget, Utils.TryParseXmlAttrib(textNodeWidget.Attributes?["interactable"], false), parentWidget, MouseCursor.PointingHand)
    {
        fntPath = string.Format(FONTS_PATH, Utils.TryParseXmlAttrib(textNodeWidget.Attributes?["font"], DEFAULT_FONT));
        FontSize = Utils.TryParseXmlAttrib(textNodeWidget.Attributes?["size"], DEFAULT_FONT_SIZE);
        string textCol = Utils.TryParseXmlAttrib(textNodeWidget.Attributes?["color"], "white");
        TextColor = Utils.ColorHexFromString(textCol);
        string outlineCol = Utils.TryParseXmlAttrib(textNodeWidget.Attributes?["outline"], "black");
        OutlineColor = Utils.ColorHexFromString(outlineCol);
        InitGraphics();

        string text = Utils.TryParseXmlAttrib(textNodeWidget.Attributes?["text"], string.Empty);
        string translation = TranslationManager.Get(text);
        Text = translation != text ? translation : text;
    }

    private void InitGraphics()
    {
        fnt = ResourcesManager.GetFont(fntPath);
    }

    private void UpdateSize() => Size = Raylib.MeasureTextEx(fnt, _text, FontSize, 1);

    public override void Draw()
    {
        base.Draw();

        if (!Visible)
            return;

        var outlineCol = OutlineColor;
        outlineCol.A = TextColor.A;
        Utils.DrawTextOutlinedEx(fnt, Text, Position, Origin, FontSize, Rotation, Tint, outlineCol);
    }
}
