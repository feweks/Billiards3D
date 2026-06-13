using Game.Common.Data;
using Raylib_cs;

namespace Game.Client.Entities;

class PoolBallEntity : ModelEntity
{
    public PoolBallData NetData { get; internal set; }
    private bool useLerp;

    public PoolBallEntity(PoolBallData data, string? map) : base(map)
    {
        NetData = data;
        BoundingBoxRotation = false;
        LoadModelData($"resources/gfx/models/balls/pool_ball_{data.Identifier}.obj");
        Position = data.Position;
        Raylib.TraceLog(TraceLogLevel.Info, $"{NetData.Identifier}, {NetData.Position}");
    }

    public void UpdateNetworkData(PoolBallData ballData, bool lerp)
    {
        NetData = ballData;
        useLerp = lerp;
    }

    public override void Update(float dt)
    {
        base.Update(dt);

        Visible = !NetData.Pocketed;

        if (useLerp)
        {
            float lerpAmount = dt * 45f;
            Position = Raymath.Vector3Lerp(Position, NetData.Position, lerpAmount);
            Rotation.X = Raymath.LerpAngle(Rotation.X, NetData.Rotation.X, lerpAmount);
            Rotation.Y = Raymath.LerpAngle(Rotation.Y, NetData.Rotation.Y, lerpAmount);
            Rotation.Z = Raymath.LerpAngle(Rotation.Z, NetData.Rotation.Z, lerpAmount);
        }
        else
        {

            Position = NetData.Position;
            Rotation = NetData.Rotation;
        }
    }

    public override void Draw()
    {
        base.Draw();
    }
}
