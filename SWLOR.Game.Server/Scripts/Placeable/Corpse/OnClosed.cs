using SWLOR.Game.Server.NWN;
using SWLOR.Game.Server.GameObject;

namespace SWLOR.Game.Server.Scripts.Placeable.Corpse
{
    public class OnClosed: IScript
    {
        public void Main()
        {
            NWPlaceable container = _.OBJECT_SELF;
            NWItem firstItem = _.GetFirstItemInInventory(container);
            NWCreature corpseOwner = container.GetLocalObject("CORPSE_BODY");

            if (!firstItem.IsValid)
            {
                container.Destroy();

                corpseOwner.AssignCommand(() =>
                {
                    _.SetIsDestroyable(true);
                });
            }
        }

        public void SubscribeEvents()
        {
        }

        public void UnsubscribeEvents()
        {
        }
    }
}
