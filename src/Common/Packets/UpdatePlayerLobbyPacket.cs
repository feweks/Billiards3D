using Game.Common.Data;
using Game.Common.Enums;
using LiteNetLib.Utils;

namespace Game.Common.Packets;

class UpdatePlayerLobbyPacket : LobbyPacket
{
    public PlayerLobbyData? PlayerData { get; set; }

    public UpdatePlayerLobbyPacket() : base(PacketType.UpdatePlayerLobby, PacketSendMode.Unreliable) { }

    public override void Serialize(NetDataWriter writer)
    {
        base.Serialize(writer);

        PlayerData!.Serialize(writer);
    }

    public override void Deserialize(NetDataReader reader)
    {
        base.Deserialize(reader);

        PlayerData = new PlayerLobbyData(null);
        PlayerData.Deserialize(reader);
    }
}
