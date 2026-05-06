using System.Numerics;
using Game.Client.Net;
using Game.Common;
using ImGuiNET;
using Raylib_cs;

namespace Game.Client.States;

class MainMenuState : GameState
{
    private string nickInp = "";
    private string codeInp = "";

    public MainMenuState() : base("main_menu_state", new Vector3(1, 1, 1), new Vector3(0, 0, 0), 75f)
    {

    }

    public override void Update(float dt)
    {
        base.Update(dt);

        if (GameClient.LobbyData != null && GameClient.LobbyData.Started)
        {
            ChangeState(new PlayState());
        }
    }

    public override void Draw()
    {
        base.Draw();

        Raylib.DrawGrid(5, 3);
    }

    public override void DrawImGui()
    {
        base.DrawImGui();

        ImGui.Begin("MainMenu");

        if (GameClient.LobbyStatus != Common.Packets.JoinedLobbyStatus.Success)
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

            if (GameClient.LobbyStatus == Common.Packets.JoinedLobbyStatus.NickCollision)
            {
                ImGui.TextColored(Utils.ColorToVec4(Color.Red), "Failed to join lobby: player with that name is already in");
            }
            else if (GameClient.LobbyStatus == Common.Packets.JoinedLobbyStatus.Missing)
            {
                ImGui.TextColored(Utils.ColorToVec4(Color.Red), "Failed to join lobby: lobby with that code does not exist");
            }
            else if (GameClient.LobbyStatus == Common.Packets.JoinedLobbyStatus.Full)
            {
                ImGui.TextColored(Utils.ColorToVec4(Color.Red), "Failed to join lobby: lobby is full");
            }
        }
        else if (GameClient.LobbyStatus == Common.Packets.JoinedLobbyStatus.Success && GameClient.LobbyData != null)
        {
            ImGui.Text($"Lobby {GameClient.LobbyData.Code} ({GameClient.LobbyData.GetPlayerCount()}/2)");

            ImGui.TextColored(Utils.ColorToVec4(Color.Green), $"{GameClient.LobbyData.Host.Nickname}");

            if (GameClient.LobbyData.Guest.Nickname != null)
            {
                ImGui.Text($"{GameClient.LobbyData.Guest.Nickname}");
            }

            if (GameClient.LobbyData.GetPlayerCount() == 2 && GameClient.IsHost())
            {
                if (ImGui.Button("Start"))
                {
                    GameClient.StartLobby();
                }
            }
        }

        ImGui.End();
    }
}
