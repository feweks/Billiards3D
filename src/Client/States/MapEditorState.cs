using System.Numerics;
using Game.Client.Data;
using Game.Client.Data.Files;
using Game.Client.Entities;
using Game.Client.Managers;
using ImGuiNET;
using Raylib_cs;

namespace Game.Client.States;

enum GuiPopupType
{
    None = 0,
    CreateMap,
    CreateEntity
}

enum TransformationType
{
    None = 0,
    Position,
    Rotation,
    Scale
}

enum EditorEntityType
{
    ModelEntity = 0,
    LightEntity,
    BillboardEntity
}

class SelectedEntityData
{
    public GameEntity? Entity { get; internal set; }
    public string ResourcePathInp = string.Empty;
    private Vector3 transAxes = Vector3.Zero;
    private Vector3 previousTrans;
    private TransformationType transformationType = TransformationType.None;

    public void Select(GameEntity ent)
    {
        if (Entity != null)
            Deselect();

        Entity = ent;
        ResourcePathInp = ent is ModelEntity mdlEnt ? mdlEnt.Path ?? string.Empty : string.Empty;
        ent.Tint = Color.Green;
    }

    public void Deselect()
    {
        if (Entity == null)
            return;

        Entity.Tint = Color.White;
        ResourcePathInp = string.Empty;
        Entity = null;

        if (transformationType != TransformationType.None)
            EndTransform(true);
    }

    public void BeginTransform(TransformationType type, Vector3 curTransform)
    {
        if (transformationType != TransformationType.None)
            EndTransform(true);

        transformationType = type;
        previousTrans = curTransform;
    }

    public void UpdateTransform(float dt)
    {
        if (Entity == null || transformationType == TransformationType.None)
            return;

        if (Raylib.IsKeyReleased(KeyboardKey.X))
        {
            transAxes = new Vector3(1, 0, 0);
        }
        else if (Raylib.IsKeyReleased(KeyboardKey.Y))
        {
            transAxes = new Vector3(0, 1, 0);
        }
        else if (Raylib.IsKeyReleased(KeyboardKey.Z))
        {
            transAxes = new Vector3(0, 0, 1);
        }

        var mouseDelta = Raylib.GetMouseDelta();
        if (mouseDelta.Length() == 0)
            return;

        var changeVec = new Vector3(mouseDelta.X, -mouseDelta.Y, mouseDelta.X) * transAxes * dt;

        switch (transformationType)
        {
            case TransformationType.Position:
                Entity.Position += changeVec;
                break;
            case TransformationType.Rotation:
                Entity.Rotation += changeVec;
                break;
            case TransformationType.Scale:
                Entity.Scale += changeVec;
                break;
        }
    }

    public void EndTransform(bool reset)
    {
        if (reset && Entity != null)
        {
            switch (transformationType)
            {
                case TransformationType.Position:
                    Entity.Position = previousTrans;
                    break;
                case TransformationType.Rotation:
                    Entity.Rotation = previousTrans;
                    break;
                case TransformationType.Scale:
                    Entity.Scale = previousTrans;
                    break;
            }
        }

        transformationType = TransformationType.None;
        previousTrans = Vector3.Zero;
    }
}

class MapEditorState : GameState
{
    private const uint MAX_INP_CHARS = 100;

    private static bool drawGridOpt = true;
    private static bool previewTableOpt = true;
    private static bool toggleLightingOpt = true;
    private static bool drawLightsOriginOpt = false;

    private bool cameraLocked = false;
    private GuiPopupType guiState = GuiPopupType.None;
    private string mapCreationNameInp = string.Empty;
    private int curSelectedMapInp = 0;
    private int curSelectedEntityInp = 0;
    private int curSelectedEntityTypeInp = 0;

    private SelectedEntityData curSelectedEntity;
    private GameEntity poolTable;

    public MapEditorState() : base("map_editor_state", new Vector3(-1, 1, 1), Vector3.One, 75f)
    {
        curSelectedEntity = new SelectedEntityData();

        poolTable = new ModelEntity("resources/gfx/models/pool_table.obj", Vector3.Zero, null)
        {
            CastsShadow = true
        };
        PlaceEntity(poolTable);
    }

