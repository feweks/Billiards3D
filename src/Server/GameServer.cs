using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Game.Common;
using Game.Common.Data;
using Game.Common.Packets;
using Game.Server.Data;
using Raylib_cs;

namespace Game.Server;

class GameServer
{
    private const uint MAX_PACKET_SIZE = 4096;

    public ConcurrentDictionary<string, ServerLobbyData> Lobbies { get; }

    private GameServerConfig config;
    private Dictionary<PoolGamemodeType, PoolGamemodeConfig> gamemodeConfigs;
    private Socket listener;
    private List<Socket> clients;
    private Thread? connThread;
    private Thread? lobbiesThread;
    private bool running = false;

    public GameServer(GameServerConfig config, Dictionary<PoolGamemodeType, PoolGamemodeConfig> gamemodeConfigs)
    {
        this.config = config;
        this.gamemodeConfigs = gamemodeConfigs;
        listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        listener.Bind(new IPEndPoint(IPAddress.Any, config.Port));
        clients = new List<Socket>();
        running = true;

        Lobbies = new ConcurrentDictionary<string, ServerLobbyData>();
    }

    public void Start()
    {
        listener.Listen();

        connThread = new Thread(new ThreadStart(UpdateConnections))
        {
            IsBackground = false
        };
        connThread.Start();

        lobbiesThread = new Thread(new ThreadStart(UpdateLobbies))
        {
            IsBackground = true,
        };
        lobbiesThread.Start();
    }

    private string CreateLobby()
    {
        var builder = new StringBuilder();

        for (int i = 0; i < GameData.LobbyCodeLength; i++)
        {
            bool number = Raylib.GetRandomValue(0, 1) == 0;

            if (number)
            {
                int val = Raylib.GetRandomValue(0, 9);
                if (val == 0 && i == 0) // no lobby starting with 0
                    val++;

                builder.Append(val);
            }
            else
            {
                char c = (char)Raylib.GetRandomValue(65, 90); // ASCII from A to Z
                builder.Append(c);
            }
        }

        string code = builder.ToString();
        if (Lobbies.ContainsKey(code))
        {
            Raylib.TraceLog(TraceLogLevel.Warning, $"Generated already existing lobby code!");
            return CreateLobby();
        }

        var lobbyData = new GameLobbyData(code);
        Lobbies.TryAdd(code, new ServerLobbyData(lobbyData, gamemodeConfigs[PoolGamemodeType.Classic]));

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
            case PacketType.Ping:
                {
                    Send(client, packet);
                    break;
                }
            case PacketType.HostLobby:
                {
                    var hostPacket = (HostLobbyPacket)packet;
                    string hostName = hostPacket.Sender;
                    Send(client, new HostLobbyPacket()
                    {
                        LobbyCode = CreateLobby(),
                        Sender = hostName
                    });

                    break;
                }
            case PacketType.JoinLobby:
                {
                    var joinPacket = (JoinLobbyPacket)packet;
                    var response = new JoinedLobbyPacket()
                    {
                        LobbyCode = joinPacket.LobbyCode,
                        Sender = joinPacket.Sender,
                        LobbyData = null
                    };

                    if (!Lobbies.TryGetValue(joinPacket.LobbyCode, out ServerLobbyData? lobbyData))
                    {
                        response.Status = JoinedLobbyStatus.Missing;
                    }
                    else
                    {
                        if (lobbyData.Lobby.CheckIfPlayerExists(joinPacket.Sender))
                        {
                            response.Status = JoinedLobbyStatus.NickCollision;
                        }
                        else
                        {
                            response.Status = JoinedLobbyStatus.Success;
                            response.LobbyData = lobbyData.Lobby;

                            if (lobbyData.HostConnection == null)
                            {
                                lobbyData.HostConnection = client;
                                lobbyData.Lobby.Host.Nickname = joinPacket.Sender;
                            }
                            else if (lobbyData.GuestConnection == null)
                            {
                                lobbyData.GuestConnection = client;
                                lobbyData.Lobby.Guest.Nickname = joinPacket.Sender;
                                Send(lobbyData.HostConnection, joinPacket);
                            }
                            else
                            {
                                response.Status = JoinedLobbyStatus.Full;
                            }
                        }
                    }

                    Send(client, response);

                    break;
                }
            case PacketType.StartLobby:
                {
                    var startPacket = (StartLobbyPacket)packet;

                    if (!Lobbies.TryGetValue(startPacket.LobbyCode, out ServerLobbyData? lobbyData))
                    {
                        Raylib.TraceLog(TraceLogLevel.Warning, $"Failed to start lobby {startPacket.LobbyCode}: lobby does not exist");
                        break;
                    }

                    lobbyData.Start();
                    lobbyData.Broadcast(this, new UpdateLobbyPacket() { LobbyData = lobbyData.Lobby });

                    break;
                }
            default:
                {
                    Raylib.TraceLog(TraceLogLevel.Warning, $"Failed to process packet {packet}: no packet logic of that type");
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

    public void Send(Socket client, Packet packet)
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

    private void UpdateLobbies()
    {
        float delta = 1f / config.Tickrate;

        while (running)
        {
            foreach (var lobby in Lobbies.Values)
            {
                lobby.Update(delta);

                if (lobby.Lobby.Started)
                {
                    lobby.Broadcast(this, new UpdateLobbyPacket()
                    {
                        LobbyData = lobby.Lobby
                    });
                }
            }

            Thread.Sleep((int)(delta * 1000));
        }
    }

    public bool IsRunning() => running;

    public void Shutdown()
    {
        running = false;
    }
}
