using System.Collections.ObjectModel;
using Raylib_cs;

namespace Game.Client;

using TranslationData = Dictionary<string, string>;

static class Translation
{
    private static List<string> availableLanguages = null!;
    private static TranslationData? translationData;

    public static void Init()
    {
        string translationsFileData = Resources.GetFile("resources/data/translations.txt");
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

        string fileData = Resources.GetFile($"resources/data/translations/{language}.str");
        translationData = new TranslationData();

        foreach (string line in fileData.Split('\n'))
        {
            string key = string.Empty;
            string value = string.Empty;

            bool readingValue = false;
            bool readingKey = true;
            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];

                if (c == ' ' && !readingValue)
                    continue;

                if (c == '=' && readingKey)
                {
                    readingKey = false;
                    continue;
                }

                if (c == '"')
                {
                    readingValue = !readingValue;
                    continue;
                }

                if (readingKey)
                    key += c;
                else if (readingValue)
                    value += c;
            }

            if (key == string.Empty)
            {
                Raylib.TraceLog(TraceLogLevel.Warning, $"Failed to parse translation line {line}: key expected");
                continue;
            }

            if (value == string.Empty)
            {
                Raylib.TraceLog(TraceLogLevel.Warning, $"Failed to parse translation line {line}: value expected");
                continue;
            }

            translationData.Add(key, value);
        }
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
