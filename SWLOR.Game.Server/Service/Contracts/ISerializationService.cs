using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.NWN.NWScript;

namespace SWLOR.Game.Server.Service.Contracts
{
    public interface ISerializationService
    {
        NWCreature DeserializeCreature(string base64String, Location location);
        NWItem DeserializeItem(string base64String, Location targetLocation);
        NWItem DeserializeItem(string base64String, NWPlaceable target);
        string Serialize(NWObject @object);
    }
}
