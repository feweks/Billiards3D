using Game.Common.Enums;
using LiteNetLib;
using LiteNetLib.Utils;

namespace Game.Common.Packets;

class LeaveLobbyPacket : LobbyPacket
{
    public DisconnectReason Reason { get; set; }

    public LeaveLobbyPacket() : base(PacketType.LeaveLobby) { }

    public override void Serialize(NetDataWriter writer)
    {
        base.Serialize(writer);

        writer.Put((byte)Reason);
    }

    public override void Deserialize(NetDataReader reader)
    {
        base.Deserialize(reader);

        Reason = (DisconnectReason)reader.GetByte();
    }
}
