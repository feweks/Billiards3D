using System.Text.Json.Serialization;
using Raylib_cs;

namespace Game.Data.Files;

class GameConfigFileData
{
    public int[] RenderResolution { get; set; } = [1280, 720];
    public string WindowTitle { get; set; } = "Game";
    public ConfigFlags[] Flags { get; set; } = [];

    public void ApplyFlags()
    {
        foreach (var flag in Flags)
            Raylib.SetConfigFlags(flag);
    }
}

[JsonSerializable(typeof(GameConfigFileData))]
partial class GameConfigFileDataCtx : JsonSerializerContext;
