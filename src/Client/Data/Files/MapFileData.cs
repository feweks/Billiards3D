using System.Numerics;
using System.Text.Json.Serialization;

namespace Game.Client.Data.Files;

class MapFileData
{
    public Vector3 AmbientColor;
    public List<MapLightFileData> Lights { get; set; } = [];
    public List<MapModelFileData> Models { get; set; } = [];
    public List<MapBillboardFileData> Billboards { get; set; } = [];
}

[JsonSerializable(typeof(MapFileData))]
[JsonSourceGenerationOptions(IncludeFields = true, WriteIndented = true, PropertyNameCaseInsensitive = true)]
partial class MapFileDataCtx : JsonSerializerContext;
