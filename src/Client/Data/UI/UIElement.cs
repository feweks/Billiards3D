using System.Numerics;
using System.Xml;
using Game.Client.Net;
using Raylib_cs;

namespace Game.Client.Data.UI;

class UIElement
{
    private const string CONSTRAINT_TYPE_CONNECTED = "connected";
    private const string CONSTRAINT_TYPE_HOST = "host";

    public Vector2 Position;
    public Vector2 Size;
    public Vector2 Origin;
    public float Rotation { get; set; } = 0f;
    public Color Tint = Color.White;
    public string Name { get; }

    public bool Active { get; set; } = true;
    public bool Visible { get; set; } = true;
    public bool Useable { get; set; } = true;
    public string[] Constraints { get; }

    public UIElement(Vector2 pos, Vector2 size, string name)
    {
        Position = pos;
        Size = size;
        Origin = Size * 0.5f;
        Name = name;
        Constraints = [];
    }

    public UIElement(XmlNode elementNode, Vector2 center)
    {
        int width = Utils.TryParseXmlAttrib(elementNode.Attributes?["width"], 0);
        int height = Utils.TryParseXmlAttrib(elementNode.Attributes?["height"], 0);

        var xAttrib = elementNode.Attributes?["x"];
        float x = (xAttrib != null && xAttrib.Value == "center") ? center.X : Utils.TryParseXmlAttrib(xAttrib, 0);

        var yAttrib = elementNode.Attributes?["y"];
        float y = (yAttrib != null && yAttrib.Value == "center") ? center.Y : Utils.TryParseXmlAttrib(yAttrib, 0);

        Name = Utils.TryParseXmlAttrib(elementNode.Attributes?["name"], string.Empty);
        Active = Utils.TryParseXmlAttrib(elementNode.Attributes?["active"], true);
        Visible = Utils.TryParseXmlAttrib(elementNode.Attributes?["visible"], true);
        Constraints = Utils.TryParseXmlAttrib(elementNode.Attributes?["constraints"], string.Empty).Split('&');

        Position = new Vector2(x, y);
        Size = new Vector2(width, height);
        Origin = Size * 0.5f;
    }

    public virtual void Update(float dt)
    {
        Useable = true;
        foreach (var constraint in Constraints)
        {
            switch (constraint)
            {
                case CONSTRAINT_TYPE_CONNECTED:
                    Useable = GameClient.CheckConnection();
                    break;
                case CONSTRAINT_TYPE_HOST:
                    Useable = GameClient.IsHost();
                    break;
            }
        }
    }

    public virtual void Draw()
    {

    }
}
