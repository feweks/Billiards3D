using Game.Common.Enums;

namespace Game.Common.Packets;

class ChatMessageLobbyPacket : LobbyPacket
{
    public string? Content { get; set; }

    public ChatMessageLobbyPacket() : base(PacketType.ChatMessageLobby) { }

    public override void Serialize(BinaryWriter writer)
    {
        base.Serialize(writer);

        writer.Write(Content != null);
        if (Content != null)
            writer.Write(Content);
    }

    public override void Deserialize(BinaryReader reader)
    {
        base.Deserialize(reader);

        bool valid = reader.ReadBoolean();
        if (valid)
            Content = reader.ReadString();
    }
}
