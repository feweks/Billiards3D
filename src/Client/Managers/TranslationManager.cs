using System.Collections.ObjectModel;
using Raylib_cs;

namespace Game.Client.Managers;

static class TranslationManager
{
    private static List<string> availableLanguages = null!;
    private static Dictionary<string, string>? translationData;

    public static void Init()
    {
        string translationsFileData = ResourcesManager.GetFile("resources/data/translations.txt");
        availableLanguages = translationsFileData.Split(';').ToList();
    }

    public static ReadOnlyCollection<string> GetLanguages() => availableLanguages.AsReadOnly();

    public static void Load(string language)
    {
        if (!availableLanguages.Contains(language))
        {
            Raylib.TraceLog(TraceLogLevel.Warning, $"Failed to load translation for language {language}: language does not have translation");
            return;
        }

        translationData = ResourcesManager.GetKvp($"resources/data/translations/{language}.kvp");
    }

    public static string Get(string key) => Get(key, null);

    public static string Get(string key, params string[]? format)
    {
        if (translationData == null)
        {
            Raylib.TraceLog(TraceLogLevel.Warning, $"Failed to get translation key {key}: translation data is not initialized");
            return key;
        }

        if (!translationData.TryGetValue(key, out string? value))
        {
            Raylib.TraceLog(TraceLogLevel.Warning, $"Failed to get translation key {key}: translation data is not initialized");
            return key;
        }

        string finalValue = value;
        if (format != null)
            finalValue = string.Format(finalValue, format);

        return finalValue;
    }
}
