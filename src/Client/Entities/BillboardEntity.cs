using System.Numerics;
using Game.Client.Data;
using Game.Client.Data.Files;
using Game.Client.Managers;
using Game.Client.States;
using Raylib_cs;

namespace Game.Client.Entities;

class BillboardEntity : GameEntity
{
    public string? Path { get; set; }
    public bool CastsShadow { get; set; }
    public Vector2 Size { get; set; } = Vector2.One;

    public GameState? State { get; set; }

    private Texture2D texture;

    public BillboardEntity(string? map) : base(Vector3.Zero, Vector3.Zero, Vector3.One, null, map)
    {
        LoadTexture(null);
    }

    public BillboardEntity(BillboardEntity billEnt) : base(billEnt)
    {
        CastsShadow = billEnt.CastsShadow;
        Size = billEnt.Size;

        LoadTexture(billEnt.Path);
    }

    public BillboardEntity(MapBillboardFileData fileData, string? map) : base(fileData, map)
    {
        CastsShadow = fileData.CastsShadow;
        Size = fileData.Size;

        LoadTexture(fileData.Path);
    }

    public void LoadTexture(string? path)
    {
        Path = path;
        texture = ResourcesManager.GetTexture(path);

        UpdateBoundingBox();
    }

    public override BoundingBox UpdateBoundingBox()
    {
        float thickness = 0.1f;

        var minOffset = new Vector3(Size.X * 0.5f, 0, thickness * 0.5f);
        var maxOffset = new Vector3(Size.X * 0.5f, Size.Y, thickness * 0.5f);

        var result = new BoundingBox()
        {
            Min = Position - minOffset,
            Max = Position + maxOffset
        };

        return result;
    }

    public override RayCollision CheckCollisionRay(Ray ray) => Raylib.GetRayCollisionBox(ray, BoundingBox);

    public override BillboardEntity Copy() => new BillboardEntity(this);

    public override void Draw()
    {
        if (!Visible || State == null)
            return;

        if (LightingShader != null)
            Raylib.BeginShaderMode(LightingShader.Shader);

        var src = new Rectangle(0, 0, texture.Dimensions);
        var origin = new Vector2(0.5f, 0f) * Size;
        Raylib.DrawBillboardPro(State.Camera, texture, src, Position, Vector3.UnitY, Size, origin, Rotation.X, Tint);

        if (LightingShader != null)
            Raylib.EndShaderMode();
    }
}
