using System.Net.Sockets;
using System.Numerics;
using Game.Common.Data;
using Game.Common.Packets;
using Raylib_cs;

namespace Game.Server.Data;

class ServerLobbyData
{
    public GameLobbyData Lobby { get; }
    public PoolGamemodeConfig GamemodeConfig { get; }
    public Socket? HostConnection { get; set; }
    public Socket? GuestConnection { get; set; }

    public ServerLobbyData(GameLobbyData lobbyData, PoolGamemodeConfig gamemodeCfg)
    {
        Lobby = lobbyData;
        GamemodeConfig = gamemodeCfg;

        Lobby.PoolCueBall = new PoolBallData()
        {
            Identifier = "cue",
            Position = gamemodeCfg.CueBallPos,
            Velocity = Vector3.Zero
        };

        for (int i = 0; i < gamemodeCfg.PoolBallsCount; i++)
        {
            var ball = new PoolBallData()
            {
                Identifier = $"{i + 1}",
                Position = gamemodeCfg.PoolBallsPos[i],
                Velocity = Vector3.Zero
            };

            Lobby.PoolBalls.Add(ball);
        }
    }

    public void Start()
    {
        Lobby.Started = true;

        int breakingPlayer = Raylib.GetRandomValue(0, 1);
        if (breakingPlayer == 0)
        {
            Lobby.CurPlayer = Lobby.Host.Nickname!;
        }
        else
        {
            Lobby.CurPlayer = Lobby.Guest.Nickname!;
        }

        Lobby.State = Common.PoolGameState.Break;
    }

    public void Update(float dt)
    {
        if (!Lobby.Started)
            return;

        bool tableCleared = true;
        UpdateBallPhysics(dt, Lobby.PoolCueBall!);
        if (Lobby.PoolCueBall!.Velocity.Length() != 0)
            tableCleared = false;

        foreach (var ball in Lobby.PoolBalls)
        {
            UpdateBallPhysics(dt, ball);

            if (ball.Velocity.Length() != 0)
                tableCleared = false;
        }

        if (!tableCleared)
        {
            foreach (var ballA in Lobby.PoolBalls)
            {
                foreach (var ballB in Lobby.PoolBalls.Where(b => b.Identifier != ballA.Identifier))
                {
                    if (Raylib.CheckCollisionSpheres(ballA.Position, GamemodeConfig.PoolBallRadius, ballB.Position, GamemodeConfig.PoolBallRadius))
                    {
                        HandleCollision(ballA, ballB);
                    }

                    if (Raylib.CheckCollisionSpheres(ballA.Position, GamemodeConfig.PoolBallRadius, Lobby.PoolCueBall!.Position, GamemodeConfig.PoolBallRadius))
                    {
                        HandleCollision(ballA, Lobby.PoolCueBall);
                    }
                }
            }
        }
        else
        {

        }
    }

    private void UpdateBallPhysics(float dt, PoolBallData ball)
    {
        ball.Position += ball.Velocity * dt;

        ball.Velocity *= MathF.Pow(GamemodeConfig.PoolBallFriction, dt * 60);
        float radius = GamemodeConfig.PoolBallRadius;

        if (MathF.Abs(ball.Velocity.X) < 0.01f)
            ball.Velocity.X = 0;
        if (MathF.Abs(ball.Velocity.Z) < 0.01f)
            ball.Velocity.Z = 0;

        var minPos = new Vector3(ball.Position.X - radius, 0, ball.Position.Z - radius);
        var maxPos = new Vector3(ball.Position.X + radius, 0, ball.Position.Z + radius);

        float halfWidth = GamemodeConfig.PoolTableWidth / 2;
        float halfLength = GamemodeConfig.PoolTableLength / 2;

        if (minPos.X < -halfWidth)
        {
            ball.Position.X = -halfWidth + radius;
            ball.Velocity.X *= -1;
        }
        else if (maxPos.X > halfWidth)
        {
            ball.Position.X = halfWidth - radius;
            ball.Velocity.X *= -1;
        }

        if (minPos.Z < -halfLength)
        {
            ball.Position.Z = -halfLength + radius;
            ball.Velocity.Z *= -1;
        }
        else if (maxPos.Z > halfLength)
        {
            ball.Position.Z = halfLength - radius;
            ball.Velocity.Z *= -1;
        }

        const float VEL_SCALE = 60f;
        float rotationSpeed = ball.Velocity.Length() * VEL_SCALE;
        ball.Rotation += new Vector3(rotationSpeed, 0, rotationSpeed) * dt;
    }

    private void HandleCollision(PoolBallData ballA, PoolBallData ballB)
    {
        Vector3 delta = ballB.Position - ballA.Position;
        delta.Y = 0;

        float dist = delta.Length();
        if (dist == 0)
            return;

        Vector3 normal = delta * (1 / dist);
        Vector3 relativeVel = ballB.Velocity - ballA.Velocity;
        float velAlongNormal = Vector3.Dot(relativeVel, normal);
        if (velAlongNormal > 0)
            return;

        float rest = 0.98f;

        float j = -(1 + rest) * velAlongNormal;
        j /= 1 / GamemodeConfig.PoolBallMass + 1 / GamemodeConfig.PoolBallMass;

        Vector3 impulse = normal * j;

        float m = 1 / GamemodeConfig.PoolBallMass;
        ballA.Velocity -= impulse * m;
        ballB.Velocity += impulse * m;
    }

    public void Broadcast(GameServer server, Packet packet)
    {
        if (HostConnection != null && HostConnection.Connected)
            server.Send(HostConnection, packet);

        if (GuestConnection != null && GuestConnection.Connected)
            server.Send(GuestConnection, packet);
    }
}
