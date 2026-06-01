using System.Numerics;
using Game.Common.Enums;
using Game.Server.Data.Files;
using Raylib_cs;

namespace Game.Common.Data;


class GameLobbyData : ISerializableData
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
            Mass = gamemode.PoolCueBall.Mass,
            Friction = gamemode.PoolBallFriction,
            Radius = gamemode.PoolCueBall.Radius
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
                Mass = gamemode.PoolBalls[i].Mass,
                Friction = gamemode.PoolBallFriction,
                Radius = gamemode.PoolBalls[i].Radius
            };

            PoolBalls.Add(ball);
        }
    }

    public GameLobbyData(BinaryReader reader)
    {
        Deserialize(reader);
    }

    public GameLobbyData Copy()
    {
        var stream = new MemoryStream();
        var writer = new BinaryWriter(stream);
        Serialize(writer);

        var readStream = new MemoryStream(stream.ToArray());
        var reader = new BinaryReader(readStream);
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
        SimulateBall(dt, PoolCueBall, settings.PoolTableWidth, settings.PoolTableLength);

        foreach (var ball in PoolBalls)
        {
            SimulateBall(dt, ball, settings.PoolTableWidth, settings.PoolTableLength);
        }

        if (simulateCollisions)
        {
            SimulateCollisions();
            SimulatePockets(settings);
        }
    }

    private void SimulateBall(float dt, PoolBallData ball, float tableWidth, float tableLength)
    {
        ball.Position += ball.Velocity * dt;

        ball.Velocity *= MathF.Pow(ball.Friction, dt * 60);
        float radius = ball.Radius;

        if (MathF.Abs(ball.Velocity.X) < 0.01f)
            ball.Velocity.X = 0;
        if (MathF.Abs(ball.Velocity.Z) < 0.01f)
            ball.Velocity.Z = 0;

        var minPos = new Vector3(ball.Position.X - radius, 0, ball.Position.Z - radius);
        var maxPos = new Vector3(ball.Position.X + radius, 0, ball.Position.Z + radius);

        float halfWidth = tableWidth / 2;
        float halfLength = tableLength / 2;
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

    private void SimulateCollisions()
    {
        for (int i = 0; i < PoolBalls.Count; i++)
        {
            var ballA = PoolBalls[i];
            for (int j = i + 1; j < PoolBalls.Count; j++)
            {
                var ballB = PoolBalls[j];

                if (!ballA.Pocketed && !ballB.Pocketed && CheckBallsCollision(ballA, ballB))
                    HandleCollision(ballA, ballB);
            }

            if (CheckBallsCollision(ballA, PoolCueBall))
            {
                HandleCollision(ballA, PoolCueBall);
            }
        }
    }

    private void SimulatePockets(LobbySettingsData settings)
    {
        foreach (var pocketPos in settings.PoolPockets)
        {
            foreach (var ball in PoolBalls.Where(b => !b.Pocketed))
            {
                if (CheckPocketBallCollision(ball, pocketPos, settings.PoolPocketRadius))
                {
                    ball.Pocketed = true;
                    ball.PocketPos = pocketPos;
                    OnBallPocketed?.Invoke(ball);
                }
            }

            if (CheckPocketBallCollision(PoolCueBall, pocketPos, settings.PoolPocketRadius))
            {
                PoolCueBall.Velocity = Vector3.Zero;
                OnBallPocketed?.Invoke(PoolCueBall);
                return;
            }
        }
    }

    public bool CheckBallsCollision(PoolBallData ballA, PoolBallData ballB)
    {
        float radiusA = ballA.Radius;
        float radiusB = ballB.Radius;

        bool coll = Raylib.CheckCollisionSpheres(ballA.Position, radiusA, ballB.Position, radiusB);
        return coll;
    }

    public bool CheckPocketBallCollision(PoolBallData ball, Vector3 pocketPos, float pocketRadius)
    {
        bool coll = Raylib.CheckCollisionSpheres(ball.Position, ball.Radius, pocketPos, pocketRadius);
        return coll;
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
        float massA = ballA.Mass;

        float j = -(1 + rest) * velAlongNormal;
        j /= 1 / massA + 1 / massA;

        Vector3 impulse = normal * j;

        float m = 1 / massA;
        ballA.Velocity -= impulse * m;
        ballB.Velocity += impulse * m;

        OnBallsCollision?.Invoke(ballA, ballB);
    }

    public void Serialize(BinaryWriter writer)
    {
        writer.Write(Code);
        Host.Serialize(writer);
        Guest.Serialize(writer);
        writer.Write(Started);
        writer.Write((byte)State);
        writer.Write(CurPlayer);
        writer.Write(CanPlaceCueBall);

        PoolCueBall.Serialize(writer);

        writer.Write(PoolBalls.Count);
        for (int i = 0; i < PoolBalls.Count; i++)
        {
            PoolBalls[i].Serialize(writer);
        }
    }

    public void Deserialize(BinaryReader reader)
    {
        Code = reader.ReadString();
        Host ??= new PlayerLobbyData(null);
        Host.Deserialize(reader);
        Guest ??= new PlayerLobbyData(null);
        Guest.Deserialize(reader);
        Started = reader.ReadBoolean();
        State = (PoolGameState)reader.ReadByte();
        CurPlayer = reader.ReadString();
        CanPlaceCueBall = reader.ReadBoolean();

        PoolCueBall ??= new PoolBallData();
        PoolCueBall.Deserialize(reader);

        int ballsCount = reader.ReadInt32();
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
