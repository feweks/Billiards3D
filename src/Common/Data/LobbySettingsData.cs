
using System.Numerics;
using Game.Common.Enums;
using Game.Server.Data.Files;
using LiteNetLib.Utils;

namespace Game.Common.Data;

class LobbySettingsData : INetSerializable
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

    public LobbySettingsData(NetDataReader reader) => Deserialize(reader);

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(PoolTableWidth);
        writer.Put(PoolTableLength);
        writer.Put(MapIndex);
        writer.Put(EnableHelperLines);
        writer.Put((byte)Gamemode);
        writer.Put(Tickrate);
        writer.Put((ushort)PoolPockets.Count);
        for (int i = 0; i < PoolPockets.Count; i++)
        {
            writer.Put(PoolPockets[i].X);
            writer.Put(PoolPockets[i].Y);
            writer.Put(PoolPockets[i].Z);
        }
        writer.Put(PoolPocketRadius);
        writer.Put(PoolBallFriction);
        writer.Put(PoolBallRadius);
        writer.Put(PoolBallMass);
    }

    public void Deserialize(NetDataReader reader)
    {
        PoolTableWidth = reader.GetFloat();
        PoolTableLength = reader.GetFloat();
        MapIndex = reader.GetUShort();
        EnableHelperLines = reader.GetBool();
        Gamemode = (PoolGamemodeType)reader.GetByte();
        Tickrate = reader.GetFloat();
        ushort pocketsCount = reader.GetUShort();
        PoolPockets = new List<Vector3>();
        for (int i = 0; i < pocketsCount; i++)
        {
            PoolPockets.Add(new Vector3(reader.GetFloat(), reader.GetFloat(), reader.GetFloat()));
        }
        PoolPocketRadius = reader.GetFloat();
        PoolBallFriction = reader.GetFloat();
        PoolBallRadius = reader.GetFloat();
        PoolBallMass = reader.GetFloat();
    }
}
