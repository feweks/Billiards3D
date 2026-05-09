using System.Numerics;
using System.Text.Json.Serialization;
using Game.Common;

namespace Game.Server.Data;

class PoolGamemodeConfig
{
    public float PoolTableWidth { get; set; } = 0.95f;
    public float PoolTableLength { get; set; } = 1.88f;
    public float PoolTablePocketRadius { get; set; } = 0.04f;
    public List<Vector3> PoolTablePockets { get; set; } = new List<Vector3>();
    public int PoolBallsCount { get; set; } = 0;
    public float PoolBallFriction { get; set; } = 0.98f;
    public float PoolBallMass { get; set; } = 0.17f;
    public float PoolBallRadius { get; set; } = 0.035f;
    public Vector3 CueBallPos { get; set; } = Vector3.Zero;
    public List<Vector3> PoolBallsPos { get; set; } = [];

    public static PoolGamemodeConfig GetDefault(PoolGamemodeType gmType)
    {
        var cfg = new PoolGamemodeConfig();

        switch (gmType)
        {
            case PoolGamemodeType.Classic:
                cfg.PoolTablePockets = [
                    new Vector3(-cfg.PoolTableWidth / 2 - (cfg.PoolTablePocketRadius * 0.95f), 1, 0),
                    new Vector3(cfg.PoolTableWidth / 2 + (cfg.PoolTablePocketRadius * 0.95f), 1, 0),
                    new Vector3(-cfg.PoolTableWidth / 2.05f, 1, -cfg.PoolTableLength / 2.05f),
                    new Vector3(cfg.PoolTableWidth / 2.05f, 1, -cfg.PoolTableLength / 2.05f),
                    new Vector3(-cfg.PoolTableWidth / 2.05f, 1, cfg.PoolTableLength / 2.05f),
                    new Vector3(cfg.PoolTableWidth / 2.05f, 1, cfg.PoolTableLength / 2.05f),
                ];
                cfg.PoolBallsCount = 15;
                cfg.CueBallPos = new Vector3(0, 1, 0.5f);
                cfg.PoolBallsPos = [
                    new Vector3(0, 1, -0.35f),
                    new Vector3(0.035f, 1, -0.41f),
                    new Vector3(-0.035f, 1, -0.41f),
                    new Vector3(0.07f, 1, -0.48f),
                    new Vector3(-0.035f, 1, -0.55f),
                    new Vector3(-0.07f, 1, -0.48f),
                    new Vector3(0.105f, 1, -0.55f),
                    new Vector3(0.035f, 1, -0.55f),
                    new Vector3(0, 1, -0.48f),
                    new Vector3(-0.105f, 1, -0.55f),
                    new Vector3(0.140f, 1, -0.62f),
                    new Vector3(0.07f, 1, -0.62f),
                    new Vector3(0, 1, -0.62f),
                    new Vector3(-0.07f, 1, -0.62f),
                    new Vector3(-0.140f, 1, -0.62f)
                ];

                break;
            default:
                throw new NotImplementedException($"Config for pool gamemode {gmType} is not yet implemented");
        }

        return cfg;
    }
}

[JsonSerializable(typeof(PoolGamemodeConfig))]
[JsonSourceGenerationOptions(IncludeFields = true, WriteIndented = true)]
partial class PoolGamemodeConfigCtx : JsonSerializerContext;
