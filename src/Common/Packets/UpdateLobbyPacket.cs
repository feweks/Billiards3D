using System.Diagnostics.CodeAnalysis;
using Game.Common.Data;

namespace Game.Common.Packets;

class UpdateLobbyPacket : LobbyPacket
{
    public GameLobbyData? LobbyData { get; set; }

    [SetsRequiredMembers]
    public UpdateLobbyPacket() : base(PacketType.UpdateLobby) { }

    public override void Serialize(BinaryWriter writer)
    {
        base.Serialize(writer);

        LobbyData!.Serialize(writer);
    }

    public override void Deserialize(BinaryReader reader)
    {
        base.Deserialize(reader);

        LobbyData = new GameLobbyData(LobbyCode);
        LobbyData.Deserialize(reader);
    }
}
