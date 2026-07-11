using System.Numerics;
using Game.Common.Enums;
using Game.Server.Data.Files;
using LiteNetLib.Utils;
using Raylib_cs;

namespace Game.Common.Data;


class GameLobbyData : INetSerializable
{
    public string Code { get; internal set; } = string.Empty;
    public PlayerLobbyData Host { get; set; } = null!;
    public PlayerLobbyData Guest { get; set; } = null!;
    public bool Started { get; set; } = false;
    public PoolBallData PoolCueBall { get; set; } = null!;
    public List<PoolBallData> PoolBalls { get; internal set; } = null!;
    public PoolGameState State { get; set; } = PoolGameState.None;
    public string CurPlayer { get; set; } = string.Empty;
    public bool CanPlaceCueBall { get; set; } = false;

    public event Action<PoolBallData>? OnRailTouched;
    public event Action<PoolBallData, PoolBallData>? OnBallsCollision;
    public event Action<PoolBallData>? OnBallPocketed;

    public GameLobbyData(string code, PoolGamemodeConfigFileData gamemode)
    {
        Code = code;

        Host = new PlayerLobbyData(null);
        Guest = new PlayerLobbyData(null);

        PoolBalls = new List<PoolBallData>();

        PoolCueBall = new PoolBallData()
        {
            Identifier = "cue",
            Position = gamemode.PoolCueBall.Position,
            Velocity = Vector3.Zero,
            Type = PoolBallType.Cue,
        };

        for (ushort i = 0; i < gamemode.PoolBallsCount; i++)
        {
            var ball = new PoolBallData()
            {
                Identifier = $"{i + 1}",
                Index = i,
                Position = gamemode.PoolBalls[i].Position,
                Type = gamemode.PoolBalls[i].Type,
                Velocity = Vector3.Zero,
            };

            PoolBalls.Add(ball);
        }
    }

    public GameLobbyData(NetDataReader reader)
    {
        Deserialize(reader);
    }

    public GameLobbyData Copy()
    {
        var writer = new NetDataWriter();
        Serialize(writer);
        var reader = new NetDataReader(writer);
        var lobby = new GameLobbyData(reader);
        return lobby;
    }

    public PlayerLobbyData GetPlayerByNick(string nick) => Host.Nickname == nick ? Host : Guest;

    public bool CheckIfPlayerExists(string nick) => Host.Nickname == nick || Guest.Nickname == nick;

    public int GetPlayerCount()
    {
        if (Host.Nickname == null && Guest.Nickname == null)
            return 0;

        if (Guest.Nickname == null)
            return 1;

        return 2;
    }

    public PlayerLobbyData GetCurrentPlayer()
    {
        if (CurPlayer == Host.Nickname)
            return Host;

        return Guest;
    }

    public void BeginSimulation(Vector3 playerAimDir, float playerCueForce)
    {
        PoolCueBall.Velocity = playerAimDir * playerCueForce;
    }

    public void UpdateSimulation(float dt, bool simulateCollisions, LobbySettingsData settings)
    {
        SimulateBall(dt, PoolCueBall, settings);

        foreach (var ball in PoolBalls)
        {
            SimulateBall(dt, ball, settings);
        }

        if (simulateCollisions)
        {
            SimulateCollisions(settings);
            SimulatePockets(settings);
        }
    }

    private void SimulateBall(float dt, PoolBallData ball, LobbySettingsData settings)
    {
        ball.Position += ball.Velocity * dt;

        ball.Velocity *= MathF.Pow(settings.PoolBallFriction, dt * 60);
        float radius = settings.PoolBallRadius;

        if (MathF.Abs(ball.Velocity.X) < 0.01f)
            ball.Velocity.X = 0;
        if (MathF.Abs(ball.Velocity.Z) < 0.01f)
            ball.Velocity.Z = 0;

        var minPos = new Vector3(ball.Position.X - radius, 0, ball.Position.Z - radius);
        var maxPos = new Vector3(ball.Position.X + radius, 0, ball.Position.Z + radius);

        float halfWidth = settings.PoolTableWidth / 2;
        float halfLength = settings.PoolTableLength / 2;
        bool railTouched = false;

        if (minPos.X < -halfWidth)
        {
            ball.Position.X = -halfWidth + radius;
            ball.Velocity.X *= -1;
            railTouched = true;
        }
        else if (maxPos.X > halfWidth)
        {
            ball.Position.X = halfWidth - radius;
            ball.Velocity.X *= -1;
            railTouched = true;
        }

        if (minPos.Z < -halfLength)
        {
            ball.Position.Z = -halfLength + radius;
            ball.Velocity.Z *= -1;
            railTouched = true;
        }
        else if (maxPos.Z > halfLength)
        {
            ball.Position.Z = halfLength - radius;
            ball.Velocity.Z *= -1;
            railTouched = true;
        }

        if (railTouched)
            OnRailTouched?.Invoke(ball);

        const float VEL_SCALE = 300f;
        float rotationSpeed = ball.Velocity.Length() * VEL_SCALE;
        ball.Rotation += new Vector3(rotationSpeed, 0, rotationSpeed) * dt;
    }

