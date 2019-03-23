using NWN;
using SWLOR.Game.Server.GameObject;
using static NWN._;
using Object = NWN.Object;

namespace SWLOR.Game.Server.Event.Module
{
    public class OnModuleEnterStealthAfter: IRegisteredEvent
    {
        public bool Run(params object[] args)
        {
            NWObject stealther = Object.OBJECT_SELF;
            _.SetActionMode(stealther, ACTION_MODE_STEALTH, FALSE);
            _.FloatingTextStringOnCreature("NWN stealth mode is disabled on this server.", stealther, FALSE);
            return true;
        }
    }
}
