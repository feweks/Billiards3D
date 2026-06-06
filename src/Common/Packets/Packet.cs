using Raylib_cs;
using Game.Common.Enums;

namespace Game.Common.Packets;

class Packet
{
    public static Packet Create(PacketType type)
    {
        switch (type)
        {
            case PacketType.Ping:
                return new PingPacket();
            case PacketType.HostLobby:
                return new HostLobbyPacket();
            case PacketType.JoinLobby:
                return new JoinLobbyPacket();
            case PacketType.JoinedLobby:
                return new JoinedLobbyPacket();
            case PacketType.StartLobby:
                return new StartLobbyPacket();
            case PacketType.UpdateLobby:
                return new UpdateLobbyPacket();
            case PacketType.UpdatePlayerLobby:
                return new UpdatePlayerLobbyPacket();
            case PacketType.ShotLobby:
                return new ShotLobbyPacket();
            case PacketType.PlaceCueLobby:
                return new PlaceCueLobbyPacket();
            case PacketType.LeaveLobby:
                return new LeaveLobbyPacket();
            case PacketType.ChangeLobbySettings:
                return new ChangeLobbySettingsPacket();
            case PacketType.ChatMessageLobby:
                return new ChatMessageLobbyPacket();
            case PacketType.InitializeUnreliableConnection:
                return new InitializeUnreliableConnectionPacket();
            default:
                Raylib.TraceLog(TraceLogLevel.Warning, $"Failed to create packet of type {type}");
                return new Packet(PacketType.Ping);
        }
    }

    public Guid SenderGuid { get; set; }
    public PacketType Type { get; }
    public PacketSendMode SendMode { get; }

    public Packet(PacketType type, PacketSendMode sendMode = PacketSendMode.Reliable)
    {
        Type = type;
        SendMode = sendMode;
    }

    public virtual void Serialize(BinaryWriter writer)
    {
        writer.Write(SenderGuid.ToByteArray());
        writer.Write((byte)Type);
    }

    public virtual void Deserialize(BinaryReader reader)
    {

    }
}
