namespace Game.Common.Data;

interface ISerializableData
{
    public void Serialize(BinaryWriter writer);

    public void Deserialize(BinaryReader reader);
}
