namespace Game.Common.Enums;

enum PacketType : byte
{
    Ping = 0,
    HostLobby,
    JoinLobby,
    JoinedLobby,
    StartLobby,
    UpdateLobby,
    UpdatePlayerLobby,
    ShotLobby,
    PlaceCueLobby,
    LeaveLobby,
    ChangeLobbySettings,
    ChatMessageLobby,
    InitializeUnreliableConnection
}