    public override void Update(float dt)
    {
        base.Update(dt);

        if (Raylib.IsKeyReleased(KeyboardKey.F4))
            cameraLocked = !cameraLocked;

        if (!cameraLocked)
            Raylib.UpdateCamera(ref Camera, CameraMode.Free);

        poolTable.Visible = previewTableOpt;

        if (ImGui.IsAnyItemActive() || ImGui.IsAnyItemFocused() || ImGui.IsAnyItemHovered())
            return;

        var selEnt = curSelectedEntity.Entity;
        if (selEnt == null)
        {
            if (Raylib.IsMouseButtonReleased(MouseButton.Left))
            {
                var mouseCol = new RayCollision() { Distance = float.MaxValue };
                Ray mouseRay = Raylib.GetScreenToWorldRay(Raylib.GetMousePosition(), Camera);
                GameEntity? mouseEnt = null;

                foreach (var ent in Entities.Where(e => e.Map == CurLoadedMap))
                {
                    var entCol = ent.CheckCollisionRay(mouseRay);
                    if (entCol.Hit && entCol.Distance < mouseCol.Distance)
                    {
                        mouseCol = entCol;
                        mouseEnt = ent;
                    }
                }

                if (mouseCol.Hit && mouseEnt != null)
                {
                    curSelectedEntity.Select(mouseEnt);
                }
            }
        }
        else
        {
            if (Raylib.IsKeyReleased(KeyboardKey.G))
                curSelectedEntity.BeginTransform(TransformationType.Position, selEnt.Position);
            else if (Raylib.IsKeyReleased(KeyboardKey.R))
                curSelectedEntity.BeginTransform(TransformationType.Rotation, selEnt.Rotation);
            else if (Raylib.IsKeyReleased(KeyboardKey.S))
                curSelectedEntity.BeginTransform(TransformationType.Scale, selEnt.Scale);

            if (Raylib.IsMouseButtonReleased(MouseButton.Left))
                curSelectedEntity.EndTransform(false);

            if (Raylib.IsKeyReleased(KeyboardKey.Escape))
                curSelectedEntity.EndTransform(true);

            curSelectedEntity.UpdateTransform(dt);

            if (Raylib.IsKeyReleased(KeyboardKey.Delete))
            {
                DeleteEntity(selEnt);
                return;
            }

            if (Raylib.IsMouseButtonReleased(MouseButton.Right))
            {
                curSelectedEntity.Deselect();
            }
        }
    }

    private void SaveMap(string name)
    {
        string path = GetMapPath(name);

        var mapData = new MapFileData()
        {
            AmbientColor = LightingShader.GetAmbient()
        };
        foreach (var ent in Entities.Where(e => e.Map == name))
        {
            if (ent is ModelEntity mdlEnt)
            {
                mapData.Models.Add(new MapModelFileData(mdlEnt));
            }
            else if (ent is LightEntity lightEnt)
            {
                mapData.Lights.Add(new MapLightFileData(lightEnt));
            }
            else if (ent is BillboardEntity billEnt)
            {
                mapData.Billboards.Add(new MapBillboardFileData(billEnt));
            }
            else
            {
                throw new NotImplementedException($"Map serialization not implemented for entity of type {ent.GetType().Name}");
            }
        }

        ResourcesManager.SaveJson(path, mapData, MapFileDataCtx.Default.MapFileData);
        Raylib.TraceLog(TraceLogLevel.Info, $"Saved map {name} at {path}");
    }

    private void DeleteEntity(GameEntity ent)
    {
        if (ent is ModelEntity mdlEnt)
            RemoveEntity(mdlEnt);
        else if (ent is LightEntity lightEnt)
            RemoveLight(lightEnt);
        else if (ent is BillboardEntity billEnt)
            RemoveBillboard(billEnt);
        else
            throw new NotImplementedException($"Entity deleting not implemented for entity of type {ent.GetType().Name}");
    }

