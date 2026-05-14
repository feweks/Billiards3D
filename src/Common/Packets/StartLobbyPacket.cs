using Game.Common.Enums;

namespace Game.Common.Packets;

class StartLobbyPacket : LobbyPacket
{
    public StartLobbyPacket() : base(PacketType.StartLobby) { }
}
