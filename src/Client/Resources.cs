using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Raylib_cs;

namespace Game.Client;

static class Resources
{
    private static Dictionary<string, Texture2D> textures = new Dictionary<string, Texture2D>();
    private static Dictionary<string, Model> models = new Dictionary<string, Model>();
    private static Dictionary<string, Font> fonts = new Dictionary<string, Font>();

    private static Texture2D errorTex;
    private static Model errorMdl;
    private static Font errorFnt;

    public static void Init()
    {
        const int ERROR_IMG_SIZE = 100;
        Image errorImg = Raylib.GenImageChecked(ERROR_IMG_SIZE, ERROR_IMG_SIZE, ERROR_IMG_SIZE / 2, ERROR_IMG_SIZE / 2, Color.Magenta, Color.Black);
        errorTex = Raylib.LoadTextureFromImage(errorImg);
        Raylib.UnloadImage(errorImg);

        Mesh errorMesh = Raylib.GenMeshCube(0.5f, 0.5f, 0.5f);
        errorMdl = Raylib.LoadModelFromMesh(errorMesh);
        Material errorMat = Raylib.GetMaterial(ref errorMdl, 0);
        Raylib.SetMaterialTexture(ref errorMat, MaterialMapIndex.Albedo, errorTex);

        errorFnt = Raylib.GetFontDefault();
    }

    public static Texture2D GetTexture(string path)
    {
        if (textures.TryGetValue(path, out Texture2D cacheTex))
            return cacheTex;

        if (!File.Exists(path))
        {
            Raylib.TraceLog(TraceLogLevel.Warning, $"Failed to load texture {path}: file not found");
            return errorTex;
        }

        Texture2D tex = Raylib.LoadTexture(path);

        if (!Raylib.IsTextureValid(tex))
        {
            Raylib.TraceLog(TraceLogLevel.Warning, $"Failed to load texture {path}: invalid data");
            return errorTex;
        }

        textures.Add(path, tex);
        return tex;
    }

    public static Model GetModel(string path)
    {
        if (models.TryGetValue(path, out Model cacheMdl))
            return cacheMdl;

        if (!File.Exists(path))
        {
            Raylib.TraceLog(TraceLogLevel.Warning, $"Failed to load model {path}: file not found");
            return errorMdl;
        }

        Model mdl = Raylib.LoadModel(path);

        if (!Raylib.IsModelValid(mdl))
        {
            Raylib.TraceLog(TraceLogLevel.Warning, $"Failed to load model {path}: invalid data");
            return errorMdl;
        }

        models.Add(path, mdl);
        return mdl;
    }

    public static Font GetFont(string path)
    {
        if (fonts.TryGetValue(path, out Font cacheFont))
            return cacheFont;

        if (!File.Exists(path))
        {
            Raylib.TraceLog(TraceLogLevel.Warning, $"Failed to load font {path}: file not found");
            return errorFnt;
        }

        Font fnt = Raylib.LoadFontEx(path, 120, null, 400);

        if (!Raylib.IsFontValid(fnt))
        {
            Raylib.TraceLog(TraceLogLevel.Warning, $"Failed to load font {path}: invalid data");
            return errorFnt;
        }

        fonts.Add(path, fnt);
        return fnt;
    }

    public static string GetFile(string path)
    {
        if (!File.Exists(path))
        {
            Raylib.TraceLog(TraceLogLevel.Warning, $"Failed to load file {path}: file not found");
            return string.Empty;
        }

        return File.ReadAllText(path);
    }

    public static T GetJson<T>(string path, JsonTypeInfo<T> ctx) where T : new()
    {
        string serializedData = GetFile(path);
        if (serializedData == string.Empty)
        {
            Raylib.TraceLog(TraceLogLevel.Warning, $"Failed to load json {path}: invalid file data");
            return new T();
        }

        T? deserializedData = JsonSerializer.Deserialize(serializedData, ctx);
        if (deserializedData == null)
        {
            Raylib.TraceLog(TraceLogLevel.Warning, $"Failed to load json {path}: invalid deserialized data");
            return new T();
        }

        return deserializedData;
    }

    public static void Unload()
    {
        foreach (var tex in textures.Values)
            Raylib.UnloadTexture(tex);
        textures.Clear();

        foreach (var mdl in models.Values)
            Raylib.UnloadModel(mdl);
        models.Clear();

        foreach (var fnt in fonts.Values)
            Raylib.UnloadFont(fnt);
        fonts.Clear();
    }

    public static void Shutdown()
    {
        Unload();

        Raylib.UnloadTexture(errorTex);
        Raylib.UnloadModel(errorMdl);
    }
}
