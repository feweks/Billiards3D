using System.Numerics;
using Game.Common;

namespace Game.Server.Data;

class PoolBallConfig
{
    public required Vector3 Position;
    public required PoolBallType Type { get; set; }
    public float Mass { get; set; } = 0.17f;
    public float Radius { get; set; } = 0.035f;
}
