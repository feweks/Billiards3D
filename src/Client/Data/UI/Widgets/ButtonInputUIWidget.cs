using System.Numerics;
using System.Xml;
using Game.Client.Data.UI.Containers;
using Game.Client.Managers;
using Raylib_cs;

namespace Game.Client.Data.UI.Widgets;

class ButtonInputUIWidget : InputUIWidget
{
    public float ScaleFactor { get; }

    private Font fnt;
    private UIDynamicBox box;
    private Color textTint = Color.White;

    public ButtonInputUIWidget(Vector2 pos, Vector2 size, string name, string text, float scaleFactor = 0.5f) : base(pos, size, name, text, MouseCursor.PointingHand)
    {
        box = new UIDynamicBox(pos, size);
        fnt = ResourcesManager.GetFont(TextFontPath);
        ScaleFactor = scaleFactor;
    }

    public ButtonInputUIWidget(XmlNode buttonInputWidgetNode, UIContainer parentContainer) : base(buttonInputWidgetNode, parentContainer, MouseCursor.PointingHand)
    {
        box = new UIDynamicBox(Position, Size);
        fnt = ResourcesManager.GetFont(TextFontPath);
        ScaleFactor = Utils.TryParseXmlAttrib(buttonInputWidgetNode.Attributes?["textscale"], 0.75f);
    }

    public override void Update(float dt)
    {
        base.Update(dt);

        box.Update();
        box.Position = AbsolutePosition;

        Clicked = false;
        Hovered = false;

        if (!Active)
            return;

        if (!Useable)
        {
            Tint = Raylib.ColorAlpha(Color.Gray, 0.95f);
            textTint = Tint;
            box.Tint = Tint;
            return;
        }

        var collider = box.Collider;

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

        box.Tint = Tint;
    }

    public override void Draw()
    {
        base.Draw();

        box.Draw();
        int fontSize = (int)Math.Floor(Size.Y * ScaleFactor);
        Vector2 textSize = Raylib.MeasureTextEx(fnt, Text, fontSize, 1);
        var fontPos = AbsolutePosition + new Vector2(0, textSize.Y * ((1 - ScaleFactor) * 0.5f));

        Vector2 textOrigin = textSize * 0.5f;
        Utils.DrawTextOutlinedEx(fnt, Text, fontPos, textOrigin, fontSize, Rotation, textTint, Color.Black);
    }
}
