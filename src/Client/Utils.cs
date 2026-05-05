using Raylib_cs;
using System.Numerics;

namespace Game.Client;

static class Utils
{
    public static Vector4 ColorToVec4(Color col) => new Vector4(col.R, col.G, col.B, col.A) / 255f;

    public static Color ColorFromVec4(Vector4 vec) => new Color(vec.X, vec.Y, vec.Z, vec.W);
}
