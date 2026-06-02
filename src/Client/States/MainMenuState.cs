using System.Numerics;
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

        if (GameClient.LobbyData != null && GameClient.LobbyData.Started)
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

        if (GameClient.LobbyStatus != JoinedLobbyStatus.Success)
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

            if (GameClient.LobbyStatus == JoinedLobbyStatus.NickCollision)
            {
                ImGui.TextColored(Utils.ColorToVec4(Color.Red), "Failed to join lobby: player with that name is already in");
            }
            else if (GameClient.LobbyStatus == JoinedLobbyStatus.Missing)
            {
                ImGui.TextColored(Utils.ColorToVec4(Color.Red), "Failed to join lobby: lobby with that code does not exist");
            }
            else if (GameClient.LobbyStatus == JoinedLobbyStatus.Full)
            {
                ImGui.TextColored(Utils.ColorToVec4(Color.Red), "Failed to join lobby: lobby is full");
            }
        }
        else if (GameClient.LobbyStatus == JoinedLobbyStatus.Success && GameClient.LobbyData != null)
        {
            ImGui.Text($"Lobby {GameClient.LobbyData.Code} ({GameClient.LobbyData.GetPlayerCount()}/2)");
            ImGui.SameLine();
            if (ImGui.Button("Copy"))
            {
                Raylib.SetClipboardText(GameClient.LobbyData.Code);
            }

            ImGui.TextColored(Utils.ColorToVec4(Color.Green), $"{GameClient.LobbyData.Host.Nickname}");

            if (GameClient.LobbyData.Guest.Nickname != null)
            {
                ImGui.Text($"{GameClient.LobbyData.Guest.Nickname}");
            }

            if (GameClient.LobbySettings != null)
            {
                string[] maps = ["test_room", "jan"];

                if (GameClient.IsHost())
                {
                    if (ImGui.Combo("Current Map", ref curMapInp, maps, maps.Length))
                    {
                        GameClient.LobbySettings.MapIndex = (ushort)curMapInp;
                        Raylib.TraceLog(TraceLogLevel.Info, $"Changed curmap to {maps[curMapInp]}");
                        GameClient.SendLobbyPacket(new ChangeLobbySettingsPacket() { Settings = GameClient.LobbySettings });
                    }
                }
                else
                {
                    ImGui.Text($"Selected map: {maps[GameClient.LobbySettings.MapIndex]}");
                }
            }

            if (GameClient.LobbyData.GetPlayerCount() == 2 && GameClient.IsHost())
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
