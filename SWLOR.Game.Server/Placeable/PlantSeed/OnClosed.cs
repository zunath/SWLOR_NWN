using SWLOR.Game.Server.Event;
using SWLOR.Game.Server.GameObject;

using NWN;
using SWLOR.Game.Server.Service;


namespace SWLOR.Game.Server.Placeable.PlantSeed
{
    public class OnClosed: IRegisteredEvent
    {
        public bool Run(params object[] args)
        {
            NWPlaceable container = (NWGameObject.OBJECT_SELF);
            NWPlayer oPC = (_.GetLastClosedBy());
            
            foreach (NWItem item in container.InventoryItems)
            {
                ItemService.ReturnItem(oPC, item);
            }

            container.Destroy();
            return true;
        }
    }
}
