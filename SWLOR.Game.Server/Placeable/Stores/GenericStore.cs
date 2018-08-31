using SWLOR.Game.Server.Event;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.NWN.Contracts;
using Object = SWLOR.Game.Server.NWN.NWScript.Object;

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
            NWPlaceable self = NWPlaceable.Wrap(Object.OBJECT_SELF);
            NWObject oPC = NWObject.Wrap(_.GetLastUsedBy());
            string storeTag = self.GetLocalString("STORE_TAG");
            Object store = _.GetObjectByTag(storeTag);

            _.OpenStore(store, oPC.Object);

            return true;
        }
    }
}
