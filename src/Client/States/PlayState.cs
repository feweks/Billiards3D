using System.Numerics;
using Game.Client.Data.Files;
using Game.Client.Entities;
using Game.Client.Net;
using Game.Common;
using Game.Common.Data;
using Game.Common.Packets;
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

    bool canShoot = false;
    float camYaw = 0f;
    float camPitch = 20 * Raylib.DEG2RAD;
    float camDistance = 1f;
    Vector3 camPos;

    float updateTime = 0f;

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

        if (GameClient.LobbyData == null)
            return;

        poolCue.Visible = GameClient.LobbyData.State == PoolGameState.Break;

        poolCueBall.Data = GameClient.LobbyData.PoolCueBall!;
        for (int i = 0; i < GameClient.LobbyData.PoolBalls.Count; i++)
        {
            poolBalls[i].Data = GameClient.LobbyData.PoolBalls[i];
        }

        var netPlayer = GameClient.GetSelfPlayer();
        var curPlayer = GameClient.LobbyData.GetPlayerByNick(GameClient.LobbyData.CurPlayer);

        float cueLerpAmount = 10f;
        if (curPlayer.Nickname == netPlayer.Nickname)
        {
            if (GameClient.LobbyData.State == PoolGameState.Break)
            {
                if (!canShoot)
                    canShoot = true;

                UpdateCamera(dt);
                UpdateCue(dt, netPlayer);
                netPlayer.CamPos = camPos;
                cueLerpAmount = 50f;
            }
        }
        else
        {
            Camera.Position = Raymath.Vector3Lerp(Camera.Position, curPlayer.CamPos, dt * 10f);
        }

        Camera.Target = poolCueBall.Position;

        float cueDistance = 0.6f + (curPlayer.CueForce * 0.05f);
        poolCue.Position = Vector3.Lerp(poolCue.Position, poolCueBall.Position - (curPlayer.AimDir * cueDistance), dt * cueLerpAmount);
        poolCue.Rotation.Y = Raymath.LerpAngle(poolCue.Rotation.Y, -90f + MathF.Atan2(curPlayer.AimDir.X, curPlayer.AimDir.Z) * Raylib.RAD2DEG, dt * cueLerpAmount);

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

        updateTime += dt;
        if (updateTime > 0.03f)
        {
            GameClient.Send(new UpdatePlayerLobbyPacket()
            {
                LobbyCode = GameClient.LobbyData.Code,
                Sender = GameClient.PlayerNick!,
                PlayerData = netPlayer
            });

            updateTime = 0f;
        }
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

        camPos = new Vector3(
            Camera.Target.X + camDistance * MathF.Cos(camPitch) * MathF.Sin(camYaw),
            Camera.Target.Y + camDistance * MathF.Sin(camPitch),
            Camera.Target.Z + camDistance * MathF.Cos(camPitch) * MathF.Cos(camYaw)
        );
        Camera.Position = Raymath.Vector3Lerp(Camera.Position, camPos, dt * 10f);
    }

    private void UpdateCue(float dt, PlayerLobbyData netPlayer)
    {
        if (Raylib.IsMouseButtonDown(MouseButton.Left))
        {
            netPlayer.CueForce = Math.Clamp(netPlayer.CueForce + Raylib.GetMouseDelta().Y * -(dt * 2), MIN_CUE_FORCE, MAX_CUE_FORCE);
        }

        Vector3 aimDir = GetAimDir();
        netPlayer.AimDir = aimDir;
        if (Raylib.IsMouseButtonReleased(MouseButton.Left) && canShoot)
        {
            canShoot = false;
            GameClient.Send(new ShotLobbyPacket()
            {
                LobbyCode = GameClient.LobbyData!.Code,
                Sender = GameClient.PlayerNick!
            });
            Raylib.TraceLog(TraceLogLevel.Info, "Shot");
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

    public override void DrawUI()
    {
        base.DrawUI();

        if (GameClient.LobbyData == null)
            return;

        if (GameClient.LobbyData.State == PoolGameState.Break)
        {
            Raylib.DrawText($"{GameClient.LobbyData.CurPlayer} is breaking", 5, 3, 24, Color.White);
        }
    }
}
