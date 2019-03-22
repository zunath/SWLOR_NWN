
using NWN;

namespace SWLOR.Game.Server.Event.Module
{
    internal class OnModuleUnacquireItem : IRegisteredEvent
    {
        public bool Run(params object[] args)
        {
            _.ExecuteScript("x2_mod_def_unaqu", Object.OBJECT_SELF);
            return true;
        }
    }
}
