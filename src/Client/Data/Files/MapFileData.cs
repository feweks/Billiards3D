using System.Text.Json.Serialization;

namespace Game.Client.Data.Files;

class MapFileData
{
    public List<MapEntityFileData> Entities { get; set; } = [];
}

[JsonSerializable(typeof(MapFileData))]
[JsonSourceGenerationOptions(IncludeFields = true, WriteIndented = true)]
partial class MapFileDataCtx : JsonSerializerContext;
