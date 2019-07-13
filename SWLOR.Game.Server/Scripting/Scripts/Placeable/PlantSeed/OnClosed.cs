using NWN;
using SWLOR.Game.Server.Event;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Scripting.Contracts;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.Scripting.Scripts.Placeable.PlantSeed
{
    public class OnClosed: IScript
    {
        public void SubscribeEvents()
        {
        }

        public void UnsubscribeEvents()
        {
        }

        public void Main()
        {
            NWPlaceable container = (NWGameObject.OBJECT_SELF);
            NWPlayer oPC = (_.GetLastClosedBy());
            
            foreach (NWItem item in container.InventoryItems)
            {
                ItemService.ReturnItem(oPC, item);
            }

            container.Destroy();
        }
    }
}
