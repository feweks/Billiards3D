using Game.Common.Enums;

namespace Game.Common.Data;


class GameLobbyData : ISerializableData
{
    public string Code { get; internal set; }
    public PlayerLobbyData Host { get; set; }
    public PlayerLobbyData Guest { get; set; }
    public bool Started { get; set; } = false;
    public PoolBallData? PoolCueBall { get; set; }
    public List<PoolBallData> PoolBalls { get; }
    public PoolGameState State { get; set; } = PoolGameState.None;
    public string CurPlayer { get; set; } = string.Empty;
    public bool CanPlaceCueBall { get; set; } = false;

    public GameLobbyData(string code)
    {
        Code = code;

        Host = new PlayerLobbyData(null);
        Guest = new PlayerLobbyData(null);

        PoolBalls = new List<PoolBallData>();
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

        PoolCueBall!.Serialize(writer);

        writer.Write(PoolBalls.Count);
        for (int i = 0; i < PoolBalls.Count; i++)
        {
            PoolBalls[i].Serialize(writer);
        }
    }

    public void Deserialize(BinaryReader reader)
    {
        Code = reader.ReadString();
        Host.Deserialize(reader);
        Guest.Deserialize(reader);
        Started = reader.ReadBoolean();
        State = (PoolGameState)reader.ReadByte();
        CurPlayer = reader.ReadString();
        CanPlaceCueBall = reader.ReadBoolean();

        PoolCueBall = new PoolBallData();
        PoolCueBall.Deserialize(reader);

        int ballsCount = reader.ReadInt32();
        PoolBalls.Clear();
        for (int i = 0; i < ballsCount; i++)
        {
            var ball = new PoolBallData();
            ball.Deserialize(reader);

            PoolBalls.Add(ball);
        }
    }
}
