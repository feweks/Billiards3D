using System.Numerics;
using Game.Client.Data.Files;
using Game.Client.Entities;
using ImGuiNET;
using Raylib_cs;

namespace Game.Client.States;

enum TransformationType
{
    None = 0,
    Position,
    Rotation,
    Scale
}

class MapEditorState : GameState
{
    private const string MAPS_DIR = "resources/data/maps/";

    bool lockCamera = false;
    int curSelectedMap = 0;

    GameEntity? curSelectedEntity;
    Vector3 curSelectedEntityTransAxis;
    Vector3 curSelectedEntityPreviousTrans;
    TransformationType transType = TransformationType.None;

    public MapEditorState() : base("map_editor_state", new Vector3(-1, 1, -1), new Vector3(0, 1, 0), 75)
    {

    }

    public override void Update(float dt)
    {
        base.Update(dt);

        if (Raylib.IsKeyReleased(KeyboardKey.F4))
            lockCamera = !lockCamera;

        if (!lockCamera)
            Raylib.UpdateCamera(ref Camera, CameraMode.Free);

        if (curSelectedEntity == null)
        {
            if (Raylib.IsMouseButtonReleased(MouseButton.Left))
            {
                Ray clickRay = Raylib.GetScreenToWorldRay(Raylib.GetMousePosition(), Camera);
                var clickCol = new RayCollision()
                {
                    Hit = false,
                    Distance = float.MaxValue
                };
                GameEntity? clickEnt = null;

                foreach (var ent in Entities)
                {
                    var entCol = ent.CheckCollisionRay(clickRay);
                    if (entCol.Hit && entCol.Distance < clickCol.Distance)
                    {
                        clickCol = entCol;
                        clickEnt = ent;
                    }
                }

                if (clickCol.Hit && clickEnt != null && clickEnt.Map == CurLoadedMap)
                {
                    curSelectedEntity = clickEnt;
                    curSelectedEntity.Tint = Color.Green;
                }
            }
        }
        else
        {
            if (Raylib.IsKeyReleased(KeyboardKey.G))
            {
                transType = TransformationType.Position;
                curSelectedEntityPreviousTrans = curSelectedEntity.Position;
            }

            if (Raylib.IsKeyReleased(KeyboardKey.R))
            {
                transType = TransformationType.Rotation;
                curSelectedEntityPreviousTrans = curSelectedEntity.Rotation;
            }

            if (Raylib.IsKeyReleased(KeyboardKey.S))
            {
                transType = TransformationType.Scale;
                curSelectedEntityPreviousTrans = curSelectedEntity.Scale;
            }

            if (transType != TransformationType.None)
            {
                if (Raylib.IsKeyReleased(KeyboardKey.X))
                    curSelectedEntityTransAxis = Vector3.UnitX;

                if (Raylib.IsKeyReleased(KeyboardKey.Y))
                    curSelectedEntityTransAxis = Vector3.UnitY;

                if (Raylib.IsKeyReleased(KeyboardKey.Z))
                    curSelectedEntityTransAxis = Vector3.UnitZ;

                var modAmount = Raylib.GetMouseDelta() * 0.01f;
                if (transType == TransformationType.Rotation)
                    modAmount *= 5;

                var mod = new Vector3(
                    modAmount.X * curSelectedEntityTransAxis.X,
                    modAmount.Y * curSelectedEntityTransAxis.Y,
                    modAmount.X * curSelectedEntityTransAxis.Z
                );

                bool reset = Raylib.IsKeyReleased(KeyboardKey.Escape);
                switch (transType)
                {
                    case TransformationType.Position:
                        curSelectedEntity.Position += mod;
                        if (reset)
                            curSelectedEntity.Position = curSelectedEntityPreviousTrans;
                        break;
                    case TransformationType.Rotation:
                        curSelectedEntity.Rotation += mod;
                        if (reset)
                            curSelectedEntity.Rotation = curSelectedEntityPreviousTrans;
                        break;
                    case TransformationType.Scale:
                        curSelectedEntity.Scale += mod;
                        if (reset)
                            curSelectedEntity.Scale = curSelectedEntityPreviousTrans;
                        break;
                }

                if (Raylib.IsMouseButtonReleased(MouseButton.Left) || reset)
                {
                    transType = TransformationType.None;
                    curSelectedEntityTransAxis = Vector3.Zero;
                    curSelectedEntityPreviousTrans = Vector3.Zero;
                }
            }

            if (Raylib.IsKeyReleased(KeyboardKey.Delete))
            {
                RemoveEntity(curSelectedEntity);
                curSelectedEntity = null;
            }

            if (Raylib.IsMouseButtonReleased(MouseButton.Right))
            {
                curSelectedEntity!.Tint = Color.White;
                curSelectedEntity = null;
            }
        }
    }

    private void SaveMap()
    {
        if (CurLoadedMap == null)
            return;

        string mapPath = MAPS_DIR + CurLoadedMap + ".json";

        var mapData = new MapFileData();
        foreach (var ent in Entities)
        {
            if (ent.Map != CurLoadedMap)
                continue;

            mapData.Entities.Add(new MapEntityFileData()
            {
                ModelPath = ent.ModelPath,
                Culling = ent.Culling,
                Position = ent.Position,
                Rotation = ent.Rotation,
                Scale = ent.Scale,
                Name = ent.Name
            });
        }

        Resources.SaveJson(mapPath, mapData, MapFileDataCtx.Default.MapFileData);
        Raylib.TraceLog(TraceLogLevel.Info, $"Saved map to {mapPath}");
    }

    public override void Draw()
    {
        base.Draw();

        Raylib.DrawGrid(20, 1);
    }

    public override void DrawImGui()
    {
        base.DrawImGui();

        DrawMapDataGui();
        DrawEntityDataGui();
    }

    private void DrawMapDataGui()
    {
        ImGui.Begin("Map Data");

        if (CurLoadedMap != null)
            ImGui.Text($"Currently loaded map: {CurLoadedMap}");
        else
            ImGui.Text($"No loaded map");

        string[] maps = Resources.GetDirectoryFiles(MAPS_DIR).Select(p => p.Replace(".json", string.Empty).Replace(MAPS_DIR, string.Empty)).ToArray();
        ImGui.Combo("Maps", ref curSelectedMap, maps, maps.Length);

        if (ImGui.Button("Load"))
        {
            if (CurLoadedMap != null)
                UnloadMap();
            LoadMap(maps[curSelectedMap]);
        }

        if (CurLoadedMap != null)
        {
            ImGui.SameLine();
            if (ImGui.Button("Save"))
            {
                SaveMap();
            }
        }

        ImGui.End();
    }

    private void DrawEntityDataGui()
    {
        if (curSelectedEntity == null)
            return;

        ImGui.Begin("Entity Data");

        ImGui.Text($"Entity {curSelectedEntity.ModelPath}");

        Vector3 entPos = curSelectedEntity.Position;
        if (ImGui.InputFloat3("Entity Position", ref entPos))
            curSelectedEntity.Position = entPos;

        Vector3 entRot = curSelectedEntity.Rotation;
        if (ImGui.InputFloat3("Entity Rotation", ref entRot))
            curSelectedEntity.Rotation = entRot;

        Vector3 entScale = curSelectedEntity.Scale;
        if (ImGui.InputFloat3("Entity Scale", ref entScale))
            curSelectedEntity.Scale = entScale;

        if (ImGui.Button("Remove Entity"))
        {
            RemoveEntity(curSelectedEntity);
            curSelectedEntity = null;
        }

        ImGui.End();
    }
}
