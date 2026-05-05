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

        ImGui.InputText("Nickname", ref nickInp, 32);
        ImGui.InputText("Code", ref codeInp, GameData.LobbyCodeLength);

        if (ImGui.Button("Host"))
        {
            GameClient.HostLobby();
        }
        ImGui.SameLine();
        if (ImGui.Button("Join"))
        {

        }

        ImGui.End();
    }
}
