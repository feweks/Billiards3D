using Raylib_cs;
using System.Numerics;

namespace Game.Client;

static class Utils
{
    public static Vector2 GetScreenScaleFactor()
    {
        float x = Program.Instance!.Config.RenderResolution[0] / (float)Raylib.GetScreenWidth();
        float y = Program.Instance!.Config.RenderResolution[1] / (float)Raylib.GetScreenHeight();

        return new Vector2(x, y);
    }

    public static Vector2 GetMousePos() => Raylib.GetMousePosition() * GetScreenScaleFactor();

    public static Vector4 ColorToVec4(Color col) => new Vector4(col.R, col.G, col.B, col.A) / 255f;

    public static Color ColorFromVec4(Vector4 vec) => new Color(vec.X, vec.Y, vec.Z, vec.W);

    public static Vector3 ColorToVec3(Color col) => new Vector3(col.R, col.G, col.B) / 255f;

    public static Color ColorFromVec3(Vector3 vec) => new Color(vec.X, vec.Y, vec.Z, 1f);

    public static Matrix4x4 CalculateMatrix(Vector3 pos, Vector3 rot, Vector3 scale)
    {
        var matScale = Raymath.MatrixScale(scale.X, scale.Y, scale.Z);
        var matRot = Raymath.MatrixRotateXYZ(rot * Raylib.DEG2RAD);
        var matTrans = Raymath.MatrixTranslate(pos.X, pos.Y, pos.Z);

        return Raymath.MatrixMultiply(Raymath.MatrixMultiply(matScale, matRot), matTrans);
    }
}
