namespace Game.Common.Enums;

enum PacketType : byte
{
    None = 0,
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
}
