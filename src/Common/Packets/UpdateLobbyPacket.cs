using Game.Common.Data;
using Game.Common.Enums;
using LiteNetLib.Utils;

namespace Game.Common.Packets;

class UpdateLobbyPacket : LobbyPacket
{
    public GameLobbyData? LobbyData { get; set; }

    public UpdateLobbyPacket() : base(PacketType.UpdateLobby, PacketSendMode.Unreliable) { }

    public override void Serialize(NetDataWriter writer)
    {
        base.Serialize(writer);

        LobbyData!.Serialize(writer);
    }

    public override void Deserialize(NetDataReader reader)
    {
        base.Deserialize(reader);

        LobbyData = new GameLobbyData(reader);
    }
}
