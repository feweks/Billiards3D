using System.Numerics;
using ImGuiNET;
using Raylib_cs;

namespace Game.Client.States;

class MainMenuState : GameState
{
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

        if (ImGui.Button("PLAY"))
        {
            ChangeState(new PlayState());
        }

        ImGui.End();
    }
}
