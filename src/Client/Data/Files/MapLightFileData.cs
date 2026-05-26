using Raylib_cs;

namespace Game.Client.Data.Files;

class MapLightFileData : MapObjectFileData
{
    public required bool Enabled { get; set; }
    public required Color Color;
    public required float Intensity { get; set; }
}
