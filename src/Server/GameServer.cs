using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Game.Common.Data;
using Game.Common.Packets;
using Game.Common.Enums;
using Game.Server.Data;
using Game.Server.Data.Files;
using Raylib_cs;

namespace Game.Server;

class GameServer
{
    public ConcurrentDictionary<string, ServerLobbyData> Lobbies { get; }
    public ConcurrentDictionary<Guid, ServerClientData> Clients { get; }

    private GameServerConfigFileData config;
    private Dictionary<PoolGamemodeType, PoolGamemodeConfigFileData> gamemodeConfigs;
    private Socket tcpListener;
    private Socket udpListener;
    private Thread? reliableConnectionsThread;
    private Thread? unreliableConnectionsThread;
    private Thread? lobbiesThread;
    private bool running = false;

    public GameServer(GameServerConfigFileData config, Dictionary<PoolGamemodeType, PoolGamemodeConfigFileData> gamemodeConfigs)
    {
        this.config = config;
        this.gamemodeConfigs = gamemodeConfigs;

        tcpListener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        tcpListener.Bind(new IPEndPoint(IPAddress.Any, config.TcpPort));

        udpListener = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        udpListener.Bind(new IPEndPoint(IPAddress.Any, config.UdpPort));

        running = true;

        Clients = new ConcurrentDictionary<Guid, ServerClientData>();
        Lobbies = new ConcurrentDictionary<string, ServerLobbyData>();
    }

    public void Start()
    {
        tcpListener.Listen();

        reliableConnectionsThread = new Thread(new ThreadStart(UpdateReliableConnections))
        {
            IsBackground = false
        };
        reliableConnectionsThread.Start();

        unreliableConnectionsThread = new Thread(new ThreadStart(UpdateUnreliableConnections))
        {
            IsBackground = false
        };
        unreliableConnectionsThread.Start();

        lobbiesThread = new Thread(new ThreadStart(UpdateLobbies))
        {
            IsBackground = true,
        };
        lobbiesThread.Start();
    }

