using System.Net.Sockets;
using System.Numerics;
using Game.Common.Data;
using Game.Common.Enums;
using Game.Common.Packets;
using Game.Server.Data.Files;
using LiteNetLib;
using Raylib_cs;

namespace Game.Server.Data;

class ServerLobbyData
{
    public GameLobbyData Data { get; }
    public LobbySettingsData Settings { get; set; }
    public PoolGamemodeConfigFileData GamemodeConfig { get; }
    public LiteNetPeer? HostPeer { get; set; }
    public LiteNetPeer? GuestPeer { get; set; }

    private GameServer server;

    private List<PoolBallData> cueHitBallsTurn;
    private List<PoolBallData> pocketedBallsTurn;
    private bool cueBallPocketedTurn = false;
    private bool railTouchedTurn = false;
    private bool canHitBlackTurn = false;

    private float elapsedTime = 0f;

    public ServerLobbyData(GameServer server, string code, LiteNetPeer hostPeer, PoolGamemodeConfigFileData gamemodeCfg, GameServerConfigFileData serverCfg)
    {
        this.server = server;

        Data = new GameLobbyData(code, gamemodeCfg);
        GamemodeConfig = gamemodeCfg;
        HostPeer = hostPeer;

        Settings = new LobbySettingsData(gamemodeCfg, 1f / serverCfg.Tickrate);

        cueHitBallsTurn = new List<PoolBallData>();
        pocketedBallsTurn = new List<PoolBallData>();

        Data.OnRailTouched += (ball) =>
        {
            railTouchedTurn = true;
        };

        Data.OnBallsCollision += (ballA, ballB) =>
        {
            if (ballA.Identifier == "cue")
                return;

            if (!cueHitBallsTurn.Contains(ballA))
                cueHitBallsTurn.Add(ballA);
        };

        Data.OnBallPocketed += (ball) =>
        {
            if (ball.Identifier != "cue")
            {
                pocketedBallsTurn.Add(ball);
                return;
            }

            Data.GetCurrentPlayer().PlacePos = new Vector3(Settings.PoolTableWidth / 2, 1.1f, Settings.PoolTableLength / 2);
            cueBallPocketedTurn = true;
        };
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

        Data.State = PoolGameState.Aiming;
    }

    public void Update(float dt)
    {
        if (!Data.Started)
            return;

        elapsedTime += dt;

        Data.UpdateSimulation(dt, Data.State != PoolGameState.BallInHand, Settings);

        bool isTableCalm = true;
        if (Data.PoolCueBall.Velocity.Length() != 0)
            isTableCalm = false;

        foreach (var ball in Data.PoolBalls.Where(b => !b.Pocketed))
        {
            if (ball.Velocity.Length() != 0)
                isTableCalm = false;
        }

        switch (Data.State)
        {
            case PoolGameState.Updating:
                UpdateTable(isTableCalm);
                break;
            case PoolGameState.BallInHand:
                UpdateCueBall(isTableCalm);
                break;
        }

        Broadcast(new UpdateLobbyPacket() { LobbyData = Data });
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
    }

    private void UpdateCueBall(bool calm)
    {
        if (!calm)
            return;

        Vector3 plyPlacePos = Data.GetPlayerByNick(Data.CurPlayer).PlacePos + new Vector3(0, 0.01f, 0);
        Data.PoolCueBall.Position = plyPlacePos;

        float halfWidth = GamemodeConfig.PoolTableWidth / 2;
        float halfLength = GamemodeConfig.PoolTableLength / 2;
        float radius = Settings.PoolBallRadius;
        var minPos = new Vector3(-halfWidth + radius, 1f, -halfLength + radius);
        var maxPos = new Vector3(halfWidth - radius, 1.01f, halfLength - radius);

        Data.PoolCueBall.Position = Raymath.Vector3Clamp(Data.PoolCueBall.Position, minPos, maxPos);

        Data.CanPlaceCueBall = true;
        foreach (var ball in Data.PoolBalls.Where(b => !b.Pocketed))
        {
            if (Data.CheckBallsCollision(Data.PoolCueBall, ball, Settings.PoolBallRadius))
            {
                Data.CanPlaceCueBall = false;
                break;
            }
        }

        foreach (var pocket in GamemodeConfig.PoolTablePockets)
        {
            if (Data.CheckPocketBallCollision(Data.PoolCueBall, Settings.PoolBallRadius, pocket, GamemodeConfig.PoolTablePocketRadius))
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

    public void BeginTurn()
    {
        var ply = Data.GetCurrentPlayer();
        Data.BeginSimulation(ply.AimDir, ply.CueForce);
        Data.State = PoolGameState.Updating;
        canHitBlackTurn = ply.BallType != PoolBallType.None && !Data.PoolBalls.Any(b => !b.Pocketed && b.Type == ply.BallType);
    }

    public void EndTurn(PoolGameState nextState, bool changePlayer)
    {
        Data.State = nextState;
        Data.Host.CueForce = 0;
        Data.Guest.CueForce = 0;

        pocketedBallsTurn.Clear();
        cueHitBallsTurn.Clear();
        cueBallPocketedTurn = false;
        railTouchedTurn = false;
        canHitBlackTurn = false;

        if (changePlayer)
            ChangePlayer();
    }

    public void LeavePlayer(string player, DisconnectReason reason)
    {
        Broadcast(new LeaveLobbyPacket()
        {
            Sender = player,
            Reason = reason
        });

        string winningPlayer = "";
        if (player == Data.Host.Nickname && Data.Guest.Nickname != null)
        {
            winningPlayer = Data.Guest.Nickname;
            HostPeer = null;
        }
        else if (player == Data.Guest.Nickname && Data.Host.Nickname != null)
        {
            winningPlayer = Data.Host.Nickname;
            GuestPeer = null;

            if (!Data.Started)
            {
                Data.Guest = new PlayerLobbyData(null);
            }
        }

        if (Data.Started)
        {
            EndTurn(PoolGameState.Finished, false);
            Data.CurPlayer = winningPlayer;
        }
    }

    public PlayerLobbyData? GetPlayerByPeer(LiteNetPeer peer)
    {
        if (HostPeer?.Id == peer.Id)
            return Data.Host;

        if (GuestPeer?.Id == peer.Id)
            return Data.Guest;

        return null;
    }

    public void Broadcast(Packet packet)
    {
        if (HostPeer != null && HostPeer.ConnectionState == ConnectionState.Connected)
            server.Send(HostPeer, packet);

        if (GuestPeer != null && GuestPeer.ConnectionState == ConnectionState.Connected)
            server.Send(GuestPeer, packet);
    }
}
