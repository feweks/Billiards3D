using System.Numerics;
using Game.Client.Data.Files;
using Game.Client.Entities;
using Game.Client.Net;
using Raylib_cs;

namespace Game.Client.States;

class PlayState : GameState
{
    const float MIN_CAM_DISTANCE = 0.2f;
    const float MAX_CAM_DISTANCE = 1.5f;
    const float MIN_CUE_FORCE = 0f;
    const float MAX_CUE_FORCE = 6f;
    const float POCKET_RADIUS = 0.04f;

    GameEntity poolCue;
    PoolBallEntity poolCueBall;
    PoolBallEntity[] poolBalls;
    //List<Vector3> poolPockets;

    float cueForce = 0f;

    float camYaw = 0f;
    float camPitch = 20 * Raylib.DEG2RAD;
    float camDistance = 1f;
    Vector3 camPos;

    public PlayState() : base("play_state", new Vector3(1, 1, 1), new Vector3(0, 0, 0), 75f)
    {
        PlaceEntity(new GameEntity("resources/gfx/models/pool_table.obj", Vector3.Zero));

        poolCue = new GameEntity("resources/gfx/models/pool_cue.obj", Vector3.Zero)
        {
            Scale = new Vector3(0.75f)
        };
        PlaceEntity(poolCue);

        poolCueBall = new PoolBallEntity(GameClient.LobbyData!.PoolCueBall!);
        PlaceEntity(poolCueBall);

        int ballsCount = GameClient.LobbyData!.PoolBalls.Count;
        poolBalls = new PoolBallEntity[ballsCount];
        for (int i = 0; i < ballsCount; i++)
        {
            poolBalls[i] = new PoolBallEntity(GameClient.LobbyData.PoolBalls[i]);
            PlaceEntity(poolBalls[i]);
        }

        /*poolPockets = [
            new Vector3(-PoolBallEntity.POOL_TABLE_WIDTH / 2 - POCKET_RADIUS, 1, 0),
            new Vector3(PoolBallEntity.POOL_TABLE_WIDTH / 2 + POCKET_RADIUS, 1, 0),
            new Vector3(-PoolBallEntity.POOL_TABLE_WIDTH / 2.05f, 1, -PoolBallEntity.POOL_TABLE_LENGTH / 2.05f),
            new Vector3(PoolBallEntity.POOL_TABLE_WIDTH / 2.05f, 1, -PoolBallEntity.POOL_TABLE_LENGTH / 2.05f),
            new Vector3(-PoolBallEntity.POOL_TABLE_WIDTH / 2.05f, 1, PoolBallEntity.POOL_TABLE_LENGTH / 2.05f),
            new Vector3(PoolBallEntity.POOL_TABLE_WIDTH / 2.05f, 1, PoolBallEntity.POOL_TABLE_LENGTH / 2.05f),
        ];*/
    }

    public override void Update(float dt)
    {
        base.Update(dt);

        UpdateCamera(dt);
        //UpdateCue(dt);

        camPos = new Vector3(
            Camera.Target.X + camDistance * MathF.Cos(camPitch) * MathF.Sin(camYaw),
            Camera.Target.Y + camDistance * MathF.Sin(camPitch),
            Camera.Target.Z + camDistance * MathF.Cos(camPitch) * MathF.Cos(camYaw)
        );

        Camera.Target = poolCueBall.Position;
        Camera.Position = Raymath.Vector3Lerp(Camera.Position, camPos, dt * 10f);

        /*foreach (var ballA in poolBalls)
        {
            foreach (var pocketPos in poolPockets)
            {
                if (Raylib.CheckCollisionBoxSphere(ballA.BoundingBox, pocketPos, POCKET_RADIUS))
                {
                    ballA.Active = false;
                    ballA.Visible = false;
                    continue;
                }
            }

            foreach (var ballB in poolBalls)
            {
                if (ballA.Active && ballB.Active && ballA.CheckCollisions(ballB))
                    ballA.HandleCollision(ballB);

                if (ballA.CheckCollisions(poolCueBall))
                    ballA.HandleCollision(poolCueBall);
            }
        }*/
    }

    private void UpdateCamera(float dt)
    {
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
    }

    /*private void UpdateCue(float dt)
    {
        if (Raylib.IsMouseButtonDown(MouseButton.Left))
        {
            cueForce = Math.Clamp(cueForce + Raylib.GetMouseDelta().Y * -(dt * 2), MIN_CUE_FORCE, MAX_CUE_FORCE);
        }

        Vector3 aimDir = GetAimDir();
        if (Raylib.IsMouseButtonReleased(MouseButton.Left))
        {
            poolCueBall.Velocity = aimDir * cueForce;
            cueForce = 0f;
        }

        float cueDistance = 0.6f + (cueForce * 0.05f);
        poolCue.Position = poolCueBall.Position - (aimDir * cueDistance);
        poolCue.Rotation.Y = -90f + MathF.Atan2(aimDir.X, aimDir.Z) * Raylib.RAD2DEG;
    }*/

    /*private Vector3 GetAimDir()
    {
        Vector3 forward = Vector3.Normalize(Camera.Target - Camera.Position);
        forward.Y = 0;

        return Vector3.Normalize(forward);
    }*/

    public override void Draw()
    {
        base.Draw();

        /*if (DebugView)
        {
            Raylib.DrawGrid(5, 3);
            const float LINE_Y = 1.01f;

            Raylib.DrawLine3D(new Vector3(-PoolBallEntity.POOL_TABLE_WIDTH / 2, LINE_Y, 0), new Vector3(PoolBallEntity.POOL_TABLE_WIDTH / 2, LINE_Y, 0), Color.Yellow);
            Raylib.DrawLine3D(new Vector3(0, LINE_Y, -PoolBallEntity.POOL_TABLE_LENGTH / 2), new Vector3(0, LINE_Y, PoolBallEntity.POOL_TABLE_LENGTH / 2), Color.Blue);

            foreach (var pocketPos in poolPockets)
                Raylib.DrawSphereWires(pocketPos, POCKET_RADIUS, 8, 8, Color.Purple);
        }*/
    }
}
