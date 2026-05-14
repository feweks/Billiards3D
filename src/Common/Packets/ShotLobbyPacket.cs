using Game.Common.Enums;

namespace Game.Common.Packets;

class ShotLobbyPacket : LobbyPacket
{
    public ShotLobbyPacket() : base(PacketType.ShotLobby) { }
}
