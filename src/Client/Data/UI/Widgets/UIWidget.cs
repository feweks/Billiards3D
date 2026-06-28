using Game.Client.Data.UI.Widgets.Input;
using Game.Client.Net;
using Raylib_cs;
using System.Numerics;
using System.Xml;

namespace Game.Client.Data.UI.Widgets;

class UIWidget
{
    public const string CONSTRAINT_TYPE_CONNECTED = "connected";

    public static readonly Color HoveredColor = Raylib.GetColor(0xcfcfcfff);
    public static readonly Color PressedColor = Raylib.GetColor(0x9e9e9eff);
    public static readonly Color InactiveColor = Raylib.GetColor(0x9e9e9ee1);

    public static bool DrawDebug { get; set; } = false;

    public Vector2 Position { get => ParentWidget != null ? ParentWidget.Position - ParentWidget.Origin + RelativePosition : RelativePosition; }
    public Vector2 RelativePosition;
    public Vector2 Size;
    public float Rotation { get; set; }
    public Vector2 Origin { get => Size * 0.5f; }
    public UIWidget? ParentWidget { get; set; }
    public bool Shallow { get; set; } = false;
    public List<UIWidget> ChildWidgets { get; }

    public bool Visible
    {
        get => _visible;
        set
        {
            _visible = value;
            UpdateChildWidgets();
        }
    }

    public bool Active
    {
        get => _active;
        set
        {
            _active = value;
            UpdateChildWidgets();
        }
    }

    public bool Interactive { get; }
    public string Name { get; set; } = string.Empty;
    public string[] Constraints { get; }

    public bool Hovered { get; set; } = false;
    public bool Pressed { get; set; } = false;
    public bool Clicked { get; set; } = false;
    public UIWidget? HoveredChild { get; set; }
    public UIWidget? PressedChild { get; set; }
    public UIWidget? ClickedChild { get; set; }
    public MouseCursor HoverCursor { get; }
    public Color Tint = Color.White;

    private bool _visible = true;
    private bool _active = true;

    public UIWidget(Vector2 position, Vector2 size, bool interactive, MouseCursor cursor = MouseCursor.Default)
    {
        ChildWidgets = new List<UIWidget>();
        RelativePosition = position;
        Size = size;
        Interactive = interactive;
        HoverCursor = cursor;
        Constraints = [];
    }

    public UIWidget(XmlNode widgetNode, bool interactive, UIWidget? parentWidget, MouseCursor cursor)
    {
        ChildWidgets = new List<UIWidget>();

        Name = Utils.TryParseXmlAttrib(widgetNode.Attributes?["name"], string.Empty);
        Visible = Utils.TryParseXmlAttrib(widgetNode.Attributes?["visible"], true);
        Active = Utils.TryParseXmlAttrib(widgetNode.Attributes?["active"], true);

        int width = Utils.TryParseXmlAttrib(widgetNode.Attributes?["width"], 0);
        int height = Utils.TryParseXmlAttrib(widgetNode.Attributes?["height"], (int)(width * 0.66f));
        Size = new Vector2(width, height);

        string posXStr = Utils.TryParseXmlAttrib(widgetNode.Attributes?["x"], "0");
        string posYStr = Utils.TryParseXmlAttrib(widgetNode.Attributes?["y"], "0");

        Vector2 centerPos = parentWidget != null ? parentWidget.Size / 2 : new Vector2(Program.Instance!.Config.RenderWidth / 2, Program.Instance!.Config.RenderHeight / 2);
        int posX = posXStr != "center" ? int.Parse(posXStr) : (int)centerPos.X;
        int posY = posYStr != "center" ? int.Parse(posYStr) : (int)centerPos.Y;

        RelativePosition = new Vector2(posX, posY);

        Constraints = Utils.TryParseXmlAttrib(widgetNode.Attributes?["constraints"], string.Empty).Split('&');

        Interactive = interactive;
        HoverCursor = cursor;
    }

    public void AddChildWidget(UIWidget child, bool shallow = false)
    {
        child.ParentWidget = this;
        child.Shallow = shallow;
        ChildWidgets.Add(child);
    }

    public void RemoveChildWidget(UIWidget child)
    {
        child.ParentWidget = null;
        child.Shallow = false;
        ChildWidgets.Remove(child);
    }

    public UIWidget? GetChildWidgetByName(string name) => ChildWidgets.FirstOrDefault(w => w.Name == name);

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
            return;
        }

        if (Constraints.Length > 0 && CheckConstraints())
            Active = false;

        if (Interactive)
        {
            if (Hovered)
                Tint = HoveredColor;

            if (Pressed)
                Tint = PressedColor;
        }

        foreach (var child in ChildWidgets)
        {
            child.Update(dt);

            if (child.Shallow)
                continue;

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

    private void UpdateChildWidgets()
    {
        foreach (var child in ChildWidgets)
        {
            child.Visible = _visible;
            child.Active = _active;
        }
    }

    private bool CheckConstraints()
    {
        bool result = false;

        foreach (var constraint in Constraints)
        {
            switch (constraint)
            {
                case CONSTRAINT_TYPE_CONNECTED:
                    result |= !GameClient.CheckConnection();
                    break;
            }
        }

        return result;
    }

    public virtual void Draw()
    {
        if (!Visible)
            return;

        foreach (var child in ChildWidgets)
        {
            child.Draw();

            if (DrawDebug)
                DrawChildDebug(child);
        }
    }

    private static void DrawChildDebug(UIWidget child)
    {
        Rectangle rec = child.GetInteractableArea();
        Color col = Color.White;

        if (child is TextUIWidget)
            col = Color.Green;
        else if (child is DynamicBoxUIWidget)
            col = Color.Blue;
        else if (child is ContainerUIWidget)
            col = Color.DarkBlue;
        else if (child is ButtonInputUIWidget)
            col = Color.Red;

        Raylib.DrawRectangleRec(rec, Raylib.ColorAlpha(col, 0.5f));
    }
}
