using System.Diagnostics.CodeAnalysis;
using Game.Common.Data;

namespace Game.Common.Packets;

class UpdatePlayerLobbyPacket : LobbyPacket
{
    public PlayerLobbyData? PlayerData { get; set; }

    [SetsRequiredMembers]
    public UpdatePlayerLobbyPacket() : base(PacketType.UpdatePlayerLobby) { }

    public override void Serialize(BinaryWriter writer)
    {
        base.Serialize(writer);

        PlayerData!.Serialize(writer);
    }

    public override void Deserialize(BinaryReader reader)
    {
        base.Deserialize(reader);

        PlayerData = new PlayerLobbyData(null);
        PlayerData.Deserialize(reader);
    }
}
