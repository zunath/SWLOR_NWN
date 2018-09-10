using NWN;
using SWLOR.Game.Server.Event;
using SWLOR.Game.Server.GameObject;

namespace SWLOR.Game.Server.Placeable.FuelBay
{
    public class OnOpened: IRegisteredEvent
    {
        private readonly INWScript _;

        public OnOpened(INWScript script)
        {
            _ = script;
        }

        public bool Run(params object[] args)
        {
            NWPlaceable chest = NWPlaceable.Wrap(Object.OBJECT_SELF);
            NWPlayer player = NWPlayer.Wrap(_.GetLastOpenedBy());

            player.SendMessage("Place the appropriate fuel inside the container. Click on the control tower when you're finished.");

            chest.IsUseable = false;
            return true;
        }
    }
}
