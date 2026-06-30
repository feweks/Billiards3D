using System.Numerics;
using System.Xml;
using Game.Client.Data.UI.Widgets;
using Game.Client.Data.UI.Widgets.Input;
using Game.Client.Managers;
using Game.Client.States;
using Raylib_cs;

namespace Game.Client.Data.UI;

class UserInterface
{
    public const string UI_PATH = "resources/data/ui/{0}.xml";

    public UIWidget Root { get; }
    public string DescriptorPath { get; }

    public UIWidget? HoveredWidget { get; internal set; }
    public UIWidget? PressedWidget { get; internal set; }
    public UIWidget? ClickedWidget { get; internal set; }

    public UserInterface(GameState state)
    {
        Vector2 renderSize = new Vector2(Program.Instance!.Config.RenderWidth, Program.Instance!.Config.RenderHeight);
        Root = new UIWidget(renderSize / 2, renderSize, false)
        {
            Name = "root"
        };

        DescriptorPath = string.Format(UI_PATH, state.Name);
        var xmlDoc = ResourcesManager.GetXml(DescriptorPath);
        if (xmlDoc == null)
        {
            Raylib.TraceLog(TraceLogLevel.Warning, $"{state.Name} UI: Failed to load ui data from {DescriptorPath}, invalid file data");
            return;
        }

        ParseUIDocument(xmlDoc, Root, state.Name);
    }

    public void Update(float dt)
    {
        HoveredWidget = null;
        PressedWidget = null;
        ClickedWidget = null;

        Root.Update(dt);

        if (Root.Hovered && Root.HoveredChild != null)
            HoveredWidget = Root.HoveredChild;

        if (Root.Pressed && Root.PressedChild != null)
            PressedWidget = Root.PressedChild;

        if (Root.Clicked && Root.ClickedChild != null)
            ClickedWidget = Root.ClickedChild;

        Raylib.SetMouseCursor(HoveredWidget == null ? MouseCursor.Default : HoveredWidget.HoverCursor);
    }

    public void Draw()
    {
        Root.Draw();
    }

    private void ParseUIDocument(XmlDocument xmlDoc, UIWidget rootWidget, string stateName)
    {
        var rootNode = xmlDoc.SelectSingleNode("ui");
        if (rootNode == null)
        {
            Raylib.TraceLog(TraceLogLevel.Warning, $"Failed to load ui data for state {stateName}: root ui node is missing");
            return;
        }

        ParseUINode(rootNode, rootWidget, stateName);
    }

    private void ParseUINode(XmlNode widgetNode, UIWidget parentWidget, string stateName)
    {
        var containerNodes = widgetNode.SelectNodes(ContainerUIWidget.NODE_NAME);
        if (containerNodes != null)
        {
            foreach (XmlNode containerNode in containerNodes)
            {
                var containerWidget = new ContainerUIWidget(containerNode, parentWidget);
                parentWidget.AddChildWidget(containerWidget);

                ParseUINode(containerNode, containerWidget, stateName);
            }
        }

        var textNodes = widgetNode.SelectNodes(TextUIWidget.NODE_NAME);
        if (textNodes != null)
        {
            foreach (XmlNode textNode in textNodes)
            {
                var textWidget = new TextUIWidget(textNode, parentWidget);
                parentWidget.AddChildWidget(textWidget);
            }
        }

        var inputNodes = widgetNode.SelectNodes(InputUIWidget.NODE_NAME);
        if (inputNodes != null)
        {
            foreach (XmlNode inputNode in inputNodes)
            {
                var inputWidget = ParseInputUINode(inputNode, parentWidget, stateName);
                if (inputWidget != null)
                    parentWidget.AddChildWidget(inputWidget);
            }
        }

        var iconNodes = widgetNode.SelectNodes(IconBoxUIWidget.NODE_NAME);
        if (iconNodes != null)
        {
            foreach (XmlNode iconNode in iconNodes)
            {
                var iconWidget = new IconBoxUIWidget(iconNode, parentWidget);
                parentWidget.AddChildWidget(iconWidget);
            }
        }

        var uiNodes = widgetNode.SelectNodes("ui");
        if (uiNodes != null)
        {
            foreach (XmlNode uiNode in uiNodes)
            {
                string uiSource = Utils.TryParseXmlAttrib(uiNode.Attributes?["src"], string.Empty);
                string? descriptorPath = Path.GetDirectoryName(DescriptorPath);

                if (descriptorPath == null)
                {
                    Raylib.TraceLog(TraceLogLevel.Warning, $"{stateName} UI: Failed to parse UI node: invalid descriptor filepath");
                    continue;
                }

                string uiPath = Path.Combine(descriptorPath, uiSource);
                var doc = ResourcesManager.GetXml(uiPath);
                if (doc == null)
                {
                    Raylib.TraceLog(TraceLogLevel.Warning, $"{stateName} UI: Failed to parse UI node: invalid descriptor file");
                    continue;
                }

                ParseUIDocument(doc, parentWidget, stateName);
            }
        }
    }

    private static InputUIWidget? ParseInputUINode(XmlNode inputNode, UIWidget parentWidget, string stateName)
    {
        string type = Utils.TryParseXmlAttrib(inputNode.Attributes?["type"], "none");

        switch (type)
        {
            case ButtonInputUIWidget.INPUT_NODE_TYPE:
                return new ButtonInputUIWidget(inputNode, parentWidget);
            default:
                Raylib.TraceLog(TraceLogLevel.Warning, $"{stateName} UI: Failed to parse input widget of type {type}");
                return null;
        }
    }
}
