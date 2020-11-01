using SWLOR.Game.Server.Core.NWScript;
using SWLOR.Game.Server.Legacy.GameObject;

namespace SWLOR.Game.Server.Legacy.Scripts.Placeable.Corpse
{
    public class OnClosed: IScript
    {
        public void Main()
        {
            NWPlaceable container = NWScript.OBJECT_SELF;
            NWItem firstItem = NWScript.GetFirstItemInInventory(container);
            NWCreature corpseOwner = container.GetLocalObject("CORPSE_BODY");

            if (!firstItem.IsValid)
            {
                container.Destroy();
            }

            corpseOwner.AssignCommand(() =>
            {
                NWScript.SetIsDestroyable(true);
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
