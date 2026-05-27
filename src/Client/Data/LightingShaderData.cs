using System.Diagnostics;
using System.Numerics;
using Game.Client.Entities;
using Raylib_cs;

namespace Game.Client.Data;

struct LightData
{
    public int EnabledLoc { get; set; }
    public int PositionLoc { get; set; }
    public int ColorLoc { get; set; }
    public int IntensityLoc { get; set; }
    public int DirectionLoc { get; set; }
    public int CutoffLoc { get; set; }
    public int SpotExponentLoc { get; set; }
}

class LightingShaderData
{
    public const uint LIGHTS_COUNT = 8;

    private Vector3 ambientColor;
    private int ambientColorLoc;
    private int lightingEnabledLoc;
    private LightData[] lights = new LightData[LIGHTS_COUNT];

    public Shader Shader;

    public LightingShaderData()
    {
        Shader = Resources.GetShader("resources/data/shaders/psx_lighting.vs", "resources/data/shaders/psx_lighting.fs");

        ambientColorLoc = Raylib.GetShaderLocation(Shader, "ambientColor");
        lightingEnabledLoc = Raylib.GetShaderLocation(Shader, "useLighting");

        for (int i = 0; i < LIGHTS_COUNT; i++)
        {
            var lightData = new LightData()
            {
                EnabledLoc = Raylib.GetShaderLocation(Shader, $"lights[{i}].enabled"),
                PositionLoc = Raylib.GetShaderLocation(Shader, $"lights[{i}].position"),
                ColorLoc = Raylib.GetShaderLocation(Shader, $"lights[{i}].color"),
                IntensityLoc = Raylib.GetShaderLocation(Shader, $"lights[{i}].intensity"),
                DirectionLoc = Raylib.GetShaderLocation(Shader, $"lights[{i}].direction"),
                CutoffLoc = Raylib.GetShaderLocation(Shader, $"lights[{i}].cutoff"),
                SpotExponentLoc = Raylib.GetShaderLocation(Shader, $"lights[{i}].spotExponent")
            };
            lights[i] = lightData;
        }
    }

    public void UpdateLight(LightEntity light)
    {
        Debug.Assert(light.Index < LIGHTS_COUNT, $"More lights than max amount ({LIGHTS_COUNT}, light with index {light.Index})");

        var lightData = lights[light.Index];

        Raylib.SetShaderValue(Shader, lightData.EnabledLoc, light.Enabled ? 1 : 0, ShaderUniformDataType.Int);
        Raylib.SetShaderValue(Shader, lightData.PositionLoc, light.Position, ShaderUniformDataType.Vec3);
        Raylib.SetShaderValue(Shader, lightData.ColorLoc, new Vector3(light.Color.R, light.Color.G, light.Color.B) / 255f, ShaderUniformDataType.Vec3);
        Raylib.SetShaderValue(Shader, lightData.IntensityLoc, light.Intensity, ShaderUniformDataType.Float);
        Raylib.SetShaderValue(Shader, lightData.DirectionLoc, light.Direction, ShaderUniformDataType.Vec3);
        Raylib.SetShaderValue(Shader, lightData.CutoffLoc, light.Cutoff * Raylib.DEG2RAD, ShaderUniformDataType.Float);
        Raylib.SetShaderValue(Shader, lightData.SpotExponentLoc, light.SpotExponent, ShaderUniformDataType.Float);
    }

    public void SetAmbient(Vector3 ambient)
    {
        ambientColor = ambient;

        Raylib.SetShaderValue(Shader, ambientColorLoc, ambient, ShaderUniformDataType.Vec3);
    }

    public Vector3 GetAmbient() => ambientColor;

    public void Toggle(bool state) => Raylib.SetShaderValue(Shader, lightingEnabledLoc, state ? 1 : 0, ShaderUniformDataType.Int);
}
