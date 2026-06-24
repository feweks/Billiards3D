using System.Numerics;
using Game.Client.Data.UI.Widgets;
using Game.Client.Managers;
using Game.Client.Net;
using Game.Common.Data;
using Game.Common.Enums;
using Game.Common.Packets;
using ImGuiNET;
using Raylib_cs;

namespace Game.Client.States;

class MainMenuState : GameState
{
    string nickInp = "";
    string codeInp = "";
    int curMapInp = 0;

    private Font miscFnt;

    Vector3 cameraBasePos = new Vector3(0.615f, 1.513f, 2.916f);
    Vector3 cameraBaseTarget = new Vector3(-1.22f, 1.483f, 2.185f);
    float elapsedSwayTime = 0.01f;
    float swaySpeed = 0.5f;
    float horizontalSwayAmount = 0.15f;
    float verticalSwayAmount = 0.07f;

    Font titleFnt;
    float titleY = 75;
    int titleFntSize = 102;
    Color[] titleColors;
    float titleSwayTime = 2.88f;
    float titleElapsedSwayTime = 0f;
    float titleRotTarget = 5;
    float titleRot = -5;
    float bpm = 168;

    private Music music;

    public MainMenuState() : base("main_menu_state", new Vector3(1, 1, 1), new Vector3(0, 0, 0), 75f)
    {
        Camera.Position = cameraBasePos;
        Camera.Target = cameraBaseTarget;

        titleFnt = ResourcesManager.GetFont("resources/gfx/fonts/whacky_joe.ttf");
        miscFnt = ResourcesManager.GetFont("resources/gfx/fonts/pixellari.ttf");

        titleColors = [Color.White, Color.White];

        music = Raylib.LoadMusicStream("resources/music/main_menu.ogg");
        music.Looping = true;

        Raylib.SetMasterVolume(0.05f);

        LoadMap("mnu_jan");
        titleSwayTime = 60000f / bpm * 4f / 1000f;
        Raylib.TraceLog(TraceLogLevel.Info, $"Title sway time: {titleSwayTime}");

        Vector2 screenCenter = new Vector2(Program.Instance!.Config.RenderWidth / 2, Program.Instance!.Config.RenderHeight / 2);

        //UI.Widgets.Add(new DynamicBoxUIWidget(screenCenter, new Vector2(200, 75), true));
        var cont = new ContainerUIWidget(screenCenter, new Vector2(400, 400), "box");
        cont.AddChildWidget(new DynamicBoxUIWidget(cont.Size / 2, new Vector2(100, 50), true));
        cont.AddChildWidget(new TextUIWidget(new Vector2(50, 50), "test", "resources/gfx/fonts/pixellari.ttf", 24, true, Color.White, Color.Black));
        cont.AddChildWidget(new ButtonUIWidget(new Vector2(150, 150), new Vector2(300, 75), "TEST BTTN"));
        UI.Widgets.Add(cont);

        Raylib.PlayMusicStream(music);
    }

    public override void Update(float dt)
    {
        base.Update(dt);

        Raylib.UpdateMusicStream(music);

        elapsedSwayTime += dt * swaySpeed;
        float camOffsetX = (MathF.Sin(elapsedSwayTime * 0.8f) * 0.7f + MathF.Sin(elapsedSwayTime * 1.5f) * 0.3f) * horizontalSwayAmount;
        float camOffsetY = (MathF.Sin(elapsedSwayTime * 1.1f) * 0.6f + MathF.Sin(elapsedSwayTime * 2.3f) * 0.4f) * verticalSwayAmount;

        Camera.Target = new Vector3(
            cameraBaseTarget.X + camOffsetX,
            cameraBaseTarget.Y + camOffsetY,
            cameraBaseTarget.Z
        );

        titleElapsedSwayTime += dt;
        if (titleElapsedSwayTime >= titleSwayTime)
        {
            titleElapsedSwayTime = 0.01f;
            titleRotTarget *= -1f;
        }

        float t = titleElapsedSwayTime / titleSwayTime;
        titleRot = Raymath.LerpAngle(-titleRotTarget, titleRotTarget, Utils.EaseValue(EasingType.EaseInCirc, t));

        if (GameClient.Lobby.Data != null && GameClient.Lobby.Data.Started)
        {
            Thread.Sleep(100);
            ChangeState(new PlayState());
        }
    }

    public override void Draw()
    {
        base.Draw();
    }

