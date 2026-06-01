using Game.Common.Enums;
using Game.Common.Data;

namespace Game.Common.Packets;

class JoinedLobbyPacket : LobbyPacket
{
    public JoinedLobbyStatus Status { get; set; }
    public GameLobbyData? LobbyData { get; set; } = null;
    public LobbySettingsData? LobbySettings { get; set; } = null;

    public JoinedLobbyPacket() : base(PacketType.JoinedLobby) { }

    public override void Serialize(BinaryWriter writer)
    {
        base.Serialize(writer);

        writer.Write((byte)Status);
        if (Status == JoinedLobbyStatus.Success && LobbyData != null && LobbySettings != null)
        {
            LobbyData.Serialize(writer);
            LobbySettings.Serialize(writer);
        }
    }

    public override void Deserialize(BinaryReader reader)
    {
        base.Deserialize(reader);

        Status = (JoinedLobbyStatus)reader.ReadByte();
        if (Status == JoinedLobbyStatus.Success)
        {
            LobbyData = new GameLobbyData(reader);
            LobbySettings = new LobbySettingsData(reader);
        }
    }
}
