namespace Game.Common.Data;

static class GameData
{
    public static string Author => "feweks";
    public static string Name => "billiards";
    public static string AuthorPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), Author);
    public static string DataPath => Path.Combine(AuthorPath, Name);
    public static string ServerDataPath => Path.Combine(DataPath, "server_data");
    public static uint LobbyCodeLength => 6;
    public static uint MaxPacketSize => 4096;
    public static Version Version => new Version(0, 6, 1);
}
