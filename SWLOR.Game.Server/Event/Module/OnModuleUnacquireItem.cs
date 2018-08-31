using SWLOR.Game.Server.NWN.Contracts;
using SWLOR.Game.Server.NWN.NWScript;

namespace SWLOR.Game.Server.Event.Module
{
    internal class OnModuleUnacquireItem : IRegisteredEvent
    {
        private readonly INWScript _;

        public OnModuleUnacquireItem(INWScript script)
        {
            _ = script;
        }

        public bool Run(params object[] args)
        {
            _.ExecuteScript("x2_mod_def_unaqu", Object.OBJECT_SELF);
            return true;
        }
    }
}
