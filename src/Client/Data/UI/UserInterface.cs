using System.Globalization;
using System.Numerics;
using System.Xml;
using Game.Client.Data.UI.Containers;
using Game.Client.Data.UI.Widgets;
using Game.Client.Managers;
using Game.Client.States;
using Raylib_cs;

namespace Game.Client.Data.UI;

class UserInterface
{
    public List<UIContainer> Containers { get; }

    public UIWidget? ClickedWidget { get; internal set; }

    public UserInterface(GameState state)
    {
        Containers = new List<UIContainer>();

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
        MouseCursor cursorType = MouseCursor.Default;
        ClickedWidget = null;
        foreach (var container in Containers)
        {
            container.Update(dt);

            if (container.Active)
            {
                if (container.HoveredWidget != null && container.HoveredWidget is InputUIWidget inputWidget)
                    cursorType = inputWidget.HoveredCursorType;

                if (container.ClickedWidget != null)
                    ClickedWidget = container.ClickedWidget;
            }
        }

        Raylib.SetMouseCursor(cursorType);
    }

    public UIContainer? GetContainerByName(string name) => Containers.FirstOrDefault(c => c.Name == name);

    public void Draw()
    {
        foreach (var container in Containers)
            container.Draw();
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
        if (containerNodes != null)
        {
            foreach (XmlNode containerNode in containerNodes)
            {
                var container = ParseUIContainerNode(containerNode);

                if (container != null)
                {
                    Containers.Add(container);

                    var inputWidgetsNodes = containerNode.SelectNodes("input");

                    if (inputWidgetsNodes != null)
                    {
                        foreach (XmlNode inputWidgetNode in inputWidgetsNodes)
                        {
                            var inputWidget = ParseUIInputWidget(inputWidgetNode, container);

                            if (inputWidget != null)
                                container.AddWidget(inputWidget);
                        }
                    }
                }
            }
        }
    }

    private static UIContainer? ParseUIContainerNode(XmlNode containerNode)
    {
        string containerType = Utils.TryParseXmlAttrib(containerNode.Attributes?["type"], UIContainer.CONTAINER_TYPE_NONE);

        UIContainer? container = null;
        switch (containerType)
        {
            case UIContainer.CONTAINER_TYPE_NONE:
                container = new UIContainer(containerNode);
                break;
            case UIContainer.CONTAINER_TYPE_BOX:
                container = new BoxUIContainer(containerNode);
                break;
        }

        return container;
    }

    private static InputUIWidget? ParseUIInputWidget(XmlNode inputWidgetNode, UIContainer container)
    {
        string inputWidgetType = Utils.TryParseXmlAttrib(inputWidgetNode.Attributes?["type"], InputUIWidget.INPUT_WIDGET_TYPE_BUTTON);

        InputUIWidget? inputWidget = null;
        switch (inputWidgetType)
        {
            case InputUIWidget.INPUT_WIDGET_TYPE_BUTTON:
                inputWidget = new ButtonInputUIWidget(inputWidgetNode, container);
                break;
        }

        return inputWidget;
    }
}
