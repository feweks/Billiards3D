using Raylib_cs;

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
            default:
                Raylib.TraceLog(TraceLogLevel.Warning, $"Failed to create packet of type {type}");
                return new Packet(PacketType.Ping);
        }
    }

    public PacketType Type { get; }

    public Packet(PacketType type)
    {
        Type = type;
    }

    public virtual void Serialize(BinaryWriter writer)
    {
        writer.Write((byte)Type);
    }

    public virtual void Deserialize(BinaryReader reader)
    {

    }
}
