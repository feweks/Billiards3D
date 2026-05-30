using System.Numerics;
using System.Text.Json.Serialization;
using Game.Client.Entities;

namespace Game.Client.Data.Files;

class MapBillboardFileData : MapEntityFileData
{
    public string? Path { get; set; }
    public bool CastsShadow { get; set; }
    public Vector2 Size { get; set; }

    [JsonConstructor]
    public MapBillboardFileData(Vector3 position, Vector3 rotation, Vector3 scale, string? name, string? path, bool castsShadow, Vector2 size) : base(position, rotation, scale, name)
    {
        Path = path;
        CastsShadow = castsShadow;
        Size = size;
    }

    public MapBillboardFileData(BillboardEntity billEnt) : base(billEnt.Position, billEnt.Rotation, billEnt.Scale, billEnt.Name)
    {
        Path = billEnt.Path;
        CastsShadow = billEnt.CastsShadow;
        Size = billEnt.Size;
    }
}
