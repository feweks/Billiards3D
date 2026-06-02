using System.Numerics;
using Game.Common.Enums;

namespace Game.Server.Data.Files;

class PoolBallConfigFileData
{
    public required Vector3 Position;
    public required PoolBallType Type { get; set; }
}
