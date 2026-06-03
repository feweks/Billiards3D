using System.Numerics;
using Game.Client.Data;
using Game.Client.Entities;
using Game.Client.Net;
using Game.Common;
using Game.Common.Data;
using Game.Common.Enums;
using Game.Common.Packets;
using ImGuiNET;
using Raylib_cs;

namespace Game.Client.States;

class MainMenuState : GameState
{
    private string nickInp = "";
    private string codeInp = "";
    private int curMapInp = 0;

    public MainMenuState() : base("main_menu_state", new Vector3(1, 1, 1), new Vector3(0, 0, 0), 75f)
    {
    }

    public override void Update(float dt)
    {
        base.Update(dt);

        if (GameClient.Lobby.Data != null && GameClient.Lobby.Data.Started)
        {
            Thread.Sleep(100);
            ChangeState(new PlayState());
        }
    }

    public override void Draw()
    {
        base.Draw();

        Raylib.DrawGrid(5, 3);
    }

    public override void DrawUI()
    {
        base.DrawUI();

        string txt = $"billiards v{GameData.Version}";
        int txtY = (int)(Program.Instance!.Config.RenderResolution[1] - Raylib.MeasureTextEx(Raylib.GetFontDefault(), txt, 24, 1).Y);
        Raylib.DrawText(txt, 3, txtY, 24, Color.White);
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
                string[] maps = ["test_room", "jan"];

                if (GameClient.IsHost())
                {
                    if (ImGui.Combo("Current Map", ref curMapInp, maps, maps.Length))
                    {
                        GameClient.Lobby.Settings.MapIndex = (ushort)curMapInp;
                        Raylib.TraceLog(TraceLogLevel.Info, $"Changed curmap to {maps[curMapInp]}");
                        GameClient.SendLobbyPacket(new ChangeLobbySettingsPacket() { Settings = GameClient.Lobby.Settings });
                    }
                }
                else
                {
                    ImGui.Text($"Selected map: {maps[GameClient.Lobby.Settings.MapIndex]}");
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
}
