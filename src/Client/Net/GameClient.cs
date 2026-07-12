using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using Game.Client.Data.Files;
using Game.Common.Enums;
using Game.Common.Data;
using Game.Common.Packets;
using Raylib_cs;
using Game.Client.Managers;
using LiteNetLib;
using LiteNetLib.Utils;

namespace Game.Client.Net;

static class GameClient
{
    public static double Latency { get; internal set; } = 0;
    public static ClientLobby Lobby { get; internal set; } = new ClientLobby();
    public static NetServerFileData? Config { get; internal set; }

    private static LiteNetManager? client;
    private static EventBasedLiteNetListener? listener;
    private static LiteNetPeer? serverPeer;

    public static void Init()
    {
        Config = ResourcesManager.GetJson("resources/data/net_config.json", NetServerFileDataCtx.Default.NetServerFileData);

        IPAddress ip = IPAddress.Parse("127.0.0.1");
        if (IPAddress.TryParse(Config.Ip, out IPAddress? parsedAddr))
            ip = parsedAddr;

        try
        {
            listener = new EventBasedLiteNetListener();
            client = new LiteNetManager(listener);
            client.Start();
            serverPeer = client.Connect(new IPEndPoint(ip, Config.Port), GameData.NetConnectionKey);
            listener.NetworkReceiveEvent += ProcessEvent;
            listener.PeerConnectedEvent += (peer) =>
            {
                Raylib.TraceLog(TraceLogLevel.Info, $"[NET CLIENT] Succesfully connected to server");
            };
        }
        catch (Exception error)
        {
            Raylib.TraceLog(TraceLogLevel.Warning, $"[NET CLIENT]: Failed to connect to server [error: {error.Message}]");
            Reset();
        }
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

    public static void LeaveLobby() => SendLobbyPacket(new LeaveLobbyPacket() { Reason = DisconnectReason.DisconnectPeerCalled });

    public static void SendPacket(Packet packet)
    {
        if (!CheckConnection())
        {
            Raylib.TraceLog(TraceLogLevel.Warning, $"Failed to send packet {packet.Type}: client is not connected");
            return;
        }

        var packetWriter = new NetDataWriter();
        packet.Serialize(packetWriter);
        serverPeer!.Send(packetWriter, packet.SendMode == PacketSendMode.Reliable ? DeliveryMethod.ReliableOrdered : DeliveryMethod.Unreliable);
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

    private static void ProcessEvent(LiteNetPeer peer, NetPacketReader reader, DeliveryMethod deliveryMethod)
    {
        var packetType = (PacketType)reader.GetByte();
        var packet = Packet.Create(packetType);
        packet.Deserialize(reader);
        ProcessPacket(packet);
        reader.Recycle();
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

                    Raylib.TraceLog(TraceLogLevel.Info, $"[NET CLIENT] Player {leavePacket.Sender} left the lobby [reason: {leavePacket.Reason}]");

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

    public static void Update(float dt)
    {
        if (!CheckConnection())
            return;

        Latency = serverPeer!.Ping;
        client!.PollEvents();
    }

    public static PlayerLobbyData GetSelfPlayer() => Lobby.Data!.Host.Nickname == Lobby.PlayerNick ? Lobby.Data.Host : Lobby.Data.Guest;

    public static bool IsHost() => Lobby.Data != null && Lobby.Data.Host.Nickname == Lobby.PlayerNick;

    public static bool CheckConnection() => client != null && client.IsRunning && serverPeer != null && serverPeer.ConnectionState == ConnectionState.Connected;

    public static void Shutdown()
    {
        if (CheckConnection())
        {
            if (Lobby.Data != null && Lobby.PlayerNick != null)
            {
                LeaveLobby();
                Thread.Sleep(500);
                Raylib.TraceLog(TraceLogLevel.Info, $"Left lobby on shutdown");
            }

            client!.Stop();

            Reset();
        }
    }

    private static void Reset()
    {
        Latency = 0;
        Lobby = new ClientLobby();
        Config = null;
    }
}
