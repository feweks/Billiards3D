using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Game.Common.Data;
using Game.Common.Enums;
using Game.Server.Data.Files;
using Raylib_cs;

namespace Game.Server;

class Program
{
    GameServerConfigFileData serverConfig;
    GameServer server;
    bool running = false;
    Dictionary<PoolGamemodeType, PoolGamemodeConfigFileData> gamemodeConfigs;

    public Program(string[] args)
    {
        var startTime = DateTime.Now;

        _ = Raylib.SetRandomSeed((uint)DateTimeOffset.UtcNow.ToUnixTimeSeconds());
        SetupConfigPaths();

        serverConfig = GetData("srv_config.json", GameServerConfigFileDataCtx.Default.GameServerConfigFileData);
        gamemodeConfigs = new Dictionary<PoolGamemodeType, PoolGamemodeConfigFileData>();
        LoadGamemodeConfig(PoolGamemodeType.Classic);

        server = new GameServer(serverConfig, gamemodeConfigs);
        Raylib.TraceLog(TraceLogLevel.Info, $"Server started [TCP: {serverConfig.TcpPort}, UDP: {serverConfig.UdpPort}]");

        var elapsedTime = DateTime.Now.Millisecond - startTime.Millisecond;
        Raylib.TraceLog(TraceLogLevel.Info, $"Server loaded in {Math.Abs(Math.Round(elapsedTime * 0.001, 4))}s");

        running = true;
    }

    private static string UpdateConsole()
    {
        Console.Write(">> ");
        string? cmd = Console.ReadLine() ?? string.Empty;
        return cmd;
    }

    private void LoadGamemodeConfig(PoolGamemodeType type)
    {
        string gamemodesPath = Path.Combine(GameData.ServerDataPath, "gamemodes");
        if (!Directory.Exists(gamemodesPath))
        {
            Directory.CreateDirectory(gamemodesPath);
        }

        string typePath = Path.Combine("gamemodes", type.ToString().ToLower() + ".json");
        string fullPath = Path.Combine(GameData.ServerDataPath, typePath);

        if (!File.Exists(fullPath))
        {
            Raylib.TraceLog(TraceLogLevel.Info, $"Initializing gamemode config for {type} at {fullPath}");

            var gmCfg = PoolGamemodeConfigFileData.GetDefault(type);
            string serialized = JsonSerializer.Serialize(gmCfg, PoolGamemodeConfigFileDataCtx.Default.PoolGamemodeConfigFileData);
            File.WriteAllText(fullPath, serialized);
        }

        gamemodeConfigs.Add(type, GetData(typePath, PoolGamemodeConfigFileDataCtx.Default.PoolGamemodeConfigFileData, false));
    }

    private static T GetData<T>(string path, JsonTypeInfo<T> ctx, bool writeOnFailure = true) where T : new()
    {
        string fullPath = Path.Combine(GameData.ServerDataPath, path);
        if (!File.Exists(fullPath))
        {
            Raylib.TraceLog(TraceLogLevel.Warning, $"Failed to load server datafile {fullPath}: file does not exist");
            string defaultSerializedData = JsonSerializer.Serialize(new T(), ctx);
            File.WriteAllText(fullPath, defaultSerializedData);

            return GetData(path, ctx, writeOnFailure);
        }

        try
        {
            string serializedData = File.ReadAllText(fullPath);
            T? deserialized = JsonSerializer.Deserialize(serializedData, ctx);

            if (deserialized == null)
            {
                Raylib.TraceLog(TraceLogLevel.Warning, $"Failed to load server datafile {fullPath}: invalid data");
                return new T();
            }

            Raylib.TraceLog(TraceLogLevel.Info, $"Loaded server datafile {fullPath}");

            return deserialized;
        }
        catch (Exception error)
        {
            Raylib.TraceLog(TraceLogLevel.Warning, $"Failed to load server datafile {path}: error: {error.Message}");
            return new T();
        }
    }

    private static void SetupConfigPaths()
    {
        if (!Directory.Exists(GameData.AuthorPath))
        {
            Directory.CreateDirectory(GameData.AuthorPath);
            Raylib.TraceLog(TraceLogLevel.Info, $"Created author directory [{GameData.AuthorPath}]");
        }

        if (!Directory.Exists(GameData.DataPath))
        {
            Directory.CreateDirectory(GameData.DataPath);
            Raylib.TraceLog(TraceLogLevel.Info, $"Created gamedata directory [{GameData.DataPath}]");
        }

        if (!Directory.Exists(GameData.ServerDataPath))
        {
            Directory.CreateDirectory(GameData.ServerDataPath);
            Raylib.TraceLog(TraceLogLevel.Info, $"Created serverdata directory [{GameData.ServerDataPath}]");
        }

        Raylib.TraceLog(TraceLogLevel.Info, $"Serverdata directory [{GameData.ServerDataPath}]");
    }

    private Tuple<string, TraceLogLevel> ParseCommand(string[] cmd)
    {
        switch (cmd[0])
        {
            case "help":
                string helpTxt = $"\n1. help: shows all commands\n2. exit, close, stop: closes and exits the server";
                return new Tuple<string, TraceLogLevel>(helpTxt, TraceLogLevel.Info);
            case "exit":
            case "close":
            case "stop":
                Shutdown();
                return new Tuple<string, TraceLogLevel>("Closing", TraceLogLevel.Info);
            case "ls":
                string lsTxt = "Lobbies:\n";
                var lastLobby = server.Lobbies.Last();
                foreach (var lobby in server.Lobbies)
                {
                    lsTxt += $"#{lobby.Key}: {lobby.Value.Data.GetPlayerCount()}/2";

                    if (lastLobby.Key != lobby.Key)
                        lsTxt += "\n";
                }

                return new Tuple<string, TraceLogLevel>(lsTxt, TraceLogLevel.Info);
            default:
                return new Tuple<string, TraceLogLevel>($"Unknown command {cmd[0]}", TraceLogLevel.Warning);
        }
    }

    public void Run()
    {
        server.Start();
        while (running)
        {
            string input = UpdateConsole();
            var cmdResult = ParseCommand(input.Split(' '));
            Raylib.TraceLog(cmdResult.Item2, cmdResult.Item1);
        }
    }

    public void Shutdown()
    {
        running = false;

        if (server.IsRunning())
            server.Shutdown();
    }
}
