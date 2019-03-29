using NWN;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Item.Contracts;
using SWLOR.Game.Server.Service;

using SWLOR.Game.Server.ValueObject;

namespace SWLOR.Game.Server.Event.Delayed
{
    public class FinishActionItem: IRegisteredEvent
    {
        public bool Run(params object[] args)
        {
            using (new Profiler(nameof(FinishActionItem)))
            {
                string className = (string)args[0];
                NWPlayer user = (NWPlayer)args[1];
                NWItem itemObject = (NWItem)args[2];
                NWObject target = (NWObject)args[3];
                Location targetLocation = (Location)args[4];
                Vector userPosition = (Vector)args[5];
                CustomData customData = (CustomData)args[6];

                IActionItem actionItem = ItemService.GetActionItemHandler(className);
                ItemService.FinishActionItem(
                    actionItem,
                    user,
                    itemObject,
                    target,
                    targetLocation,
                    userPosition,
                    customData);

                return true;
            }
        }
    }
}
