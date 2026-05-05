using System.Net;
using System.Net.Sockets;
using System.Text;
using Game.Common;
using Game.Common.Packets;
using Game.Server.Data;
using Raylib_cs;

namespace Game.Server;

class GameServer
{
    private const uint MAX_PACKET_SIZE = 4096;

    private Socket listener;
    private List<Socket> clients;
    private Thread? connThread;
    private bool running = false;

    public GameServer(GameServerConfig config)
    {
        listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        listener.Bind(new IPEndPoint(IPAddress.Any, config.Port));
        clients = new List<Socket>();
        running = true;
    }

    public void Start()
    {
        listener.Listen();

        connThread = new Thread(new ThreadStart(UpdateConnections))
        {
            IsBackground = false
        };
        connThread.Start();
    }

    private string CreateLobby()
    {
        var builder = new StringBuilder();

        for (int i = 0; i < GameData.LobbyCodeLength; i++)
        {
            int val = Raylib.GetRandomValue(0, 9);
            builder.Append(val);
        }

        string code = builder.ToString();

        Raylib.TraceLog(TraceLogLevel.Info, $"Created new lobby [code {code}]");
        return code;
    }

    private void AcceptClient(Socket client)
    {
        client.Blocking = false;
        clients.Add(client);
        Raylib.TraceLog(TraceLogLevel.Info, "Client connected to server");
    }

    private void ProcessPacket(Socket client, Packet packet)
    {
        switch (packet.Type)
        {
            case PacketType.HostLobby:
                {
                    var response = new HostLobbyPacket() { Code = CreateLobby() };
                    Send(client, response);

                    break;
                }
        }
    }

    private void ProcessClient(Socket client, int bytesCount, byte[] buf)
    {
        if (bytesCount == 0)
        {
            Raylib.TraceLog(TraceLogLevel.Info, $"Client disconnected from server");
            clients.Remove(client);
            client.Close();
            return;
        }

        var stream = new MemoryStream(buf, false);
        var reader = new BinaryReader(stream);
        var packetType = (PacketType)reader.ReadByte();
        var packet = Packet.Create(packetType);
        packet.Deserialize(reader);
        ProcessPacket(client, packet);
    }

    private void Send(Socket client, Packet packet)
    {
        var memStream = new MemoryStream();
        var binStream = new BinaryWriter(memStream);
        packet.Serialize(binStream);

        client.Send(memStream.ToArray());
    }

    private void UpdateConnections()
    {
        while (running)
        {
            var readList = new List<Socket>() { listener };
            readList.AddRange(clients);

            Socket.Select(readList, null, null, 10000);

            foreach (var socket in readList)
            {
                if (socket == listener)
                {
                    try
                    {
                        AcceptClient(listener.Accept());
                    }
                    catch (Exception error)
                    {
                        Raylib.TraceLog(TraceLogLevel.Warning, $"Failed to accept connection: {error.Message}");
                    }
                }
                else
                {
                    try
                    {
                        byte[] buf = new byte[MAX_PACKET_SIZE];
                        int recvBytes = socket.Receive(buf);
                        ProcessClient(socket, recvBytes, buf);
                    }
                    catch (SocketException error)
                    {
                        Raylib.TraceLog(TraceLogLevel.Warning, $"Failed to receive msg from client: {error.Message} [ERROR CODE {error.SocketErrorCode}]");

                        if (error.SocketErrorCode == SocketError.ConnectionReset)
                        {
                            clients.Remove(socket);
                            socket.Close();
                        }
                    }
                }
            }
        }
    }

    public bool IsRunning() => running;

    public void Shutdown()
    {
        running = false;
    }
}
