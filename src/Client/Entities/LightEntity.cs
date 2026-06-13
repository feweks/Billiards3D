using System.Numerics;
using Game.Client.Data;
using Game.Client.Data.Files;
using Raylib_cs;

namespace Game.Client.Entities;

class LightEntity : GameEntity
{
    private const float LIGHT_SPHERE_RADIUS = 0.1f;

    public static bool DrawLightSources { get; set; } = false;
    public static bool[] TakenIndexes { get; } = new bool[LightingShaderData.LIGHTS_COUNT];

    public int Index { get; } = 0;
    public bool Enabled { get; set; } = true;
    public float Intensity { get; set; } = 0f;
    public Color Color = Color.White;
    public Vector3 Direction = Vector3.Zero;
    public float Cutoff { get; set; }
    public float SpotExponent { get; set; }

    private BoundingBox defaultBox = new BoundingBox();

    public LightEntity(string? map) : base(Vector3.Zero, Vector3.Zero, Vector3.One, null, map)
    {
        Index = FindFreeLightIndex();
        Raylib.TraceLog(TraceLogLevel.Info, $"Created new light with idx {Index}");
    }

    public LightEntity(LightEntity lightEnt) : base(lightEnt)
    {
        Index = FindFreeLightIndex();

        Enabled = lightEnt.Enabled;
        Intensity = lightEnt.Intensity;
        Direction = lightEnt.Direction;
        Cutoff = lightEnt.Cutoff;
        Color = lightEnt.Color;
        SpotExponent = lightEnt.SpotExponent;
    }

    public LightEntity(MapLightFileData fileData, string? map) : base(fileData, map)
    {
        Index = FindFreeLightIndex();

        Enabled = fileData.Enabled;
        Intensity = fileData.Intensity;
        Direction = fileData.Direction;
        Color = fileData.Color;
        Cutoff = fileData.Cutoff;
        SpotExponent = fileData.SpotExponent;
    }

    public override void Update(float dt)
    {
        if (!Active)
            return;

        LightingShader?.UpdateLight(this);
    }

    public override BoundingBox UpdateBoundingBox() => defaultBox;

    public override RayCollision CheckCollisionRay(Ray ray) => Raylib.GetRayCollisionSphere(ray, Position, LIGHT_SPHERE_RADIUS);

    public override LightEntity Copy() => new LightEntity(this);

    private static int FindFreeLightIndex()
    {
        for (int i = 0; i < LightingShaderData.LIGHTS_COUNT; i++)
        {
            if (!TakenIndexes[i])
            {
                TakenIndexes[i] = true;
                return i;
            }
        }

        throw new IndexOutOfRangeException($"Failed to get index for light: all indexes are taken");
    }

    public override void Draw()
    {
        if (!DrawLightSources)
            return;

        Raylib.DrawSphereEx(Position, LIGHT_SPHERE_RADIUS, 8, 8, Utils.ColorFromVec4(Utils.ColorToVec4(Tint) * Utils.ColorToVec4(Color)));

        const float DIR_VEC_LEN = 0.5f;
        var scaledDir = Raymath.Vector3Normalize(Direction) * DIR_VEC_LEN;
        Raylib.DrawLine3D(Position, Position + scaledDir, Color.Red);
    }

    public override void Destroy()
    {
        TakenIndexes[Index] = false;
    }
}
