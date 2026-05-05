
namespace Game.Common.Packets;

class HostLobbyPacket : Packet
{
    public string? Code { get; set; } = null;

    public HostLobbyPacket() : base(PacketType.HostLobby) { }

    public override void Serialize(BinaryWriter writer)
    {
        base.Serialize(writer);

        writer.Write(Code ?? string.Empty);
    }

    public override void Deserialize(BinaryReader reader)
    {
        base.Deserialize(reader);

        Code = reader.ReadString();
    }
}
