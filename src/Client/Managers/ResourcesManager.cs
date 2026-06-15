using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using System.Xml;
using Raylib_cs;

namespace Game.Client.Managers;

static class ResourcesManager
{
    private static readonly Dictionary<string, Texture2D> textures = [];
    private static readonly Dictionary<string, Model> models = [];
    private static readonly Dictionary<string, Font> fonts = [];

    private static Texture2D errorTex;
    private static Model errorMdl;
    private static Font errorFnt;
    private static Shader errorShd;
    private static string? errorFragShader;
    private static string? errorVertShader;

    public static void Init()
    {
        const int ERROR_IMG_SIZE = 128;
        Image errorImg = Raylib.GenImageChecked(ERROR_IMG_SIZE, ERROR_IMG_SIZE, ERROR_IMG_SIZE / 2, ERROR_IMG_SIZE / 2, Color.Magenta, Color.Black);
        errorTex = Raylib.LoadTextureFromImage(errorImg);
        Raylib.UnloadImage(errorImg);

        Mesh errorMesh = Raylib.GenMeshCube(0.5f, 0.5f, 0.5f);
        errorMdl = Raylib.LoadModelFromMesh(errorMesh);
        Material errorMat = Raylib.GetMaterial(ref errorMdl, 0);
        Raylib.SetMaterialTexture(ref errorMat, MaterialMapIndex.Albedo, errorTex);

        errorFnt = Raylib.GetFontDefault();

        errorFragShader = GetFile("resources/data/shaders/default.fs");
        errorVertShader = GetFile("resources/data/shaders/default.vs");

        errorShd = Raylib.LoadShaderFromMemory(errorVertShader, errorFragShader);
    }

    public static Model GetErrorModel() => errorMdl;

    public static Texture2D GetErrorTex() => errorTex;

    public static Font GetErrorFont() => errorFnt;

    public static Shader GetErrorShader() => errorShd;

    public static Texture2D GetTexture(string? path)
    {
        if (path == null)
            return errorTex;

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

    public static Model GetModel(string? path)
    {
        if (path == null)
            return errorMdl;

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

    public static Font GetFont(string? path)
    {
        if (path == null)
            return errorFnt;

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

    public static Shader GetShader(string? vertexPath = null, string? fragmentPath = null)
    {
        Debug.Assert(errorVertShader != null && errorFragShader != null, "Uninitialized resources system");

        string vertSrc = errorVertShader;
        string fragSrc = errorFragShader;

        if (vertexPath != null)
        {
            if (File.Exists(vertexPath))
                vertSrc = GetFile(vertexPath);
            else
                Raylib.TraceLog(TraceLogLevel.Warning, $"Failed to load vertex shader {vertexPath}: file does not exist");
        }

        if (fragmentPath != null)
        {
            if (File.Exists(fragmentPath))
                fragSrc = GetFile(fragmentPath);
            else
                Raylib.TraceLog(TraceLogLevel.Warning, $"Failed to load fragment shader {fragmentPath}: file does not exist");
        }

        Shader shd = Raylib.LoadShaderFromMemory(vertSrc, fragSrc);
        return shd;
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

    public static void SaveFile(string path, string? content) => File.WriteAllText(path, content);

    public static string[] GetDirectoryFiles(string path, string? extension = null)
    {
        if (!Directory.Exists(path))
        {
            Raylib.TraceLog(TraceLogLevel.Warning, $"Failed to list files of {path}: directory does not exist");
            return [];
        }

        string[] files = Directory.GetFiles(path);
        if (extension != null)
            files = files.Where(f => f.EndsWith(extension)).ToArray();

        return files;
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

    public static void SaveJson<T>(string path, T data, JsonTypeInfo<T> ctx)
    {
        string serializedData = JsonSerializer.Serialize(data, ctx);
        SaveFile(path, serializedData);
    }

    public static XmlDocument? GetXml(string path)
    {
        var doc = new XmlDocument();
        string fData = GetFile(path);
        if (fData == string.Empty)
        {
            Raylib.TraceLog(TraceLogLevel.Warning, $"Failed to load xml document {path}: invalid file data");
            return null;
        }

        doc.LoadXml(fData);
        return doc;
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
