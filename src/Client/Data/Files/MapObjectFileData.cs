using System.Numerics;

namespace Game.Client.Data.Files;

class MapObjectFileData
{
    public required Vector3 Position;
    public required Vector3 Rotation = Vector3.Zero;
    public required Vector3 Scale = Vector3.One;
    public string? Name { get; set; }
}
