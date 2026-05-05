using System.Diagnostics.CodeAnalysis;

namespace Game.Common.Packets;

class JoinLobbyPacket : LobbyPacket
{
    [SetsRequiredMembers]
    public JoinLobbyPacket() : base(PacketType.JoinLobby) { }
}
