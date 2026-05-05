using System.Net.Sockets;
using Game.Common.Data;

namespace Game.Server.Data;

class ServerLobbyData
{
    public required GameLobbyData Lobby { get; set; }
    public Socket? HostConnection { get; set; }
    public Socket? GuestConnection { get; set; }
}
