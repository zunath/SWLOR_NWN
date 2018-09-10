using NWN;
using SWLOR.Game.Server.Event;
using SWLOR.Game.Server.GameObject;
using static NWN.NWScript;

namespace SWLOR.Game.Server.Placeable.DisabledStructure
{
    public class OnUsed: IRegisteredEvent
    {
        private readonly INWScript _;

        public OnUsed(INWScript script)
        {
            _ = script;
        }

        public bool Run(params object[] args)
        {
            NWPlayer user = NWPlayer.Wrap(_.GetLastUsedBy());
            
            user.SendMessage("The base is currently out of fuel and this object cannot be powered online.");
            
            return true;
        }
    }
}
