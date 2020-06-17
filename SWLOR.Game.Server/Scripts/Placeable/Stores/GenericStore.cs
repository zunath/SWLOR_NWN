using SWLOR.Game.Server.NWN;
using SWLOR.Game.Server.GameObject;

namespace SWLOR.Game.Server.Scripts.Placeable.Stores
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
            NWPlaceable self = (_.OBJECT_SELF);
            NWObject oPC = (_.GetLastUsedBy());
            string storeTag = self.GetLocalString("STORE_TAG");
            uint store = _.GetObjectByTag(storeTag);

            _.OpenStore(store, oPC.Object);
        }
    }
}
