using System.Diagnostics.CodeAnalysis;

namespace Game.Common.Packets;

class StartLobbyPacket : LobbyPacket
{
    [SetsRequiredMembers]
    public StartLobbyPacket() : base(PacketType.StartLobby) { }
}
