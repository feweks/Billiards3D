
namespace Game.Common.Data;

class PlayerLobbyData : ISerializableData
{
    public string? Nickname { get; set; }

    public PlayerLobbyData(string? nick)
    {
        Nickname = nick;
    }

    public void Serialize(BinaryWriter writer)
    {
        writer.Write(Nickname ?? string.Empty);
    }

    public void Deserialize(BinaryReader reader)
    {
        string nick = reader.ReadString();
        Nickname = nick != string.Empty ? nick : null;
    }
}
