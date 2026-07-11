using System.Text.Json.Serialization;

namespace Game.Server.Data.Files;

class GameServerConfigFileData
{
    public int Port { get; set; } = 2606;
    public int Tickrate { get; set; } = 128;
}

[JsonSerializable(typeof(GameServerConfigFileData))]
[JsonSourceGenerationOptions(IncludeFields = true, WriteIndented = true)]
partial class GameServerConfigFileDataCtx : JsonSerializerContext;
