using Game.Common.Enums;

namespace Game.Common.Packets;

class InitializeUnreliableConnectionPacket : Packet
{
    public InitializeUnreliableConnectionPacket() : base(PacketType.InitializeUnreliableConnection, PacketSendMode.Unreliable) { }
}
