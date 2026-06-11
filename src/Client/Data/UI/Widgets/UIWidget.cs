using Raylib_cs;
using System.Numerics;

namespace Game.Client.Data.UI.Widgets;

abstract class UIWidget
{
    public readonly Color NormalColor = Color.White;
    public readonly Color HoverColor = new Color(200, 200, 200, 255);
    public readonly Color PressColor = Color.Gray;

    public Vector2 Position;
    public Vector2 Size;
    public float Rotation { get; set; } = 0f;
    public Color Tint = Color.White;

    public bool Clicked { get; internal set; }
    public bool Hovered { get; internal set; }

    public UIWidget(Vector2 pos, Vector2 size)
    {
        Position = pos;
        Size = size;
    }

    public virtual void Update(float dt) { }

    public virtual void Draw() { }
}
