
using System.Numerics;
using Game.Common.Enums;
using Game.Server.Data.Files;

namespace Game.Common.Data;

class LobbySettingsData : ISerializableData
{
    public float PoolTableWidth { get; set; }
    public float PoolTableLength { get; set; }
    public int MapIndex { get; set; }
    public PoolGamemodeType Gamemode { get; set; }
    public float Tickrate { get; set; }
    public List<Vector3> PoolPockets { get; set; } = [];
    public float PoolPocketRadius { get; set; }

    public LobbySettingsData(PoolGamemodeConfigFileData gamemodeCfg, float tickrate)
    {
        PoolTableWidth = gamemodeCfg.PoolTableWidth;
        PoolTableLength = gamemodeCfg.PoolTableLength;
        MapIndex = 0;
        Gamemode = PoolGamemodeType.Classic;
        Tickrate = tickrate;
        PoolPockets = gamemodeCfg.PoolTablePockets;
        PoolPocketRadius = gamemodeCfg.PoolTablePocketRadius;
    }

    public LobbySettingsData(BinaryReader reader) => Deserialize(reader);

    public void Serialize(BinaryWriter writer)
    {
        writer.Write(PoolTableWidth);
        writer.Write(PoolTableLength);
        writer.Write(MapIndex);
        writer.Write((byte)Gamemode);
        writer.Write(Tickrate);
        writer.Write(PoolPockets.Count);
        for (int i = 0; i < PoolPockets.Count; i++)
        {
            writer.Write(PoolPockets[i].X);
            writer.Write(PoolPockets[i].Y);
            writer.Write(PoolPockets[i].Z);
        }
        writer.Write(PoolPocketRadius);
    }

    public void Deserialize(BinaryReader reader)
    {
        PoolTableWidth = reader.ReadSingle();
        PoolTableLength = reader.ReadSingle();
        MapIndex = reader.ReadInt32();
        Gamemode = (PoolGamemodeType)reader.ReadByte();
        Tickrate = reader.ReadSingle();
        int pocketsCount = reader.ReadInt32();
        PoolPockets = new List<Vector3>();
        for (int i = 0; i < pocketsCount; i++)
        {
            PoolPockets.Add(new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle()));
        }
        PoolPocketRadius = reader.ReadSingle();
    }
}
