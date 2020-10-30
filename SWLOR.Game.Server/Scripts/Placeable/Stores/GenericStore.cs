using SWLOR.Game.Server.Core.NWScript;
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
            NWPlaceable self = (NWScript.OBJECT_SELF);
            NWObject oPC = (NWScript.GetLastUsedBy());
            string storeTag = self.GetLocalString("STORE_TAG");
            uint store = NWScript.GetObjectByTag(storeTag);

            NWScript.OpenStore(store, oPC.Object);
        }
    }
}
