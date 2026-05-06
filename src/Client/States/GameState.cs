using System.Numerics;
using Game.Client.Entities;
using Game.Client.Net;
using ImGuiNET;
using Raylib_cs;

namespace Game.Client.States;

abstract class GameState
{
    public static bool DebugView { get; internal set; } = false;
    public static bool CursorLocked { get; internal set; } = false;

    public Camera3D Camera;
    public string Name { get; }

    private List<GameEntity> entities;

    public GameState(string name, Vector3 camPos, Vector3 camTarget, float fovy)
    {
        Camera = new Camera3D(camPos, camTarget, Vector3.UnitY, fovy, CameraProjection.Perspective);
        Name = name;

        entities = new List<GameEntity>();
    }

    public void PlaceEntity(GameEntity ent)
    {
        entities.Add(ent);
    }

    public virtual void Update(float dt)
    {
        if (Raylib.IsKeyPressed(KeyboardKey.F1))
        {
            GameEntity.DrawWired = !GameEntity.DrawWired;
        }

        if (Raylib.IsKeyPressed(KeyboardKey.F2))
        {
            CursorLocked = !CursorLocked;

            if (CursorLocked)
                Raylib.DisableCursor();
            else
                Raylib.EnableCursor();
        }

        if (Raylib.IsKeyReleased(KeyboardKey.F3))
            DebugView = !DebugView;

        foreach (var ent in entities)
            ent.Update(dt);
    }

    public virtual void Draw()
    {
        foreach (var ent in entities)
        {
            ent.Draw();

            if (DebugView)
                Raylib.DrawBoundingBox(ent.BoundingBox, Color.Red);
        }
    }

    public virtual void DrawUI() { }

    public virtual void DrawImGui()
    {
        if (DebugView)
        {
            ImGui.Begin("DebugView");

            var cfg = Program.Instance!.Config;
            string info = $"FPS: {Raylib.GetFPS()}\n";
            info += $"Render Res: {cfg.RenderResolution[0]}x{cfg.RenderResolution[1]}\n";
            info += $"Window Res: {Raylib.GetScreenWidth()}x{Raylib.GetScreenHeight()}\n";
            info += $"Fullscreen: {Raylib.IsWindowFullscreen()}\n";
            info += $"Server: {GameClient.Config?.Ip}:{GameClient.Config?.Port}\n";
            info += $"Connected: {GameClient.CheckConnection()}\n";
            info += $"Ping: {GameClient.Latency}ms";
            ImGui.Text(info);

            ImGui.End();
        }
    }

    public void ChangeState(GameState next)
    {
        Destroy();
        Program.Instance!.SetState(next);

        Raylib.TraceLog(TraceLogLevel.Info, $"Changed state [{Name} -> {next.Name}]");
    }

    public virtual void Destroy() { }
}
