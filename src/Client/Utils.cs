using Game.Client.Data;
using Game.Client.Managers;
using Game.Client.States;
using Raylib_cs;
using System.Collections.ObjectModel;
using System.Numerics;

namespace Game.Client;

static class Utils
{
    private static List<string>? playableMaps;
    private static Vector2[] outlinedTextOffsets = [
        new Vector2(-1, 0),
        new Vector2(1, 0),
        new Vector2(0, -1),
        new Vector2(0, 1)
    ];

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

    public static string GetKeyCode(KeyboardKey key, bool shift)
    {
        int keyNum = (int)key;

        if (keyNum >= (int)KeyboardKey.A && keyNum <= (int)KeyboardKey.Z)
        {
            string result = key.ToString();
            if (!shift)
                result = result.ToLower();

            return result;
        }

        if (keyNum >= (int)KeyboardKey.Zero && keyNum <= (int)KeyboardKey.Nine && !shift)
        {
            return (keyNum - (int)KeyboardKey.Zero).ToString();
        }

        switch (key)
        {
            case KeyboardKey.Apostrophe:
                return !shift ? "'" : $"{'"'}";
            case KeyboardKey.Comma:
                return !shift ? "," : "<";
            case KeyboardKey.Minus:
                return !shift ? "-" : "_";
            case KeyboardKey.Period:
                return !shift ? "." : ">";
            case KeyboardKey.Slash:
                return !shift ? "/" : "?";
            case KeyboardKey.Backslash:
                return !shift ? @"\" : "|";
            case KeyboardKey.Equal:
                return !shift ? "=" : "+";
            case KeyboardKey.Semicolon:
                return !shift ? ";" : ":";
            case KeyboardKey.LeftBracket:
                return !shift ? "[" : "{";
            case KeyboardKey.RightBracket:
                return !shift ? "]" : "}";
            case KeyboardKey.Zero: // Keys 0 - 9 cant be now pressed without shift
                return ")";
            case KeyboardKey.One:
                return "!";
            case KeyboardKey.Two:
                return "@";
            case KeyboardKey.Three:
                return "#";
            case KeyboardKey.Four:
                return "$";
            case KeyboardKey.Five:
                return "%";
            case KeyboardKey.Six:
                return "^";
            case KeyboardKey.Seven:
                return "&";
            case KeyboardKey.Eight:
                return "*";
            case KeyboardKey.Nine:
                return "(";
            case KeyboardKey.Space:
                return " ";
        }

        return string.Empty;
    }

    public static RayCollision SphereRayCast(Vector3 rayStart, Vector3 rayDir, float castRadius, Vector3 targetCenter, float targetRadius)
    {
        var ray = new Ray(rayStart, rayDir);
        float combinedRadius = targetRadius + castRadius;

        return Raylib.GetRayCollisionSphere(ray, targetCenter, combinedRadius);
    }

    public static void DrawTextOutlined(Font fnt, string text, Vector2 pos, float size, Color textCol, Color outlineCol, float spacing = 1) =>
        DrawTextOutlinedEx(fnt, text, pos, Vector2.Zero, size, 0, textCol, outlineCol, spacing);

    public static void DrawTextOutlinedEx(Font fnt, string text, Vector2 pos, Vector2 origin, float size, float rot, Color textCol, Color outlineCol, float spacing = 1)
    {
        for (int i = 0; i < outlinedTextOffsets.Length; i++)
            Raylib.DrawTextPro(fnt, text, pos + outlinedTextOffsets[i], origin, rot, size, spacing, outlineCol);

        Raylib.DrawTextPro(fnt, text, pos, origin, rot, size, spacing, textCol);
    }

    public static string[] GetPlayableMaps()
    {
        playableMaps ??= InitPlayableMaps();

        return playableMaps.ToArray();
    }

    private static List<string> InitPlayableMaps()
    {
        var maps = new List<string>();
        string[] lines = ResourcesManager.GetFile("resources/data/playable_maps.txt").Split(';');

        foreach (string mapName in lines)
        {
            maps.Add(mapName);
        }
        Raylib.TraceLog(TraceLogLevel.Info, $"Initialized playable maps [maps count: {maps.Count}]");

        return maps;
    }
}
