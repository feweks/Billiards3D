using System.Numerics;
using Game.Common.Data;
using Raylib_cs;

namespace Game.Client.Entities;

class PoolBallEntity : ModelEntity
{
    private PoolBallData netData;
    private bool useLerp;

    public PoolBallEntity(PoolBallData data, string? map) : base(map)
    {
        netData = data;
        BoundingBoxRotation = false;
        LoadModelData($"resources/gfx/models/balls/pool_ball_{data.Identifier}.obj");
        Position = data.Position;
        Raylib.TraceLog(TraceLogLevel.Info, $"{netData.Identifier}, {netData.Position}");
    }

    public void UpdateNetworkData(PoolBallData ballData, bool lerp)
    {
        netData = ballData;
        useLerp = lerp;
    }

    public override void Update(float dt)
    {
        base.Update(dt);

        Visible = !netData.Pocketed;

        if (useLerp)
        {
            float lerpAmount = dt * 45f;
            Position = Raymath.Vector3Lerp(Position, netData.Position, lerpAmount);
            Rotation.X = Raymath.LerpAngle(Rotation.X, netData.Rotation.X, lerpAmount);
            Rotation.Y = Raymath.LerpAngle(Rotation.Y, netData.Rotation.Y, lerpAmount);
            Rotation.Z = Raymath.LerpAngle(Rotation.Z, netData.Rotation.Z, lerpAmount);
        }
        else
        {

            Position = netData.Position;
            Rotation = netData.Rotation;
        }
    }

    public override void Draw()
    {
        base.Draw();
    }
}
