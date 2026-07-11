using Game.Common.Enums;
using Game.Common.Data;
using LiteNetLib.Utils;

namespace Game.Common.Packets;

class JoinedLobbyPacket : LobbyPacket
{
    public JoinedLobbyStatus Status { get; set; }
    public GameLobbyData? LobbyData { get; set; } = null;
    public LobbySettingsData? LobbySettings { get; set; } = null;

    public JoinedLobbyPacket() : base(PacketType.JoinedLobby) { }

    public override void Serialize(NetDataWriter writer)
    {
        base.Serialize(writer);

        writer.Put((byte)Status);
        if (Status == JoinedLobbyStatus.Success && LobbyData != null && LobbySettings != null)
        {
            LobbyData.Serialize(writer);
            LobbySettings.Serialize(writer);
        }
    }

    public override void Deserialize(NetDataReader reader)
    {
        base.Deserialize(reader);

        Status = (JoinedLobbyStatus)reader.GetByte();
        if (Status == JoinedLobbyStatus.Success)
        {
            LobbyData = new GameLobbyData(reader);
            LobbySettings = new LobbySettingsData(reader);
        }
    }
}