    private void DeleteMap()
    {
        if (CurLoadedMap == null)
            return;

        string mapPath = GetMapPath(CurLoadedMap);
        UnloadMap();

        File.Delete(mapPath);
    }

    public override void Draw()
    {
        base.Draw();

        if (drawGridOpt)
            Raylib.DrawGrid(20, 1);
    }

    public override void DrawImGui()
    {
        base.DrawImGui();

        DrawEditorOptionsGui();
        DrawMapDataGui();
        DrawEntitiesDataGui();

        switch (guiState)
        {
            case GuiPopupType.CreateMap:
                DrawMapCreationGui();
                break;
            case GuiPopupType.CreateEntity:
                DrawEntityCreationGui();
                break;
        }
    }

    private void DrawEditorOptionsGui()
    {
        ImGui.Begin("Editor Options");

        ImGui.Text("F1 - draw wireframe\nF2 - hide & lock mouse\nF3 - display debug view\nF4 - lock camera");

        ImGui.Checkbox("Draw Grid", ref drawGridOpt);
        ImGui.Checkbox("Preview Pool Table", ref previewTableOpt);
        if (ImGui.Checkbox("Preview Lighting", ref toggleLightingOpt))
            LightingShader.Toggle(toggleLightingOpt);
        if (ImGui.Checkbox("Draw Lights Origin", ref drawLightsOriginOpt))
        {
            LightEntity.DrawLightSources = drawLightsOriginOpt;
        }

        ImGui.End();
    }

