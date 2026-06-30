using System.Numerics;
using Game.Client.Data.UI.Widgets;
using Game.Client.Data.UI.Widgets.Input;
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
    private const float MENU_MUSIC_BPM = 168;

    string nickInp = "";
    string codeInp = "";
    int curMapInp = 0;

    Vector3 camBasePos = new Vector3(0.615f, 1.513f, 2.916f);
    Vector3 camBaseTarget = new Vector3(-1.22f, 1.483f, 2.185f);
    float elapsedCamSwayTime = 0.01f;
    float camSwaySpeed = 0.5f;
    float horizontalCamSwayAmount = 0.15f;
    float verticalCamSwayAmount = 0.07f;

    TextUIWidget? titleText1;
    TextUIWidget? titleText2;
    float titleSwayTime = 0f;
    float titleElapsedSwayTime = 0f;
    float titleRotTarget = 5;
    float titleRot = -5;

    private Music music;

    public MainMenuState() : base("main_menu_state", new Vector3(1, 1, 1), new Vector3(0, 0, 0), 75f)
    {
        Camera.Position = camBasePos;
        Camera.Target = camBaseTarget;

        music = Raylib.LoadMusicStream("resources/music/main_menu.ogg");
        music.Looping = true;

        Raylib.SetMasterVolume(0.05f);

        LoadMap("mnu_jan");
        titleSwayTime = 60000f / MENU_MUSIC_BPM * 4f / 1000f;
        Raylib.TraceLog(TraceLogLevel.Info, $"Title sway time: {titleSwayTime}");

        titleText1 = UI.Root.GetChildWidgetByName("title1") as TextUIWidget;
        titleText2 = UI.Root.GetChildWidgetByName("title2") as TextUIWidget;
        if (UI.Root.GetChildWidgetByName("version") is TextUIWidget versionText)
            versionText.Text = TranslationManager.Get("mainmenu.version", GameData.Version.ToString());

        Raylib.PlayMusicStream(music);
    }

    public override void Update(float dt)
    {
        base.Update(dt);

        Raylib.UpdateMusicStream(music);

        elapsedCamSwayTime += dt * camSwaySpeed;
        float camOffsetX = (MathF.Sin(elapsedCamSwayTime * 0.8f) * 0.7f + MathF.Sin(elapsedCamSwayTime * 1.5f) * 0.3f) * horizontalCamSwayAmount;
        float camOffsetY = (MathF.Sin(elapsedCamSwayTime * 1.1f) * 0.6f + MathF.Sin(elapsedCamSwayTime * 2.3f) * 0.4f) * verticalCamSwayAmount;

        Camera.Target = new Vector3(
            camBaseTarget.X + camOffsetX,
            camBaseTarget.Y + camOffsetY,
            camBaseTarget.Z
        );

        titleElapsedSwayTime += dt;
        if (titleElapsedSwayTime >= titleSwayTime)
        {
            titleElapsedSwayTime = 0f;
            titleRotTarget *= -1f;
        }

        float t = titleElapsedSwayTime / titleSwayTime;
        titleRot = Raymath.LerpAngle(-titleRotTarget, titleRotTarget, Utils.EaseValue(EasingType.EaseInCirc, t));

        if (titleText1 != null && titleText2 != null)
        {
            titleText1.Rotation = titleRot;
            titleText2.Rotation = titleRot;
        }

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

    public override void OnUIWidgetClicked(string name, UIWidget widget)
    {
        switch (name)
        {
            case "play":
                {
                    var menuCont = UI.Root.GetChildWidgetByName("menu");
                    var lobbiesCont = UI.Root.GetChildWidgetByName("lobby-list");
                    if (menuCont == null || lobbiesCont == null)
                        break;

                    menuCont.Active = menuCont.Visible = false;
                    lobbiesCont.Active = lobbiesCont.Visible = true;

                    break;
                }
            case "settings":
                {
                    Raylib.TraceLog(TraceLogLevel.Info, $"Clicked settings");
                    break;
                }
            case "quit":
                {
                    Program.Instance!.Shutdown();
                    break;
                }
            case "lobbies-return":
                {
                    var menuCont = UI.Root.GetChildWidgetByName("menu");
                    var lobbiesCont = UI.Root.GetChildWidgetByName("lobby-list");
                    if (menuCont == null || lobbiesCont == null)
                        break;

                    menuCont.Active = menuCont.Visible = true;
                    lobbiesCont.Active = lobbiesCont.Visible = false;

                    break;
                }
        }
    }
}
