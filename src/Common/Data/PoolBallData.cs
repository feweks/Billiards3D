using System.Numerics;
using Game.Common.Enums;
using LiteNetLib.Utils;

namespace Game.Common.Data;

class PoolBallData : INetSerializable
{
    public string Identifier { get; set; } = "";
    public ushort Index { get; set; }
    public PoolBallType Type { get; set; }
    public Vector3 Position = Vector3.Zero;
    public Vector3 Velocity = Vector3.Zero;
    public Vector3 Rotation = Vector3.Zero;
    public bool Pocketed { get; set; } = false;
    public Vector3 PocketPos;

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(Identifier);
        writer.Put(Index);
        writer.Put((byte)Type);
        writer.Put(Pocketed);

        writer.Put(Position.X);
        writer.Put(Position.Y);
        writer.Put(Position.Z);

        writer.Put(Velocity.X);
        writer.Put(Velocity.Y);
        writer.Put(Velocity.Z);

        writer.Put(Rotation.X);
        writer.Put(Rotation.Y);
        writer.Put(Rotation.Z);

        writer.Put(PocketPos.X);
        writer.Put(PocketPos.Y);
        writer.Put(PocketPos.Z);
    }

    public void Deserialize(NetDataReader reader)
    {
        Identifier = reader.GetString();
        Index = reader.GetUShort();
        Type = (PoolBallType)reader.GetByte();
        Pocketed = reader.GetBool();

        Position.X = reader.GetFloat();
        Position.Y = reader.GetFloat();
        Position.Z = reader.GetFloat();

        Velocity.X = reader.GetFloat();
        Velocity.Y = reader.GetFloat();
        Velocity.Z = reader.GetFloat();

        Rotation.X = reader.GetFloat();
        Rotation.Y = reader.GetFloat();
        Rotation.Z = reader.GetFloat();

        PocketPos.X = reader.GetFloat();
        PocketPos.Y = reader.GetFloat();
        PocketPos.Z = reader.GetFloat();
    }
}
