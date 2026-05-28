namespace Game.Client.Data.Files;

class MapEntityFileData : MapObjectFileData
{
    public required string? ModelPath { get; set; }
    public required bool Culling { get; set; }
    public required bool HasShadow { get; set; }
}
