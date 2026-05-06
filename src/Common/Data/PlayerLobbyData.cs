using System.Numerics;

namespace Game.Common.Data;

class PlayerLobbyData : ISerializableData
{
    public string? Nickname { get; set; }
    public Vector3 CamPos;
    public Vector3 AimDir;
    public float CueForce { get; set; }

    public PlayerLobbyData(string? nick)
    {
        Nickname = nick;
    }

    public void Serialize(BinaryWriter writer)
    {
        writer.Write(Nickname ?? string.Empty);

        writer.Write(CamPos.X);
        writer.Write(CamPos.Y);
        writer.Write(CamPos.Z);

        writer.Write(AimDir.X);
        writer.Write(AimDir.Y);
        writer.Write(AimDir.Z);

        writer.Write(CueForce);
    }

    public void Deserialize(BinaryReader reader)
    {
        string nick = reader.ReadString();
        Nickname = nick != string.Empty ? nick : null;

        CamPos.X = reader.ReadSingle();
        CamPos.Y = reader.ReadSingle();
        CamPos.Z = reader.ReadSingle();

        AimDir.X = reader.ReadSingle();
        AimDir.Y = reader.ReadSingle();
        AimDir.Z = reader.ReadSingle();

        CueForce = reader.ReadSingle();
    }
}
