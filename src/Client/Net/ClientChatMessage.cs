using Game.Common.Packets;

namespace Game.Client.Net;

class ClientChatMessage
{
    public string Sender { get; set; }
    public string Content { get; set; }

    public ClientChatMessage(ChatMessageLobbyPacket packet)
    {
        Sender = packet.Sender;
        Content = packet.Content!;
    }
}
