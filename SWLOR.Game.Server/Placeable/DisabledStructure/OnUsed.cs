using NWN;
using SWLOR.Game.Server.Event;
using SWLOR.Game.Server.GameObject;

namespace SWLOR.Game.Server.Placeable.DisabledStructure
{
    public class OnUsed: IRegisteredEvent
    {
        public bool Run(params object[] args)
        {
            NWPlayer user = (_.GetLastUsedBy());
            
            user.SendMessage("The base is currently out of fuel and this object cannot be powered online.");
            
            return true;
        }
    }
}
