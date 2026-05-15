using System.Numerics;

namespace Game.Client.Data.Files;

class MapObjectFileData
{
    public Vector3 Position;
    public Vector3 Rotation = Vector3.Zero;
    public Vector3 Scale = Vector3.One;
    public string? Name { get; set; }
}
