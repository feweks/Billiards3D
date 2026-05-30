using System.Numerics;
using System.Text.Json.Serialization;
using Game.Client.Entities;
using Raylib_cs;

namespace Game.Client.Data.Files;

class MapLightFileData : MapEntityFileData
{
    public bool Enabled { get; set; }
    public Color Color { get; set; }
    public float Intensity { get; set; }
    public Vector3 Direction { get; set; }
    public float Cutoff { get; set; }
    public float SpotExponent { get; set; }

    [JsonConstructor]
    public MapLightFileData(Vector3 position, Vector3 rotation, Vector3 scale, string? name, bool enabled, Color color, float intensity, Vector3 direction, float cutoff, float spotExponent) : base(position, rotation, scale, name)
    {
        Enabled = enabled;
        Color = color;
        Intensity = intensity;
        Direction = direction;
        Cutoff = cutoff;
        SpotExponent = spotExponent;
    }

    public MapLightFileData(LightEntity lightEnt) : base(lightEnt.Position, lightEnt.Rotation, lightEnt.Scale, lightEnt.Name)
    {
        Enabled = lightEnt.Enabled;
        Color = lightEnt.Color;
        Intensity = lightEnt.Intensity;
        Direction = lightEnt.Direction;
        Cutoff = lightEnt.Cutoff;
        SpotExponent = lightEnt.SpotExponent;
    }
}
