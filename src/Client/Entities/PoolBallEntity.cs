using System.Numerics;
using Game.Common.Data;
using Raylib_cs;

namespace Game.Client.Entities;

class PoolBallEntity : GameEntity
{
    public PoolBallData Data { get; set; }

    public PoolBallEntity(PoolBallData data) : base($"resources/gfx/models/pool_ball_{data.Identifier}.obj", data.Position)
    {
        Data = data;
        Raylib.TraceLog(TraceLogLevel.Info, $"{Data.Identifier}, {Data.Position}");
    }

    public override void Update(float dt)
    {
        base.Update(dt);

        float lerpAmount = dt * 15f;

        Position = Raymath.Vector3Lerp(Position, Data.Position, lerpAmount);
        Rotation.X = Raymath.LerpAngle(Rotation.X, Data.Rotation.X, lerpAmount);
        Rotation.Y = Raymath.LerpAngle(Rotation.Y, Data.Rotation.Y, lerpAmount);
        Rotation.Z = Raymath.LerpAngle(Rotation.Z, Data.Rotation.Z, lerpAmount);
    }

    public override void Draw()
    {
        base.Draw();
    }
}
