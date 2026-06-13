using System.Numerics;
using Game.Client.Managers;
using Raylib_cs;

namespace Game.Client.Data.UI.Widgets;

class ButtonUIWidget : UIWidget
{
    private const int N_PATCH_OFFSET = 24;

    private Texture2D tex;
    private Font fnt;
    private float fntScaleFactor;
    private NPatchInfo nPatchData;
    private string text;
    private Color textTint = Color.White;

    public ButtonUIWidget(string text, Vector2 pos, Vector2 size, float scaleFactor = 0.5f) : base(pos, size)
    {
        tex = ResourcesManager.GetTexture("resources/gfx/ui/ui_base.png");
        fnt = ResourcesManager.GetFont("resources/gfx/fonts/pixellari.ttf");
        nPatchData = new NPatchInfo()
        {
            Left = N_PATCH_OFFSET,
            Top = N_PATCH_OFFSET,
            Right = N_PATCH_OFFSET,
            Bottom = N_PATCH_OFFSET,
            Source = new Rectangle(0, 0, tex.Width, tex.Height),
            Layout = NPatchLayout.NinePatch
        };
        fntScaleFactor = scaleFactor;
        this.text = text;
    }

    public override void Update(float dt)
    {
        base.Update(dt);

        Clicked = false;
        Hovered = false;

        var collider = new Rectangle(Position, Size);

        textTint = NormalColor;
        Tint = NormalColor;

        if (Raylib.CheckCollisionPointRec(Utils.GetMousePos(), collider))
        {
            textTint = HoverColor;
            Tint = HoverColor;
            Hovered = true;

            if (Raylib.IsMouseButtonDown(MouseButton.Left))
            {
                Tint = PressColor;
            }

            if (Raylib.IsMouseButtonReleased(MouseButton.Left))
            {
                Clicked = true;
            }
        }
    }

    public override void Draw()
    {
        base.Draw();

        var dest = new Rectangle(Position, Size);
        Raylib.DrawTextureNPatch(tex, nPatchData, dest, Vector2.Zero, Rotation, Tint);

        float fontSize = Size.Y * fntScaleFactor;
        Vector2 textSize = Raylib.MeasureTextEx(fnt, text, fontSize, 1);
        Vector2 fontPos = Position + (Size / 2) - (textSize / 2);

        //Raylib.DrawTextPro(fnt, text, fontPos, Vector2.Zero, Rotation, fontSize, 1, textTint);
        Utils.DrawTextOutlinedEx(fnt, text, fontPos, Vector2.Zero, fontSize, Rotation, textTint, Color.Black);
    }
}
