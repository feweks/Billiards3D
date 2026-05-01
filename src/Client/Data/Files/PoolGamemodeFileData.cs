using System.Numerics;
using System.Text.Json.Serialization;

namespace Game.Client.Data.Files;

class PoolGamemodeFileData
{
    public int PoolBallsCount { get; set; } = 0;
    public Vector3 CueBallPos { get; set; } = Vector3.Zero;
    public List<Vector3> PoolBallsPos { get; set; } = [];
}

[JsonSerializable(typeof(PoolGamemodeFileData))]
[JsonSourceGenerationOptions(IncludeFields = true)]
partial class PoolGamemodeFileDataCtx : JsonSerializerContext;
