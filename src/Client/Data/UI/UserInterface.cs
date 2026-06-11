using Game.Client.Data.UI.Widgets;
using Game.Client.States;
using Raylib_cs;

namespace Game.Client.Data.UI;

class UserInterface
{
    public List<UIWidget> Widgets { get; }

    public UserInterface(GameState state)
    {
        Widgets = new List<UIWidget>();
    }

    public void Update(float dt)
    {
        bool anyHovered = false;
        foreach (var widget in Widgets)
        {
            widget.Update(dt);

            if (widget.Hovered)
                anyHovered = true;
        }

        Raylib.SetMouseCursor(anyHovered ? MouseCursor.PointingHand : MouseCursor.Arrow);
    }

    public void Draw()
    {
        foreach (var widget in Widgets)
            widget.Draw();
    }
}
