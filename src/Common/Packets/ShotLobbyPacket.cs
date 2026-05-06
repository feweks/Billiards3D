using System.Diagnostics.CodeAnalysis;

namespace Game.Common.Packets;

class ShotLobbyPacket : LobbyPacket
{
    [SetsRequiredMembers]
    public ShotLobbyPacket() : base(PacketType.ShotLobby) { }
}
