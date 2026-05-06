namespace Game.Common;

enum PacketType : byte
{
    Ping = 0,
    HostLobby,
    JoinLobby,
    JoinedLobby,
    StartLobby
}
