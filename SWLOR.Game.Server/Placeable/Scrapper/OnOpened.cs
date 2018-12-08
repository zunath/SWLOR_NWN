using NWN;
using SWLOR.Game.Server.Event;
using SWLOR.Game.Server.GameObject;

namespace SWLOR.Game.Server.Placeable.Scrapper
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
            NWPlayer player = _.GetLastOpenedBy();
            NWPlaceable container = Object.OBJECT_SELF;
            player.FloatingText("Components placed inside this container will have all bonuses stripped and their level will be reduced to zero.");
            container.IsLocked = true;
            container.IsUseable = false;
            return true;
        }
    }
}
