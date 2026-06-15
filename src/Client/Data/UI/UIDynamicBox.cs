using System.Numerics;
using Game.Client.Managers;
using Raylib_cs;

namespace Game.Client.Data.UI;

class UIDynamicBox
{
    private const int N_PATCH_OFFSET = 24;

    public Vector2 Position;
    public Vector2 Size;
    public Vector2 Origin;
    public float Rotation { get; set; }
    public Color Tint = Color.White;
    public Rectangle Collider;

    private NPatchInfo nPatchData;
    private Texture2D tex;

    public UIDynamicBox(Vector2 pos, Vector2 size)
    {
        Position = pos;
        Size = size;
        Origin = Size * 0.5f;

        tex = ResourcesManager.GetTexture("resources/gfx/ui/ui_base.png");
        nPatchData = new NPatchInfo()
        {
            Left = N_PATCH_OFFSET,
            Top = N_PATCH_OFFSET,
            Right = N_PATCH_OFFSET,
            Bottom = N_PATCH_OFFSET,
            Source = new Rectangle(0, 0, tex.Width, tex.Height),
            Layout = NPatchLayout.NinePatch
        };
    }

    public void Update()
    {
        Collider = new Rectangle(Position - Origin, Size);
    }

    public void Draw()
    {
        var dest = new Rectangle(Position, Size);
        Raylib.DrawTextureNPatch(tex, nPatchData, dest, Origin, Rotation, Tint);
    }
}
