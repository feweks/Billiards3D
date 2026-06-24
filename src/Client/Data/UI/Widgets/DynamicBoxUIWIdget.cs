using System.Numerics;
using Game.Client.Managers;
using Raylib_cs;

namespace Game.Client.Data.UI.Widgets;

class DynamicBoxUIWidget : UIWidget
{
    private const string BOX_TEXTURE_PATH = "resources/gfx/ui/ui_dynamic_box.png";
    private const int BOX_NPATCH_OFFSET = 20;

    private NPatchInfo nPatchData;
    private Texture2D texture;

    public DynamicBoxUIWidget(Vector2 pos, Vector2 size, bool interactive) : base(pos, size, interactive, MouseCursor.PointingHand)
    {
        InitGraphics();
    }

    private void InitGraphics()
    {
        texture = ResourcesManager.GetTexture(BOX_TEXTURE_PATH);
        nPatchData = new NPatchInfo()
        {
            Left = BOX_NPATCH_OFFSET,
            Top = BOX_NPATCH_OFFSET,
            Bottom = BOX_NPATCH_OFFSET,
            Right = BOX_NPATCH_OFFSET,
            Source = new Rectangle(0, 0, texture.Dimensions),
            Layout = NPatchLayout.NinePatch
        };
    }

    public override void Draw()
    {
        base.Draw();

        if (!Visible)
            return;

        var dest = new Rectangle(Position, Size);
        Raylib.DrawTextureNPatch(texture, nPatchData, dest, Origin, Rotation, Tint);
    }
}
