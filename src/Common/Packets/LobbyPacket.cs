using System.Diagnostics.CodeAnalysis;

namespace Game.Common.Packets;

class LobbyPacket : Packet
{
    public required string Sender { get; set; } = string.Empty;
    public required string LobbyCode { get; set; } = string.Empty;

    [SetsRequiredMembers]
    public LobbyPacket(PacketType type) : base(type) { }

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
