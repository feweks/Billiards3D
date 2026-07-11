using Game.Common.Enums;
using LiteNetLib.Utils;

namespace Game.Common.Packets;

class LobbyPacket : Packet
{
    public string Sender { get; set; } = string.Empty;
    public string LobbyCode { get; set; } = string.Empty;

    public LobbyPacket(PacketType type, PacketSendMode sendMode = PacketSendMode.Reliable) : base(type, sendMode) { }

    public override void Serialize(NetDataWriter writer)
    {
        base.Serialize(writer);

        writer.Put(LobbyCode);
        writer.Put(Sender);
    }

    public override void Deserialize(NetDataReader reader)
    {
        base.Deserialize(reader);

        LobbyCode = reader.GetString();
        Sender = reader.GetString();
    }
}
