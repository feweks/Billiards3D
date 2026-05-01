using System.Numerics;
using Raylib_cs;

namespace Game.Client.Entities;

class PoolBallEntity : GameEntity
{
    public const float POOL_TABLE_WIDTH = 0.95f;
    public const float POOL_TABLE_LENGTH = 1.88f;
    public const float BALL_FRICTION = 0.98f;

    public float Mass { get; } = 0.17f;

    public PoolBallEntity(string identifier, Vector3 pos) : base($"resources/gfx/models/pool_ball_{identifier}.obj", pos) { }

    public override void Update(float dt)
    {
        base.Update(dt);

        if (Raylib.IsKeyPressed(KeyboardKey.G))
        {
            float multiplier = 0.75f;
            Velocity = new Vector3(Raylib.GetRandomValue(-5, 5) * multiplier, 0, Raylib.GetRandomValue(-5, 5) * multiplier);
        }

        UpdatePhysics(dt);
    }

    private void UpdatePhysics(float dt)
    {
        Velocity *= MathF.Pow(BALL_FRICTION, dt * 60);
        float radius = (BoundingBox.Max.X - BoundingBox.Min.X) * 0.5f;

        if (MathF.Abs(Velocity.X) < 0.01f)
            Velocity.X = 0;
        if (MathF.Abs(Velocity.Z) < 0.01f)
            Velocity.Z = 0;

        var minPos = new Vector3(Position.X - radius, 0, Position.Z - radius);
        var maxPos = new Vector3(Position.X + radius, 0, Position.Z + radius);

        float halfWidth = POOL_TABLE_WIDTH / 2;
        float halfLength = POOL_TABLE_LENGTH / 2;

        if (minPos.X < -halfWidth)
        {
            Position.X = -halfWidth + radius;
            Velocity.X *= -1;
        }
        else if (maxPos.X > halfWidth)
        {
            Position.X = halfWidth - radius;
            Velocity.X *= -1;
        }

        if (minPos.Z < -halfLength)
        {
            Position.Z = -halfLength + radius;
            Velocity.Z *= -1;
        }
        else if (maxPos.Z > halfLength)
        {
            Position.Z = halfLength - radius;
            Velocity.Z *= -1;
        }

        const float VEL_SCALE = 5f;
        float rotationSpeed = Velocity.Length() * VEL_SCALE;
        Rotation += new Vector3(rotationSpeed, 0, rotationSpeed);
    }

    public bool CheckCollisions(PoolBallEntity ballEnt) => Raylib.CheckCollisionBoxes(BoundingBox, ballEnt.BoundingBox);

    public void HandleCollision(PoolBallEntity ballEnt)
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
    }

    public override void Draw()
    {
        base.Draw();
    }
}
