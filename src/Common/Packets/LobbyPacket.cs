using Game.Common.Enums;

namespace Game.Common.Packets;

class LobbyPacket : Packet
{
    public string Sender { get; set; } = string.Empty;
    public string LobbyCode { get; set; } = string.Empty;

    public LobbyPacket(PacketType type, PacketSendMode sendMode = PacketSendMode.Reliable) : base(type, sendMode) { }

    public override void Serialize(BinaryWriter writer)
    {
        base.Serialize(writer);

        writer.Write(LobbyCode);
        writer.Write(Sender);
    }

    public override void Deserialize(BinaryReader reader)
    {
        base.Deserialize(reader);

        LobbyCode = reader.ReadString();
        Sender = reader.ReadString();
    }
}
