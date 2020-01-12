using NWN;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.NWScript;
using SWLOR.Game.Server.Scripting.Contracts;
using _ = SWLOR.Game.Server.NWScript._;

namespace SWLOR.Game.Server.Scripting.Placeable.Stores
{
    public class GenericStore: IScript
    {
        public void SubscribeEvents()
        {
        }

        public void UnsubscribeEvents()
        {
        }

        public void Main()
        {
            NWPlaceable self = (NWGameObject.OBJECT_SELF);
            NWObject oPC = (_.GetLastUsedBy());
            string storeTag = self.GetLocalString("STORE_TAG");
            NWGameObject store = _.GetObjectByTag(storeTag);

            _.OpenStore(store, oPC.Object);
        }
    }
}
