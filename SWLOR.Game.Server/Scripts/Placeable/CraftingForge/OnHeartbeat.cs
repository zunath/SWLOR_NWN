using SWLOR.Game.Server.NWN;
using SWLOR.Game.Server.GameObject;

namespace SWLOR.Game.Server.Scripts.Placeable.CraftingForge
{
    public class OnHeartbeat: IScript
    {
        public void Main()
        {
            NWPlaceable forge = (_.OBJECT_SELF);
            int charges = forge.GetLocalInt("FORGE_CHARGES");

            if (charges > 0)
            {
                charges--;
                forge.SetLocalInt("FORGE_CHARGES", charges);
            }

            if (charges <= 0)
            {
                NWPlaceable flames = (forge.GetLocalObject("FORGE_FLAMES"));
                flames.Destroy();
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
