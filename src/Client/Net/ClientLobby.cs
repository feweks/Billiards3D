using Game.Common.Data;
using Game.Common.Enums;

namespace Game.Client.Net;

class ClientLobby
{
    public GameLobbyData? Data { get; internal set; } = null;
    public LobbySettingsData? Settings { get; internal set; } = null;
    public List<ClientChatMessage> ChatHistory { get; internal set; } = new List<ClientChatMessage>();
    public JoinedLobbyStatus Status { get; internal set; } = JoinedLobbyStatus.None;
    public string? PlayerNick { get; internal set; } = null;

    public void Join(GameLobbyData data, LobbySettingsData settings, string nick)
    {
        Data = data;
        Settings = settings;
        PlayerNick = nick;
        Status = JoinedLobbyStatus.Success;
    }

    public bool IsConnected() => Data != null && Status == JoinedLobbyStatus.Success;

    public void Reset()
    {
        Data = null;
        Settings = null;
        ChatHistory.Clear();
        Status = JoinedLobbyStatus.None;
        PlayerNick = null;
    }
}
