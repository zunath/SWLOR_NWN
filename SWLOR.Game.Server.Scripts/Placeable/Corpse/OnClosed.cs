using NWN;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Scripting.Contracts;

namespace SWLOR.Game.Server.Scripts.Placeable.Corpse
{
    public class OnClosed: IScript
    {
        public void Main()
        {
            NWPlaceable container = NWGameObject.OBJECT_SELF;
            NWItem firstItem = _.GetFirstItemInInventory(container);
            NWCreature corpseOwner = container.GetLocalObject("CORPSE_BODY");

            if (!firstItem.IsValid)
            {
                container.Destroy();
            }

            corpseOwner.AssignCommand(() =>
            {
                _.SetIsDestroyable(_.TRUE);
            });
        }

        public void SubscribeEvents()
        {
        }

        public void UnsubscribeEvents()
        {
        }
    }
}
