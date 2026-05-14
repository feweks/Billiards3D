using System.Diagnostics;
using System.Numerics;
using Game.Client.Entities;
using Game.Client.Net;
using Game.Common.Enums;
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
    float playerCueForce = 0;

    public PlayState() : base("play_state", new Vector3(1, 1, 1), new Vector3(0, 0, 0), 75f)
    {
        Debug.Assert(GameClient.LobbyData != null, "Cannot load game: client is not connected to any lobby");

        var startingPlayer = GameClient.LobbyData.GetCurrentPlayer();

        poolTable = new GameEntity("resources/gfx/models/pool_table.obj", Vector3.Zero);
        PlaceEntity(poolTable);

        poolCueBall = new PoolBallEntity(GameClient.LobbyData.PoolCueBall!);
        PlaceEntity(poolCueBall);

        int ballsCount = GameClient.LobbyData.PoolBalls.Count;
        poolBalls = new PoolBallEntity[ballsCount];
        for (int i = 0; i < ballsCount; i++)
        {
            poolBalls[i] = new PoolBallEntity(GameClient.LobbyData.PoolBalls[i]);
            PlaceEntity(poolBalls[i]);
        }

        poolCue = new GameEntity("resources/gfx/models/pool_cue.obj", GetCueTargetPos(startingPlayer.AimDir, startingPlayer.CueForce))
        {
            Scale = new Vector3(0.75f),
            Rotation = new Vector3(0, GetCueTargetRotY(startingPlayer.AimDir), 0)
        };
        PlaceEntity(poolCue);

        Camera.Target = poolCueBall.Position;
        Camera.Position = new Vector3(
            Camera.Target.X + camDistance * MathF.Cos(camPitch) * MathF.Sin(camYaw),
            Camera.Target.Y + camDistance * MathF.Sin(camPitch),
            Camera.Target.Z + camDistance * MathF.Cos(camPitch) * MathF.Cos(camYaw)
        );
    }

    public override void Update(float dt)
    {
        base.Update(dt);

        if (GameClient.LobbyData == null)
            return;

        poolCue.Visible = GameClient.LobbyData.State == PoolGameState.Breaking || GameClient.LobbyData.State == PoolGameState.Aiming;

        poolCueBall.Data = GameClient.LobbyData.PoolCueBall!;
        for (int i = 0; i < GameClient.LobbyData.PoolBalls.Count; i++)
        {
            poolBalls[i].Data = GameClient.LobbyData.PoolBalls[i];
        }

        var netPlayer = GameClient.GetSelfPlayer();
        var curPlayer = GameClient.LobbyData.GetCurrentPlayer();

        float cueLerpAmount = 15f;
        if (curPlayer.Nickname == netPlayer.Nickname)
        {
            if (poolCue.Visible)
            {
                if (!canShoot)
                    canShoot = true;

                UpdateCamera(dt, netPlayer);
                UpdateCue(dt, netPlayer);
            }

            if (GameClient.LobbyData.State == PoolGameState.BallInHand)
            {
                UpdateCueBall(dt, netPlayer);
            }
        }

        if (GameClient.LobbyData.State == PoolGameState.BallInHand)
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

        float curPlayerCueForce = curPlayer.Nickname == netPlayer.Nickname ? playerCueForce : netPlayer.CueForce;
        poolCue.Position = Vector3.Lerp(poolCue.Position, GetCueTargetPos(curPlayer.AimDir, curPlayerCueForce), dt * cueLerpAmount);
        poolCue.Rotation.Y = Raymath.LerpAngle(poolCue.Rotation.Y, GetCueTargetRotY(curPlayer.AimDir), dt * cueLerpAmount);

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
            playerCueForce = Math.Clamp(playerCueForce + Raylib.GetMouseDelta().Y * -(dt * 2), MIN_CUE_FORCE, MAX_CUE_FORCE);
        }

        Vector3 aimDir = GetAimDir();
        netPlayer.AimDir = aimDir;
        netPlayer.CueForce = playerCueForce;
        if (Raylib.IsMouseButtonReleased(MouseButton.Left) && canShoot && playerCueForce > 0)
        {
            canShoot = false;
            GameClient.SendLobbyPacket(new ShotLobbyPacket());
            Raylib.TraceLog(TraceLogLevel.Info, $"Shot with force: {playerCueForce}");
            playerCueForce = 0;
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

    private Vector3 GetCueTargetPos(Vector3 aimDir, float curForce)
    {
        float cueDistance = 0.6f + (curForce * 0.05f);
        return poolCueBall.Position - (aimDir * cueDistance);
    }

    private float GetCueTargetRotY(Vector3 aimDir) => -90f + MathF.Atan2(aimDir.X, aimDir.Z) * Raylib.RAD2DEG;

    public override void Draw()
    {
        base.Draw();

        if (GameClient.LobbyData == null)
            return;

        if (DebugView && GameClient.LobbyData.State == PoolGameState.BallInHand)
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

        if (GameClient.LobbyData.State == PoolGameState.Breaking)
        {
            Raylib.DrawText($"{GameClient.LobbyData.CurPlayer} is breaking", 5, 3, 24, Color.White);
        }

        if (GameClient.LobbyData.State == PoolGameState.Aiming)
        {
            Raylib.DrawText($"{GameClient.LobbyData.CurPlayer} is aiming", 5, 3, 24, Color.White);
        }

        if (GameClient.LobbyData.State == PoolGameState.BallInHand)
        {
            Raylib.DrawText($"{GameClient.LobbyData.CurPlayer} has cue ball in hand", 5, 3, 24, Color.White);
        }

        var ply = GameClient.GetSelfPlayer();
        if (poolCue.Visible && ply.Nickname == GameClient.LobbyData!.CurPlayer)
        {
            string forceTxt = $"Cue Force: {playerCueForce:0.00}";
            int forceTxtSize = Raylib.MeasureText(forceTxt, 24);

            Raylib.DrawText(forceTxt, Program.Instance!.Config.RenderResolution[0] / 2 - forceTxtSize / 2, 3, 24, Color.White);
        }

        if (ply.BallType != PoolBallType.None)
        {
            string ballTypeTxt = $"You are {ply.BallType.ToString().ToLower()}";
            int ballTypeTxtSize = Raylib.MeasureText(ballTypeTxt, 24);

            Raylib.DrawText(ballTypeTxt, Program.Instance!.Config.RenderResolution[0] - ballTypeTxtSize, 3, 24, Color.White);
        }

        if (GameClient.LobbyData.State == PoolGameState.Finished)
        {
            string winnerTxt = $"{GameClient.LobbyData.GetPlayerByNick(GameClient.LobbyData.CurPlayer).Nickname} won";
            int winnerTxtSize = Raylib.MeasureText(winnerTxt, 24);

            Raylib.DrawText(winnerTxt, Program.Instance!.Config.RenderResolution[0] / 2 - winnerTxtSize / 2, 3, 24, Color.White);
        }
    }
}
