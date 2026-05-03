using System.Net;
using System.Net.Sockets;
using System.Text;
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

    public bool IsRunning() => running;

    private void AcceptClient(Socket client)
    {
        client.Blocking = false;
        clients.Add(client);
        Raylib.TraceLog(TraceLogLevel.Info, "Client connected to server");
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

        string msg = Encoding.UTF8.GetString(buf, 0, bytesCount);
        Raylib.TraceLog(TraceLogLevel.Info, $"Client send: {msg}");
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

    public void Shutdown()
    {
        running = false;
    }
}
