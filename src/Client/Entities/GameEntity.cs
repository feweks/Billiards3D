using Game.Client.Data;
using Game.Client.Data.Files;
using Raylib_cs;
using System.Numerics;

namespace Game.Client.Entities;

abstract class GameEntity
{
    public Vector3 Position;
    public Vector3 Rotation = Vector3.Zero;
    public Vector3 Scale = Vector3.One;
    public Vector3 Velocity = Vector3.Zero;
    public LightingShaderData? LightingShader { get; internal set; }

    public string? Name { get; set; }
    public string? Map { get; set; }
    public Color Tint = Color.White;

    public bool Visible { get; set; } = true;
    public bool Active { get; set; } = true;

    public BoundingBox BoundingBox { get => boundingBox; }

    private BoundingBox boundingBox;

    public GameEntity(Vector3 pos, Vector3 rot, Vector3 scale, string? name, string? map)
    {
        Position = pos;
        Rotation = rot;
        Scale = scale;
        Name = name;
        Map = map;
    }

    public GameEntity(GameEntity ent)
    {
        Position = ent.Position;
        Rotation = ent.Rotation;
        Scale = ent.Scale;
        Name = ent.Name;
        Map = ent.Map;
    }

    public GameEntity(MapEntityFileData fileData, string? map)
    {
        Position = fileData.Position;
        Rotation = fileData.Rotation;
        Scale = fileData.Scale;
        Name = fileData.Name;
        Map = map;
    }

    public virtual void SetLightingShader(LightingShaderData shader)
    {
        LightingShader = shader;
    }

    public virtual void Update(float dt)
    {
        if (!Active)
            return;

        boundingBox = UpdateBoundingBox();

        Position += Velocity * dt;
    }

    public virtual RayCollision CheckCollisionRay(Ray ray)
    {
        var result = new RayCollision()
        {
            Hit = false,
            Distance = float.MaxValue
        };

        return result;
    }

    public virtual GameEntity Copy()
    {
        throw new NotImplementedException("Do not copy raw GameEntity");
    }

    public virtual void Draw()
    {

    }

    public virtual BoundingBox UpdateBoundingBox()
    {
        return new BoundingBox();
    }
}
