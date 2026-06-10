using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using Game.Client.Data.Files;
using Game.Common.Enums;
using Game.Common.Data;
using Game.Common.Packets;
using Raylib_cs;
using System.Text;

namespace Game.Client.Net;

static class GameClient
{
    private const float MAX_LATENCY_TIMER = 1f;

    public static double Latency { get; internal set; } = 0;
    public static ClientLobby Lobby { get; internal set; } = new ClientLobby();
    public static NetServerFileData? Config { get; internal set; }
    public static Guid ClientGuid { get; internal set; } = Guid.Empty;

    private static TcpClient? tcpClient;
    private static UdpClient? udpClient;
    private static NetworkStream? tcpStream;
    private static Thread? tcpRecieveThread;
    private static Thread? udpRecieveThread;
    private static bool running = false;
    private static Stopwatch latencyStopwatch = new Stopwatch();
    private static float latencyTimer = 0f;

    public static void Init()
    {
        Config = Resources.GetJson("resources/data/net_config.json", NetServerFileDataCtx.Default.NetServerFileData);

        IPAddress ip = IPAddress.Parse("127.0.0.1");
        if (IPAddress.TryParse(Config.Ip, out IPAddress? parsedAddr))
        {
            ip = parsedAddr;
        }

        tcpClient = new TcpClient();
        tcpClient.Connect(new IPEndPoint(ip, Config.TcpPort));
        tcpStream = tcpClient.GetStream();

        udpClient = new UdpClient();
        udpClient.Connect(new IPEndPoint(ip, Config.UdpPort));

        tcpRecieveThread = new Thread(new ThreadStart(UpdateReliableConnection))
        {
            IsBackground = false,
        };
        tcpRecieveThread.Start();

        udpRecieveThread = new Thread(new ThreadStart(UpdateUnreliableConnection))
        {
            IsBackground = false,
        };
        udpRecieveThread.Start();

        running = true;

        Raylib.TraceLog(TraceLogLevel.Info, "[NET CLIENT] Connected to server");
    }

    public static void HostLobby(string nick)
    {
        if (!CheckConnection())
        {
            Raylib.TraceLog(TraceLogLevel.Warning, $"[NET CLIENT] Failed to host lobby: client is not connected");
            return;
        }

        SendPacket(new HostLobbyPacket() { Sender = nick });
    }

    public static void JoinLobby(string code, string nick)
    {
        if (!CheckConnection())
        {
            Raylib.TraceLog(TraceLogLevel.Warning, $"[NET CLIENT] Failed to join lobby: client is not connected");
            return;
        }

        SendPacket(new JoinLobbyPacket()
        {
            LobbyCode = code,
            Sender = nick
        });
    }

    public static void StartLobby()
    {
        if (!IsHost())
        {
            Raylib.TraceLog(TraceLogLevel.Warning, $"[NET CLIENT] Failed to start lobby: client is not host");
            return;
        }

        SendLobbyPacket(new StartLobbyPacket());
    }

    public static void LeaveLobby() => SendLobbyPacket(new LeaveLobbyPacket());

    public static void SendPacket(Packet packet)
    {
        if (!CheckConnection())
        {
            Raylib.TraceLog(TraceLogLevel.Warning, $"Failed to send packet {packet.Type}: client is not connected");
            return;
        }

        if (ClientGuid == Guid.Empty)
        {
            Raylib.TraceLog(TraceLogLevel.Warning, $"Failed to send packet {packet.Type}: client is not registered on the server");
            return;
        }

        packet.SenderGuid = ClientGuid;

        var packetStream = new MemoryStream();
        var packetWriter = new BinaryWriter(packetStream);
        packet.Serialize(packetWriter);
        byte[] buf = packetStream.ToArray();

        if (packet.SendMode == PacketSendMode.Reliable)
            tcpStream!.Write(buf);
        else if (packet.SendMode == PacketSendMode.Unreliable)
            udpClient!.Send(buf);
    }

