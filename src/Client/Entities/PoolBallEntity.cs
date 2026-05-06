using System.Numerics;
using Game.Common.Data;
using Raylib_cs;

namespace Game.Client.Entities;

class PoolBallEntity : GameEntity
{
    public PoolBallData Data { get; }

    public PoolBallEntity(PoolBallData data) : base($"resources/gfx/models/pool_ball_{data.Identifier}.obj", data.Position)
    {
        Data = data;

        Raylib.TraceLog(TraceLogLevel.Info, $"{Data.Identifier}, {Data.Position}");
    }

    public override void Update(float dt)
    {
        base.Update(dt);

        Position = Data.Position;
    }


    //public bool CheckCollisions(PoolBallEntity ballEnt) => Raylib.CheckCollisionBoxes(BoundingBox, ballEnt.BoundingBox);

    /*public void HandleCollision(PoolBallEntity ballEnt)
    {
        Vector3 delta = ballEnt.Position - Position;
        delta.Y = 0;

        float dist = delta.Length();
        if (dist == 0)
            return;

        Vector3 normal = delta * (1 / dist);
        Vector3 relativeVel = ballEnt.Velocity - Velocity;
        float velAlongNormal = Vector3.Dot(relativeVel, normal);
        if (velAlongNormal > 0)
            return;

        float rest = 0.98f;

        float j = -(1 + rest) * velAlongNormal;
        j /= 1 / Mass + 1 / Mass;

        Vector3 impulse = normal * j;

        float m = 1 / Mass;
        Velocity -= impulse * m;
        ballEnt.Velocity += impulse * m;
    }*/

    public override void Draw()
    {
        base.Draw();
    }
}
