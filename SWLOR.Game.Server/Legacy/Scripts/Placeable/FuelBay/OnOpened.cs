using SWLOR.Game.Server.Core.NWScript;
using SWLOR.Game.Server.Legacy.GameObject;

namespace SWLOR.Game.Server.Legacy.Scripts.Placeable.FuelBay
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
            NWPlaceable chest = (NWScript.OBJECT_SELF);
            NWPlayer player = (NWScript.GetLastOpenedBy());

            player.SendMessage("Place the appropriate fuel inside the container. Click on the control tower when you're finished.");

            chest.IsUseable = false;
            chest.SetLocalObject("BAY_ACCESSOR", player);
        }
    }
}
