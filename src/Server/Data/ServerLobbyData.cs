using System.Net.Sockets;
using System.Numerics;
using Game.Common;
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

    private List<PoolBallData> cueHitBallsTurn;
    private List<PoolBallData> pocketedBallsTurn;

    private bool cueBallPocketed;

    public ServerLobbyData(GameLobbyData lobbyData, PoolGamemodeConfig gamemodeCfg)
    {
        Lobby = lobbyData;
        GamemodeConfig = gamemodeCfg;

        Lobby.PoolCueBall = new PoolBallData()
        {
            Identifier = "cue",
            Position = gamemodeCfg.PoolCueBall.Position,
            Velocity = Vector3.Zero,
            Type = PoolBallType.Cue,
        };

        for (ushort i = 0; i < gamemodeCfg.PoolBallsCount; i++)
        {
            var ball = new PoolBallData()
            {
                Identifier = $"{i + 1}",
                Index = i,
                Position = gamemodeCfg.PoolBalls[i].Position,
                Type = gamemodeCfg.PoolBalls[i].Type,
                Velocity = Vector3.Zero
            };

            Lobby.PoolBalls.Add(ball);
        }

        cueHitBallsTurn = new List<PoolBallData>();
        pocketedBallsTurn = new List<PoolBallData>();
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

        Lobby.State = PoolGameState.Break;
    }

    public void Update(float dt)
    {
        if (!Lobby.Started)
            return;

        bool isTableCalm = true;
        UpdateBallPhysics(dt, Lobby.PoolCueBall!);
        if (Lobby.PoolCueBall!.Velocity.Length() != 0)
            isTableCalm = false;

        foreach (var ball in Lobby.PoolBalls.Where(b => !b.Pocketed))
        {
            UpdateBallPhysics(dt, ball);

            if (ball.Velocity.Length() != 0)
                isTableCalm = false;
        }

        UpdateCollisions();

        switch (Lobby.State)
        {
            case PoolGameState.Update:
                UpdateTable(isTableCalm);
                break;
            case PoolGameState.PlaceWhite:
                UpdateCueBall(isTableCalm);
                break;
        }
    }

    private void ResolveTurn()
    {
        var ply = Lobby.GetPlayerByNick(Lobby.CurPlayer);

        bool cueHitSomething = cueHitBallsTurn.Count > 0;
        bool cueFoul = !cueHitSomething;
        bool anyPocketed = pocketedBallsTurn.Count > 0;
        bool blackPocketed = pocketedBallsTurn.Any(b => b.Type == PoolBallType.BlackBall);
        bool playerHasRemainingBalls = Lobby.PoolBalls.Any(b => b.Type == ply.BallType && !b.Pocketed);
        bool isBlackPhase = ply.BallType != PoolBallType.None && !playerHasRemainingBalls;

        if (cueFoul || cueBallPocketed)
        {
            EndTurn(PoolGameState.PlaceWhite, true);
            return;
        }

        if (blackPocketed)
        {
            bool win = isBlackPhase && cueHitSomething && !playerHasRemainingBalls;

            EndTurn(PoolGameState.End, !win);
            return;
        }

        if (!cueHitSomething)
        {
            EndTurn(PoolGameState.PlaceWhite, true);
            return;
        }

        var firstHit = cueHitBallsTurn.First();

        if (!ValidateHit(firstHit, ply))
        {
            EndTurn(PoolGameState.PlaceWhite, true);
            return;
        }

        if (ply.BallType == PoolBallType.None && anyPocketed)
        {
            var firstValid = pocketedBallsTurn.FirstOrDefault(b => b.Type == PoolBallType.Solid || b.Type == PoolBallType.Striped);

            if (firstValid != null)
            {
                ply.BallType = firstValid.Type;
                var otherType = ply.BallType == PoolBallType.Striped ? PoolBallType.Solid : PoolBallType.Striped;
                var otherPlayer = ply.Nickname == Lobby.Host.Nickname ? Lobby.Guest : Lobby.Host;
                otherPlayer.BallType = otherType;
            }

            EndTurn(PoolGameState.Shooting, false);
            return;
        }

        if (!anyPocketed)
        {
            EndTurn(PoolGameState.Shooting, true);
            return;
        }

        bool validPocket = pocketedBallsTurn.Any(b => b.Type == ply.BallType);

        if (!validPocket)
        {
            EndTurn(PoolGameState.PlaceWhite, true);
            return;
        }

        EndTurn(PoolGameState.Shooting, false);
    }

    private void UpdateTable(bool calm)
    {
        if (calm)
        {
            ResolveTurn();
            return;
        }

        foreach (var pocketPos in GamemodeConfig.PoolTablePockets)
        {
            foreach (var ball in Lobby.PoolBalls.Where(b => !b.Pocketed))
            {
                if (CheckPocketBallCollision(ball, pocketPos))
                {
                    ball.Pocketed = true;
                    ball.PocketPos = pocketPos;
                    pocketedBallsTurn.Add(ball);
                }
            }

            if (CheckPocketBallCollision(Lobby.PoolCueBall!, pocketPos))
            {
                Lobby.GetPlayerByNick(Lobby.CurPlayer).PlacePos = Vector3.Zero;
                Lobby.PoolCueBall!.Velocity = Vector3.Zero;
                cueBallPocketed = true;
                return;
            }
        }
    }

    private bool ValidateHit(PoolBallData ball, PlayerLobbyData ply)
    {
        if (ply.BallType == PoolBallType.None)
            return ball.Type != PoolBallType.BlackBall;

        if (!Lobby.PoolBalls.Any(b => !b.Pocketed && b.Type == ply.BallType))
        {
            return ball.Type == PoolBallType.BlackBall;
        }

        return ball.Type == ply.BallType;
    }

    private void UpdateCueBall(bool calm)
    {
        if (!calm)
            return;

        Vector3 plyPlacePos = Lobby.GetPlayerByNick(Lobby.CurPlayer).PlacePos + new Vector3(0, 0.01f, 0);
        Lobby.PoolCueBall!.Position = plyPlacePos;

        float halfWidth = GamemodeConfig.PoolTableWidth / 2;
        float halfLength = GamemodeConfig.PoolTableLength / 2;
        float radius = GamemodeConfig.PoolCueBall.Radius;
        var minPos = new Vector3(-halfWidth + radius, 1f, -halfLength + radius);
        var maxPos = new Vector3(halfWidth - radius, 1.01f, halfLength - radius);

        Lobby.PoolCueBall.Position = Raymath.Vector3Clamp(Lobby.PoolCueBall.Position, minPos, maxPos);

        Lobby.CanPlaceCueBall = true;
        foreach (var ball in Lobby.PoolBalls.Where(b => !b.Pocketed))
        {
            if (CheckBallsCollision(Lobby.PoolCueBall, ball))
            {
                Lobby.CanPlaceCueBall = false;
                break;
            }
        }

        foreach (var pocket in GamemodeConfig.PoolTablePockets)
        {
            if (CheckPocketBallCollision(Lobby.PoolCueBall, pocket))
            {
                Lobby.CanPlaceCueBall = false;
                break;
            }
        }
    }

    private void ChangePlayer()
    {
        string nextPlayer = Lobby.CurPlayer == Lobby.Host.Nickname ? Lobby.Guest.Nickname! : Lobby.Host.Nickname!;
        Lobby.CurPlayer = nextPlayer;
    }

    private bool CheckBallsCollision(PoolBallData ballA, PoolBallData ballB)
    {
        float radiusA = GamemodeConfig.PoolBalls[ballA.Index].Radius;
        float radiusB = GamemodeConfig.PoolBalls[ballB.Index].Radius;

        bool coll = Raylib.CheckCollisionSpheres(ballA.Position, radiusA, ballB.Position, radiusB);
        return coll;
    }

    private bool CheckPocketBallCollision(PoolBallData ball, Vector3 pocketPos)
    {
        float ballRadius = GamemodeConfig.PoolBalls[ball.Index].Radius;
        float pocketRadius = GamemodeConfig.PoolTablePocketRadius;

        bool coll = Raylib.CheckCollisionSpheres(ball.Position, ballRadius, pocketPos, pocketRadius);
        return coll;
    }

    private void UpdateBallPhysics(float dt, PoolBallData ball)
    {
        ball.Position += ball.Velocity * dt;

        ball.Velocity *= MathF.Pow(GamemodeConfig.PoolBallFriction, dt * 60);
        float radius = GamemodeConfig.PoolBalls[ball.Index].Radius;

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

        const float VEL_SCALE = 300f;
        float rotationSpeed = ball.Velocity.Length() * VEL_SCALE;
        ball.Rotation += new Vector3(rotationSpeed, 0, rotationSpeed) * dt;
    }

    private void UpdateCollisions()
    {
        if (Lobby.State != PoolGameState.PlaceWhite)
        {
            for (int i = 0; i < Lobby.PoolBalls.Count; i++)
            {
                var ballA = Lobby.PoolBalls[i];
                for (int j = i + 1; j < Lobby.PoolBalls.Count; j++)
                {
                    var ballB = Lobby.PoolBalls[j];

                    if (!ballA.Pocketed && !ballB.Pocketed && CheckBallsCollision(ballA, ballB))
                        HandleCollision(ballA, ballB);
                }

                if (CheckBallsCollision(ballA, Lobby.PoolCueBall!))
                {
                    HandleCollision(ballA, Lobby.PoolCueBall!);

                    if (!cueHitBallsTurn.Contains(ballA))
                        cueHitBallsTurn.Add(ballA);
                }
            }
        }
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
        float massA = GamemodeConfig.PoolBalls[ballA.Index].Mass;

        float j = -(1 + rest) * velAlongNormal;
        j /= 1 / massA + 1 / massA;

        Vector3 impulse = normal * j;

        float m = 1 / massA;
        ballA.Velocity -= impulse * m;
        ballB.Velocity += impulse * m;
    }

    private void EndTurn(PoolGameState nextState, bool changePlayer)
    {
        Lobby.State = nextState;
        pocketedBallsTurn.Clear();
        cueHitBallsTurn.Clear();

        var ply = Lobby.GetPlayerByNick(Lobby.CurPlayer);
        ply.CueForce = 0;
        cueBallPocketed = false;

        if (changePlayer)
            ChangePlayer();
    }

    public void Broadcast(GameServer server, Packet packet)
    {
        if (HostConnection != null && HostConnection.Connected)
            server.Send(HostConnection, packet);

        if (GuestConnection != null && GuestConnection.Connected)
            server.Send(GuestConnection, packet);
    }
}
