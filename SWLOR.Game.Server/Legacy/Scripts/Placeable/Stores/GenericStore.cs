using SWLOR.Game.Server.Core.NWScript;
using SWLOR.Game.Server.Legacy.GameObject;

namespace SWLOR.Game.Server.Legacy.Scripts.Placeable.Stores
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
            var storeTag = self.GetLocalString("STORE_TAG");
            var store = NWScript.GetObjectByTag(storeTag);

            NWScript.OpenStore(store, oPC.Object);
        }
    }
}
