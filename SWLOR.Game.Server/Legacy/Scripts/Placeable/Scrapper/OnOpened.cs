using SWLOR.Game.Server.Core.NWScript;
using SWLOR.Game.Server.Legacy.GameObject;

namespace SWLOR.Game.Server.Legacy.Scripts.Placeable.Scrapper
{
    public class OnOpened: IScript
    {
        public void SubscribeEvents()
        {
        }

        public void UnsubscribeEvents()
        {
        }

        public void Main()
        {
            NWPlayer player = NWScript.GetLastOpenedBy();
            NWPlaceable container = NWScript.OBJECT_SELF;
            player.FloatingText("Components placed inside this container will have all bonuses stripped and their level will be reduced to zero.");
            container.IsLocked = true;
            container.IsUseable = false;
        }
    }
}
