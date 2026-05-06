namespace Game.Common.Packets;

class PingPacket : Packet
{
    public PingPacket() : base(PacketType.Ping) { }
}