    private string CreateLobby(ServerClientData host)
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
            return CreateLobby(host);
        }

        var svLobby = new ServerLobbyData(code, host, gamemodeConfigs[PoolGamemodeType.Classic], config);
        Lobbies.TryAdd(code, svLobby);

        Raylib.TraceLog(TraceLogLevel.Info, $"Created new lobby [code {code}]");
        return code;
    }

    private void AcceptClient(Socket client)
    {
        client.Blocking = false;
        Guid clientGuid = Guid.NewGuid();
        var clientData = new ServerClientData(client, clientGuid);

        if (!Clients.TryAdd(clientGuid, clientData))
        {
            Raylib.TraceLog(TraceLogLevel.Warning, $"Failed to add client {client.RemoteEndPoint} to clients list");
            return;
        }

        client.Send(clientGuid.ToByteArray());

        Raylib.TraceLog(TraceLogLevel.Info, $"Client connected to server from {client.RemoteEndPoint} [User GUID: {clientGuid}]");
    }

    private void DisconnectClient(ServerClientData clientData)
    {
        if (!Clients.TryRemove(clientData.Guid, out var _))
        {
            Raylib.TraceLog(TraceLogLevel.Warning, $"Failed to remove client {clientData.Guid} from client pool");
            return;
        }

        clientData.TcpConnection.Close();
    }

    private void ProcessPacket(ServerClientData client, Packet packet)
    {
        if (packet is LobbyPacket lobbyPacket)
        {
            Lobbies.TryGetValue(lobbyPacket.LobbyCode, out ServerLobbyData? lobbyData);
            ProcessLobbyPacket(client, lobbyPacket, lobbyData);
            return;
        }

        switch (packet.Type)
        {
            case PacketType.Ping:
                {
                    Send(client, packet);
                    break;
                }
            case PacketType.InitializeUnreliableConnection:
                break;
            default:
                {
                    Raylib.TraceLog(TraceLogLevel.Warning, $"Failed to process packet {packet}: no packet logic of that type");
                    break;
                }
        }
    }

    private void ProcessLobbyPacket(ServerClientData client, LobbyPacket packet, ServerLobbyData? lobby)
    {
        switch (packet.Type)
        {
            case PacketType.HostLobby:
                {
                    var hostPacket = (HostLobbyPacket)packet;
                    string hostName = hostPacket.Sender;
                    Send(client, new HostLobbyPacket()
                    {
                        LobbyCode = CreateLobby(client),
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
                        LobbyData = null,
                        LobbySettings = null
                    };

                    if (lobby == null)
                    {
                        response.Status = JoinedLobbyStatus.Missing;
                    }
                    else
                    {
                        if (lobby.Data.CheckIfPlayerExists(joinPacket.Sender))
                        {
                            response.Status = JoinedLobbyStatus.NickCollision;
                        }
                        else
                        {
                            response.Status = JoinedLobbyStatus.Success;
                            response.LobbyData = lobby.Data;
                            response.LobbySettings = lobby.Settings;

                            if (lobby.Data.Host.Nickname == null)
                            {
                                lobby.HostClient = client;
                                lobby.Data.Host.Nickname = joinPacket.Sender;
                            }
                            else if (lobby.Data.Guest.Nickname == null)
                            {
                                lobby.GuestClient = client;
                                lobby.Data.Guest.Nickname = joinPacket.Sender;
                                Send(lobby.HostClient!, joinPacket);
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

                    if (lobby == null)
                    {
                        Raylib.TraceLog(TraceLogLevel.Warning, $"Failed to start lobby {startPacket.LobbyCode}: lobby does not exist");
                        break;
                    }

                    lobby.Start();

                    break;
                }
            case PacketType.LeaveLobby:
                {
                    if (lobby == null)
                    {
                        Raylib.TraceLog(TraceLogLevel.Warning, $"Failed to leave lobby {packet.LobbyCode}: lobby does not exist");
                        break;
                    }

                    lobby.Broadcast(this, (LeaveLobbyPacket)packet);

                    string winningPlayer = "";
                    if (packet.Sender == lobby.Data.Host.Nickname)
                    {
                        winningPlayer = lobby.Data.Guest.Nickname!;
                        lobby.HostClient = null;
                    }
                    else if (packet.Sender == lobby.Data.Guest.Nickname)
                    {
                        winningPlayer = lobby.Data.Host.Nickname!;
                        lobby.GuestClient = null;

                        if (!lobby.Data.Started)
                        {
                            lobby.Data.Guest = new PlayerLobbyData(null);
                        }
                    }

                    if (lobby.Data.Started)
                    {
                        lobby.EndTurn(PoolGameState.Finished, false);
                        lobby.Data.CurPlayer = winningPlayer;
                    }

                    break;
                }
            case PacketType.UpdatePlayerLobby:
                {
                    var updatePlayerPacket = (UpdatePlayerLobbyPacket)packet;

                    if (lobby == null)
                    {
                        Raylib.TraceLog(TraceLogLevel.Warning, $"Failed to update lobby {updatePlayerPacket.LobbyCode}: lobby does not exist");
                        break;
                    }

                    if (updatePlayerPacket.PlayerData == null)
                        break;

                    var sendingPlayer = lobby.Data.GetPlayerByNick(updatePlayerPacket.Sender);

                    if (sendingPlayer.Nickname == lobby.Data.CurPlayer && lobby.Data.State != PoolGameState.Updating)
                    {
                        sendingPlayer.AimDir = updatePlayerPacket.PlayerData.AimDir;
                        sendingPlayer.CamPos = updatePlayerPacket.PlayerData.CamPos;
                        sendingPlayer.CueForce = updatePlayerPacket.PlayerData.CueForce;
                        sendingPlayer.PlacePos = updatePlayerPacket.PlayerData.PlacePos;
                    }

                    break;
                }
            case PacketType.ShotLobby:
                {
                    var shotPacket = (ShotLobbyPacket)packet;

                    if (lobby == null)
                    {
                        Raylib.TraceLog(TraceLogLevel.Warning, $"Failed to register shot in lobby {shotPacket.LobbyCode}: lobby does not exist");
                        break;
                    }

                    lobby.BeginTurn();

                    break;
                }
            case PacketType.PlaceCueLobby:
                {
                    var placePacket = (PlaceCueLobbyPacket)packet;

                    if (lobby == null)
                    {
                        Raylib.TraceLog(TraceLogLevel.Warning, $"Failed to place cue ball in lobby {placePacket.LobbyCode}: lobby does not exist");
                        break;
                    }

                    if (lobby.Data.CanPlaceCueBall)
                    {
                        lobby.Data.State = PoolGameState.Aiming;
                    }

                    break;
                }
            case PacketType.ChangeLobbySettings:
                {
                    var settingsPacket = (ChangeLobbySettingsPacket)packet;

                    if (lobby == null)
                    {
                        Raylib.TraceLog(TraceLogLevel.Warning, $"Failed to change settings in lobby {settingsPacket.LobbyCode}: lobby does not exist");
                        break;
                    }

                    if (settingsPacket.Settings != null)
                    {
                        lobby.Settings = settingsPacket.Settings;

                        if (lobby.GuestClient != null)
                            Send(lobby.GuestClient, settingsPacket);
                    }

                    break;
                }
            case PacketType.ChatMessageLobby:
                {
                    var chatPacket = (ChatMessageLobbyPacket)packet;

                    if (lobby == null)
                    {
                        Raylib.TraceLog(TraceLogLevel.Warning, $"Failed to process chat message from lobby {chatPacket.LobbyCode}: lobby does not exist");
                        break;
                    }

                    if (lobby.HostClient != null)
                        Send(lobby.HostClient, chatPacket);

                    if (lobby.GuestClient != null)
                        Send(lobby.GuestClient, chatPacket);

                    break;
                }
            default:
                Raylib.TraceLog(TraceLogLevel.Warning, $"Failed to process packet {packet.Type}: no packet processing logic exists");
                break;
        }
    }

    private void ProcessClient(ServerClientData client, int bytesCount, byte[] buf)
    {
        var stream = new MemoryStream(buf, 0, bytesCount);
        var reader = new BinaryReader(stream);
        var packetType = (PacketType)reader.ReadByte();
        var packet = Packet.Create(packetType);
        packet.Deserialize(reader);
        ProcessPacket(client, packet);
    }

    public void Send(ServerClientData client, Packet packet)
    {
        if (packet.SendMode == PacketSendMode.Reliable)
        {
            if (!client.TcpConnection.Connected)
            {
                Raylib.TraceLog(TraceLogLevel.Warning, $"Failed to send reliable packet of type {packet.Type}: client is not connected");
                return;
            }
        }
        else if (packet.SendMode == PacketSendMode.Unreliable)
        {
            if (client.UdpEndPoint == null)
            {
                Raylib.TraceLog(TraceLogLevel.Warning, $"Failed to send unreliable packet of type {packet.Type}: client is not connected");
                return;
            }
        }

        if (!Clients.ContainsKey(client.Guid))
        {
            Raylib.TraceLog(TraceLogLevel.Warning, $"Failed to send packet of type {packet.Type}: client is not initialized");
            return;
        }

        var memStream = new MemoryStream();
        var binStream = new BinaryWriter(memStream);
        packet.Serialize(binStream);
        byte[] buf = memStream.ToArray();

        if (packet.SendMode == PacketSendMode.Reliable)
            client.TcpConnection.Send(buf);
        else if (packet.SendMode == PacketSendMode.Unreliable)
            udpListener.SendTo(buf, client.UdpEndPoint!);
    }

    private void UpdateReliableConnections()
    {
        while (running)
        {
            var readList = new List<Socket>() { tcpListener };
            readList.AddRange(Clients.Values.Select(c => c.TcpConnection));

            Socket.Select(readList, null, null, 100);

            foreach (var socket in readList)
            {
                if (socket == tcpListener)
                {
                    try
                    {
                        AcceptClient(tcpListener.Accept());
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
                        byte[] buf = new byte[GameData.MaxPacketSize];
                        int recvBytes = socket.Receive(buf);

                        if (recvBytes == 0)
                        {
                            var errClientData = Clients.Values.First(c => c.TcpConnection == socket);
                            Raylib.TraceLog(TraceLogLevel.Info, $"Client {errClientData.Guid} disconnected from server");
                            DisconnectClient(errClientData);
                            continue;
                        }

                        var clientGuid = new Guid(buf.AsSpan(0, 16));
                        if (!Clients.TryGetValue(clientGuid, out ServerClientData? clientData))
                        {
                            Raylib.TraceLog(TraceLogLevel.Warning, $"Incoming data from not connected client [guid: {clientGuid}]");
                            continue;
                        }

                        ProcessClient(clientData, recvBytes - 16, buf.Skip(16).ToArray());
                    }
                    catch (SocketException error)
                    {
                        Raylib.TraceLog(TraceLogLevel.Warning, $"Failed to receive msg from client: {error.Message} [ERROR CODE {error.SocketErrorCode}]");

                        var clientData = Clients.Values.First(c => c.TcpConnection == socket);
                        if (error.SocketErrorCode == SocketError.ConnectionReset)
                        {
                            DisconnectClient(clientData);
                        }
                    }
                }
            }
        }
    }

    private void UpdateUnreliableConnections()
    {
        EndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);
        while (running)
        {
            try
            {
                byte[] buf = new byte[GameData.MaxPacketSize];
                int bytesRecv = udpListener.ReceiveFrom(buf, ref remoteEP);
                if (bytesRecv == 0)
                {
                    Raylib.TraceLog(TraceLogLevel.Warning, $"Failed to update unreliable connection {remoteEP}: received 0 bytes");
                    continue;
                }

                var clientGuid = new Guid(buf.AsSpan(0, 16));
                if (!Clients.TryGetValue(clientGuid, out ServerClientData? clientData))
                {
                    Raylib.TraceLog(TraceLogLevel.Info, $"Failed to receive from client {clientGuid}: client is not initialized");
                    continue;
                }

                if (clientData.UdpEndPoint == null)
                {
                    clientData.UdpEndPoint = (IPEndPoint)remoteEP;
                    Raylib.TraceLog(TraceLogLevel.Info, $"Initialized unreliable client {clientGuid} with endpoint {remoteEP}");
                }

                ProcessClient(clientData, bytesRecv, buf.Skip(16).ToArray());
            }
            catch (Exception error)
            {
                Raylib.TraceLog(TraceLogLevel.Warning, $"Failed to update unreliable connections: {error.Message}");
            }
        }
    }

    private void UpdateLobbies()
    {
        float delta = 1f / config.Tickrate;
        int sleepAmount = (int)(delta * 1000);

        while (running)
        {
            foreach (var lobby in Lobbies.Values)
            {
                lobby.Update(delta, this);

                if (lobby.HostClient == null && lobby.GuestClient == null)
                {
                    if (!Lobbies.Remove(lobby.Data.Code, out ServerLobbyData? _))
                        Raylib.TraceLog(TraceLogLevel.Warning, $"Failed to remove lobby {lobby.Data.Code}");
                    else
                        Raylib.TraceLog(TraceLogLevel.Info, $"Lobby {lobby.Data.Code} has decayed");
                }
            }

            Thread.Sleep(sleepAmount);
        }
    }

    public bool IsRunning() => running;

    public void Shutdown()
    {
        running = false;
        udpListener.Close();
    }
}
