using System.Text.Json.Serialization;

namespace Game.Server.Data;

class GameServerConfig
{
    public int Port { get; set; } = 2606;
    public int Tickrate { get; set; } = 30;
}

[JsonSerializable(typeof(GameServerConfig))]
[JsonSourceGenerationOptions(IncludeFields = true, WriteIndented = true)]
partial class GameServerConfigCtx : JsonSerializerContext;
