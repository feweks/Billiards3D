using Game.Common.Data;
using Game.Common.Enums;
using LiteNetLib.Utils;

namespace Game.Common.Packets;

class ChangeLobbySettingsPacket : LobbyPacket
{
    public LobbySettingsData? Settings { get; set; }

    public ChangeLobbySettingsPacket() : base(PacketType.ChangeLobbySettings) { }

    public override void Serialize(NetDataWriter writer)
    {
        base.Serialize(writer);

        writer.Put(Settings != null);
        Settings?.Serialize(writer);
    }

    public override void Deserialize(NetDataReader reader)
    {
        base.Deserialize(reader);

        bool valid = reader.GetBool();
        if (valid)
        {
            Settings = new LobbySettingsData(reader);
        }
    }
}
