using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Raylib_cs;

namespace Game.Client;

static class Resources
{
    public static void Init() { }

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

    public static void Shutdown()
    {

    }
}
