using System.Numerics;
using System.Text.Json.Serialization;
using Game.Client.Entities;

namespace Game.Client.Data.Files;

class MapModelFileData : MapEntityFileData
{
    public string? Path { get; set; }
    public bool Culling { get; set; }
    public bool CastsShadow { get; set; }
    public bool ApplyLighting { get; set; }

    [JsonConstructor]
    public MapModelFileData(Vector3 position, Vector3 rotation, Vector3 scale, string? name, string? path, bool culling, bool castsShadow, bool applyLighting = true) : base(position, rotation, scale, name)
    {
        Path = path;
        Culling = culling;
        CastsShadow = castsShadow;
        ApplyLighting = applyLighting;
    }

    public MapModelFileData(ModelEntity mdlEnt) : base(mdlEnt.Position, mdlEnt.Rotation, mdlEnt.Scale, mdlEnt.Name)
    {
        Path = mdlEnt.Path;
        Culling = mdlEnt.Culling;
        CastsShadow = mdlEnt.CastsShadow;
        ApplyLighting = mdlEnt.ApplyLighting;
    }
}
