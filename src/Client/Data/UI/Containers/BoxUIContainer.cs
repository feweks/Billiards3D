using System.Numerics;
using System.Xml;

namespace Game.Client.Data.UI.Containers;

class BoxUIContainer : UIContainer
{
    private UIDynamicBox box;

    public BoxUIContainer(Vector2 pos, Vector2 size, string name) : base(pos, size, name)
    {
        box = new UIDynamicBox(pos, size);
    }

    public BoxUIContainer(XmlNode boxContainerNode) : base(boxContainerNode)
    {
        box = new UIDynamicBox(Position, Size);
    }

    public override void Update(float dt)
    {
        base.Update(dt);
    }

    public override void Draw()
    {
        base.Draw();

        if (!Visible)
            return;

        box.Draw();
    }
}
