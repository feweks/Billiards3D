using System.Diagnostics.CodeAnalysis;
using Game.Common.Data;

namespace Game.Common.Packets;

enum JoinedLobbyStatus : byte
{
    None = 0,
    Success,
    NickCollision,
    Missing,
    Full
}

class JoinedLobbyPacket : LobbyPacket
{
    public required JoinedLobbyStatus Status { get; set; }
    public GameLobbyData? LobbyData { get; set; } = null;

    [SetsRequiredMembers]
    public JoinedLobbyPacket() : base(PacketType.JoinedLobby) { }

    public override void Serialize(BinaryWriter writer)
    {
        base.Serialize(writer);

        writer.Write((byte)Status);
        if (Status == JoinedLobbyStatus.Success && LobbyData != null)
        {
            LobbyData.Serialize(writer);
        }
    }

    public override void Deserialize(BinaryReader reader)
    {
        base.Deserialize(reader);

        Status = (JoinedLobbyStatus)reader.ReadByte();
        if (Status == JoinedLobbyStatus.Success)
        {
            LobbyData = new GameLobbyData(string.Empty);
            LobbyData.Deserialize(reader);
        }
    }
}
