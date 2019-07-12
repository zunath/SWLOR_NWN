using NWN;
using SWLOR.Game.Server.Event;
using SWLOR.Game.Server.GameObject;

namespace SWLOR.Game.Server.Placeable.FuelBay
{
    public class OnOpened: IRegisteredEvent
    {
        public bool Run(params object[] args)
        {
            NWPlaceable chest = (NWGameObject.OBJECT_SELF);
            NWPlayer player = (_.GetLastOpenedBy());

            player.SendMessage("Place the appropriate fuel inside the container. Click on the control tower when you're finished.");

            chest.IsUseable = false;
            chest.SetLocalObject("BAY_ACCESSOR", player);
            return true;
        }
    }
}
