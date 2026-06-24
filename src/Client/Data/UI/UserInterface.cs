using System.Xml;
using Game.Client.Data.UI.Widgets;
using Game.Client.Managers;
using Game.Client.States;
using Raylib_cs;

namespace Game.Client.Data.UI;

class UserInterface
{
    public List<UIWidget> Widgets { get; }
    public bool DrawDebug { get; set; } = false;

    public UIWidget? HoveredWidget { get; internal set; }
    public UIWidget? PressedWidget { get; internal set; }
    public UIWidget? ClickedWidget { get; internal set; }

    public UserInterface(GameState state)
    {
        Widgets = new List<UIWidget>();

        var xmlDoc = ResourcesManager.GetXml($"resources/data/ui/{state.Name}.xml");
        if (xmlDoc == null)
        {
            Raylib.TraceLog(TraceLogLevel.Warning, $"Failed to load ui data for state {state.Name}");
            return;
        }

        ParseUIDocument(xmlDoc, state.Name);
    }

    public void Update(float dt)
    {
        HoveredWidget = null;
        PressedWidget = null;
        ClickedWidget = null;

        foreach (var widget in Widgets)
        {
            widget.Update(dt);

            if (widget.Hovered)
                HoveredWidget = widget.HoveredChild ?? widget;

            if (widget.Pressed)
                PressedWidget = widget.PressedChild ?? widget;

            if (widget.Clicked)
                ClickedWidget = widget.ClickedChild ?? widget;
        }

        Raylib.SetMouseCursor(HoveredWidget == null ? MouseCursor.Default : HoveredWidget.HoverCursor);
    }

    public void Draw()
    {
        foreach (var widget in Widgets)
        {
            widget.Draw();

            if (DrawDebug)
                Raylib.DrawRectangleRec(widget.GetInteractableArea(), Raylib.ColorAlpha(Color.Blue, 0.7f));
        }
    }

    private void ParseUIDocument(XmlDocument xmlDoc, string stateName)
    {
        var rootNode = xmlDoc.SelectSingleNode("ui");
        if (rootNode == null)
        {
            Raylib.TraceLog(TraceLogLevel.Warning, $"Failed to load ui data for state {stateName}: root ui node is missing");
            return;
        }

        var containerNodes = rootNode.SelectNodes("container");
    }
}
