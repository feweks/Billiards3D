using System.Numerics;
using System.Xml;
using Game.Client.Data.UI.Widgets;
using Raylib_cs;

namespace Game.Client.Data.UI.Containers;

class UIContainer : UIElement
{
    public const string CONTAINER_TYPE_NONE = "none";
    public const string CONTAINER_TYPE_BOX = "box";

    public List<UIWidget> Widgets { get; }
    public UIWidget? HoveredWidget { get; internal set; }
    public UIWidget? ClickedWidget { get; internal set; }

    public UIContainer(Vector2 pos, Vector2 size, string name) : base(pos, size, name)
    {
        Widgets = new List<UIWidget>();
    }

    public UIContainer(XmlNode containerNode) : base(containerNode, new Vector2(Program.Instance!.Config.RenderWidth / 2, Program.Instance!.Config.RenderHeight / 2))
    {
        Widgets = new List<UIWidget>();
    }

    public void AddWidget(UIWidget widget)
    {
        widget.RelativePosition = Position;
        Widgets.Add(widget);
    }

    public UIWidget? GetWidgetByName(string name) => Widgets.FirstOrDefault(w => w.Name == name);

    public override void Update(float dt)
    {
        if (!Active)
            return;

        HoveredWidget = null;
        ClickedWidget = null;
        foreach (var widget in Widgets)
        {
            widget.Update(dt);

            if (widget.Hovered)
                HoveredWidget = widget;

            if (widget.Clicked)
                ClickedWidget = widget;
        }
    }

    public override void Draw()
    {
        if (!Visible)
            return;

        foreach (var widget in Widgets)
        {
            widget.Draw();
        }
    }
}
