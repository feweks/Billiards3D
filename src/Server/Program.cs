using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Game.Common;
using Game.Server.Data;
using Raylib_cs;

namespace Game.Server;

class Program
{
    GameServerConfig serverConfig;
    GameServer server;
    bool running = false;

    public Program(string[] args)
    {
        var startTime = DateTime.Now;

        SetupConfigPaths();

        serverConfig = GetData("srv_config.json", GameServerConfigCtx.Default.GameServerConfig);

        server = new GameServer(serverConfig);
        Raylib.TraceLog(TraceLogLevel.Info, $"Server started on port {serverConfig.Port}");

        var elapsedTime = DateTime.Now.Millisecond - startTime.Millisecond;
        Raylib.TraceLog(TraceLogLevel.Info, $"Server loaded in {elapsedTime * 0.001}s");

        running = true;
    }

    private static string UpdateConsole()
    {
        Console.Write(">> ");
        string? cmd = Console.ReadLine() ?? string.Empty;
        return cmd;
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
            default:
                return new Tuple<string, TraceLogLevel>($"Unknown command {cmd}", TraceLogLevel.Warning);
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
