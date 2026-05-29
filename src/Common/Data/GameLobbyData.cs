using System.Numerics;
using Game.Common.Enums;
using Game.Server.Data.Files;

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
                Velocity = Vector3.Zero
            };

            PoolBalls.Add(ball);
        }
    }

    public GameLobbyData(BinaryReader reader)
    {
        Deserialize(reader);
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

        PoolCueBall = new PoolBallData();
        PoolCueBall.Deserialize(reader);

        int ballsCount = reader.ReadInt32();
        PoolBalls ??= new List<PoolBallData>();
        PoolBalls.Clear();
        for (int i = 0; i < ballsCount; i++)
        {
            var ball = new PoolBallData();
            ball.Deserialize(reader);

            PoolBalls.Add(ball);
        }
    }
}
