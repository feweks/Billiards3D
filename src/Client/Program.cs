using System.Numerics;
using Game.Client.Data.Files;
using Game.Client.Net;
using Game.Client.States;
using Raylib_cs;
using rlImGui_cs;
using Game.Client.Managers;

namespace Game.Client;

class Program
{
    public static Program? Instance { get; internal set; }

    public bool DebugMode { get; }
    public GameConfigFileData Config { get; }

    private Shader postShader;
    private Shader uiShader;
    private int uiShaderTimeLoc;
    private RenderTexture2D gameRenderTex;
    private RenderTexture2D uiRenderTex;
    private GameState? curState;

    public Program(string[] args)
    {
        Instance ??= this;
        DebugMode = args.Contains("-debug");
        bool editor = args.Contains("-editor");

        Config = ResourcesManager.GetJson("resources/data/game_config.json", GameConfigFileDataCtx.Default.GameConfigFileData);
        Config.ApplyFlags();

        Raylib.InitWindow(1280, 720, Config.WindowTitle); // TODO: Load window size based on settings
        Raylib.InitAudioDevice();
        Raylib.SetExitKey(KeyboardKey.Null);

        ResourcesManager.Init();
        TranslationManager.Init();
        TranslationManager.Load("pl"); // TODO: Load translation based on settings

        rlImGui.Setup(true);
        if (!editor)
            GameClient.Init();

        Raylib.SetTargetFPS(200);

        gameRenderTex = Raylib.LoadRenderTexture(Config.RenderWidth, Config.RenderHeight);
        uiRenderTex = Raylib.LoadRenderTexture(Config.RenderWidth, Config.RenderHeight);

        curState = !editor ? new MainMenuState() : new MapEditorState();

        postShader = ResourcesManager.GetShader(null, "resources/data/shaders/psx_post.fs");
        uiShader = ResourcesManager.GetShader(null, "resources/data/shaders/vcr.fs");

        Texture2D noiseTex = ResourcesManager.GetTexture("resources/gfx/noise.png");
        int noiseTexLoc = Raylib.GetShaderLocation(uiShader, "noiseTex");
        Raylib.SetShaderValueTexture(uiShader, noiseTexLoc, noiseTex);
        uiShaderTimeLoc = Raylib.GetShaderLocation(uiShader, "iTime");
    }

    private void Update(float dt)
    {
        const float DT_TRESHOLD = 0.3f;
        if (dt > DT_TRESHOLD)
        {
            Raylib.TraceLog(TraceLogLevel.Warning, $"Normalizing dt ({dt} -> {DT_TRESHOLD})");
            dt = DT_TRESHOLD;
        }

        Raylib.SetShaderValue(uiShader, uiShaderTimeLoc, (float)Raylib.GetTime(), ShaderUniformDataType.Float);

        curState?.Update(dt);
        GameClient.Update(dt);
    }

    private void Draw()
    {
        Raylib.BeginTextureMode(gameRenderTex);
        Raylib.ClearBackground(Color.Black);

        if (curState != null)
        {
            Raylib.BeginMode3D(curState.Camera);
            curState.Draw();
            Raylib.EndMode3D();
        }

        Raylib.EndTextureMode();

        Raylib.BeginTextureMode(uiRenderTex);
        Raylib.ClearBackground(Color.Blank);

        curState?.DrawUI();

        Raylib.EndTextureMode();

        Raylib.BeginDrawing();
        Raylib.ClearBackground(Color.Black);

        Texture2D gameRtt = gameRenderTex.Texture;
        var gameTexSrc = new Rectangle(0, 0, gameRtt.Width, -gameRtt.Height);
        var gameTexDest = new Rectangle(0, 0, Raylib.GetScreenWidth(), Raylib.GetScreenHeight());

        Raylib.BeginShaderMode(postShader);
        Raylib.DrawTexturePro(gameRtt, gameTexSrc, gameTexDest, Vector2.Zero, 0, Color.White);
        Raylib.EndShaderMode();

        Texture2D uiRtt = uiRenderTex.Texture;
        var uiTexSrc = new Rectangle(0, 0, uiRtt.Width, -uiRtt.Height);
        var uiTexDest = new Rectangle(0, 0, Raylib.GetScreenWidth(), Raylib.GetScreenHeight());

        Raylib.BeginShaderMode(uiShader);
        Raylib.DrawTexturePro(uiRtt, uiTexSrc, uiTexDest, Vector2.Zero, 0, Color.White);
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
        ResourcesManager.Shutdown();
        Raylib.CloseWindow();
    }
}
