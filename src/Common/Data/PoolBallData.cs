using System.Numerics;
using Game.Common.Enums;

namespace Game.Common.Data;

class PoolBallData : ISerializableData
{
    public string Identifier { get; set; } = "";
    public ushort Index { get; set; }
    public PoolBallType Type { get; set; }
    public Vector3 Position = Vector3.Zero;
    public Vector3 Velocity = Vector3.Zero;
    public Vector3 Rotation = Vector3.Zero;
    public bool Pocketed { get; set; } = false;
    public Vector3 PocketPos;

    public void Serialize(BinaryWriter writer)
    {
        writer.Write(Identifier);
        writer.Write(Index);
        writer.Write((byte)Type);
        writer.Write(Pocketed);

        writer.Write(Position.X);
        writer.Write(Position.Y);
        writer.Write(Position.Z);

        writer.Write(Velocity.X);
        writer.Write(Velocity.Y);
        writer.Write(Velocity.Z);

        writer.Write(Rotation.X);
        writer.Write(Rotation.Y);
        writer.Write(Rotation.Z);

        writer.Write(PocketPos.X);
        writer.Write(PocketPos.Y);
        writer.Write(PocketPos.Z);
    }

    public void Deserialize(BinaryReader reader)
    {
        Identifier = reader.ReadString();
        Index = reader.ReadUInt16();
        Type = (PoolBallType)reader.ReadByte();
        Pocketed = reader.ReadBoolean();

        Position.X = reader.ReadSingle();
        Position.Y = reader.ReadSingle();
        Position.Z = reader.ReadSingle();

        Velocity.X = reader.ReadSingle();
        Velocity.Y = reader.ReadSingle();
        Velocity.Z = reader.ReadSingle();

        Rotation.X = reader.ReadSingle();
        Rotation.Y = reader.ReadSingle();
        Rotation.Z = reader.ReadSingle();

        PocketPos.X = reader.ReadSingle();
        PocketPos.Y = reader.ReadSingle();
        PocketPos.Z = reader.ReadSingle();
    }
}
