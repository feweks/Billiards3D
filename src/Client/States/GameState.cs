using System.Numerics;
using Game.Client.Data;
using Game.Client.Data.Files;
using Game.Client.Data.UI;
using Game.Client.Entities;
using Game.Client.Managers;
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
    public UserInterface UI { get; }

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

        UI = new UserInterface(this);
    }

    public void LoadMap(string mapName)
    {
        if (CurLoadedMap != null)
        {
            Raylib.TraceLog(TraceLogLevel.Warning, $"Failed to load map {mapName}: map is already loaded");
            return;
        }

        string mapPath = GetMapPath(mapName);
        MapFileData mapData = ResourcesManager.GetJson(mapPath, MapFileDataCtx.Default.MapFileData);

        foreach (var entData in mapData.Models)
        {
            var mdlEnt = new ModelEntity(entData, mapName);
            PlaceEntity(mdlEnt);
        }

        foreach (var lightData in mapData.Lights)
        {
            var ent = new LightEntity(lightData, mapName);
            PlaceLight(ent);
        }

        foreach (var billboardData in mapData.Billboards)
        {
            var bill = new BillboardEntity(billboardData, mapName);
            PlaceBillboard(bill);
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

    public void RemoveLight(LightEntity light)
    {
        LightEntity.TakenIndexes[light.Index] = false;
        LightingShader.RemoveLight(light);
        Entities.Remove(light);
    }

    public void PlaceBillboard(BillboardEntity billboard)
    {
        billboard.State = this;
        Entities.Add(billboard);
    }

    public void RemoveBillboard(BillboardEntity billboard)
    {
        Entities.Remove(billboard);
    }

    public virtual void Update(float dt)
    {
        if (Raylib.IsKeyPressed(KeyboardKey.F1))
        {
            ModelEntity.DrawWired = !ModelEntity.DrawWired;
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

        if (Raylib.IsKeyReleased(KeyboardKey.F11))
        {
            if (!Raylib.IsWindowFullscreen())
            {
                int monitor = Raylib.GetCurrentMonitor();

                Raylib.SetWindowSize(Raylib.GetMonitorWidth(monitor), Raylib.GetMonitorHeight(monitor));
            }

            Raylib.ToggleFullscreen();
        }

        UI.Update(dt);

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

        foreach (var ent in Entities.Where(e => (e is ModelEntity mdlEnt && mdlEnt.CastsShadow && e.Visible) || (e is BillboardEntity billEnt && billEnt.CastsShadow && e.Visible)))
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

    public virtual void DrawUI()
    {
        UI.Draw();
    }

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
            info += $"TcpServer: {GameClient.Config?.Ip}:{GameClient.Config?.TcpPort}\n";
            info += $"UdpServer: {GameClient.Config?.Ip}:{GameClient.Config?.UdpPort}\n";
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
        for (int i = 0; i < LightingShaderData.LIGHTS_COUNT; i++)
        {
            LightEntity.TakenIndexes[i] = false;
        }
    }
}
