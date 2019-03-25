using NWN;
using SWLOR.Game.Server.Event;
using SWLOR.Game.Server.GameObject;

namespace SWLOR.Game.Server.Placeable.ResourceBay
{
    public class OnOpened : IRegisteredEvent
    {
        public bool Run(params object[] args)
        {
            NWPlaceable chest = Object.OBJECT_SELF;
            NWPlayer player = _.GetLastOpenedBy();

            player.SendMessage("Retrieve any resources from this container. When finished, use the control tower or walk away.");

            chest.IsUseable = false;
            chest.SetLocalObject("BAY_ACCESSOR", player);
            return true;
        }
    }
}
