using System.Numerics;

namespace Game.Common.Data;

class PlayerLobbyData : ISerializableData
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

    public void Serialize(BinaryWriter writer)
    {
        writer.Write(Nickname ?? string.Empty);
        writer.Write((byte)BallType);

        writer.Write(CamPos.X);
        writer.Write(CamPos.Y);
        writer.Write(CamPos.Z);

        writer.Write(AimDir.X);
        writer.Write(AimDir.Y);
        writer.Write(AimDir.Z);

        writer.Write(PlacePos.X);
        writer.Write(PlacePos.Y);
        writer.Write(PlacePos.Z);

        writer.Write(CueForce);
    }

    public void Deserialize(BinaryReader reader)
    {
        string nick = reader.ReadString();
        Nickname = nick != string.Empty ? nick : null;
        BallType = (PoolBallType)reader.ReadByte();

        CamPos.X = reader.ReadSingle();
        CamPos.Y = reader.ReadSingle();
        CamPos.Z = reader.ReadSingle();

        AimDir.X = reader.ReadSingle();
        AimDir.Y = reader.ReadSingle();
        AimDir.Z = reader.ReadSingle();

        PlacePos.X = reader.ReadSingle();
        PlacePos.Y = reader.ReadSingle();
        PlacePos.Z = reader.ReadSingle();

        CueForce = reader.ReadSingle();
    }
}
