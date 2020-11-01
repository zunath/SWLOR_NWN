using SWLOR.Game.Server.Core.NWScript;
using SWLOR.Game.Server.Legacy.GameObject;

namespace SWLOR.Game.Server.Legacy.Scripts.Placeable.CraftingForge
{
    public class OnHeartbeat: IScript
    {
        public void Main()
        {
            NWPlaceable forge = (NWScript.OBJECT_SELF);
            var charges = forge.GetLocalInt("FORGE_CHARGES");

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
