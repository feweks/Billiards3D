using System.Net;
using System.Net.Sockets;

namespace Game.Server.Data;

class ServerClientData
{
    public Socket TcpConnection { get; }
    public IPEndPoint? UdpEndPoint { get; set; }
    public Guid Guid { get; }

    public ServerClientData(Socket client, Guid clientGuid)
    {
        TcpConnection = client;
        Guid = clientGuid;
    }
}
