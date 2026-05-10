using System.Diagnostics.CodeAnalysis;

namespace Game.Common.Packets;

class LeaveLobbyPacket : LobbyPacket
{
    [SetsRequiredMembers]
    public LeaveLobbyPacket() : base(PacketType.LeaveLobby) { }
}
