using Game.Common.Data;
using Game.Common.Enums;

namespace Game.Common.Packets;

class ChangeLobbySettingsPacket : LobbyPacket
{
    public LobbySettingsData? Settings { get; set; }

    public ChangeLobbySettingsPacket() : base(PacketType.ChangeLobbySettings) { }

    public override void Serialize(BinaryWriter writer)
    {
        base.Serialize(writer);

        writer.Write(Settings != null);
        Settings?.Serialize(writer);
    }

    public override void Deserialize(BinaryReader reader)
    {
        base.Deserialize(reader);

        bool valid = reader.ReadBoolean();
        if (valid)
        {
            Settings = new LobbySettingsData(reader);
        }
    }
}
