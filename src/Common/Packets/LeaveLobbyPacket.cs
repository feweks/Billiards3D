using Game.Common.Enums;

namespace Game.Common.Packets;

class LeaveLobbyPacket : LobbyPacket
{
    public LeaveLobbyPacket() : base(PacketType.LeaveLobby) { }
}
