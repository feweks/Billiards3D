using Game.Common.Enums;
using LiteNetLib.Utils;

namespace Game.Common.Packets;

class ChatMessageLobbyPacket : LobbyPacket
{
    public string? Content { get; set; }

    public ChatMessageLobbyPacket() : base(PacketType.ChatMessageLobby) { }

    public override void Serialize(NetDataWriter writer)
    {
        base.Serialize(writer);

        writer.Put(Content != null);
        if (Content != null)
            writer.Put(Content);
    }

    public override void Deserialize(NetDataReader reader)
    {
        base.Deserialize(reader);

        bool valid = reader.GetBool();
        if (valid)
            Content = reader.GetString();
    }
}
