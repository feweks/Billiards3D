using Game.Common.Enums;

namespace Game.Common.Packets;

class JoinLobbyPacket : LobbyPacket
{
    public JoinLobbyPacket() : base(PacketType.JoinLobby) { }
}
