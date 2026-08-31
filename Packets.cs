using Fika.Core.Networking.LiteNetLib.Utils;

namespace UniversalCoopExfil;

public struct ExfilEnteredPacket : INetSerializable
{
    public int NetId;
    public string Name;
    public bool Entered;
    
    public void Serialize(NetDataWriter writer)
    {
        writer.Put(NetId);
        writer.Put(Entered);
        writer.Put(Name);
    }

    public void Deserialize(NetDataReader reader)
    {
        NetId = reader.GetInt();
        Entered = reader.GetBool();
        Name = reader.GetString();
    }
}