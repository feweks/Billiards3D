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
            Raylib.TraceLog(TraceLogLevel.Info, $"{ball.Position}");
        }
    }

    public void Start()
    {
        Lobby.Started = true;
    }

    public void Update(float dt)
    {
        if (!Lobby.Started)
            return;

        foreach (var ball in Lobby.PoolBalls)
        {
            UpdateBallPhysics(dt, ball);
        }
    }

    private void UpdateBallPhysics(float dt, PoolBallData ball)
    {
        /*ball.Velocity *= MathF.Pow(GamemodeConfig.PoolBallFriction, dt * 60);
        float radius = GamemodeConfig.PoolBallRadius;

        if (MathF.Abs(ball.Velocity.X) < 0.01f)
            ball.Velocity.X = 0;
        if (MathF.Abs(ball.Velocity.Z) < 0.01f)
            ball.Velocity.Z = 0;

        var minPos = new Vector3(ball.Position.X - radius, 0, ball.Position.Z - radius);
        var maxPos = new Vector3(ball.Position.X + radius, 0, ball.Position.Z + radius);

        float halfWidth = GamemodeConfig.PoolTableWidth / 2;
        float halfLength = GamemodeConfig.PoolTableWidth / 2;

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
        ball.Rotation += new Vector3(rotationSpeed, 0, rotationSpeed) * dt;*/
    }

    public void Broadcast(GameServer server, Packet packet)
    {
        if (HostConnection != null && HostConnection.Connected)
            server.Send(HostConnection, packet);

        if (GuestConnection != null && GuestConnection.Connected)
            server.Send(GuestConnection, packet);
    }
}