    public static void SendLobbyPacket(LobbyPacket packet)
    {
        if (!CheckConnection())
        {
            Raylib.TraceLog(TraceLogLevel.Warning, $"Failed to send lobby packet {packet.Type}: client is not connected");
            return;
        }

        if (!Lobby.IsConnected() || Lobby.PlayerNick == null)
        {
            Raylib.TraceLog(TraceLogLevel.Warning, $"Failed to send lobby packet {packet.Type}: client is not connected to any lobby");
            return;
        }

        packet.LobbyCode = Lobby.Data!.Code;
        packet.Sender = Lobby.PlayerNick;

        SendPacket(packet);
    }

    private static void ProcessPacket(Packet packet)
    {
        if (!CheckConnection())
        {
            Raylib.TraceLog(TraceLogLevel.Warning, $"[NET CLIENT] Cannot process packet {packet.Type}: client is not connected");
            return;
        }

        switch (packet.Type)
        {
            case PacketType.Ping:
                {
                    latencyStopwatch.Stop();
                    Latency = latencyStopwatch.Elapsed.TotalMilliseconds;
                    latencyStopwatch.Reset();

                    break;
                }
            case PacketType.HostLobby:
                {
                    var hostPacket = (HostLobbyPacket)packet;
                    Raylib.TraceLog(TraceLogLevel.Info, $"[NET CLIENT] Server created lobby [code: {hostPacket.LobbyCode}]");
                    JoinLobby(hostPacket.LobbyCode, hostPacket.Sender);

                    break;
                }
            case PacketType.JoinLobby:
                {
                    if (!Lobby.IsConnected())
                        break;

                    var joinPacket = (JoinLobbyPacket)packet;
                    Raylib.TraceLog(TraceLogLevel.Info, $"[NET CLIENT] Player joined lobby: {joinPacket.Sender}");

                    Lobby.Data!.Guest.Nickname = joinPacket.Sender;

                    break;
                }
            case PacketType.JoinedLobby:
                {
                    var joinedPacket = (JoinedLobbyPacket)packet;

                    if (joinedPacket.Status == JoinedLobbyStatus.Success)
                    {
                        Raylib.TraceLog(TraceLogLevel.Info, $"[NET CLIENT] Joined lobby {joinedPacket.LobbyCode}");
                        Lobby.Join(joinedPacket.LobbyData!, joinedPacket.LobbySettings!, joinedPacket.Sender);
                        break;
                    }
                    else
                    {
                        Lobby.Status = joinedPacket.Status;
                    }

                    Raylib.TraceLog(TraceLogLevel.Warning, $"[NET CLIENT] Failed to join lobby {joinedPacket.LobbyCode}: {joinedPacket.Status}");

                    break;
                }
            case PacketType.UpdateLobby:
                {
                    var updatePacket = (UpdateLobbyPacket)packet;
                    Lobby.Data = updatePacket.LobbyData;

                    break;
                }
            case PacketType.LeaveLobby:
                {
                    var leavePacket = (LeaveLobbyPacket)packet;
                    if (!Lobby.IsConnected())
                        break;

                    Raylib.TraceLog(TraceLogLevel.Info, $"[NET CLIENT] Player {leavePacket.Sender} left the lobby");

                    if (leavePacket.Sender == Lobby.PlayerNick)
                    {
                        Lobby.Status = JoinedLobbyStatus.None;
                        Lobby.PlayerNick = null;

                        if (!Lobby.Data!.Started)
                        {
                            Lobby.Reset();
                        }
                    }
                    else
                    {
                        if (!Lobby.Data!.Started)
                        {
                            if (IsHost())
                            {
                                Lobby.Data.Guest = new PlayerLobbyData(null);
                            }
                            else
                            {
                                LeaveLobby();
                            }
                        }
                    }

                    break;
                }
            case PacketType.ChangeLobbySettings:
                {
                    var settingsPacket = (ChangeLobbySettingsPacket)packet;
                    Raylib.TraceLog(TraceLogLevel.Info, $"[NET CLIENT] Changed lobby settings");

                    Lobby.Settings = settingsPacket.Settings;

                    break;
                }
            case PacketType.ChatMessageLobby:
                {
                    var chatPacket = (ChatMessageLobbyPacket)packet;
                    if (!Lobby.IsConnected())
                        break;

                    Raylib.TraceLog(TraceLogLevel.Info, $"[NET CLIENT] player {chatPacket.Sender} said {chatPacket.Content}");
                    Lobby.ChatHistory.Add(new ClientChatMessage(chatPacket));

                    break;
                }
        }
    }

