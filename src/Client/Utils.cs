using Game.Client.Data;
using Game.Client.Managers;
using Game.Client.States;
using Raylib_cs;
using System.Collections.ObjectModel;
using System.Numerics;

namespace Game.Client;

enum EasingType : ushort
{
    EaseInSine = 0,
    EaseOutSine,
    EaseInOutSine,
    EaseInQuad,
    EaseOutQuad,
    EaseInOutQuad,
    EaseInCubic,
    EaseOutCubic,
    EaseInOutCubic,
    EaseInQuart,
    EaseOutQuart,
    EaseInOutQuart,
    EaseInQuint,
    EaseOutQuint,
    EaseInOutQuint,
    EaseInExpo,
    EaseOutExpo,
    EaseInOutExpo,
    EaseInCirc,
    EaseOutCirc,
    EaseInOutCirc,
    EaseInBack,
    EaseOutBack,
    EaseInOutBack,
    EaseInElastic,
    EaseOutElastic,
    EaseInOutElastic,
    EaseInBounce,
    EaseOutBounce,
    EaseInOutBounce
}


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

    public static float EaseValue(EasingType type, float t)
    {
        switch (type)
        {
            case EasingType.EaseInSine:
                return 1 - MathF.Cos(t * MathF.PI / 2);
            case EasingType.EaseOutSine:
                return MathF.Sin(t * MathF.PI / 2);
            case EasingType.EaseInOutSine:
                return -(MathF.Cos(MathF.PI * t) - 1) / 2;
            case EasingType.EaseInQuad:
                return t * t;
            case EasingType.EaseOutQuad:
                return 1 - (1 - t) * (1 - t);
            case EasingType.EaseInOutQuad:
                return t < 0.5 ? 2 * t * t : 1 - MathF.Pow(-2 * t + 2, 2) / 2;
            case EasingType.EaseInCubic:
                return t * t * t;
            case EasingType.EaseOutCubic:
                return 1 - MathF.Pow(1 - t, 3);
            case EasingType.EaseInOutCubic:
                return t < 0.5 ? 4 * t * t * t : 1 - MathF.Pow(-2 * t + 2, 3) / 2;
            case EasingType.EaseInQuart:
                return t * t * t * t;
            case EasingType.EaseOutQuart:
                return 1 - MathF.Pow(1 - t, 4);
            case EasingType.EaseInOutQuart:
                return t < 0.5 ? 8 * t * t * t * t : 1 - MathF.Pow(-2 * t + 2, 4) / 2;
            case EasingType.EaseInQuint:
                return t * t * t * t * t;
            case EasingType.EaseOutQuint:
                return 1 - MathF.Pow(1 - t, 5);
            case EasingType.EaseInOutQuint:
                return t < 0.5 ? 16 * t * t * t * t * t : 1 - MathF.Pow(-2 * t + 2, 5) / 2;
            case EasingType.EaseInExpo:
                return t == 0 ? 0 : MathF.Pow(2, 10 * t - 10);
            case EasingType.EaseOutExpo:
                return t == 1 ? 1 : 1 - MathF.Pow(2, -10 * t);
            case EasingType.EaseInOutExpo:
                return t == 0
                        ? 0
                        : t == 1
                        ? 1
                        : t < 0.5 ? MathF.Pow(2, 20 * t - 10) / 2
                        : (2 - MathF.Pow(2, -20 * t + 10)) / 2;
            case EasingType.EaseInCirc:
                return 1 - MathF.Sqrt(1 - MathF.Pow(t, 2));
            case EasingType.EaseOutCirc:
                return MathF.Sqrt(1 - MathF.Pow(t - 1, 2));
            case EasingType.EaseInOutCirc:
                return t < 0.5
                        ? (1 - MathF.Sqrt(1 - MathF.Pow(2 * t, 2))) / 2
                        : (MathF.Sqrt(1 - MathF.Pow(-2 * t + 2, 2)) + 1) / 2;
            case EasingType.EaseInBack:
                {
                    const float c1 = 1.70158f;
                    const float c3 = c1 + 1;

                    return c3 * t * t * t - c1 * t * t;
                }
            case EasingType.EaseOutBack:
                {
                    const float c1 = 1.70158f;
                    const float c3 = c1 + 1;

                    return 1 + c3 * MathF.Pow(t - 1, 3) + c1 * MathF.Pow(t - 1, 2);
                }
            case EasingType.EaseInOutBack:
                {
                    const float c1 = 1.70158f;
                    const float c2 = c1 * 1.525f;

                    return t < 0.5
                            ? MathF.Pow(2 * t, 2) * ((c2 + 1) * 2 * t - c2) / 2
                            : (MathF.Pow(2 * t - 2, 2) * ((c2 + 1) * (t * 2 - 2) + c2) + 2) / 2;
                }
            case EasingType.EaseInElastic:
                {
                    const float c4 = 2 * MathF.PI / 3;

                    return t == 0
                        ? 0
                        : t == 1
                        ? 1
                        : -MathF.Pow(2, 10 * t - 10) * MathF.Sin((t * 10 - 10.75f) * c4);
                }
            case EasingType.EaseOutElastic:
                {
                    const float c4 = 2 * MathF.PI / 3;

                    return t == 0
                            ? 0
                            : t == 1
                            ? 1
                            : MathF.Pow(2, -10 * t) * MathF.Sin((t * 10 - 0.75f) * c4) + 1;
                }
            case EasingType.EaseInOutElastic:
                {
                    const float c5 = 2 * MathF.PI / 4.5f;

                    return t == 0
                      ? 0
                      : t == 1
                      ? 1
                      : t < 0.5
                      ? -(MathF.Pow(2, 20 * t - 10) * MathF.Sin((20 * t - 11.125f) * c5)) / 2
                      : MathF.Pow(2, -20 * t + 10) * MathF.Sin((20 * t - 11.125f) * c5) / 2 + 1;
                }
            case EasingType.EaseInBounce:
                return 1 - EaseValue(EasingType.EaseOutBounce, 1 - t);
            case EasingType.EaseOutBounce:
                {
                    const float n1 = 7.5625f;
                    const float d1 = 2.75f;

                    if (t < 1 / d1)
                    {
                        return n1 * t * t;
                    }
                    else if (t < 2 / d1)
                    {
                        return n1 * (t -= 1.5f / d1) * t + 0.75f;
                    }
                    else if (t < 2.5 / d1)
                    {
                        return n1 * (t -= 2.25f / d1) * t + 0.9375f;
                    }
                    else
                    {
                        return n1 * (t -= 2.625f / d1) * t + 0.984375f;
                    }
                }
            case EasingType.EaseInOutBounce:
                return t < 0.5
                        ? (1 - EaseValue(EasingType.EaseInBounce, 1 - 2 * t)) / 2
                        : (1 + EaseValue(EasingType.EaseOutBounce, 2 * t - 1)) / 2;
            default:
                return t;
        }
    }
}
