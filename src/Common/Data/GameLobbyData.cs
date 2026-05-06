
namespace Game.Common.Data;

class GameLobbyData : ISerializableData
{
    public string Code { get; internal set; }
    public PlayerLobbyData Host { get; }
    public PlayerLobbyData Guest { get; }
    public bool Started { get; set; } = false;

    public GameLobbyData(string code)
    {
        Code = code;

        Host = new PlayerLobbyData(null);
        Guest = new PlayerLobbyData(null);
    }

    public bool CheckIfPlayerExists(string nick) => Host.Nickname == nick || Guest.Nickname == nick;

    public int GetPlayerCount()
    {
        if (Host.Nickname == null && Guest.Nickname == null)
            return 0;

        if (Guest.Nickname == null)
            return 1;

        return 2;
    }

    public void Serialize(BinaryWriter writer)
    {
        writer.Write(Code);
        Host.Serialize(writer);
        Guest.Serialize(writer);
    }

    public void Deserialize(BinaryReader reader)
    {
        Code = reader.ReadString();
        Host.Deserialize(reader);
        Guest.Deserialize(reader);
    }
}
