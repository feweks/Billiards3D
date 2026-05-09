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

    GameEntity poolTable;
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
        poolTable = new GameEntity("resources/gfx/models/pool_table.obj", Vector3.Zero);
        PlaceEntity(poolTable);

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
    }

    public override void Update(float dt)
    {
        base.Update(dt);

        if (GameClient.LobbyData == null)
            return;

        poolCue.Visible = GameClient.LobbyData.State == PoolGameState.Break || GameClient.LobbyData.State == PoolGameState.Shooting;

        poolCueBall.Data = GameClient.LobbyData.PoolCueBall!;
        for (int i = 0; i < GameClient.LobbyData.PoolBalls.Count; i++)
        {
            poolBalls[i].Data = GameClient.LobbyData.PoolBalls[i];
        }

        var netPlayer = GameClient.GetSelfPlayer();
        var curPlayer = GameClient.LobbyData.GetPlayerByNick(GameClient.LobbyData.CurPlayer);

        float cueLerpAmount = 35f;
        if (curPlayer.Nickname == netPlayer.Nickname)
        {
            if (poolCue.Visible)
            {
                if (!canShoot)
                    canShoot = true;

                UpdateCamera(dt, netPlayer);
                UpdateCue(dt, netPlayer);
            }

            if (GameClient.LobbyData.State == PoolGameState.PlaceWhite)
            {
                UpdateCueBall(dt, netPlayer);
            }
        }

        if (GameClient.LobbyData.State == PoolGameState.PlaceWhite)
        {
            camPos = new Vector3(0.01f, 2f, 0);
            Camera.Target = Raymath.Vector3Lerp(Camera.Target, Vector3.UnitY, dt * 10f);

            float t = (float)Raylib.GetTime();
            float pulseTime = 4f;

            float alpha = 0.25f + 0.75f * ((MathF.Sin(t * pulseTime) * 0.5f) + 0.5f);
            poolCueBall.Tint = GameClient.LobbyData!.CanPlaceCueBall ? Raylib.ColorAlpha(Color.White, alpha) : Raylib.ColorAlpha(Color.Red, alpha);
        }
        else
        {
            camPos = curPlayer.CamPos;
            Camera.Target = poolCueBall.Position;

            poolCueBall.Tint = Color.White;
        }

        Camera.Position = Raymath.Vector3Lerp(Camera.Position, camPos, dt * 10f);

        float cueDistance = 0.6f + (curPlayer.CueForce * 0.05f);
        poolCue.Position = Vector3.Lerp(poolCue.Position, poolCueBall.Position - (curPlayer.AimDir * cueDistance), dt * cueLerpAmount);
        poolCue.Rotation.Y = Raymath.LerpAngle(poolCue.Rotation.Y, -90f + MathF.Atan2(curPlayer.AimDir.X, curPlayer.AimDir.Z) * Raylib.RAD2DEG, dt * cueLerpAmount);

        updateTime += dt;
        if (updateTime > 0.03f)
        {
            GameClient.SendLobbyPacket(new UpdatePlayerLobbyPacket() { PlayerData = netPlayer });

            updateTime = 0f;
        }
    }

    private void UpdateCamera(float dt, PlayerLobbyData netPlayer)
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

        netPlayer.CamPos = new Vector3(
            Camera.Target.X + camDistance * MathF.Cos(camPitch) * MathF.Sin(camYaw),
            Camera.Target.Y + camDistance * MathF.Sin(camPitch),
            Camera.Target.Z + camDistance * MathF.Cos(camPitch) * MathF.Cos(camYaw)
        );
    }

    private void UpdateCue(float dt, PlayerLobbyData netPlayer)
    {
        if (Raylib.IsMouseButtonDown(MouseButton.Left))
        {
            netPlayer.CueForce = Math.Clamp(netPlayer.CueForce + Raylib.GetMouseDelta().Y * -(dt * 2), MIN_CUE_FORCE, MAX_CUE_FORCE);
        }

        Vector3 aimDir = GetAimDir();
        netPlayer.AimDir = aimDir;
        if (Raylib.IsMouseButtonReleased(MouseButton.Left) && canShoot && netPlayer.CueForce > 0)
        {
            canShoot = false;
            GameClient.SendLobbyPacket(new ShotLobbyPacket());
            Raylib.TraceLog(TraceLogLevel.Info, "Shot");
        }
    }

    private void UpdateCueBall(float dt, PlayerLobbyData netPlayer)
    {
        Ray ray = Raylib.GetScreenToWorldRay(Raylib.GetMousePosition(), Camera);
        RayCollision collision = Raylib.GetRayCollisionBox(ray, poolTable.BoundingBox);

        if (collision.Hit)
        {
            netPlayer.PlacePos = new Vector3(collision.Point.X, 1, collision.Point.Z);

            if (Raylib.IsMouseButtonReleased(MouseButton.Left))
            {
                GameClient.SendLobbyPacket(new PlaceCueLobbyPacket());
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

        if (GameClient.LobbyData == null)
            return;

        if (DebugView && GameClient.LobbyData.State == PoolGameState.PlaceWhite)
        {
            Ray ray = Raylib.GetScreenToWorldRay(Raylib.GetMousePosition(), Camera);
            Raylib.DrawRay(ray, Color.Orange);
        }
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

        if (GameClient.LobbyData.State == PoolGameState.Shooting)
        {
            Raylib.DrawText($"{GameClient.LobbyData.CurPlayer} is shooting", 5, 3, 24, Color.White);
        }

        if (GameClient.LobbyData.State == PoolGameState.PlaceWhite)
        {
            Raylib.DrawText($"{GameClient.LobbyData.CurPlayer} is placing", 5, 3, 24, Color.White);
        }

        var ply = GameClient.GetSelfPlayer();
        if (poolCue.Visible && ply.Nickname == GameClient.LobbyData!.CurPlayer)
        {
            string forceTxt = $"Cue Force: {ply.CueForce:0.00}";
            int forceTxtSize = Raylib.MeasureText(forceTxt, 24);

            Raylib.DrawText(forceTxt, Program.Instance!.Config.RenderResolution[0] / 2 - forceTxtSize / 2, 3, 24, Color.White);
        }
    }
}
