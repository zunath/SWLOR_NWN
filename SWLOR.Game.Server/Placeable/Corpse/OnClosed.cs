
using System.Linq;
using NWN;
using SWLOR.Game.Server.Event;
using SWLOR.Game.Server.GameObject;
using static NWN._;

namespace SWLOR.Game.Server.Placeable.Corpse
{
    public class OnClosed: IRegisteredEvent
    {
        public bool Run(params object[] args)
        {
            NWPlaceable container = Object.OBJECT_SELF;
            NWItem firstItem = _.GetFirstItemInInventory(container);
            NWCreature corpseOwner = container.GetLocalObject("CORPSE_BODY");

            if (!firstItem.IsValid)
            {
                container.Destroy();
            }

            corpseOwner.AssignCommand(() =>
            {
                _.SetIsDestroyable(TRUE);
            });

            return true;
        }
    }
}
