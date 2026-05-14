using Game.Common.Enums;

namespace Game.Common.Packets;

class HostLobbyPacket : LobbyPacket
{
    public HostLobbyPacket() : base(PacketType.HostLobby) { }
}
