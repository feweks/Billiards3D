using System.Net.Sockets;
using System.Numerics;
using Game.Common.Data;
using Game.Common.Enums;
using Game.Common.Packets;
using Game.Server.Data.Files;
using Raylib_cs;

namespace Game.Server.Data;

class ServerLobbyData
{
    public GameLobbyData Data { get; }
    public PoolGamemodeConfigFileData GamemodeConfig { get; }
    public Socket? HostConnection { get; set; }
    public Socket? GuestConnection { get; set; }

    private List<PoolBallData> cueHitBallsTurn;
    private List<PoolBallData> pocketedBallsTurn;
    private bool cueBallPocketedTurn = false;
    private bool railTouchedTurn = false;
    private bool canHitBlackTurn = false;

    public ServerLobbyData(GameLobbyData lobbyData, PoolGamemodeConfigFileData gamemodeCfg)
    {
        Data = lobbyData;
        GamemodeConfig = gamemodeCfg;

        Data.PoolCueBall = new PoolBallData()
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

            Data.PoolBalls.Add(ball);
        }

        cueHitBallsTurn = new List<PoolBallData>();
        pocketedBallsTurn = new List<PoolBallData>();
    }

    public void Start()
    {
        Data.Started = true;

        int breakingPlayer = Raylib.GetRandomValue(0, 1);
        if (breakingPlayer == 0)
        {
            Data.CurPlayer = Data.Host.Nickname!;
        }
        else
        {
            Data.CurPlayer = Data.Guest.Nickname!;
        }

        Data.State = PoolGameState.Breaking;
    }

    public void Update(float dt, GameServer server)
    {
        if (!Data.Started)
            return;

        bool isTableCalm = true;
        UpdateBallPhysics(dt, Data.PoolCueBall!);
        if (Data.PoolCueBall!.Velocity.Length() != 0)
            isTableCalm = false;

        foreach (var ball in Data.PoolBalls.Where(b => !b.Pocketed))
        {
            UpdateBallPhysics(dt, ball);

            if (ball.Velocity.Length() != 0)
                isTableCalm = false;
        }

        UpdateCollisions();

        switch (Data.State)
        {
            case PoolGameState.Updating:
                UpdateTable(isTableCalm);
                break;
            case PoolGameState.BallInHand:
                UpdateCueBall(isTableCalm);
                break;
        }

        Broadcast(server, new UpdateLobbyPacket()
        {
            LobbyData = Data
        });
    }

    private void ResolveTurn()
    {
        var ply = Data.GetCurrentPlayer();

        if (pocketedBallsTurn.Count > 0)
        {
            var blackPocketed = pocketedBallsTurn.Find(b => b.Type == PoolBallType.BlackBall);

            if (blackPocketed != null)
            {
                bool couldPocketBlack = canHitBlackTurn && !cueBallPocketedTurn;

                EndTurn(PoolGameState.Finished, !couldPocketBlack);
                return;
            }
        }

        bool foul = false;
        foul |= cueBallPocketedTurn;
        var firstBall = cueHitBallsTurn.FirstOrDefault();
        foul |= firstBall == null;
        PoolBallType requiredType = canHitBlackTurn ? PoolBallType.BlackBall : ply.BallType;
        foul |= firstBall != null && ply.BallType != PoolBallType.None && firstBall.Type != requiredType;
        bool legalAfterContact = pocketedBallsTurn.Count > 0 || railTouchedTurn;
        foul |= !legalAfterContact;

        if (foul)
        {
            EndTurn(PoolGameState.BallInHand, true);
            return;
        }

        bool tableOpened = ply.BallType == PoolBallType.None;
        if (tableOpened && pocketedBallsTurn.Count > 0)
        {
            bool pocketedSolids = pocketedBallsTurn.Any(b => b.Type == PoolBallType.Solid);
            bool pocketedStriped = pocketedBallsTurn.Any(b => b.Type == PoolBallType.Striped);

            if (!(pocketedSolids && pocketedStriped))
            {
                ply.BallType = pocketedBallsTurn.First(b => b.Type == PoolBallType.Solid || b.Type == PoolBallType.Striped).Type;

                PlayerLobbyData otherPlayer;
                if (ply.Nickname == Data.Host.Nickname)
                    otherPlayer = Data.Guest;
                else
                    otherPlayer = Data.Host;

                otherPlayer.BallType = ply.BallType == PoolBallType.Solid ? PoolBallType.Striped : PoolBallType.Solid;

                EndTurn(PoolGameState.Aiming, false);
                return;
            }

            EndTurn(PoolGameState.Aiming, true);
            return;
        }

        bool continueTurn = pocketedBallsTurn.Any(b => b.Type == ply.BallType);
        EndTurn(PoolGameState.Aiming, !continueTurn);
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
            foreach (var ball in Data.PoolBalls.Where(b => !b.Pocketed))
            {
                if (CheckPocketBallCollision(ball, pocketPos))
                {
                    ball.Pocketed = true;
                    ball.PocketPos = pocketPos;
                    pocketedBallsTurn.Add(ball);
                }
            }

            if (CheckPocketBallCollision(Data.PoolCueBall!, pocketPos))
            {
                Data.GetPlayerByNick(Data.CurPlayer).PlacePos = Vector3.Zero;
                Data.PoolCueBall!.Velocity = Vector3.Zero;
                cueBallPocketedTurn = true;
                return;
            }
        }
    }

    private bool ValidateHit(PoolBallData ball, PlayerLobbyData ply)
    {
        if (ply.BallType == PoolBallType.None)
            return ball.Type != PoolBallType.BlackBall;

        if (!Data.PoolBalls.Any(b => !b.Pocketed && b.Type == ply.BallType))
        {
            return ball.Type == PoolBallType.BlackBall;
        }

        return ball.Type == ply.BallType;
    }

    private void UpdateCueBall(bool calm)
    {
        if (!calm)
            return;

        Vector3 plyPlacePos = Data.GetPlayerByNick(Data.CurPlayer).PlacePos + new Vector3(0, 0.01f, 0);
        Data.PoolCueBall!.Position = plyPlacePos;

        float halfWidth = GamemodeConfig.PoolTableWidth / 2;
        float halfLength = GamemodeConfig.PoolTableLength / 2;
        float radius = GamemodeConfig.PoolCueBall.Radius;
        var minPos = new Vector3(-halfWidth + radius, 1f, -halfLength + radius);
        var maxPos = new Vector3(halfWidth - radius, 1.01f, halfLength - radius);

        Data.PoolCueBall.Position = Raymath.Vector3Clamp(Data.PoolCueBall.Position, minPos, maxPos);

        Data.CanPlaceCueBall = true;
        foreach (var ball in Data.PoolBalls.Where(b => !b.Pocketed))
        {
            if (CheckBallsCollision(Data.PoolCueBall, ball))
            {
                Data.CanPlaceCueBall = false;
                break;
            }
        }

        foreach (var pocket in GamemodeConfig.PoolTablePockets)
        {
            if (CheckPocketBallCollision(Data.PoolCueBall, pocket))
            {
                Data.CanPlaceCueBall = false;
                break;
            }
        }
    }

    private void ChangePlayer()
    {
        string nextPlayer = Data.CurPlayer == Data.Host.Nickname ? Data.Guest.Nickname! : Data.Host.Nickname!;
        Data.CurPlayer = nextPlayer;
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
            railTouchedTurn = true;
        }
        else if (maxPos.X > halfWidth)
        {
            ball.Position.X = halfWidth - radius;
            ball.Velocity.X *= -1;
            railTouchedTurn = true;
        }

        if (minPos.Z < -halfLength)
        {
            ball.Position.Z = -halfLength + radius;
            ball.Velocity.Z *= -1;
            railTouchedTurn = true;
        }
        else if (maxPos.Z > halfLength)
        {
            ball.Position.Z = halfLength - radius;
            ball.Velocity.Z *= -1;
            railTouchedTurn = true;
        }

        const float VEL_SCALE = 300f;
        float rotationSpeed = ball.Velocity.Length() * VEL_SCALE;
        ball.Rotation += new Vector3(rotationSpeed, 0, rotationSpeed) * dt;
    }

    private void UpdateCollisions()
    {
        if (Data.State != PoolGameState.BallInHand)
        {
            for (int i = 0; i < Data.PoolBalls.Count; i++)
            {
                var ballA = Data.PoolBalls[i];
                for (int j = i + 1; j < Data.PoolBalls.Count; j++)
                {
                    var ballB = Data.PoolBalls[j];

                    if (!ballA.Pocketed && !ballB.Pocketed && CheckBallsCollision(ballA, ballB))
                        HandleCollision(ballA, ballB);
                }

                if (CheckBallsCollision(ballA, Data.PoolCueBall!))
                {
                    HandleCollision(ballA, Data.PoolCueBall!);

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

    public void BeginTurn()
    {
        var ply = Data.GetCurrentPlayer();
        Data.PoolCueBall!.Velocity = ply.AimDir * ply.CueForce;
        ply.CueForce = 0;
        Data.State = PoolGameState.Updating;
        canHitBlackTurn = ply.BallType != PoolBallType.None && !Data.PoolBalls.Any(b => !b.Pocketed && b.Type == ply.BallType);
    }

    public void EndTurn(PoolGameState nextState, bool changePlayer)
    {
        Data.State = nextState;

        pocketedBallsTurn.Clear();
        cueHitBallsTurn.Clear();
        cueBallPocketedTurn = false;
        railTouchedTurn = false;
        canHitBlackTurn = false;

        var ply = Data.GetCurrentPlayer();
        ply.CueForce = 0;

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
