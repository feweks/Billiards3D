using System.Numerics;
using Game.Common.Enums;
using LiteNetLib.Utils;

namespace Game.Common.Data;

class PlayerLobbyData : INetSerializable
{
    public string? Nickname { get; set; }
    public PoolBallType BallType { get; set; } = PoolBallType.None;
    public Vector3 CamPos;
    public Vector3 AimDir;
    public Vector3 PlacePos;
    public float CueForce { get; set; }

    public PlayerLobbyData(string? nick)
    {
        Nickname = nick;
    }

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(Nickname ?? string.Empty);
        writer.Put((byte)BallType);

        writer.Put(CamPos.X);
        writer.Put(CamPos.Y);
        writer.Put(CamPos.Z);

        writer.Put(AimDir.X);
        writer.Put(AimDir.Y);
        writer.Put(AimDir.Z);

        writer.Put(PlacePos.X);
        writer.Put(PlacePos.Y);
        writer.Put(PlacePos.Z);

        writer.Put(CueForce);
    }

    public void Deserialize(NetDataReader reader)
    {
        string nick = reader.GetString();
        Nickname = nick != string.Empty ? nick : null;
        BallType = (PoolBallType)reader.GetByte();

        CamPos.X = reader.GetFloat();
        CamPos.Y = reader.GetFloat();
        CamPos.Z = reader.GetFloat();

        AimDir.X = reader.GetFloat();
        AimDir.Y = reader.GetFloat();
        AimDir.Z = reader.GetFloat();

        PlacePos.X = reader.GetFloat();
        PlacePos.Y = reader.GetFloat();
        PlacePos.Z = reader.GetFloat();

        CueForce = reader.GetFloat();
    }
}
