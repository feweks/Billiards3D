using System.Collections.Concurrent;
using System.Text;
using Game.Common.Data;
using Game.Common.Packets;
using Game.Common.Enums;
using Game.Server.Data;
using Game.Server.Data.Files;
using Raylib_cs;
using LiteNetLib;
using LiteNetLib.Utils;

namespace Game.Server;

class GameServer
{
    public ConcurrentDictionary<string, ServerLobbyData> Lobbies { get; }

    private GameServerConfigFileData config;
    private Dictionary<PoolGamemodeType, PoolGamemodeConfigFileData> gamemodeConfigs;

    private EventBasedLiteNetListener listener;
    private LiteNetManager server;

    private Thread? updateLobbiesThread;

    public GameServer(GameServerConfigFileData config, Dictionary<PoolGamemodeType, PoolGamemodeConfigFileData> gamemodeConfigs)
    {
        this.config = config;
        this.gamemodeConfigs = gamemodeConfigs;

        listener = new EventBasedLiteNetListener();
        server = new LiteNetManager(listener);
        listener.ConnectionRequestEvent += ProcessPeerRequest;
        listener.PeerConnectedEvent += ProcessPeerConnection;
        listener.NetworkReceiveEvent += ProcessEvent;

        Lobbies = new ConcurrentDictionary<string, ServerLobbyData>();
    }

    public void Start()
    {
        server.Start(config.Port);

        updateLobbiesThread = new Thread(new ThreadStart(UpdateLobbies))
        {
            IsBackground = true,
        };
        updateLobbiesThread.Start();
    }

    private string CreateLobby(LiteNetPeer hostPeer)
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
            return CreateLobby(hostPeer);
        }

        var svLobby = new ServerLobbyData(code, hostPeer, gamemodeConfigs[PoolGamemodeType.Classic], config);
        Lobbies.TryAdd(code, svLobby);

        Raylib.TraceLog(TraceLogLevel.Info, $"Created new lobby [code {code}]");
        return code;
    }

    private void ProcessPacket(LiteNetPeer clientPeer, Packet packet)
    {
        if (packet is LobbyPacket lobbyPacket)
        {
            Lobbies.TryGetValue(lobbyPacket.LobbyCode, out ServerLobbyData? lobbyData);
            ProcessLobbyPacket(clientPeer, lobbyPacket, lobbyData);
            return;
        }

        switch (packet.Type)
        {
            default:
                {
                    Raylib.TraceLog(TraceLogLevel.Warning, $"Failed to process packet {packet}: no packet logic of that type");
                    break;
                }
        }
    }

    private void ProcessLobbyPacket(LiteNetPeer clientPeer, LobbyPacket packet, ServerLobbyData? lobby)
    {
        switch (packet.Type)
        {
            case PacketType.HostLobby:
                {
                    var hostPacket = (HostLobbyPacket)packet;
                    string hostName = hostPacket.Sender;
                    Send(clientPeer, new HostLobbyPacket()
                    {
                        LobbyCode = CreateLobby(clientPeer),
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
                                lobby.HostPeer = clientPeer;
                                lobby.Data.Host.Nickname = joinPacket.Sender;
                            }
                            else if (lobby.Data.Guest.Nickname == null)
                            {
                                lobby.GuestPeer = clientPeer;
                                lobby.Data.Guest.Nickname = joinPacket.Sender;
                                Send(lobby.HostPeer!, joinPacket);
                            }
                            else
                            {
                                response.Status = JoinedLobbyStatus.Full;
                            }
                        }
                    }

                    Send(clientPeer, response);
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
                        lobby.HostPeer = null;
                    }
                    else if (packet.Sender == lobby.Data.Guest.Nickname)
                    {
                        winningPlayer = lobby.Data.Host.Nickname!;
                        lobby.GuestPeer = null;

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

                        if (lobby.GuestPeer != null)
                            Send(lobby.GuestPeer, settingsPacket);
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

                    if (lobby.HostPeer != null)
                        Send(lobby.HostPeer, chatPacket);

                    if (lobby.GuestPeer != null)
                        Send(lobby.GuestPeer, chatPacket);

                    break;
                }
            default:
                Raylib.TraceLog(TraceLogLevel.Warning, $"Failed to process packet {packet.Type}: no packet processing logic exists");
                break;
        }
    }

    private void ProcessPeerRequest(LiteConnectionRequest request) => request.AcceptIfKey(GameData.NetConnectionKey);

    private void ProcessPeerConnection(LiteNetPeer peer)
    {
        Raylib.TraceLog(TraceLogLevel.Info, $"New peer connected to server [{peer.Address}:{peer.Port}, {peer.Id}]");
    }

    private void ProcessEvent(LiteNetPeer peer, NetPacketReader reader, DeliveryMethod deliveryMethod)
    {
        var packetType = (PacketType)reader.GetByte();
        var packet = Packet.Create(packetType);
        packet.Deserialize(reader);
        ProcessPacket(peer, packet);
        reader.Recycle();
    }

    public void Send(LiteNetPeer clientPeer, Packet packet)
    {
        if (clientPeer.ConnectionState != ConnectionState.Connected)
        {
            Raylib.TraceLog(TraceLogLevel.Warning, $"Failed to send packet of type {packet} to network peer {clientPeer.Address}: peer is not connected");
            return;
        }

        var packetWriter = new NetDataWriter();
        packet.Serialize(packetWriter);

        clientPeer.Send(packetWriter, packet.SendMode == PacketSendMode.Reliable ? DeliveryMethod.ReliableOrdered : DeliveryMethod.ReliableUnordered);
    }

    private void UpdateLobbies()
    {
        float delta = 1f / config.Tickrate;
        int sleepAmount = (int)(delta * 1000);

        while (IsRunning())
        {
            server.PollEvents();

            foreach (var lobby in Lobbies.Values)
            {
                lobby.Update(delta, this);

                if (lobby.HostPeer == null && lobby.GuestPeer == null)
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

    public List<LiteNetPeer> GetClients()
    {
        var clients = new List<LiteNetPeer>();
        server.GetConnectedPeers(clients);

        return clients;
    }

    public bool IsRunning() => server.IsRunning;

    public void Shutdown()
    {
        server.Stop();
    }
}