    private void SimulateCollisions(LobbySettingsData settings)
    {
        for (int i = 0; i < PoolBalls.Count; i++)
        {
            var ballA = PoolBalls[i];
            for (int j = i + 1; j < PoolBalls.Count; j++)
            {
                var ballB = PoolBalls[j];

                if (!ballA.Pocketed && !ballB.Pocketed && CheckBallsCollision(ballA, ballB, settings.PoolBallRadius))
                    HandleCollision(ballA, ballB, settings.PoolBallMass);
            }

            if (!ballA.Pocketed && CheckBallsCollision(ballA, PoolCueBall, settings.PoolBallRadius))
            {
                HandleCollision(ballA, PoolCueBall, settings.PoolBallMass);
            }
        }
    }

    private void SimulatePockets(LobbySettingsData settings)
    {
        foreach (var pocketPos in settings.PoolPockets)
        {
            foreach (var ball in PoolBalls.Where(b => !b.Pocketed))
            {
                if (CheckPocketBallCollision(ball, settings.PoolBallRadius, pocketPos, settings.PoolPocketRadius))
                {
                    ball.Pocketed = true;
                    ball.PocketPos = pocketPos;
                    OnBallPocketed?.Invoke(ball);
                }
            }

            if (CheckPocketBallCollision(PoolCueBall, settings.PoolBallRadius, pocketPos, settings.PoolPocketRadius))
            {
                PoolCueBall.Velocity = Vector3.Zero;
                OnBallPocketed?.Invoke(PoolCueBall);
                return;
            }
        }
    }

    public bool CheckBallsCollision(PoolBallData ballA, PoolBallData ballB, float ballRadius)
    {
        bool coll = Raylib.CheckCollisionSpheres(ballA.Position, ballRadius, ballB.Position, ballRadius);
        return coll;
    }

    public bool CheckPocketBallCollision(PoolBallData ball, float ballRadius, Vector3 pocketPos, float pocketRadius)
    {
        bool coll = Raylib.CheckCollisionSpheres(ball.Position, ballRadius, pocketPos, pocketRadius);
        return coll;
    }

    private void HandleCollision(PoolBallData ballA, PoolBallData ballB, float ballMass)
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
        j /= 1 / ballMass + 1 / ballMass;

        Vector3 impulse = normal * j;

        float m = 1 / ballMass;
        ballA.Velocity -= impulse * m;
        ballB.Velocity += impulse * m;

        OnBallsCollision?.Invoke(ballA, ballB);
    }

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(Code);
        Host.Serialize(writer);
        Guest.Serialize(writer);
        writer.Put(Started);
        writer.Put((byte)State);
        writer.Put(CurPlayer);
        writer.Put(CanPlaceCueBall);

        PoolCueBall.Serialize(writer);

        writer.Put((ushort)PoolBalls.Count);
        for (int i = 0; i < PoolBalls.Count; i++)
        {
            PoolBalls[i].Serialize(writer);
        }
    }

    public void Deserialize(NetDataReader reader)
    {
        Code = reader.GetString();
        Host ??= new PlayerLobbyData(null);
        Host.Deserialize(reader);
        Guest ??= new PlayerLobbyData(null);
        Guest.Deserialize(reader);
        Started = reader.GetBool();
        State = (PoolGameState)reader.GetByte();
        CurPlayer = reader.GetString();
        CanPlaceCueBall = reader.GetBool();

        PoolCueBall ??= new PoolBallData();
        PoolCueBall.Deserialize(reader);

        ushort ballsCount = reader.GetUShort();
        PoolBalls ??= new List<PoolBallData>();
        if (PoolBalls.Count == 0)
        {
            for (int i = 0; i < ballsCount; i++)
            {
                var ball = new PoolBallData();
                ball.Deserialize(reader);

                PoolBalls.Add(ball);
            }
        }
        else
        {
            for (int i = 0; i < ballsCount; i++)
            {
                PoolBalls[i].Deserialize(reader);
            }
        }
    }
}