    public override void DrawUI()
    {
        base.DrawUI();

        string versionText = $"{TranslationManager.Get("mainmenu.title").Replace(';', ' ').ToLower()} v{GameData.Version}";
        float versionFontSize = 24;
        int versionTextY = (int)(Program.Instance!.Config.RenderHeight - Raylib.MeasureTextEx(miscFnt, versionText, versionFontSize, 1).Y);
        Utils.DrawTextOutlined(miscFnt, versionText, new Vector2(0, versionTextY), versionFontSize, Color.White, Color.Black);

        string[] titleText = TranslationManager.Get("mainmenu.title").Split(';');
        float titleYOffset = titleY;
        for (int i = 0; i < titleText.Length; i++)
        {
            string titlePart = titleText[i];
            Color titleCol = titleColors[i];

            Vector2 titleTextSize = Raylib.MeasureTextEx(titleFnt, titlePart, titleFntSize, 1);

            Utils.DrawTextOutlinedEx(titleFnt, titlePart, new Vector2(Program.Instance!.Config.RenderWidth / 2, titleYOffset), titleTextSize * 0.5f, titleFntSize, titleRot, titleCol, Color.Black);

            titleYOffset += titleFntSize * 0.8f;
        }

        bool connected = GameClient.ClientGuid != Guid.Empty && GameClient.CheckConnection();
        string connectionText = connected ? $"Connected to server (GUID: {GameClient.ClientGuid})" : "Not connected";
        Color connCol = connected ? Color.Green : Color.Red;
        Vector2 connSize = Raylib.MeasureTextEx(Raylib.GetFontDefault(), connectionText, 18, 1);

        Raylib.DrawText(connectionText, (int)(Program.Instance!.Config.RenderWidth - connSize.X), 0, 18, connCol);
    }

    public override void DrawImGui()
    {
        base.DrawImGui();

        ImGui.Begin("MainMenu");

        if (GameClient.Lobby.Status != JoinedLobbyStatus.Success)
        {
            ImGui.InputText("Nickname", ref nickInp, 32);
            ImGui.InputText("Code", ref codeInp, GameData.LobbyCodeLength);

            if (ImGui.Button("Host"))
            {
                GameClient.HostLobby(nickInp);
            }
            ImGui.SameLine();
            if (ImGui.Button("Join"))
            {
                GameClient.JoinLobby(codeInp, nickInp);
            }

            if (GameClient.Lobby.Status == JoinedLobbyStatus.NickCollision)
            {
                ImGui.TextColored(Utils.ColorToVec4(Color.Red), "Failed to join lobby: player with that name is already in");
            }
            else if (GameClient.Lobby.Status == JoinedLobbyStatus.Missing)
            {
                ImGui.TextColored(Utils.ColorToVec4(Color.Red), "Failed to join lobby: lobby with that code does not exist");
            }
            else if (GameClient.Lobby.Status == JoinedLobbyStatus.Full)
            {
                ImGui.TextColored(Utils.ColorToVec4(Color.Red), "Failed to join lobby: lobby is full");
            }
        }
        else if (GameClient.Lobby.Status == JoinedLobbyStatus.Success && GameClient.Lobby.Data != null)
        {
            ImGui.Text($"Lobby {GameClient.Lobby.Data.Code} ({GameClient.Lobby.Data.GetPlayerCount()}/2)");
            ImGui.SameLine();
            if (ImGui.Button("Copy"))
            {
                Raylib.SetClipboardText(GameClient.Lobby.Data.Code);
            }

            ImGui.TextColored(Utils.ColorToVec4(Color.Green), $"{GameClient.Lobby.Data.Host.Nickname}");

            if (GameClient.Lobby.Data.Guest.Nickname != null)
            {
                ImGui.Text($"{GameClient.Lobby.Data.Guest.Nickname}");
            }

            if (GameClient.Lobby.Settings != null)
            {
                var maps = Utils.GetPlayableMaps();

                if (GameClient.IsHost())
                {
                    bool changedSetting = false;

                    if (ImGui.Combo("Current Map", ref curMapInp, maps, maps.Length))
                    {
                        GameClient.Lobby.Settings.MapIndex = (ushort)curMapInp;
                        changedSetting = true;
                    }

                    bool enableHelperLines = GameClient.Lobby.Settings.EnableHelperLines;
                    if (ImGui.Checkbox("Enable Helper Lines", ref enableHelperLines))
                    {
                        GameClient.Lobby.Settings.EnableHelperLines = enableHelperLines;
                        changedSetting = true;
                    }

                    if (changedSetting)
                    {
                        GameClient.SendLobbyPacket(new ChangeLobbySettingsPacket() { Settings = GameClient.Lobby.Settings });
                        Raylib.TraceLog(TraceLogLevel.Info, $"Updated lobby settings");
                    }
                }
                else
                {
                    ImGui.Text($"Selected map: {maps[GameClient.Lobby.Settings.MapIndex]}");
                    ImGui.Text($"Helper Lines: {GameClient.Lobby.Settings.EnableHelperLines}");
                }
            }

            if (GameClient.Lobby.Data.GetPlayerCount() == 2 && GameClient.IsHost())
            {
                if (ImGui.Button("Start"))
                {
                    GameClient.StartLobby();
                }
            }

            if (ImGui.Button("Leave"))
            {
                GameClient.LeaveLobby();
            }
        }

        ImGui.End();
    }

    /*public override void OnUIWidgetClicked(string name, UIWidget widget)
    {
        switch (name)
        {
            case "play":
                {
                    var startingContainer = UI.GetContainerByName("menu");
                    var lobbiesContainer = UI.GetContainerByName("lobby-list");
                    if (startingContainer == null || lobbiesContainer == null)
                        break;

                    startingContainer.Active = startingContainer.Visible = false;
                    lobbiesContainer.Active = lobbiesContainer.Visible = true;

                    break;
                }
            case "settings":
                {
                    Raylib.TraceLog(TraceLogLevel.Info, $"Settings");
                    break;
                }
            case "quit":
                {
                    Program.Instance!.Shutdown();
                    break;
                }
        }
    }*/
}
