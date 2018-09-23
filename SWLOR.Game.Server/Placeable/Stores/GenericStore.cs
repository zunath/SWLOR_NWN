using NWN;
using SWLOR.Game.Server.Event;
using SWLOR.Game.Server.GameObject;

using Object = NWN.Object;

namespace SWLOR.Game.Server.Placeable.Stores
{
    public class GenericStore: IRegisteredEvent
    {
        private readonly INWScript _;

        public GenericStore(INWScript script)
        {
            _ = script;
        }

        public bool Run(params object[] args)
        {
            NWPlaceable self = (Object.OBJECT_SELF);
            NWObject oPC = (_.GetLastUsedBy());
            string storeTag = self.GetLocalString("STORE_TAG");
            Object store = _.GetObjectByTag(storeTag);

            _.OpenStore(store, oPC.Object);

            return true;
        }
    }
}
