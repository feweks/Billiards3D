using System.Numerics;
using Game.Client.Data.Files;
using Game.Client.Net;
using Game.Client.States;
using Raylib_cs;
using rlImGui_cs;

namespace Game.Client;

class Program
{
    public static Program? Instance { get; internal set; }

    public bool DebugMode { get; }
    public GameConfigFileData Config { get; }

    private Shader postShader;
    private RenderTexture2D renderTex;
    private GameState? curState;

    public Program(string[] args)
    {
        DebugMode = args.Contains("-debug");
        bool editor = args.Contains("-editor");

        Config = Resources.GetJson("resources/data/game_config.json", GameConfigFileDataCtx.Default.GameConfigFileData);
        Config.ApplyFlags();

        Raylib.InitWindow(1280, 720, Config.WindowTitle); // TODO: Load window size based on settings
        Raylib.InitAudioDevice();
        Raylib.SetExitKey(KeyboardKey.Null);

        Resources.Init();
        Translation.Init();
        Translation.Load("pl"); // TODO: Load translation based on settings

        rlImGui.Setup(true);
        if (!editor)
            GameClient.Init();

        Raylib.SetTargetFPS(200);

        renderTex = Raylib.LoadRenderTexture(Config.RenderResolution[0], Config.RenderResolution[1]);

        curState = !editor ? new MainMenuState() : new MapEditorState();

        postShader = Resources.GetShader(null, "resources/data/shaders/psx_post.fs");
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

        curState?.Update(dt);
        GameClient.Update(dt);
    }

    private void Draw()
    {
        Raylib.BeginTextureMode(renderTex);
        Raylib.ClearBackground(Color.Black);

        if (curState != null)
        {
            Raylib.BeginMode3D(curState.Camera);
            curState.Draw();
            Raylib.EndMode3D();

            curState.DrawUI();
        }

        Raylib.EndTextureMode();

        Raylib.BeginDrawing();
        Raylib.ClearBackground(Color.Black);

        Texture2D rtt = renderTex.Texture;
        var src = new Rectangle(0, 0, rtt.Width, -rtt.Height);
        var dest = new Rectangle(0, 0, Raylib.GetScreenWidth(), Raylib.GetScreenHeight());

        Raylib.BeginShaderMode(postShader);
        Raylib.DrawTexturePro(rtt, src, dest, Vector2.Zero, 0, Color.White);
        Raylib.EndShaderMode();

        rlImGui.Begin();
        curState?.DrawImGui();
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

    public void SetState(GameState state)
    {
        curState = state;
    }

    public void Shutdown()
    {
        GameClient.Shutdown();
        Resources.Shutdown();
        Raylib.CloseWindow();
    }
}
