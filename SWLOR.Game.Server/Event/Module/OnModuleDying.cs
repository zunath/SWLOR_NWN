using NWN;
using SWLOR.Game.Server.Service.Contracts;

namespace SWLOR.Game.Server.Event.Module
{
    internal class OnModuleDying : IRegisteredEvent
    {
        public bool Run(params object[] args)
        {
            _.ExecuteScript("nw_o0_dying", Object.OBJECT_SELF); // Bioware Default

            return true;

        }
    }
}
