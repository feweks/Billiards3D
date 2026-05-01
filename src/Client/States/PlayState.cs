using System.Numerics;
using System.Runtime.CompilerServices;
using Game.Client.Data.Files;
using Game.Client.Entities;
using Raylib_cs;

namespace Game.Client.States;

class PlayState : GameState
{
    const float MIN_CAM_DISTANCE = 0.2f;
    const float MAX_CAM_DISTANCE = 1.5f;
    const float MIN_CUE_FORCE = 0f;
    const float MAX_CUE_FORCE = 6f;

    PoolGamemodeFileData gamemodeData;

    GameEntity poolCue;
    PoolBallEntity poolCueBall;
    PoolBallEntity[] poolBalls;

    float cueForce = 0f;

    float camYaw = 0f;
    float camPitch = 20 * Raylib.DEG2RAD;
    float camDistance = 1f;
    Vector3 camPos;

    public PlayState() : base("play_state", new Vector3(1, 1, 1), new Vector3(0, 0, 0), 75f)
    {
        gamemodeData = Resources.GetJson("resources/data/gamemodes/gamemode_classic.json", PoolGamemodeFileDataCtx.Default.PoolGamemodeFileData);

        PlaceEntity(new GameEntity("resources/gfx/models/pool_table.obj", Vector3.Zero));

        poolCue = new GameEntity("resources/gfx/models/pool_cue.obj", Vector3.Zero)
        {
            Scale = new Vector3(0.75f)
        };
        PlaceEntity(poolCue);

        poolCueBall = new PoolBallEntity("cue", gamemodeData.CueBallPos);
        PlaceEntity(poolCueBall);

        poolBalls = new PoolBallEntity[gamemodeData.PoolBallsCount];
        for (int i = 0; i < gamemodeData.PoolBallsCount; i++)
        {
            poolBalls[i] = new PoolBallEntity((i + 1).ToString(), gamemodeData.PoolBallsPos[i]);
            PlaceEntity(poolBalls[i]);
        }

        Camera.Target = poolCueBall.Position;
    }

    public override void Update(float dt)
    {
        base.Update(dt);

        if (Raylib.IsMouseButtonDown(MouseButton.Right))
        {
            Vector2 delta = Raylib.GetMouseDelta();

            camYaw -= delta.X * 0.01f * (dt * 60);
            camPitch -= delta.Y * 0.01f * (dt * 60);

            if (camPitch > 0.75f) camPitch = 0.75f;
            if (camPitch < 0.1f) camPitch = 0.1f;
        }

        float mouseWheel = Raylib.GetMouseWheelMove() * 0.1f;
        if (mouseWheel != 0)
        {
            camDistance = Math.Clamp(camDistance - mouseWheel, MIN_CAM_DISTANCE, MAX_CAM_DISTANCE);
        }

        if (Raylib.IsMouseButtonDown(MouseButton.Left))
        {
            cueForce = Math.Clamp(cueForce + Raylib.GetMouseDelta().Y * -0.05f, MIN_CUE_FORCE, MAX_CUE_FORCE);
            Console.WriteLine(cueForce);
        }

        Vector3 aimDir = GetAimDir();
        if (Raylib.IsMouseButtonReleased(MouseButton.Left))
        {
            poolCueBall.Velocity = aimDir * cueForce;
        }

        float cueDistance = 0.6f + (cueForce * 0.05f);
        poolCue.Position = poolCueBall.Position - (aimDir * cueDistance);
        poolCue.Rotation.Y = -90f + MathF.Atan2(aimDir.X, aimDir.Z) * Raylib.RAD2DEG;

        camPos = new Vector3(
            Camera.Target.X + camDistance * MathF.Cos(camPitch) * MathF.Sin(camYaw),
            Camera.Target.Y + camDistance * MathF.Sin(camPitch),
            Camera.Target.Z + camDistance * MathF.Cos(camPitch) * MathF.Cos(camYaw)
        );

        Camera.Position = Raymath.Vector3Lerp(Camera.Position, camPos, dt * 10f);

        foreach (var ballA in poolBalls)
        {
            foreach (var ballB in poolBalls)
            {
                if (ballA.CheckCollisions(ballB))
                    ballA.HandleCollision(ballB);

                if (ballA.CheckCollisions(poolCueBall))
                    ballA.HandleCollision(poolCueBall);
            }
        }
    }

    private Vector3 GetAimDir()
    {
        Vector3 forward = Vector3.Normalize(Camera.Target - Camera.Position);
        forward.Y = 0;

        return Vector3.Normalize(forward);
    }

    public override void Draw()
    {
        base.Draw();

        if (DebugView)
        {
            Raylib.DrawGrid(5, 3);
            const float LINE_Y = 1.01f;

            Raylib.DrawLine3D(new Vector3(-PoolBallEntity.POOL_TABLE_WIDTH / 2, LINE_Y, 0), new Vector3(PoolBallEntity.POOL_TABLE_WIDTH / 2, LINE_Y, 0), Color.Yellow);
            Raylib.DrawLine3D(new Vector3(0, LINE_Y, -PoolBallEntity.POOL_TABLE_LENGTH / 2), new Vector3(0, LINE_Y, PoolBallEntity.POOL_TABLE_LENGTH / 2), Color.Blue);
        }
    }
}
