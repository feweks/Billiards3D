using Raylib_cs;
using System.Numerics;

namespace Game.Client.Data.UI.Widgets;

class UIWidget
{
    public static readonly Color HoveredColor = Raylib.GetColor(0xcfcfcfff);
    public static readonly Color PressedColor = Raylib.GetColor(0x9e9e9eff);
    public static readonly Color InactiveColor = Raylib.GetColor(0xcfcfcfaf);

    public Vector2 Position { get => ParentWidget != null ? ParentWidget.Position - ParentWidget.Origin + RelativePosition : RelativePosition; }
    public Vector2 RelativePosition;
    public Vector2 Size;
    public float Rotation { get; set; }
    public Vector2 Origin { get => Size * 0.5f; }
    public UIWidget? ParentWidget { get; set; }
    public List<UIWidget> ChildrenWidgets { get; }
    public bool Visible { get; set; } = true;
    public bool Active { get; set; } = true;
    public bool Interactive { get; }

    public bool Hovered { get; set; } = false;
    public bool Pressed { get; set; } = false;
    public bool Clicked { get; set; } = false;
    public UIWidget? HoveredChild { get; set; }
    public UIWidget? PressedChild { get; set; }
    public UIWidget? ClickedChild { get; set; }
    public MouseCursor HoverCursor { get; }
    public Color Tint = Color.White;

    public UIWidget(Vector2 position, Vector2 size, bool interactive, MouseCursor cursor = MouseCursor.Default)
    {
        RelativePosition = position;
        Size = size;
        Interactive = interactive;
        ChildrenWidgets = new List<UIWidget>();
        HoverCursor = cursor;
    }

    public void AddChildWidget(UIWidget child)
    {
        child.ParentWidget = this;
        ChildrenWidgets.Add(child);
    }

    public void RemoveChildWidget(UIWidget child)
    {
        child.ParentWidget = null;
        ChildrenWidgets.Remove(child);
    }

    public virtual Rectangle GetInteractableArea() => new Rectangle(Position - Origin, Size);

    public virtual void Update(float dt)
    {
        Clicked = false;
        Pressed = false;
        Hovered = false;
        ClickedChild = null;
        PressedChild = null;
        HoveredChild = null;

        if (Active && Interactive && Raylib.CheckCollisionPointRec(Utils.GetMousePos(), GetInteractableArea()))
        {
            Hovered = true;
            Pressed = Raylib.IsMouseButtonDown(MouseButton.Left);
            Clicked = Raylib.IsMouseButtonReleased(MouseButton.Left);
        }

        Tint = Color.White;

        if (!Active)
        {
            Tint = InactiveColor;
        }
        else
        {
            if (Interactive)
            {
                if (Hovered)
                    Tint = HoveredColor;

                if (Pressed)
                    Tint = PressedColor;
            }
        }

        foreach (var child in ChildrenWidgets)
        {
            child.Active = Active;
            child.Visible = Visible;

            if (Active)
            {
                child.Update(dt);

                if (child.Clicked)
                {
                    Clicked = true;
                    ClickedChild = child.ClickedChild ?? child;
                }

                if (child.Hovered)
                {
                    Hovered = true;
                    HoveredChild = child.HoveredChild ?? child;
                }

                if (child.Pressed)
                {
                    Pressed = true;
                    PressedChild = child.PressedChild ?? child;
                }
            }
        }
    }

    public virtual void Draw()
    {
        if (!Visible)
            return;

        foreach (var child in ChildrenWidgets)
            child.Draw();
    }
}
