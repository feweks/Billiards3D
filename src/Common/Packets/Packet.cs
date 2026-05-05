using Raylib_cs;

namespace Game.Common.Packets;

class Packet
{
    public static Packet Create(PacketType type)
    {
        switch (type)
        {
            case PacketType.HostLobby:
                return new HostLobbyPacket();
            case PacketType.JoinLobby:
                return new JoinLobbyPacket();
            case PacketType.JoinedLobby:
                return new JoinedLobbyPacket();
            default:
                Raylib.TraceLog(TraceLogLevel.Warning, $"Failed to create packet of type {type}");
                return new Packet(PacketType.HostLobby);
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
