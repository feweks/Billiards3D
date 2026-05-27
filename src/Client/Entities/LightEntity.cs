using System.Numerics;
using Game.Client.Data;
using Raylib_cs;

namespace Game.Client.Entities;

class LightEntity : GameEntity
{
    private const float LIGHT_SPHERE_RADIUS = 0.1f;

    public static bool DrawLightsSources { get; set; } = false;
    public static int IndexCounter { get; set; } = 0;

    public int Index { get; set; } = 0;
    public bool Enabled { get; set; } = true;
    public float Intensity { get; set; } = 0f;
    public Color Color = Color.White;
    public Vector3 Direction = Vector3.Zero;
    public float Cutoff { get; set; }
    public float SpotExponent { get; set; }

    public LightEntity(Vector3 pos, string? name = null) : base(null, pos, name)
    {
        Index = IndexCounter++;
        Raylib.TraceLog(TraceLogLevel.Info, $"Created new light with idx {Index}");
    }

    public override void Update(float dt)
    {
        if (!Active)
            return;

        LightingShader?.UpdateLight(this);
    }

    public override RayCollision CheckCollisionRay(Ray ray) => Raylib.GetRayCollisionSphere(ray, Position, LIGHT_SPHERE_RADIUS);

    public override void Draw()
    {
        if (!DrawLightsSources)
            return;

        Raylib.DrawSphereEx(Position, LIGHT_SPHERE_RADIUS, 8, 8, Utils.ColorFromVec4(Utils.ColorToVec4(Tint) * Utils.ColorToVec4(Color)));

        const float DIR_VEC_LEN = 0.5f;
        var scaledDir = Raymath.Vector3Normalize(Direction) * DIR_VEC_LEN;
        Raylib.DrawLine3D(Position, Position + scaledDir, Color.Red);
    }
}
