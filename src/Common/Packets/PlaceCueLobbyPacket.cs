using Game.Common.Enums;

namespace Game.Common.Packets;

class PlaceCueLobbyPacket : LobbyPacket
{
    public PlaceCueLobbyPacket() : base(PacketType.PlaceCueLobby) { }
}
