using NWN;
using SWLOR.Game.Server.Event;
using SWLOR.Game.Server.GameObject;

namespace SWLOR.Game.Server.Placeable.Stores
{
    public class GenericStore: IRegisteredEvent
    {
        public bool Run(params object[] args)
        {
            NWPlaceable self = (NWGameObject.OBJECT_SELF);
            NWObject oPC = (_.GetLastUsedBy());
            string storeTag = self.GetLocalString("STORE_TAG");
            NWGameObject store = _.GetObjectByTag(storeTag);

            _.OpenStore(store, oPC.Object);

            return true;
        }
    }
}
