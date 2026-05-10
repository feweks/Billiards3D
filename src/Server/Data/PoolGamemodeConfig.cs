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
    public PoolBallConfig PoolCueBall { get; set; } = new PoolBallConfig() { Position = Vector3.Zero, Type = PoolBallType.Cue };
    public List<PoolBallConfig> PoolBalls { get; set; } = [];

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
                cfg.PoolCueBall = new PoolBallConfig() { Position = new Vector3(0, 1, 0.5f), Type = PoolBallType.Cue };
                cfg.PoolBalls = [
                    new PoolBallConfig() { Position = new Vector3(0, 1, -0.35f), Type = PoolBallType.Solid },
                    new PoolBallConfig() { Position = new Vector3(0.035f, 1, -0.41f), Type = PoolBallType.Solid },
                    new PoolBallConfig() { Position = new Vector3(-0.035f, 1, -0.41f), Type = PoolBallType.Solid },
                    new PoolBallConfig() { Position = new Vector3(0.07f, 1, -0.48f), Type = PoolBallType.Solid },
                    new PoolBallConfig() { Position = new Vector3(-0.035f, 1, -0.55f), Type = PoolBallType.Solid },
                    new PoolBallConfig() { Position = new Vector3(-0.07f, 1, -0.48f), Type = PoolBallType.Solid },
                    new PoolBallConfig() { Position = new Vector3(0.105f, 1, -0.55f), Type = PoolBallType.Solid },
                    new PoolBallConfig() { Position = new Vector3(0, 1, -0.48f), Type = PoolBallType.BlackBall },
                    new PoolBallConfig() { Position = new Vector3(0.035f, 1, -0.55f), Type = PoolBallType.Striped },
                    new PoolBallConfig() { Position = new Vector3(-0.105f, 1, -0.55f), Type = PoolBallType.Striped },
                    new PoolBallConfig() { Position = new Vector3(0.140f, 1, -0.62f), Type = PoolBallType.Striped },
                    new PoolBallConfig() { Position = new Vector3(0.07f, 1, -0.62f), Type = PoolBallType.Striped },
                    new PoolBallConfig() { Position = new Vector3(0, 1, -0.62f), Type = PoolBallType.Striped },
                    new PoolBallConfig() { Position = new Vector3(-0.07f, 1, -0.62f), Type = PoolBallType.Striped },
                    new PoolBallConfig() { Position = new Vector3(-0.140f, 1, -0.62f), Type = PoolBallType.Striped }
                ];

                break;
            default:
                throw new NotImplementedException($"Config for pool gamemode {gmType} is not yet implemented");
        }

        return cfg;
    }
}

[JsonSerializable(typeof(PoolGamemodeConfig))]
[JsonSourceGenerationOptions(IncludeFields = true, WriteIndented = true, Converters = [typeof(JsonStringEnumConverter<PoolBallType>)])]
partial class PoolGamemodeConfigCtx : JsonSerializerContext;
