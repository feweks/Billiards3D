using System.Net;
using System.Net.Sockets;
using System.Text;
using Game.Client.Data;
using Game.Client.Data.Files;
using Game.Common;
using Game.Common.Data;
using Game.Common.Packets;
using Raylib_cs;

namespace Game.Client.Net;

static class GameClient
{
    private const int MAX_PACKET_SIZE = 1024;

    public static GameLobbyData? LobbyData { get; internal set; }
    public static JoinedLobbyStatus LobbyStatus { get; internal set; } = JoinedLobbyStatus.None;
    public static string? PlayerNick { get; internal set; } = null;

    private static NetServerFileData? config;
    private static TcpClient? client;
    private static NetworkStream? stream;
    private static bool running = false;
    private static Thread? receiveThread;

    public static void Init()
    {
        config = Resources.GetJson("resources/data/net_config.json", NetServerFileDataCtx.Default.NetServerFileData);

        IPAddress ip = IPAddress.Parse("127.0.0.1");
        if (IPAddress.TryParse(config.Ip, out IPAddress? parsedAddr))
        {
            ip = parsedAddr;
        }

        client = new TcpClient();
        client.Connect(new IPEndPoint(ip, config.Port));
        stream = client.GetStream();

        receiveThread = new Thread(new ThreadStart(UpdateConnection))
        {
            IsBackground = false,
        };
        receiveThread.Start();
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

        Send(new HostLobbyPacket()
        {
            Sender = nick
        });
    }

    public static void JoinLobby(string code, string nick)
    {
        if (!CheckConnection())
        {
            Raylib.TraceLog(TraceLogLevel.Warning, $"[NET CLIENT] Failed to join lobby: client is not connected");
            return;
        }

        Send(new JoinLobbyPacket()
        {
            LobbyCode = code,
            Sender = nick
        });
    }

    public static void Send(Packet packet)
    {
        if (!CheckConnection())
        {
            Raylib.TraceLog(TraceLogLevel.Warning, $"Failed to send packet {packet.Type}: client is not connected");
            return;
        }

        var packetStream = new MemoryStream();
        var packetWriter = new BinaryWriter(packetStream);
        packet.Serialize(packetWriter);

        stream!.Write(packetStream.ToArray());
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
            case PacketType.HostLobby:
                {
                    var hostPacket = (HostLobbyPacket)packet;
                    Raylib.TraceLog(TraceLogLevel.Info, $"[NET CLIENT] Server created lobby [code: {hostPacket.LobbyCode}]");
                    JoinLobby(hostPacket.LobbyCode, hostPacket.Sender);

                    break;
                }
            case PacketType.JoinLobby:
                {
                    if (LobbyData == null)
                        break;

                    var joinPacket = (JoinLobbyPacket)packet;
                    Raylib.TraceLog(TraceLogLevel.Info, $"[NET CLIENT] Player joined lobby: {joinPacket.Sender}");

                    LobbyData.Guest.Nickname = joinPacket.Sender;

                    break;
                }
            case PacketType.JoinedLobby:
                {
                    var joinedPacket = (JoinedLobbyPacket)packet;
                    LobbyStatus = joinedPacket.Status;

                    if (joinedPacket.Status == JoinedLobbyStatus.Success)
                    {
                        Raylib.TraceLog(TraceLogLevel.Info, $"[NET CLIENT] Joined lobby {joinedPacket.LobbyCode}");
                        LobbyData = joinedPacket.LobbyData;
                        PlayerNick = joinedPacket.Sender;
                        break;
                    }

                    Raylib.TraceLog(TraceLogLevel.Warning, $"[NET CLIENT] Failed to join lobby {joinedPacket.LobbyCode}: {joinedPacket.Status}");

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

        var memStream = new MemoryStream(buf);
        var binReader = new BinaryReader(memStream);
        var packetType = (PacketType)binReader.ReadByte();
        var packet = Packet.Create(packetType);
        packet.Deserialize(binReader);
        ProcessPacket(packet);
    }

    private static void UpdateConnection()
    {
        while (running)
        {
            try
            {
                byte[] buf = new byte[MAX_PACKET_SIZE];
                int bytesRecieved = stream!.Read(buf, 0, buf.Length);
                ProcessData(buf, bytesRecieved);
            }
            catch (Exception error)
            {
                Shutdown();
                Raylib.TraceLog(TraceLogLevel.Warning, $"[NET CLIENT] Error recv from server: {error.Message}");
            }
        }
    }

    public static bool IsHost() => LobbyData != null && LobbyData.Host.Nickname == PlayerNick;

    public static bool CheckConnection() => client != null && client.Connected;

    public static void Shutdown()
    {
        running = false;
        if (CheckConnection())
        {
            client!.Close();
        }
    }
}