    private void DrawEntitiesDataGui()
    {
        if (CurLoadedMap == null)
            return;

        ImGui.Begin("Entities");

        var selEnt = curSelectedEntity.Entity;
        if (selEnt != null)
        {
            string entName = selEnt.Name ?? string.Empty;
            if (ImGui.InputText("Entity Name", ref entName, MAX_INP_CHARS))
                selEnt.Name = entName == string.Empty ? null : entName;

            if (selEnt is ModelEntity mdlEnt)
            {
                ImGui.InputText("Entity Model Path", ref curSelectedEntity.ResourcePathInp, MAX_INP_CHARS);
                ImGui.SameLine();
                if (ImGui.Button("Load"))
                    mdlEnt.LoadModelData(curSelectedEntity.ResourcePathInp);

                bool entCulling = mdlEnt.Culling;
                if (ImGui.Checkbox("Entity Culling", ref entCulling))
                    mdlEnt.Culling = entCulling;

                bool entHasShadow = mdlEnt.CastsShadow;
                if (ImGui.Checkbox("Entity Cast Shadow", ref entHasShadow))
                    mdlEnt.CastsShadow = entHasShadow;
            }
            else if (selEnt is LightEntity lightEnt)
            {
                ImGui.Text($"Light Index: {lightEnt.Index}/{LightingShaderData.LIGHTS_COUNT} (Free Left: {LightEntity.TakenIndexes.Count(i => !i)})");

                bool lightEnabled = lightEnt.Enabled;
                if (ImGui.Checkbox("Light Enabled", ref lightEnabled))
                    lightEnt.Enabled = lightEnabled;

                var lightCol = Utils.ColorToVec3(lightEnt.Color);
                if (ImGui.ColorEdit3("Light Color", ref lightCol))
                    lightEnt.Color = Utils.ColorFromVec3(lightCol);

                var lightIntensity = lightEnt.Intensity;
                if (ImGui.InputFloat("Light Intensity", ref lightIntensity))
                    lightEnt.Intensity = lightIntensity;

                var lightDir = lightEnt.Direction;
                if (ImGui.InputFloat3("Light Direction", ref lightDir))
                    lightEnt.Direction = lightDir;

                float lightCutoffAngle = lightEnt.Cutoff;
                if (ImGui.InputFloat("Light Cutoff Angle", ref lightCutoffAngle))
                    lightEnt.Cutoff = lightCutoffAngle;

                float lightExpo = lightEnt.SpotExponent;
                if (ImGui.InputFloat("Light Spot Exponent", ref lightExpo))
                    lightEnt.SpotExponent = lightExpo;
            }
            else if (selEnt is BillboardEntity billEnt)
            {
                string texPath = billEnt.Path ?? string.Empty;
                if (ImGui.InputText("Billboard Texture Path", ref texPath, MAX_INP_CHARS))
                    billEnt.Path = texPath == string.Empty ? null : texPath;
                ImGui.SameLine();
                if (ImGui.Button("Load"))
                    billEnt.LoadTexture(billEnt.Path);

                bool castsShadow = billEnt.CastsShadow;
                if (ImGui.Checkbox("Billboard Casts Shadow", ref castsShadow))
                    billEnt.CastsShadow = castsShadow;

                Vector2 size = billEnt.Size;
                if (ImGui.InputFloat2("Billboard Size", ref size))
                    billEnt.Size = size;
            }

            Vector3 entPos = selEnt.Position;
            if (ImGui.InputFloat3("Entity Position", ref entPos))
                selEnt.Position = entPos;
            ImGui.SameLine();
            if (ImGui.Button("Reset##pos"))
                selEnt.Position = Vector3.Zero;

            Vector3 entRot = selEnt.Rotation;
            if (ImGui.InputFloat3("Entity Rotation", ref entRot))
                selEnt.Rotation = entRot;
            ImGui.SameLine();
            if (ImGui.Button("Reset##rot"))
                selEnt.Rotation = Vector3.Zero;

            Vector3 entScale = selEnt.Scale;
            if (ImGui.InputFloat3("Entity Scale", ref entScale))
                selEnt.Scale = entScale;
            ImGui.SameLine();
            if (ImGui.Button("Reset##scale"))
                selEnt.Scale = Vector3.One;
        }
        else
        {
            var mapEntities = Entities.Where(e => e.Map == CurLoadedMap).ToList();
            string[] entitiesNames = new string[mapEntities.Count];
            for (int i = 0; i < mapEntities.Count; i++)
            {
                var ent = mapEntities[i];
                string entName;
                if (ent is ModelEntity)
                    entName = "Model";
                else if (ent is LightEntity)
                    entName = "Light";
                else if (ent is BillboardEntity)
                    entName = "Billboard";
                else
                    throw new NotImplementedException($"Name displaying not impelmented for entity of type {ent.GetType().Name}");

                entName += $" #{i + 1}";
                if (ent.Name != null)
                    entName += $" ({ent.Name})";

                entitiesNames[i] = entName;
            }

            ImGui.Combo("Entities List", ref curSelectedEntityInp, entitiesNames, entitiesNames.Length);
            ImGui.SameLine();
            if (ImGui.Button("Select"))
            {
                curSelectedEntity.Select(mapEntities[curSelectedEntityInp]);
            }
        }

        if (ImGui.Button("Create"))
        {
            guiState = GuiPopupType.CreateEntity;
        }
        ImGui.SameLine();
        if (curSelectedEntity.Entity != null)
        {
            if (ImGui.Button("Delete"))
            {
                DeleteEntity(curSelectedEntity.Entity);
                curSelectedEntity.Deselect();
            }
            ImGui.SameLine();
            if (ImGui.Button("Deselect"))
            {
                curSelectedEntity.Deselect();
            }
            ImGui.SameLine();
            if (ImGui.Button("Duplicate"))
            {
                GameEntity dupedEnt;

                if (selEnt is ModelEntity mdlEnt)
                {
                    dupedEnt = mdlEnt.Copy();
                    PlaceEntity(dupedEnt);
                }
                else if (selEnt is LightEntity lightEnt)
                {
                    dupedEnt = lightEnt.Copy();
                    PlaceLight((LightEntity)dupedEnt);
                }
                else if (selEnt is BillboardEntity billEnt)
                {
                    dupedEnt = billEnt.Copy();
                    PlaceBillboard((BillboardEntity)dupedEnt);
                }
                else
                {
                    throw new NotImplementedException($"Duplicating not implemented for entity of type {selEnt?.GetType().Name}");
                }

                curSelectedEntity.Deselect();
                curSelectedEntity.Select(dupedEnt);
            }
        }

        ImGui.End();
    }

