using System.Diagnostics.CodeAnalysis;

namespace Game.Common.Packets;

class PlaceCueLobbyPacket : LobbyPacket
{
    [SetsRequiredMembers]
    public PlaceCueLobbyPacket() : base(PacketType.PlaceCueLobby) { }
}
