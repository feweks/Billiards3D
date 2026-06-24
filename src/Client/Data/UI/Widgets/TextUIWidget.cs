using System.Numerics;
using Game.Client.Managers;
using Raylib_cs;

namespace Game.Client.Data.UI.Widgets;

class TextUIWidget : UIWidget
{
    public string Text { get => text; set { text = value; UpdateSize(); } }
    public int FontSize { get; }

    public Color TextColor;
    public Color OutlineColor;

    private string text = null!; // shut up the compiler
    private Font fnt;
    private string fntPath;

    public TextUIWidget(Vector2 pos, string text, string font, int fontSize, bool interactable, Color textCol, Color outlineCol) : base(pos, Vector2.Zero, interactable, MouseCursor.PointingHand)
    {
        fntPath = font;
        FontSize = fontSize;
        TextColor = textCol;
        OutlineColor = outlineCol;
        InitGraphics();
        Text = text;
    }

    private void InitGraphics()
    {
        fnt = ResourcesManager.GetFont(fntPath);
    }

    private void UpdateSize() => Size = Raylib.MeasureTextEx(fnt, text, FontSize, 1);

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
