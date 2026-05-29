using System.Numerics;
using Game.Client.Data;
using Game.Client.Data.Files;
using Game.Client.Entities;
using Game.Client.Net;
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
    public LightingShaderData LightingShader { get; }
    public List<GameEntity> Entities { get; }

    private ShadowData shadowData;

    public GameState(string name, Vector3 camPos, Vector3 camTarget, float fovy)
    {
        shadowData = new ShadowData();

        Camera = new Camera3D(camPos, camTarget, Vector3.UnitY, fovy, CameraProjection.Perspective);
        Name = name;
        LightingShader = new LightingShaderData();
        LightingShader.SetAmbient(Vector3.One);
        LightingShader.Toggle(true);

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
                Map = mapName,
                HasShadow = entData.HasShadow
            };
            PlaceEntity(ent);
        }

        foreach (var lightData in mapData.Lights)
        {
            var ent = new LightEntity(lightData.Position, lightData.Name)
            {
                Rotation = lightData.Rotation,
                Scale = lightData.Scale,
                Map = mapName,
                Enabled = lightData.Enabled,
                Color = lightData.Color,
                Intensity = lightData.Intensity,
                Direction = lightData.Direction,
                Cutoff = lightData.Cutoff,
                SpotExponent = lightData.SpotExponent
            };
            PlaceLight(ent);
        }

        LightingShader.SetAmbient(mapData.AmbientColor);

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

    public static string GetMapPath(string mapName) => $"{MAPS_DIRECTORY}/{mapName}.json";

    public void PlaceEntity(GameEntity ent)
    {
        ent.SetLightingShader(LightingShader);
        Entities.Add(ent);
    }

    public void RemoveEntity(GameEntity ent)
    {
        Entities.Remove(ent);
    }

    public void PlaceLight(LightEntity light)
    {
        light.SetLightingShader(LightingShader);
        Entities.Add(light);
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
        Rlgl.EnableDepthMask();
        foreach (var ent in Entities)
        {
            ent.Draw();

            if (DebugView)
                Raylib.DrawBoundingBox(ent.BoundingBox, Color.Red);
        }

        Rlgl.DisableDepthMask();

        foreach (var ent in Entities.Where(e => e.HasShadow && e.Visible))
        {
            float sizeX = (ent.BoundingBox.Max.X - ent.BoundingBox.Min.X) * 1.5f;
            float sizeZ = (ent.BoundingBox.Max.Z - ent.BoundingBox.Min.Z) * 1.5f;
            var pos = new Vector3(
                ent.BoundingBox.Min.X + (ent.BoundingBox.Max.X - ent.BoundingBox.Min.X) / 2f,
                ent.BoundingBox.Min.Y + 0.001f,
                ent.BoundingBox.Min.Z + (ent.BoundingBox.Max.Z - ent.BoundingBox.Min.Z) / 2f
            );

            shadowData.Draw(pos, sizeX, sizeZ);
        }

        Rlgl.EnableDepthMask();
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

    public virtual void Destroy()
    {
        LightEntity.IndexCounter = 0;
    }
}
