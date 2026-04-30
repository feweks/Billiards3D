using System.Numerics;
using Game.Data.Files;
using Raylib_cs;
using rlImGui_cs;

namespace Game.Client;

class Program
{
    public static Program? Instance { get; internal set; }

    public bool DebugMode { get; }
    public GameConfigFileData Config { get; }

    private RenderTexture2D renderTex;

    public Program(string[] args)
    {
        DebugMode = args.Contains("-debug");
        Config = Resources.GetJson("resources/data/game_config.json", GameConfigFileDataCtx.Default.GameConfigFileData);
        Config.ApplyFlags();

        Raylib.InitWindow(1280, 720, Config.WindowTitle); // TODO: Load window size based on settings
        Raylib.InitAudioDevice();

        Resources.Init();
        rlImGui.Setup(true);

        renderTex = Raylib.LoadRenderTexture(Config.RenderResolution[0], Config.RenderResolution[1]);

        Instance ??= this;
    }

    private void Update(float dt)
    {
        const float DT_TRESHOLD = 0.3f;
        if (dt > DT_TRESHOLD)
        {
            Raylib.TraceLog(TraceLogLevel.Warning, $"Normalizing dt ({dt} -> {DT_TRESHOLD})");
            dt = DT_TRESHOLD;
        }
    }

    private void Draw()
    {
        Raylib.BeginTextureMode(renderTex);
        Raylib.ClearBackground(Color.Black);

        Raylib.EndTextureMode();

        Raylib.BeginDrawing();
        Raylib.ClearBackground(Color.Black);

        Texture2D rtt = renderTex.Texture;
        var src = new Rectangle(0, 0, rtt.Width, -rtt.Height);
        var dest = new Rectangle(0, 0, Raylib.GetScreenWidth(), Raylib.GetScreenHeight());

        Raylib.DrawTexturePro(rtt, src, dest, Vector2.Zero, 0, Color.White);

        rlImGui.Begin();

        rlImGui.End();

        Raylib.EndDrawing();
    }

    public void Run()
    {
        while (!Raylib.WindowShouldClose())
        {
            Update(Raylib.GetFrameTime());
            Draw();
        }
    }

    public void Shutdown()
    {
        Resources.Shutdown();
        Raylib.CloseWindow();
    }
}
