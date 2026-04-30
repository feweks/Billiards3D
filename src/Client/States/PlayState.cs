using System.Numerics;
using Raylib_cs;

namespace Game.Client.States;

class PlayState : GameState
{
    public PlayState() : base("play_state", new Vector3(1, 1, 1), new Vector3(0, 0, 0), 75f) { }

    public override void Update(float dt)
    {
        base.Update(dt);

        Raylib.UpdateCamera(ref Camera, CameraMode.Free);
    }

    public override void Draw()
    {
        base.Draw();

        Raylib.DrawGrid(5, 3);
    }
}
