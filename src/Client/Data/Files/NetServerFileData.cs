using System.Text.Json.Serialization;

namespace Game.Client.Data.Files;

class NetServerFileData
{
    public string Ip { get; set; } = "127.0.0.1";
    public ushort TcpPort { get; set; } = 2606;
    public ushort UdpPort { get; set; } = 2607;
}

[JsonSerializable(typeof(NetServerFileData))]
partial class NetServerFileDataCtx : JsonSerializerContext;
