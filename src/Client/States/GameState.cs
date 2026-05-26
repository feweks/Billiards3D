using System.Numerics;
using Game.Client.Data.Files;
using Game.Client.Entities;
using Game.Client.Net;
using Game.Common.Enums;
using ImGuiNET;
using Raylib_cs;

namespace Game.Client.States;

abstract class GameState
{
    public const string MAPS_DIRECTORY = "resources/data/maps";

    public static bool DebugView { get; internal set; } = false;
    public static bool CursorLocked { get; internal set; } = false;

    public Camera3D Camera;
    public string Name { get; }

    public string? CurLoadedMap { get; internal set; }
    public List<GameEntity> Entities { get; }

    public GameState(string name, Vector3 camPos, Vector3 camTarget, float fovy)
    {
        Camera = new Camera3D(camPos, camTarget, Vector3.UnitY, fovy, CameraProjection.Perspective);
        Name = name;

        Entities = new List<GameEntity>();
    }

    public void LoadMap(string mapName)
    {
        if (CurLoadedMap != null)
        {
            Raylib.TraceLog(TraceLogLevel.Warning, $"Failed to load map {mapName}: map is already loaded");
            return;
        }

        string mapPath = GetMapPath(mapName);
        MapFileData mapData = Resources.GetJson(mapPath, MapFileDataCtx.Default.MapFileData);

        foreach (var entData in mapData.Entities)
        {
            var ent = new GameEntity(entData.ModelPath, entData.Position, entData.Name)
            {
                Rotation = entData.Rotation,
                Scale = entData.Scale,
                Culling = entData.Culling,
                Map = mapName
            };
            PlaceEntity(ent);
        }

        Raylib.TraceLog(TraceLogLevel.Info, $"Loaded new map [{mapName}]");
        CurLoadedMap = mapName;
    }

    public void UnloadMap()
    {
        if (CurLoadedMap == null)
        {
            Raylib.TraceLog(TraceLogLevel.Warning, $"Failed to unload map: no map is currently loaded");
            return;
        }

        foreach (var ent in Entities.Where(e => e.Map == CurLoadedMap).ToList())
        {
            RemoveEntity(ent);
        }

        Raylib.TraceLog(TraceLogLevel.Info, $"Unloaded current map {CurLoadedMap}");
        CurLoadedMap = null;
    }

    public string GetMapPath(string mapName) => $"{MAPS_DIRECTORY}/{mapName}.json";

    public void PlaceEntity(GameEntity ent)
    {
        Entities.Add(ent);
    }

    public void RemoveEntity(GameEntity ent)
    {
        Entities.Remove(ent);
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

        foreach (var ent in Entities)
            ent.Update(dt);
    }

    public virtual void Draw()
    {
        foreach (var ent in Entities)
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
