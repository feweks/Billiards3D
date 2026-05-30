using System.Numerics;

namespace Game.Client.Data.Files;

class MapEntityFileData
{
    public Vector3 Position;
    public Vector3 Rotation = Vector3.Zero;
    public Vector3 Scale = Vector3.One;
    public string? Name { get; set; }

    public MapEntityFileData(Vector3 pos, Vector3 rot, Vector3 scale, string? name)
    {
        Position = pos;
        Rotation = rot;
        Scale = scale;
        Name = name;
    }
}
