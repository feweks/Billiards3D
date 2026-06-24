using System.Numerics;
using Raylib_cs;

namespace Game.Client.Data.UI.Widgets;

class ButtonUIWidget : UIWidget
{
    private const string BUTTON_WIDGET_FONT_PATH = "resources/gfx/fonts/pixellari.ttf";

    public DynamicBoxUIWidget Box { get; internal set; } = null!; // shut up the compiler
    public TextUIWidget Text { get; internal set; } = null!;
    public float TextScale { get; }

    private string text;

    public ButtonUIWidget(Vector2 pos, Vector2 size, string text, float textScale = 0.6f) : base(pos, size, true, MouseCursor.PointingHand)
    {
        TextScale = textScale;
        this.text = text;
        InitGraphics();
    }

    private void InitGraphics()
    {
        Box = new DynamicBoxUIWidget(Size / 2, Size, true);
        AddChildWidget(Box);
        Text = new TextUIWidget(Box.Size / 2, text, BUTTON_WIDGET_FONT_PATH, (int)(Size.Y * TextScale), false, Color.White, Color.Black);
        Box.AddChildWidget(Text);
    }

    public override void Update(float dt)
    {
        base.Update(dt);

        Text.Tint = Box.Tint;
    }
}
