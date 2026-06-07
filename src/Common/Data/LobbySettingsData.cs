
using System.Numerics;
using Game.Common.Enums;
using Game.Server.Data.Files;

namespace Game.Common.Data;

class LobbySettingsData : ISerializableData
{
    public float PoolTableWidth { get; set; }
    public float PoolTableLength { get; set; }
    public ushort MapIndex { get; set; }
    public bool EnableHelperLines { get; set; }
    public PoolGamemodeType Gamemode { get; set; }
    public float Tickrate { get; set; }
    public List<Vector3> PoolPockets { get; set; } = [];
    public float PoolPocketRadius { get; set; }
    public float PoolBallFriction { get; set; }
    public float PoolBallRadius { get; set; }
    public float PoolBallMass { get; set; }

    public LobbySettingsData(PoolGamemodeConfigFileData gamemodeCfg, float tickrate)
    {
        PoolTableWidth = gamemodeCfg.PoolTableWidth;
        PoolTableLength = gamemodeCfg.PoolTableLength;
        MapIndex = 0;
        EnableHelperLines = false;
        Gamemode = PoolGamemodeType.Classic;
        Tickrate = tickrate;
        PoolPockets = gamemodeCfg.PoolTablePockets;
        PoolPocketRadius = gamemodeCfg.PoolTablePocketRadius;
        PoolBallFriction = gamemodeCfg.PoolBallFriction;
        PoolBallRadius = gamemodeCfg.PoolBallRadius;
        PoolBallMass = gamemodeCfg.PoolBallMass;
    }

    public LobbySettingsData(BinaryReader reader) => Deserialize(reader);

    public void Serialize(BinaryWriter writer)
    {
        writer.Write(PoolTableWidth);
        writer.Write(PoolTableLength);
        writer.Write(MapIndex);
        writer.Write(EnableHelperLines);
        writer.Write((byte)Gamemode);
        writer.Write(Tickrate);
        writer.Write((ushort)PoolPockets.Count);
        for (int i = 0; i < PoolPockets.Count; i++)
        {
            writer.Write(PoolPockets[i].X);
            writer.Write(PoolPockets[i].Y);
            writer.Write(PoolPockets[i].Z);
        }
        writer.Write(PoolPocketRadius);
        writer.Write(PoolBallFriction);
        writer.Write(PoolBallRadius);
        writer.Write(PoolBallMass);
    }

    public void Deserialize(BinaryReader reader)
    {
        PoolTableWidth = reader.ReadSingle();
        PoolTableLength = reader.ReadSingle();
        MapIndex = reader.ReadUInt16();
        EnableHelperLines = reader.ReadBoolean();
        Gamemode = (PoolGamemodeType)reader.ReadByte();
        Tickrate = reader.ReadSingle();
        ushort pocketsCount = reader.ReadUInt16();
        PoolPockets = new List<Vector3>();
        for (int i = 0; i < pocketsCount; i++)
        {
            PoolPockets.Add(new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle()));
        }
        PoolPocketRadius = reader.ReadSingle();
        PoolBallFriction = reader.ReadSingle();
        PoolBallRadius = reader.ReadSingle();
        PoolBallMass = reader.ReadSingle();
    }
}
