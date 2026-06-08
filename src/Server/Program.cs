using System.Text;
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
                var res = new StringBuilder();
                res.AppendLine("1.");
                res.AppendLine("2.");
                res.AppendLine("3.");

                return new Tuple<string, TraceLogLevel>(res.ToString(), TraceLogLevel.Info);
            case "exit":
            case "close":
            case "stop":
                Shutdown();
                return new Tuple<string, TraceLogLevel>("Closing", TraceLogLevel.Info);
            case "ls":
                if (cmd.Length < 2)
                    return new Tuple<string, TraceLogLevel>("Not enough arguments (expected lb/cl)", TraceLogLevel.Warning);

                string mode = cmd[1];

                if (mode == "lb")
                {
                    if (server.Lobbies.IsEmpty)
                        return new Tuple<string, TraceLogLevel>("No lobbies are currently created", TraceLogLevel.Warning);

                    var lsTxt = new StringBuilder("Lobbies:\n");
                    var lastLobby = server.Lobbies.Last();
                    foreach (var lobby in server.Lobbies)
                    {
                        string lbTxt = $"#{lobby.Key}: {lobby.Value.Data.GetPlayerCount()}/2";
                        if (lobby.Key == lastLobby.Key)
                            lsTxt.Append(lbTxt);
                        else
                            lsTxt.AppendLine(lbTxt);
                    }

                    return new Tuple<string, TraceLogLevel>(lsTxt.ToString(), TraceLogLevel.Info);
                }
                else if (mode == "cl")
                {
                    if (server.Clients.IsEmpty)
                        return new Tuple<string, TraceLogLevel>("No clients are currently connected", TraceLogLevel.Warning);

                    var lsTxt = new StringBuilder();
                    var lastClient = server.Clients.Last();
                    foreach (var client in server.Clients)
                    {
                        string clTxt = $"{client.Key}: {client.Value.UdpEndPoint}";

                        if (client.Key == lastClient.Key)
                            lsTxt.Append(clTxt);
                        else
                            lsTxt.AppendLine(clTxt);
                    }

                    return new Tuple<string, TraceLogLevel>(lsTxt.ToString(), TraceLogLevel.Info);
                }
                else
                {
                    return new Tuple<string, TraceLogLevel>($"Unknown mode {mode}", TraceLogLevel.Warning);
                }
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
