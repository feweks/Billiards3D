using System.Text.Json.Serialization;
using Raylib_cs;

namespace Game.Client.Data.Files;

class GameConfigFileData
{
    public int[] RenderResolution { get; set; } = [1280, 720];
    public string WindowTitle { get; set; } = "Game";
    public ConfigFlags[] Flags { get; set; } = [];

    public int RenderWidth => RenderResolution[0];
    public int RenderHeight => RenderResolution[1];

    public void ApplyFlags()
    {
        foreach (var flag in Flags)
            Raylib.SetConfigFlags(flag);
    }
}

[JsonSerializable(typeof(GameConfigFileData))]
[JsonSourceGenerationOptions(IncludeFields = true)]
partial class GameConfigFileDataCtx : JsonSerializerContext;
