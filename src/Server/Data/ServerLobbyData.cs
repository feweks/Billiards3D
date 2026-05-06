using System.Net.Sockets;
using Game.Common.Data;
using Game.Common.Packets;

namespace Game.Server.Data;

class ServerLobbyData
{
    public required GameLobbyData Lobby { get; set; }
    public required PoolGamemodeConfig GamemodeConfig { get; set; }
    public Socket? HostConnection { get; set; }
    public Socket? GuestConnection { get; set; }

    public void Start()
    {
        Lobby.Started = true;
    }

    public void Update(float dt)
    {
        if (!Lobby.Started)
            return;
    }

    public void Broadcast(GameServer server, Packet packet)
    {
        if (HostConnection != null)
            server.Send(HostConnection, packet);

        if (GuestConnection != null)
            server.Send(GuestConnection, packet);
    }
}
