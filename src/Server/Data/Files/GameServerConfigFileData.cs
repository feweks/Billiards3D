using System.Text.Json.Serialization;

namespace Game.Server.Data.Files;

class GameServerConfigFileData
{
    public int TcpPort { get; set; } = 2606;
    public int UdpPort { get; set; } = 2607;
    public int Tickrate { get; set; } = 128;
}

[JsonSerializable(typeof(GameServerConfigFileData))]
[JsonSourceGenerationOptions(IncludeFields = true, WriteIndented = true)]
partial class GameServerConfigFileDataCtx : JsonSerializerContext;