    private static void ProcessData(byte[] buf, int bytesCount)
    {
        if (bytesCount == 0)
        {
            Raylib.TraceLog(TraceLogLevel.Info, "[NET CLIENT] Disconnected from the server");
            running = false;
            return;
        }

        var memStream = new MemoryStream(buf, 0, bytesCount);
        var binReader = new BinaryReader(memStream);
        binReader.ReadBytes(16); // skip first guid
        var packetType = (PacketType)binReader.ReadByte();
        var packet = Packet.Create(packetType);
        packet.Deserialize(binReader);
        ProcessPacket(packet);
    }

    private static void UpdateReliableConnection()
    {
        while (running)
        {
            try
            {
                byte[] buf = new byte[GameData.MaxPacketSize];
                int bytesRecv = tcpStream!.Read(buf, 0, buf.Length);
                if (bytesRecv == 16 && ClientGuid == Guid.Empty)
                {
                    ClientGuid = new Guid(buf.AsSpan(0, bytesRecv));
                    Raylib.TraceLog(TraceLogLevel.Info, $"[NET CLIENT] Client guid is {ClientGuid}");
                    SendPacket(new InitializeUnreliableConnectionPacket());
                    continue;
                }

                if (bytesRecv < 16)
                {
                    Raylib.TraceLog(TraceLogLevel.Warning, $"Recieved data from reliable connection is not in correct format");
                    Shutdown();
                }

                ProcessData(buf, bytesRecv);
            }
            catch (Exception error)
            {
                Shutdown();
                Raylib.TraceLog(TraceLogLevel.Warning, $"[NET CLIENT] Error recv from server: {error.Message}");
            }
        }
    }

    private static void UpdateUnreliableConnection()
    {
        IPEndPoint serverEP = new IPEndPoint(IPAddress.Any, 0);

        while (running)
        {
            try
            {
                byte[] buf = udpClient!.Receive(ref serverEP);
                int bytesRecv = buf.Length;

                if (bytesRecv <= 0)
                    continue;

                if (bytesRecv < 16)
                {
                    Raylib.TraceLog(TraceLogLevel.Warning, $"Recieved data from unreliable connection is not in correct format");
                    Shutdown();
                }

                ProcessData(buf, bytesRecv);
            }
            catch (Exception error)
            {
                Raylib.TraceLog(TraceLogLevel.Warning, $"[NET CLIENT] Error recv from unreliable server: {error.Message}");
                return;
            }
        }
    }

    public static void Update(float dt)
    {
        if (!CheckConnection())
            return;

        latencyTimer += dt;
        if (latencyTimer > MAX_LATENCY_TIMER && !latencyStopwatch.IsRunning)
        {
            latencyTimer = 0;
            SendPacket(new PingPacket());
            latencyStopwatch.Start();
        }
    }

    public static PlayerLobbyData GetSelfPlayer() => Lobby.Data!.Host.Nickname == Lobby.PlayerNick ? Lobby.Data.Host : Lobby.Data.Guest;

    public static bool IsHost() => Lobby.Data != null && Lobby.Data.Host.Nickname == Lobby.PlayerNick;

    public static bool CheckConnection() => tcpClient != null && tcpClient.Connected;

    public static void Shutdown()
    {
        running = false;
        if (CheckConnection())
        {
            if (Lobby.Data != null && Lobby.PlayerNick != null)
            {
                LeaveLobby();
                Raylib.TraceLog(TraceLogLevel.Info, $"leave lobby on shutdown");
            }

            tcpClient!.Close();
            udpClient!.Close();
        }
    }
}
