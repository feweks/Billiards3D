using System.Text.Json.Serialization;

namespace Game.Client.Data.Files;

class NetServerFileData
{
    public string Ip { get; set; } = "127.0.0.1";
    public ushort Port { get; set; } = 2606;
}

[JsonSerializable(typeof(NetServerFileData))]
partial class NetServerFileDataCtx : JsonSerializerContext;