    private void DrawMapDataGui()
    {
        ImGui.Begin("Map Data");

        bool isMapLoaded = CurLoadedMap != null;

        if (!isMapLoaded)
            ImGui.Text("No map is currently loaded");
        else
            ImGui.Text($"Currently loaded map: {CurLoadedMap}");

        string[] availableMaps = ResourcesManager.GetDirectoryFiles(MAPS_DIRECTORY).Select(m => m.Replace(MAPS_DIRECTORY, string.Empty).Replace(Path.DirectorySeparatorChar.ToString(), string.Empty).Replace(".json", string.Empty)).ToArray();
        ImGui.Combo("Select Map", ref curSelectedMapInp, availableMaps, availableMaps.Length);
        ImGui.SameLine();
        if (ImGui.Button("Load"))
        {
            LoadMap(availableMaps[curSelectedMapInp]);
        }

        if (ImGui.Button("Create"))
        {
            guiState = GuiPopupType.CreateMap;
        }
        ImGui.SameLine();
        if (isMapLoaded)
        {
            if (ImGui.Button("Save"))
            {
                SaveMap(CurLoadedMap!);
            }
            ImGui.SameLine();
            if (ImGui.Button("Delete"))
            {
                DeleteMap();
            }

            Vector3 mapAmbientCol = LightingShader.GetAmbient();
            if (ImGui.ColorEdit3("Ambient Color", ref mapAmbientCol))
                LightingShader.SetAmbient(mapAmbientCol);
        }

        ImGui.End();
    }

    private void DrawMapCreationGui()
    {
        ImGui.Begin("Create Map");

        bool canCreateMap = true;
        ImGui.InputText("Map Name", ref mapCreationNameInp, MAX_INP_CHARS);

        string mapPath = GetMapPath(mapCreationNameInp);
        if (File.Exists(mapPath))
        {
            ImGui.TextColored(Utils.ColorToVec4(Color.Red), "This map already exists");
            canCreateMap = false;
        }

        if (ImGui.Button("Create") && canCreateMap)
        {
            if (CurLoadedMap != null)
                UnloadMap();
            SaveMap(mapCreationNameInp);
            LoadMap(mapCreationNameInp);
            mapCreationNameInp = string.Empty;
            guiState = GuiPopupType.None;
        }
        ImGui.SameLine();
        if (ImGui.Button("Cancel"))
            guiState = GuiPopupType.None;

        ImGui.End();
    }

    private void DrawEntityCreationGui()
    {
        ImGui.Begin("Create Entity");

        string[] entityTypeNames = Enum.GetNames<EditorEntityType>();
        ImGui.Combo("Entity Type", ref curSelectedEntityTypeInp, entityTypeNames, entityTypeNames.Length);

        if (ImGui.Button("Create"))
        {
            var entType = (EditorEntityType)curSelectedEntityTypeInp;
            GameEntity ent;

            if (entType == EditorEntityType.LightEntity)
            {
                ent = new LightEntity(CurLoadedMap);
                PlaceLight((LightEntity)ent);
            }
            else if (entType == EditorEntityType.ModelEntity)
            {
                ent = new ModelEntity(CurLoadedMap);
                PlaceEntity(ent);
            }
            else if (entType == EditorEntityType.BillboardEntity)
            {
                ent = new BillboardEntity(CurLoadedMap);
                PlaceBillboard((BillboardEntity)ent);
            }
            else
            {
                throw new NotImplementedException($"Entity creation not implemented for entity of type {entType}");
            }
            curSelectedEntity.Select(ent);

            guiState = GuiPopupType.None;
        }
        ImGui.SameLine();
        if (ImGui.Button("Cancel"))
        {
            guiState = GuiPopupType.None;
        }

        ImGui.End();
    }
}
